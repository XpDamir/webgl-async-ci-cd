// Базовый URL сервера
// При разработке локально:
const API_URL = 'https://webgl-async-ci-cd-production.up.railway.app';
// При деплое заменить на публичный URL Railway:
// const API_URL = 'https://ваш-сервер.up.railway.app';

/**
 * Создать новую игровую сессию на сервере.
 * @returns {Promise<object>} - Объект созданной сессии
 */
export async function createSession() {
    const response = await fetch(`${API_URL}/api/sessions`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({}),
    });

    if (!response.ok) {
        throw new Error(`Ошибка создания сессии: ${response.status}`);
    }

    const data = await response.json();
    console.log('Сессия создана:', data.session);
    return data.session;
}

/**
 * Отправить ход игрока на сервер.
 * @param {number} sessionId - ID сессии
 * @param {string} move - Ход в формате "e2-e4"
 * @param {string} fen - Позиция после хода в FEN-формате
 * @returns {Promise<object>} - Обновлённая сессия
 */
export async function sendMove(sessionId, move, fen) {
    const response = await fetch(`${API_URL}/api/sessions/${sessionId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ move, fen }),
    });

    if (!response.ok) {
        throw new Error(`Ошибка отправки хода: ${response.status}`);
    }

    const data = await response.json();
    console.log('Ход отправлен:', move);
    return data.session;
}

/**
 * Запросить ход бота (асинхронно).
 * @param {number} sessionId - ID сессии
 * @returns {Promise<object>} - Статус запроса
 */
export async function requestBotMove(sessionId) {
    const response = await fetch(
        `${API_URL}/api/sessions/${sessionId}/bot-move`,
        {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({}),
        }
    );

    if (!response.ok) {
        throw new Error(`Ошибка запроса хода бота: ${response.status}`);
    }

    const data = await response.json();
    console.log('Бот думает...');
    return data;
}

/**
 * Получить текущее состояние сессии.
 * @param {number} sessionId - ID сессии
 * @returns {Promise<object>} - Сессия с сервера
 */
export async function getSession(sessionId) {
    const response = await fetch(`${API_URL}/api/sessions/${sessionId}`);

    if (!response.ok) {
        throw new Error(`Ошибка получения сессии: ${response.status}`);
    }

    const data = await response.json();
    return data.session;
}

/**
 * Получить последнюю завершённую сессию.
 * @returns {Promise<object|null>} - Сессия или null
 */
export async function getLastCompletedSession() {
    const response = await fetch(`${API_URL}/api/sessions/last`);

    if (!response.ok) {
        throw new Error(
            `Ошибка получения последней сессии: ${response.status}`
        );
    }

    const data = await response.json();
    return data.session;
}

/**
 * Опрашивать сессию, пока бот не сделает ход.
 * @param {number} sessionId - ID сессии
 * @param {number} previousMovesCount - Количество ходов до запроса бота
 * @param {number} maxAttempts - Максимальное количество попыток
 * @returns {Promise<object>} - Обновлённая сессия с ходом бота
 */
export async function pollForBotMove(
    sessionId,
    previousMovesCount,
    maxAttempts = 30
) {
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
        console.log(`Ожидание хода бота... попытка ${attempt + 1}`);

        // Ждём полсекунды между попытками
        await new Promise((resolve) => setTimeout(resolve, 500));

        const session = await getSession(sessionId);

        // Если появился новый ход — бот походил
        if (session.moves.length > previousMovesCount) {
            console.log('Бот походил:', session.moves[session.moves.length - 1]);
            return session;
        }
    }

    throw new Error('Бот не ответил за отведённое время');
}