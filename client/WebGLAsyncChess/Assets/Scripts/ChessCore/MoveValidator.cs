using System.Collections.Generic;

namespace ChessCore
{
    public static class MoveValidator
    {
        public static List<Move> GenerateMoves(GameState state)
        {
            List<Move> moves = new List<Move>();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece piece = state.Board.GetPiece(x, y);

                    if (piece.IsEmpty)
                        continue;

                    if (piece.Color != state.CurrentTurn)
                        continue;

                    switch (piece.Type)
                    {
                        case PieceType.Pawn:
                            PawnMoveGenerator.Generate(state, x, y, moves);
                            break;

                        case PieceType.Knight:
                            KnightMoveGenerator.Generate(state, x, y, moves);
                            break;

                        case PieceType.Bishop:
                            BishopMoveGenerator.Generate(state, x, y, moves);
                            break;

                        case PieceType.Rook:
                            RookMoveGenerator.Generate(state, x, y, moves);
                            break;

                        case PieceType.Queen:
                            QueenMoveGenerator.Generate(state, x, y, moves);
                            break;

                        case PieceType.King:
                            KingMoveGenerator.Generate(state, x, y, moves);
                            break;
                    }
                }
            }

            return moves;
        }

        public static void GenerateSlidingMoves(
            Board board,
            int x,
            int y,
            PieceColor color,
            int[,] directions,
            List<Move> moves)
        {
            int directionCount = directions.GetLength(0);

            for (int d = 0; d < directionCount; d++)
            {
                int dx = directions[d, 0];
                int dy = directions[d, 1];

                int currentX = x + dx;
                int currentY = y + dy;

                while (board.IsInside(currentX, currentY))
                {
                    var targetPiece = board.GetPiece(currentX, currentY);

                    if (targetPiece.IsEmpty)
                    {
                        moves.Add(new Move(
                            new BoardPosition(x, y),
                            new BoardPosition(currentX, currentY)
                        ));
                    }
                    else
                    {
                        if (targetPiece.Color != color)
                        {
                            moves.Add(new Move(
                                new BoardPosition(x, y),
                                new BoardPosition(currentX, currentY)
                            ));
                        }

                        break;
                    }

                    currentX += dx;
                    currentY += dy;
                }
            }
        }
    }
}