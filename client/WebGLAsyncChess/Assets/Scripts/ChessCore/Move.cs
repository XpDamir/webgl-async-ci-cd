namespace ChessCore
{
    public struct Move
    {
        public BoardPosition From;
        public BoardPosition To;
        public PieceType? Promotion;

        public Move(BoardPosition from, BoardPosition to, PieceType? promotion = null)
        {
            From = from;
            To = to;
            Promotion = promotion;
        }
    }
}