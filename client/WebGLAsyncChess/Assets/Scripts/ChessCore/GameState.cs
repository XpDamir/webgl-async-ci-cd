using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessCore
{
    public class GameState
    {
        public Board Board { get; private set; }
        public PieceColor CurrentTurn { get; private set; }
        public BoardPosition? EnPassantTarget { get; set; }
        public GameRecord Record { get; private set; } = new GameRecord();

        private BoardPosition? selectedPiece;
        private List<Move> currentLegalMoves = new List<Move>();

        public event Action<Move> OnMoveExecuted;

        public GameState()
        {
            Board = new Board();
            Board.Initialize();
            CurrentTurn = PieceColor.White;
        }


        public List<BoardPosition> SelectPiece(int x, int y)
        {
            var piece = Board.GetPiece(x, y);
            if (piece.IsEmpty || piece.Color != CurrentTurn) return null;

            selectedPiece = new BoardPosition(x, y);
            currentLegalMoves = MoveValidator.GenerateMoves(this)
                .Where(m => m.From.X == x && m.From.Y == y).ToList();

            return currentLegalMoves.Select(m => m.To).ToList();
        }

        public bool TryMove(int x, int y)
        {
            if (selectedPiece == null) return false;

            bool moveIsValid = currentLegalMoves.Any(m => m.To.X == x && m.To.Y == y);

            if (!moveIsValid) return false;

            var move = currentLegalMoves.First(m => m.To.X == x && m.To.Y == y);

            ExecuteLocalMove(move);

            OnMoveExecuted?.Invoke(move);

            return true;
        }

        /// <summary>
        /// Выполняет ход локально на доске без вызова события сетевой синхронизации
        /// </summary>
        public void ExecuteLocalMove(Move move)
        {
            var piece = Board.GetPiece(move.From.X, move.From.Y);

            // Превращение пешки
            Piece movingPiece = piece;
            if (move.Promotion.HasValue && move.Promotion.Value != PieceType.None)
            {
                movingPiece = new Piece(move.Promotion.Value, piece.Color);
            }

            Board.SetPiece(move.To.X, move.To.Y, movingPiece);
            Board.SetPiece(move.From.X, move.From.Y, Piece.Empty);

            CurrentTurn = (CurrentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            selectedPiece = null;
            currentLegalMoves.Clear();

            Record.AddMove(move);
        }
    }
}