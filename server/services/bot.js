/**
 * Простой шахматный бот.
 * Имитирует "размышление" (асинхронная задержка) и возвращает случайный ход.
 * В реальном проекте здесь был бы алгоритм с минимаксом или подключение к Stockfish.
 */

// Простые примеры ходов для имитации
const SAMPLE_MOVES = [
    { move: 'e2-e4', fen: 'rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1' },
    { move: 'd2-d4', fen: 'rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR b KQkq - 0 1' },
    { move: 'g1-f3', fen: 'rnbqkbnr/pppppppp/8/8/8/5N2/PPPPPPPP/RNBQKB1R b KQkq - 1 1' },
    { move: 'b1-c3', fen: 'rnbqkbnr/pppppppp/8/8/8/2N5/PPPPPPPP/R1BQKBNR b KQkq - 1 1' },
    { move: 'e7-e5', fen: 'rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2' },
    { move: 'd7-d5', fen: 'rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2' },
    { move: 'g8-f6', fen: 'rnbqkb1r/pppppppp/5n2/8/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 1 2' },
    { move: 'b8-c6', fen: 'r1bqkbnr/pppppppp/2n5/8/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 1 2' },
    { move: 'f1-c4', fen: 'rnbqkbnr/pppp1ppp/8/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR b KQkq - 1 2' },
    { move: 'd1-h5', fen: 'rnbqkbnr/pppp1ppp/8/4p2Q/4P3/8/PPPP1PPP/RNB1KBNR b KQkq - 1 2' },
];

/**
 * Асинхронно рассчитывает ход бота.
 * Имитирует задержку "размышления" от 1 до 3 секунд.
 * 
 * @param {string} fen - Текущая позиция в FEN-формате
 * @param {string[]} previousMoves - Массив уже сделанных ходов
 * @returns {Promise<{move: string, fen: string}>} - Ход бота и новая позиция
 */
export async function calculateBotMove(fen, previousMoves = []) {
    // Имитация времени "размышления" бота (1-3 секунды)
    const thinkingTime = 1000 + Math.floor(Math.random() * 2000);

    console.log(`Бот думает над ходом... (${thinkingTime / 1000} сек)`);

    return new Promise((resolve) => {
        setTimeout(() => {
            // Фильтруем ходы, которые ещё не были сделаны (упрощённо)
            const availableMoves = SAMPLE_MOVES.filter(
                (m) => !previousMoves.includes(m.move)
            );

            // Если все ходы из примера исчерпаны, возвращаем случайный
            if (availableMoves.length === 0) {
                const randomMove = SAMPLE_MOVES[
                    Math.floor(Math.random() * SAMPLE_MOVES.length)
                ];
                console.log(`Бот походил: ${randomMove.move}`);
                resolve(randomMove);
                return;
            }

            // Выбираем случайный ход из доступных
            const chosenMove = availableMoves[
                Math.floor(Math.random() * availableMoves.length)
            ];

            console.log(`Бот походил: ${chosenMove.move}`);
            resolve(chosenMove);
        }, thinkingTime);
    });
}

/**
 * Проверяет, завершена ли партия.
 * В реальном проекте здесь была бы проверка мата, пата и т.д.
 * 
 * @param {string} fen - Текущая позиция
 * @param {string[]} moves - Все ходы в партии
 * @returns {{isOver: boolean, result: string|null}}
 */
export function checkGameOver(fen, moves = []) {
    // Упрощённая проверка: завершаем партию после 20 ходов
    if (moves.length >= 20) {
        return {
            isOver: true,
            result: Math.random() > 0.5 ? 'white_win' : 'draw',
        };
    }

    return {
        isOver: false,
        result: null,
    };
}

export default {
    calculateBotMove,
    checkGameOver,
};