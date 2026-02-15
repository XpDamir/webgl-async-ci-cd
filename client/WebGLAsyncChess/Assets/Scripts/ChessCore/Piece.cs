namespace ChessCore
{
    public struct Piece
    {
        public PieceType Type; 
        public PieceColor Color; 

        public bool IsEmpty => Type == PieceType.None; 

        public static readonly Piece Empty = new Piece(PieceType.None, PieceColor.White);

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color; 
        }
    }
}