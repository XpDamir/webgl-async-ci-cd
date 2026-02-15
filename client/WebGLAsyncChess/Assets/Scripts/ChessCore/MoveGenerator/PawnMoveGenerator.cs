using System.Collections.Generic;

namespace ChessCore
{
    public static class PawnMoveGenerator
    {
        public static void Generate(
            GameState state,
            int x,
            int y,
            List<Move> moves)
        {
            Board board = state.Board;
            var piece = board.GetPiece(x, y);

            int direction = piece.Color == PieceColor.White ? 1 : -1;
            int startRow = piece.Color == PieceColor.White ? 1 : 6;
            int promotionRow = piece.Color == PieceColor.White ? 7 : 0;

            int forwardY = y + direction;

            if (board.IsInside(x, forwardY) &&
                board.GetPiece(x, forwardY).IsEmpty)
            {
                AddPawnMove(x, y, x, forwardY, promotionRow, moves);

                if (y == startRow)
                {
                    int doubleForwardY = y + direction * 2;

                    if (board.GetPiece(x, doubleForwardY).IsEmpty)
                    {
                        moves.Add(new Move(
                            new BoardPosition(x, y),
                            new BoardPosition(x, doubleForwardY)
                        ));
                    }
                }
            }

            for (int dx = -1; dx <= 1; dx += 2)
            {
                int targetX = x + dx;
                int targetY = y + direction;

                if (!board.IsInside(targetX, targetY))
                    continue;

                var targetPiece = board.GetPiece(targetX, targetY);

                if (!targetPiece.IsEmpty &&
                    targetPiece.Color != piece.Color)
                {
                    AddPawnMove(x, y, targetX, targetY, promotionRow, moves);
                }

                if (state.EnPassantTarget.HasValue &&
                    state.EnPassantTarget.Value.X == targetX &&
                    state.EnPassantTarget.Value.Y == targetY)
                {
                    moves.Add(new Move(
                        new BoardPosition(x, y),
                        new BoardPosition(targetX, targetY)
                    ));
                }
            }
        }

        private static void AddPawnMove(
            int fromX,
            int fromY,
            int toX,
            int toY,
            int promotionRow,
            List<Move> moves)
        {
            if (toY == promotionRow)
            {
                moves.Add(new Move(new BoardPosition(fromX, fromY), new BoardPosition(toX, toY), PieceType.Queen));
                moves.Add(new Move(new BoardPosition(fromX, fromY), new BoardPosition(toX, toY), PieceType.Rook));
                moves.Add(new Move(new BoardPosition(fromX, fromY), new BoardPosition(toX, toY), PieceType.Bishop));
                moves.Add(new Move(new BoardPosition(fromX, fromY), new BoardPosition(toX, toY), PieceType.Knight));
            }
            else
            {
                moves.Add(new Move(
                    new BoardPosition(fromX, fromY),
                    new BoardPosition(toX, toY)
                ));
            }
        }
    }
}