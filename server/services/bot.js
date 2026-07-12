import { Chess } from 'chess.js';

export async function calculateBotMove(fen, previousMoves = []) {
    const thinkingTime = 1000 + Math.floor(Math.random() * 1000);
    console.log(`Бот анализирует позицию... (${thinkingTime / 1000} сек)`);

    return new Promise((resolve) => {
        setTimeout(() => {
            try {
                const chess = new Chess(fen || 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1');

                if (chess.turn() === 'w') {
                    console.log('Бот: сейчас ход белых, пропускаю');
                    resolve({ move: null, fen: chess.fen(), skipped: true });
                    return;
                }

                // Проверяем завершение до хода
                if (chess.isGameOver()) {
                    console.log('Бот: игра уже завершена');
                    resolve({
                        move: null,
                        fen: chess.fen(),
                        gameOver: true,
                        result: getResult(chess),
                    });
                    return;
                }

                const legalMoves = chess.moves({ verbose: true });

                if (legalMoves.length === 0) {
                    console.log('Бот: нет легальных ходов');
                    resolve({
                        move: null,
                        fen: chess.fen(),
                        gameOver: true,
                        result: getResult(chess),
                    });
                    return;
                }

                const randomMove = legalMoves[Math.floor(Math.random() * legalMoves.length)];

                let formattedMove = `${randomMove.from}-${randomMove.to}`;
                if (randomMove.promotion) {
                    formattedMove += randomMove.promotion;
                }

                chess.move(randomMove);

                const isOver = chess.isGameOver();
                const result = isOver ? getResult(chess) : null;

                if (isOver) {
                    console.log(`Бот: игра завершена (${result})`);
                } else {
                    console.log(`Бот походил: ${formattedMove}`);
                }

                resolve({
                    move: formattedMove,
                    fen: chess.fen(),
                    gameOver: isOver,
                    result: result,
                });
            } catch (error) {
                console.error('Ошибка бота:', error.message);
                resolve({ move: null, fen: fen, error: error.message });
            }
        }, thinkingTime);
    });
}

function getResult(chess) {
    if (chess.isCheckmate()) {
        // Чей ход сейчас — тот проиграл
        return chess.turn() === 'b' ? 'white_win' : 'black_win';
    }
    if (chess.isStalemate()) return 'draw';
    if (chess.isThreefoldRepetition()) return 'draw';
    if (chess.isInsufficientMaterial()) return 'draw';
    if (chess.isDraw()) return 'draw';
    return 'draw';
}

export function checkGameOver(fen) {
    try {
        const chess = new Chess(fen);
        return {
            isOver: chess.isGameOver(),
            result: chess.isGameOver() ? getResult(chess) : null,
        };
    } catch {
        return { isOver: false, result: null };
    }
}

export default { calculateBotMove, checkGameOver };