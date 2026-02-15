using ChessCore;
using System.Collections.Generic;

public static class KnightMoveGenerator
{
    public static void Generate(
        GameState state,
        int x,
        int y,
        List<Move> moves)
    {
        Board board = state.Board;
        var piece = board.GetPiece(x, y);

        int[,] offsets =
        {
            {2,1},{2,-1},{-2,1},{-2,-1},
            {1,2},{1,-2},{-1,2},{-1,-2}
        };

        for (int i = 0; i < 8; i++)
        {
            int targetX = x + offsets[i, 0];
            int targetY = y + offsets[i, 1];

            if (!board.IsInside(targetX, targetY))
                continue;

            var target = board.GetPiece(targetX, targetY);

            if (target.IsEmpty || target.Color != piece.Color)
            {
                moves.Add(new Move(
                    new BoardPosition(x, y),
                    new BoardPosition(targetX, targetY)
                ));
            }
        }
    }
}
