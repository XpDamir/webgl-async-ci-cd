namespace ChessCore
{
    public class Board
    {
        private Piece[,] grid = new Piece[8, 8];

        public void Initialize()
        {
            for (int x = 0; x < 8; x++)
            {
                SetPiece(x, 1, new Piece(PieceType.Pawn, PieceColor.White));
                SetPiece(x, 6, new Piece(PieceType.Pawn, PieceColor.Black));
            }
            // Ладьи
            SetPiece(0, 0, new Piece(PieceType.Rook, PieceColor.White));
            SetPiece(7, 0, new Piece(PieceType.Rook, PieceColor.White));
            SetPiece(0, 7, new Piece(PieceType.Rook, PieceColor.Black));
            SetPiece(7, 7, new Piece(PieceType.Rook, PieceColor.Black));
            // Кони
            SetPiece(1, 0, new Piece(PieceType.Knight, PieceColor.White));
            SetPiece(6, 0, new Piece(PieceType.Knight, PieceColor.White));
            SetPiece(1, 7, new Piece(PieceType.Knight, PieceColor.Black));
            SetPiece(6, 7, new Piece(PieceType.Knight, PieceColor.Black));
            // Слоны
            SetPiece(2, 0, new Piece(PieceType.Bishop, PieceColor.White));
            SetPiece(5, 0, new Piece(PieceType.Bishop, PieceColor.White));
            SetPiece(2, 7, new Piece(PieceType.Bishop, PieceColor.Black));
            SetPiece(5, 7, new Piece(PieceType.Bishop, PieceColor.Black));
            // Короли и ферзи
            SetPiece(3, 0, new Piece(PieceType.Queen, PieceColor.White));
            SetPiece(3, 7, new Piece(PieceType.Queen, PieceColor.Black));
            SetPiece(4, 0, new Piece(PieceType.King, PieceColor.White));
            SetPiece(4, 7, new Piece(PieceType.King, PieceColor.Black));
        }

        public bool IsInside(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8;
        public Piece GetPiece(int x, int y) => IsInside(x, y) ? grid[x, y] : Piece.Empty;
        public void SetPiece(int x, int y, Piece piece) { if (IsInside(x, y)) grid[x, y] = piece; }
    }
}