using System.Collections.Generic;

namespace ChessCore
{
    public static class QueenMoveGenerator
    {
        public static void Generate(GameState state, int x, int y, List<Move> moves)
        {
            Board board = state.Board;
            var piece = board.GetPiece(x, y);
            PieceColor color = piece.Color;

            int[,] directions =
            {
                { 1, 0 }, { -1, 0 },
                { 0, 1 }, { 0, -1 },
                { 1, 1 }, { 1, -1 },
                { -1, 1 }, { -1, -1 }
            };

            MoveValidator.GenerateSlidingMoves(board, x, y, color, directions, moves);
        }
    }
}