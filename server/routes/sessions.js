import { Router } from 'express';
import pool from '../db.js';
import { Chess } from 'chess.js';

const router = Router();

function getResult(chess) {
    if (chess.isCheckmate()) return chess.turn() === 'b' ? 'white_win' : 'black_win';
    if (chess.isStalemate()) return 'draw';
    if (chess.isThreefoldRepetition()) return 'draw';
    if (chess.isInsufficientMaterial()) return 'draw';
    if (chess.isDraw()) return 'draw';
    return null;
}

// POST /api/sessions
router.post('/', async (req, res) => {
    try {
        const fen = req.body.fen || 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';
        const result = await pool.query('INSERT INTO sessions (fen) VALUES ($1) RETURNING *', [fen]);
        res.status(201).json({ message: 'Сессия создана', session: result.rows[0] });
    } catch (error) {
        console.error('Ошибка создания сессии:', error);
        res.status(500).json({ error: 'Не удалось создать сессию', details: error.message });
    }
});

// GET /api/sessions
router.get('/', async (req, res) => {
    try {
        const { status } = req.query;
        let query = 'SELECT * FROM sessions';
        const params = [];
        if (status) { query += ' WHERE status = $1'; params.push(status); }
        query += ' ORDER BY created_at DESC';
        const result = await pool.query(query, params);
        res.json({ count: result.rows.length, sessions: result.rows });
    } catch (error) {
        console.error('Ошибка получения сессий:', error);
        res.status(500).json({ error: 'Не удалось получить сессии', details: error.message });
    }
});

// GET /api/sessions/last
router.get('/last', async (req, res) => {
    try {
        const result = await pool.query(
            "SELECT * FROM sessions WHERE status = 'completed' ORDER BY completed_at DESC LIMIT 1"
        );
        if (result.rows.length === 0) {
            return res.status(404).json({ message: 'Нет завершённых сессий', session: null });
        }
        res.json({ message: 'Последняя завершённая сессия', session: result.rows[0] });
    } catch (error) {
        console.error('Ошибка получения последней сессии:', error);
        res.status(500).json({ error: 'Не удалось получить сессию', details: error.message });
    }
});

// GET /api/sessions/best
router.get('/best', async (req, res) => {
    try {
        const result = await pool.query(
            `SELECT * FROM sessions WHERE status = 'completed' AND duration IS NOT NULL ORDER BY duration ASC LIMIT 1`
        );
        if (result.rows.length === 0) {
            return res.json({ message: 'Нет завершённых партий', session: null });
        }
        res.json({ message: 'Лучшая партия', session: result.rows[0] });
    } catch (error) {
        console.error('Ошибка получения лучшей партии:', error);
        res.status(500).json({ error: 'Не удалось получить лучшую партию', details: error.message });
    }
});

// GET /api/sessions/:id/result
router.get('/:id/result', async (req, res) => {
    try {
        const { id } = req.params;
        const result = await pool.query('SELECT status, result, moves FROM sessions WHERE id = $1', [id]);
        if (result.rows.length === 0) return res.status(404).json({ error: 'Сессия не найдена' });
        const session = result.rows[0];
        res.json({ sessionId: parseInt(id), status: session.status, result: session.result, totalMoves: session.moves ? session.moves.length : 0 });
    } catch (error) {
        console.error('Ошибка получения результата:', error);
        res.status(500).json({ error: 'Не удалось получить результат' });
    }
});

// GET /api/sessions/:id
router.get('/:id', async (req, res) => {
    try {
        const { id } = req.params;
        const result = await pool.query('SELECT * FROM sessions WHERE id = $1', [id]);
        if (result.rows.length === 0) return res.status(404).json({ error: 'Сессия не найдена' });
        res.json({ session: result.rows[0] });
    } catch (error) {
        console.error('Ошибка получения сессии:', error);
        res.status(500).json({ error: 'Не удалось получить сессию', details: error.message });
    }
});

