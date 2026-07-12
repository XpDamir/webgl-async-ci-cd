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

                const legalMoves = chess.moves({ verbose: true });

                if (legalMoves.length === 0) {
                    console.log('Бот: нет легальных ходов');
                    const isCheckmate = chess.isCheckmate();
                    resolve({
                        move: null,
                        fen: chess.fen(),
                        gameOver: true,
                        result: isCheckmate ? 'white_win' : 'draw',
                    });
                    return;
                }

                const randomMove = legalMoves[Math.floor(Math.random() * legalMoves.length)];

                let formattedMove = `${randomMove.from}-${randomMove.to}`;
                if (randomMove.promotion) {
                    formattedMove += randomMove.promotion;
                }

                chess.move(randomMove);
                console.log(`Бот походил: ${formattedMove}`);

                resolve({ move: formattedMove, fen: chess.fen() });
            } catch (error) {
                console.error('Ошибка бота:', error.message);
                resolve({ move: null, fen: fen, error: error.message });
            }
        }, thinkingTime);
    });
}

export function checkGameOver(fen) {
    try {
        const chess = new Chess(fen);
        return {
            isOver: chess.isGameOver(),
            result: chess.isCheckmate() ? 'white_win' : chess.isDraw() ? 'draw' : null,
        };
    } catch {
        return { isOver: false, result: null };
    }
}

export default { calculateBotMove, checkGameOver };