import 'dotenv/config';
import { describe, it, before, after } from 'node:test';
import assert from 'node:assert/strict';
import express from 'express';
import cors from 'cors';
import pkg from 'pg';

const { Pool } = pkg;

// Создаём тестовое подключение к БД
const pool = new Pool({
    host: process.env.DB_HOST || 'localhost',
    port: parseInt(process.env.DB_PORT || '5432'),
    database: process.env.DB_NAME || 'chess_sessions',
    user: process.env.DB_USER || 'postgres',
    password: process.env.DB_PASSWORD || 'postgres',
});

// Импортируем роутер
import sessionsRouter from '../routes/sessions.js';

let server;
const PORT = 5000;
const BASE_URL = `http://localhost:${PORT}`;

async function request(path, options = {}) {
    const url = `${BASE_URL}${path}`;
    const response = await fetch(url, {
        headers: { 'Content-Type': 'application/json' },
        ...options,
    });
    const data = await response.json();
    return { status: response.status, data };
}

describe('Sessions API', () => {
    let testSessionId = null;

    // Перед всеми тестами: создать таблицу и запустить сервер
    before(async () => {
        // Создаём таблицу, если её нет
        await pool.query(`
            CREATE TABLE IF NOT EXISTS sessions (
                id SERIAL PRIMARY KEY,
                status VARCHAR(20) DEFAULT 'active',
                fen VARCHAR(100) DEFAULT 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1',
                moves TEXT[] DEFAULT '{}',
                result VARCHAR(20),
                duration INTEGER,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                completed_at TIMESTAMP WITH TIME ZONE
            );
        `);

        // Запускаем сервер
        const app = express();
        app.use(cors());
        app.use(express.json());
        app.use('/api/sessions', sessionsRouter);

        await new Promise((resolve) => {
            server = app.listen(PORT, () => {
                console.log(`Тестовый сервер запущен: ${BASE_URL}`);
                resolve();
            });
        });
    });

    // После всех тестов: остановить сервер
    after(async () => {
        if (server) {
            await new Promise((resolve) => server.close(resolve));
            console.log('Тестовый сервер остановлен');
        }
        await pool.end();
    });

    // Тест 1
    it('должен создать новую сессию (POST /api/sessions)', async () => {
        const { status, data } = await request('/api/sessions', {
            method: 'POST',
            body: JSON.stringify({}),
        });

        assert.equal(status, 201);
        assert.ok(data.session);
        assert.ok(data.session.id);
        assert.equal(data.session.status, 'active');

        testSessionId = data.session.id;
    });

    // Тест 2
    it('должен вернуть список сессий (GET /api/sessions)', async () => {
        const { status, data } = await request('/api/sessions');

        assert.equal(status, 200);
        assert.ok(Array.isArray(data.sessions));
        assert.ok(data.count >= 1);
    });

    // Тест 3
    it('должен вернуть сессию по ID (GET /api/sessions/:id)', async () => {
        const { status, data } = await request(`/api/sessions/${testSessionId}`);

        assert.equal(status, 200);
        assert.equal(data.session.id, testSessionId);
    });

    // Тест 4
    it('должен обновить сессию новым ходом (PUT /api/sessions/:id)', async () => {
        const { status, data } = await request(`/api/sessions/${testSessionId}`, {
            method: 'PUT',
            body: JSON.stringify({ move: 'e2-e4' }),
        });

        assert.equal(status, 200);
        assert.ok(data.session.moves.includes('e2-e4'));
    });

    // Тест 5
    it('должен завершить сессию (PUT /api/sessions/:id)', async () => {
        const { status, data } = await request(`/api/sessions/${testSessionId}`, {
            method: 'PUT',
            body: JSON.stringify({
                status: 'completed',
                result: 'white_win',
            }),
        });

        assert.equal(status, 200);
        assert.equal(data.session.status, 'completed');
        assert.equal(data.session.result, 'white_win');
        assert.ok(data.session.completed_at);
    });

    // Тест 6
    it('должен вернуть последнюю завершённую сессию (GET /api/sessions/last)', async () => {
        const { status, data } = await request('/api/sessions/last');

        assert.equal(status, 200);
        if (data.session) {
            assert.equal(data.session.status, 'completed');
        }
    });

    // Тест 7
    it('должен вернуть 404 для несуществующей сессии', async () => {
        const { status } = await request('/api/sessions/99999');

        assert.equal(status, 404);
    });

    // Тест 8
    it('должен удалить сессию (DELETE /api/sessions/:id)', async () => {
        const { status, data } = await request(`/api/sessions/${testSessionId}`, {
            method: 'DELETE',
        });

        assert.equal(status, 200);
        assert.equal(data.session.id, testSessionId);
    });
});