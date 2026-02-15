using System.Collections.Generic;

namespace ChessCore
{
    public static class KingMoveGenerator
    {
        public static void Generate(GameState state, int x, int y, List<Move> moves)
        {
            Board board = state.Board;
            var piece = board.GetPiece(x, y);
            PieceColor color = piece.Color;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int targetX = x + dx;
                    int targetY = y + dy;

                    if (!board.IsInside(targetX, targetY))
                        continue;

                    var targetPiece = board.GetPiece(targetX, targetY);

                    if (targetPiece.IsEmpty || targetPiece.Color != color)
                    {
                        moves.Add(new Move(
                            new BoardPosition(x, y),
                            new BoardPosition(targetX, targetY)
                        ));
                    }
                }
            }
        }
    }
}