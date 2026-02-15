using System.Collections.Generic;

namespace ChessCore
{
    public class GameRecord
    {
        public List<Move> Moves = new();

        public void AddMove(Move move)
        {
            Moves.Add(move);
        }

        public void Clear()
        {
            Moves.Clear();
        }
    }
}