// PUT /api/sessions/:id
router.put('/:id', async (req, res) => {
    try {
        const { id } = req.params;
        const { move, status, result } = req.body;

        const current = await pool.query('SELECT * FROM sessions WHERE id = $1', [id]);
        if (current.rows.length === 0) return res.status(404).json({ error: 'Сессия не найдена' });

        const session = current.rows[0];

        let moves = session.moves || [];
        if (move) moves = [...moves, move];

        let computedFen = session.fen;
        let newStatus = status || session.status;
        let newResult = result || session.result;
        let completedAt = session.completed_at;
        let newDuration = session.duration;

        if (move && session.status === 'active') {
            try {
                const chess = new Chess(session.fen);
                const parts = move.split('-');
                if (parts.length === 2) {
                    const from = parts[0];
                    let to = parts[1];
                    const moveObj = { from, to };
                    if (to.length === 3) {
                        moveObj.to = to.substring(0, 2);
                        moveObj.promotion = to.substring(2).toLowerCase();
                    }
                    const piece = chess.get(from);
                    if (piece && piece.type === 'p' && !moveObj.promotion) {
                        const row = parseInt(moveObj.to[1]);
                        if (row === 8 || row === 1) moveObj.promotion = 'q';
                    }
                    chess.move(moveObj);
                    computedFen = chess.fen();

                    // Проверяем завершение игры после хода игрока
                    if (chess.isGameOver()) {
                        newStatus = 'completed';
                        newResult = getResult(chess);
                        completedAt = new Date().toISOString();
                        newDuration = Math.floor((new Date(completedAt) - new Date(session.created_at)) / 1000);
                    }
                }
            } catch (e) {
                console.error('Ошибка хода игрока:', e.message);
                return res.status(400).json({ error: 'Нелегальный ход', details: e.message });
            }
        }

        const updateResult = await pool.query(
            `UPDATE sessions SET moves = $1, fen = $2, status = $3, result = $4, completed_at = $5, duration = $6 WHERE id = $7 RETURNING *`,
            [moves, computedFen, newStatus, newResult, completedAt, newDuration, id]
        );

        // Если ход белых и игра не завершена — запускаем бота
        if (computedFen.includes(' b ') && newStatus !== 'completed') {
            import('../services/bot.js').then(async (bot) => {
                try {
                    const botResult = await bot.calculateBotMove(computedFen, moves);
                    if (botResult && botResult.move) {
                        const botMoves = [...moves, botResult.move];
                        let botDuration = null;
                        let botStatus = 'active';
                        let botResult2 = null;
                        let botCompletedAt = null;

                        if (botResult.gameOver) {
                            botStatus = 'completed';
                            botResult2 = botResult.result;
                            botCompletedAt = new Date().toISOString();
                            botDuration = Math.floor((new Date(botCompletedAt) - new Date(session.created_at)) / 1000);
                        }

                        await pool.query(
                            `UPDATE sessions SET fen = $1, moves = $2, status = $3, result = $4, completed_at = $5, duration = $6 WHERE id = $7`,
                            [botResult.fen, botMoves, botStatus, botResult2, botCompletedAt, botDuration, id]
                        );
                    }
                } catch (err) {
                    console.error('Ошибка авто-бота:', err.message);
                }
            });
        }

        res.json({ message: 'Ход принят', session: updateResult.rows[0] });
    } catch (error) {
        console.error('Ошибка обновления сессии:', error);
        res.status(500).json({ error: 'Не удалось обновить сессию', details: error.message });
    }
});

// DELETE /api/sessions/:id
router.delete('/:id', async (req, res) => {
    try {
        const { id } = req.params;
        const result = await pool.query('DELETE FROM sessions WHERE id = $1 RETURNING *', [id]);
        if (result.rows.length === 0) return res.status(404).json({ error: 'Сессия не найдена' });
        res.json({ message: 'Сессия удалена', session: result.rows[0] });
    } catch (error) {
        console.error('Ошибка удаления сессии:', error);
        res.status(500).json({ error: 'Не удалось удалить сессию', details: error.message });
    }
});

export default router;