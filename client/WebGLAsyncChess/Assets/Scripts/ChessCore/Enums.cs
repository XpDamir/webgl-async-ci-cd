namespace ChessCore
{
    public enum PieceType
    {
        None,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }


    public enum PieceColor
    {
        White,
        Black
    }

    public enum GameResult
    {
        Ongoing,
        WhiteWin,
        BlackWin,
        Draw

    }
}