import { describe, it, before, after } from 'node:test';
import assert from 'node:assert/strict';

// Базовый URL для тестов
const BASE_URL = `http://localhost:${process.env.PORT || 5000}`;

/**
 * Вспомогательная функция для HTTP-запросов.
 */
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

    // Тест 1: Создание сессии
    it('должен создать новую сессию (POST /api/sessions)', async () => {
        const { status, data } = await request('/api/sessions', {
            method: 'POST',
            body: JSON.stringify({}),
        });

        assert.equal(status, 201);
        assert.ok(data.session);
        assert.ok(data.session.id);
        assert.equal(data.session.status, 'active');
        assert.ok(data.session.fen);

        testSessionId = data.session.id;
    });

    // Тест 2: Получение списка сессий
    it('должен вернуть список сессий (GET /api/sessions)', async () => {
        const { status, data } = await request('/api/sessions');

        assert.equal(status, 200);
        assert.ok(Array.isArray(data.sessions));
        assert.ok(data.count >= 1);
    });

    // Тест 3: Получение конкретной сессии
    it('должен вернуть сессию по ID (GET /api/sessions/:id)', async () => {
        const { status, data } = await request(
            `/api/sessions/${testSessionId}`
        );

        assert.equal(status, 200);
        assert.equal(data.session.id, testSessionId);
    });

    // Тест 4: Обновление сессии (добавление хода)
    it('должен обновить сессию новым ходом (PUT /api/sessions/:id)', async () => {
        const { status, data } = await request(
            `/api/sessions/${testSessionId}`,
            {
                method: 'PUT',
                body: JSON.stringify({
                    move: 'e2-e4',
                }),
            }
        );

        assert.equal(status, 200);
        assert.ok(data.session.moves.includes('e2-e4'));
    });

    // Тест 5: Завершение сессии
    it('должен завершить сессию (PUT /api/sessions/:id)', async () => {
        const { status, data } = await request(
            `/api/sessions/${testSessionId}`,
            {
                method: 'PUT',
                body: JSON.stringify({
                    status: 'completed',
                    result: 'white_win',
                }),
            }
        );

        assert.equal(status, 200);
        assert.equal(data.session.status, 'completed');
        assert.equal(data.session.result, 'white_win');
        assert.ok(data.session.completed_at);
    });

    // Тест 6: Получение последней завершённой сессии
    it('должен вернуть последнюю завершённую сессию (GET /api/sessions/last)', async () => {
        const { status, data } = await request('/api/sessions/last');

        assert.equal(status, 200);
        if (data.session) {
            assert.equal(data.session.status, 'completed');
        }
    });

    // Тест 7: Ошибка 404 для несуществующей сессии
    it('должен вернуть 404 для несуществующей сессии', async () => {
        const { status } = await request('/api/sessions/99999');

        assert.equal(status, 404);
    });

    // Тест 8: Удаление сессии
    it('должен удалить сессию (DELETE /api/sessions/:id)', async () => {
        const { status, data } = await request(
            `/api/sessions/${testSessionId}`,
            {
                method: 'DELETE',
            }
        );

        assert.equal(status, 200);
        assert.equal(data.session.id, testSessionId);
    });
});