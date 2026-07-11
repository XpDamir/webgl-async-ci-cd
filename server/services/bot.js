import { Chess } from 'chess.js';

/**
 * Асинхронно рассчитывает ход бота.
 * Бот ВСЕГДА играет за чёрных.
 * 
 * @param {string} fen - Текущая позиция в FEN-формате
 * @param {string[]} previousMoves - Массив уже сделанных ходов
 * @returns {Promise<{move: string, fen: string}>} - Ход бота и новая позиция
 */
export async function calculateBotMove(fen, previousMoves = []) {
    const thinkingTime = 1000 + Math.floor(Math.random() * 1000);
    console.log(`Бот анализирует позицию... (${thinkingTime / 1000} сек)`);

    return new Promise((resolve, reject) => {
        setTimeout(() => {
            try {
                const chess = new Chess(fen || 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1');

                // Бот всегда играет за чёрных.
                // Если сейчас ход белых — это ошибка вызова, не отвечаем null.
                if (chess.turn() === 'w') {
                    console.log('Ошибка: бот вызван во время хода белых');
                    resolve({ move: null, fen: chess.fen(), skipped: true });
                    return;
                }

                const legalMoves = chess.moves({ verbose: true });

                if (legalMoves.length === 0) {
                    console.log('У чёрных нет ходов');
                    resolve({ move: null, fen: chess.fen(), gameOver: true });
                    return;
                }

                const randomMove = legalMoves[Math.floor(Math.random() * legalMoves.length)];
                chess.move(randomMove);

                const formattedMove = `${randomMove.from}-${randomMove.to}`;
                console.log(`Бот походил (чёрные): ${formattedMove}`);

                resolve({
                    move: formattedMove,
                    fen: chess.fen(),
                });
            } catch (error) {
                console.error('Ошибка при расчёте хода бота:', error);
                reject(error);
            }
        }, thinkingTime);
    });
}

/**
 * Проверяет состояние игры
 */
export function checkGameOver(fen) {
    try {
        const chess = new Chess(fen);
        return {
            isOver: chess.isGameOver(),
            result: chess.isCheckmate() ? 'checkmate' : chess.isDraw() ? 'draw' : null,
        };
    } catch {
        return { isOver: false, result: null };
    }
}

export default {
    calculateBotMove,
    checkGameOver,
};