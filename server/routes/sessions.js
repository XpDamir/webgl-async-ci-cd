import { Router } from 'express';
import pool from '../db.js';

const router = Router();

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
        const { move, fen, status, result } = req.body;

        const current = await pool.query('SELECT * FROM sessions WHERE id = $1', [id]);
        if (current.rows.length === 0) return res.status(404).json({ error: 'Сессия не найдена' });

        const session = current.rows[0];

        let moves = session.moves || [];
        if (move) moves = [...moves, move];

        const newFen = fen || session.fen;
        const newStatus = status || session.status;
        const newResult = result || session.result;

        let completedAt = session.completed_at;
        let newDuration = session.duration;
        if (newStatus === 'completed' && session.status !== 'completed') {
            completedAt = new Date().toISOString();
            const startTime = new Date(session.created_at).getTime();
            const endTime = new Date(completedAt).getTime();
            newDuration = Math.floor((endTime - startTime) / 1000);
        }

        const updateResult = await pool.query(
            `UPDATE sessions SET moves = $1, fen = $2, status = $3, result = $4, completed_at = $5, duration = $6 WHERE id = $7 RETURNING *`,
            [moves, newFen, newStatus, newResult, completedAt, newDuration, id]
        );

        res.json({ message: 'Сессия обновлена', session: updateResult.rows[0] });
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

// POST /api/sessions/:id/bot-move
router.post('/:id/bot-move', async (req, res) => {
    try {
        const { id } = req.params;
        const current = await pool.query('SELECT * FROM sessions WHERE id = $1', [id]);

        if (current.rows.length === 0) return res.status(404).json({ error: 'Сессия не найдена' });

        const session = current.rows[0];

        if (session.status !== 'active') {
            return res.status(400).json({ error: 'Сессия уже завершена', status: session.status });
        }

        import('../services/bot.js').then(async (bot) => {
            try {
                console.log(`Бот: расчёт хода для сессии ${id}`);
                const botResult = await bot.calculateBotMove(session.fen, session.moves || []);

                if (!botResult || !botResult.move) {
                    console.log(`Бот: нет хода для сессии ${id}`);
                    return;
                }

                const updatedMoves = [...(session.moves || []), botResult.move];
                const gameState = bot.checkGameOver(botResult.fen);

                let botDuration = null;
                if (gameState.isOver) {
                    const startTime = new Date(session.created_at).getTime();
                    botDuration = Math.floor((Date.now() - startTime) / 1000);
                }

                await pool.query(
                    `UPDATE sessions SET fen = $1, moves = $2, status = $3, result = $4, completed_at = $5, duration = $6 WHERE id = $7`,
                    [botResult.fen, updatedMoves, gameState.isOver ? 'completed' : 'active', gameState.result, gameState.isOver ? new Date().toISOString() : null, botDuration, id]
                );

                console.log(`Бот: ход ${botResult.move} сохранён для сессии ${id}`);
            } catch (error) {
                console.error(`Бот: ошибка для сессии ${id}:`, error.message);
            }
        });

        res.json({ message: 'Бот думает над ходом', status: 'processing', sessionId: parseInt(id) });
    } catch (error) {
        console.error('Ошибка при запросе хода бота:', error);
        res.status(500).json({ error: 'Не удалось запустить расчёт хода бота', details: error.message });
    }
});

export default router;