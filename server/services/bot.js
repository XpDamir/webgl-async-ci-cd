import { Chess } from 'chess.js';

/**
 * Асинхронно рассчитывает ход бота с использованием библиотеки chess.js
 * 
 * @param {string} fen - Текущая позиция в FEN-формате
 * @param {string[]} previousMoves - Массив уже сделанных ходов (для истории)
 * @returns {Promise<{move: string, fen: string}>} - Ход бота и новая позиция
 */
export async function calculateBotMove(fen, previousMoves = []) {
    // Имитация времени "размышления" бота (1-2 секунды)
    const thinkingTime = 1000 + Math.floor(Math.random() * 1000);
    console.log(`Бот анализирует позицию... (${thinkingTime / 1000} сек)`);

    return new Promise((resolve, reject) => {
        setTimeout(() => {
            try {
                // Загружаем текущую позицию в движок
                // Если FEN пустой или некорректный, chess.js начнет с начальной позиции
                const chess = new Chess(fen || 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1');

                // Получаем список ВСЕХ легальных ходов для текущей стороны (черных)
                const legalMoves = chess.moves({ verbose: true });

                if (legalMoves.length === 0) {
                    console.log("Ходов больше нет (мат или пат)");
                    resolve({ move: null, fen: chess.fen() });
                    return;
                }

                // Выбираем случайный ход из списка ГАРАНТИРОВАННО легальных
                const randomMove = legalMoves[Math.floor(Math.random() * legalMoves.length)];

                // Делаем ход в виртуальной доске, чтобы получить новый FEN
                chess.move(randomMove);

                // Форматируем ход в наш стандарт "e7-e5"
                const formattedMove = `${randomMove.from}-${randomMove.to}`;

                console.log(`Бот выбрал легальный ход: ${formattedMove}`);

                resolve({
                    move: formattedMove,
                    fen: chess.fen()
                });
            } catch (error) {
                console.error("Ошибка при расчете хода бота:", error);
                reject(error);
            }
        }, thinkingTime);
    });
}

/**
 * Проверяет состояние игры (завершена ли она)
 */
export function checkGameOver(fen, moves = []) {
    const chess = new Chess(fen);
    
    return {
        isOver: chess.isGameOver(),
        result: chess.isCheckmate() ? "checkmate" : 
                chess.isDraw() ? "draw" : null
    };
}

export default {
    calculateBotMove,
    checkGameOver,
};