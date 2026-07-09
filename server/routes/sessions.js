import { Router } from 'express';
import pool from '../db.js';

const router = Router();

/**
 * POST /api/sessions
 * Создать новую игровую сессию.
 * Тело запроса может содержать начальный FEN (опционально).
 */
router.post('/', async (req, res) => {
    try {
        const fen = req.body.fen || 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';

        const result = await pool.query(
            'INSERT INTO sessions (fen) VALUES ($1) RETURNING *',
            [fen]
        );
        const session = result.rows[0];

        res.status(201).json({
            message: 'Сессия создана',
            session,
        });
    } catch (error) {
        console.error('Ошибка создания сессии:', error);
        res.status(500).json({
            error: 'Не удалось создать сессию',
            details: error.message,
        });
    }
});

/**
 * GET /api/sessions
 * Получить список всех сессий.
 * Поддерживает query-параметр ?status=active|completed
 */
router.get('/', async (req, res) => {
    try {
        const { status } = req.query;

        let query = 'SELECT * FROM sessions';
        const params = [];

        if (status) {
            query += ' WHERE status = $1';
            params.push(status);
        }

        query += ' ORDER BY created_at DESC';

        const result = await pool.query(query, params);

        res.json({
            count: result.rows.length,
            sessions: result.rows,
        });
    } catch (error) {
        console.error('Ошибка получения сессий:', error);
        res.status(500).json({
            error: 'Не удалось получить сессии',
            details: error.message,
        });
    }
});

/**
 * GET /api/sessions/last
 * Получить последнюю завершённую сессию.
 * Используется для параллельного повтора предыдущей партии.
 */
router.get('/last', async (req, res) => {
    try {
        const result = await pool.query(
            "SELECT * FROM sessions WHERE status = 'completed' ORDER BY completed_at DESC LIMIT 1"
        );

        if (result.rows.length === 0) {
            return res.status(404).json({
                message: 'Нет завершённых сессий',
                session: null,
            });
        }

        res.json({
            session: result.rows[0],
        });
    } catch (error) {
        console.error('Ошибка получения последней сессии:', error);
        res.status(500).json({
            error: 'Не удалось получить сессию',
            details: error.message,
        });
    }
});

/**
 * GET /api/sessions/:id
 * Получить конкретную сессию по ID.
 */
router.get('/:id', async (req, res) => {
    try {
        const { id } = req.params;

        const result = await pool.query('SELECT * FROM sessions WHERE id = $1', [id]);

        if (result.rows.length === 0) {
            return res.status(404).json({
                error: 'Сессия не найдена',
            });
        }

        res.json({
            session: result.rows[0],
        });
    } catch (error) {
        console.error('Ошибка получения сессии:', error);
        res.status(500).json({
            error: 'Не удалось получить сессию',
            details: error.message,
        });
    }
});

/**
 * PUT /api/sessions/:id
 * Обновить сессию (добавить ход, изменить статус, FEN).
 * Основной эндпоинт для записи ходов во время игры.
 */
router.put('/:id', async (req, res) => {
    try {
        const { id } = req.params;
        const { move, fen, status, result } = req.body;

        // Получаем текущую сессию
        const current = await pool.query('SELECT * FROM sessions WHERE id = $1', [id]);

        if (current.rows.length === 0) {
            return res.status(404).json({
                error: 'Сессия не найдена',
            });
        }

        const session = current.rows[0];

        // Обновляем массив ходов, если передан новый ход
        let moves = session.moves || [];
        if (move) {
            moves = [...moves, move];
        }

        // Обновляем FEN, если передан
        const newFen = fen || session.fen;

        // Обновляем статус и результат
        const newStatus = status || session.status;
        const newResult = result || session.result;

        // Если статус меняется на completed, фиксируем время
        let completedAt = session.completed_at;
        if (newStatus === 'completed' && session.status !== 'completed') {
            completedAt = new Date().toISOString();
        }

        const updateResult = await pool.query(
            `UPDATE sessions 
             SET moves = $1, fen = $2, status = $3, result = $4, completed_at = $5
             WHERE id = $6 
             RETURNING *`,
            [moves, newFen, newStatus, newResult, completedAt, id]
        );

        res.json({
            message: 'Сессия обновлена',
            session: updateResult.rows[0],
        });
    } catch (error) {
        console.error('Ошибка обновления сессии:', error);
        res.status(500).json({
            error: 'Не удалось обновить сессию',
            details: error.message,
        });
    }
});

/**
 * DELETE /api/sessions/:id
 * Удалить сессию по ID.
 */
router.delete('/:id', async (req, res) => {
    try {
        const { id } = req.params;

        const result = await pool.query(
            'DELETE FROM sessions WHERE id = $1 RETURNING *',
            [id]
        );

        if (result.rows.length === 0) {
            return res.status(404).json({
                error: 'Сессия не найдена',
            });
        }

        res.json({
            message: 'Сессия удалена',
            session: result.rows[0],
        });
    } catch (error) {
        console.error('Ошибка удаления сессии:', error);
        res.status(500).json({
            error: 'Не удалось удалить сессию',
            details: error.message,
        });
    }
});

/**
 * POST /api/sessions/:id/bot-move
 * Запросить асинхронный расчёт хода бота.
 * Бот "думает" в фоне, сервер сразу возвращает status: "processing".
 * Клиент должен опрашивать GET /api/sessions/:id для получения результата.
 */
router.post('/:id/bot-move', async (req, res) => {
    try {
        const { id } = req.params;

        // Получаем текущую сессию
        const current = await pool.query(
            'SELECT * FROM sessions WHERE id = $1',
            [id]
        );

        if (current.rows.length === 0) {
            return res.status(404).json({
                error: 'Сессия не найдена',
            });
        }

        const session = current.rows[0];

        // Проверяем, что сессия активна
        if (session.status !== 'active') {
            return res.status(400).json({
                error: 'Сессия уже завершена',
                status: session.status,
            });
        }

        // Запускаем асинхронный расчёт хода бота (НЕ ждём результата)
        import('../services/bot.js').then(async (bot) => {
            try {
                console.log(`Запущен асинхронный расчёт хода бота для сессии ${id}`);

                const botResult = await bot.calculateBotMove(
                    session.fen,
                    session.moves || []
                );

                // Обновляем сессию с ходом бота
                const updatedMoves = [...(session.moves || []), botResult.move];
                const gameState = bot.checkGameOver(
                    botResult.fen,
                    updatedMoves
                );

                await pool.query(
                    `UPDATE sessions 
                     SET fen = $1, moves = $2, status = $3, result = $4, 
                         completed_at = $5
                     WHERE id = $6`,
                    [
                        botResult.fen,
                        updatedMoves,
                        gameState.isOver ? 'completed' : 'active',
                        gameState.result,
                        gameState.isOver ? new Date().toISOString() : null,
                        id,
                    ]
                );

                console.log(
                    `Ход бота для сессии ${id} сохранён: ${botResult.move}`
                );
            } catch (error) {
                console.error(
                    `Ошибка при расчёте хода бота для сессии ${id}:`,
                    error
                );
            }
        });

        // Сразу отвечаем клиенту, не дожидаясь бота
        res.json({
            message: 'Бот думает над ходом',
            status: 'processing',
            sessionId: parseInt(id),
        });
    } catch (error) {
        console.error('Ошибка при запросе хода бота:', error);
        res.status(500).json({
            error: 'Не удалось запустить расчёт хода бота',
            details: error.message,
        });
    }
});

export default router;