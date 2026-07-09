import 'dotenv/config';
import express from 'express';
import cors from 'cors';
import { initDatabase } from './db.js';
import sessionsRouter from './routes/sessions.js';

const app = express();
const PORT = process.env.PORT || 5000;

// Middleware
app.use(cors());
app.use(express.json());

// Маршруты
app.use('/api/sessions', sessionsRouter);

// Корневой эндпоинт для проверки
app.get('/', (req, res) => {
    res.json({
        message: 'Шахматный сервер управления сессиями',
        version: '1.0.0',
        endpoints: {
            'POST /api/sessions': 'Создать сессию',
            'GET /api/sessions': 'Список сессий',
            'GET /api/sessions/last': 'Последняя завершённая',
            'GET /api/sessions/:id': 'Получить сессию',
            'PUT /api/sessions/:id': 'Обновить сессию',
            'DELETE /api/sessions/:id': 'Удалить сессию',
        },
    });
});

// Запуск сервера
async function start() {
    try {
        // Инициализация БД
        await initDatabase();

        app.listen(PORT, () => {
            console.log(`Сервер запущен: http://localhost:${PORT}`);
            console.log(`API: http://localhost:${PORT}/api/sessions`);
        });
    } catch (error) {
        console.error('Ошибка запуска сервера:', error);
        process.exit(1);
    }
}

start();