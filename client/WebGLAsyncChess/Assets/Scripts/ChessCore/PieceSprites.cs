using UnityEngine;
using ChessCore;

public class PieceSprites : MonoBehaviour
{
    public Sprite whitePawn;
    public Sprite whiteRook;
    public Sprite whiteKnight;
    public Sprite whiteBishop;
    public Sprite whiteQueen;
    public Sprite whiteKing;

    public Sprite blackPawn;
    public Sprite blackRook;
    public Sprite blackKnight;
    public Sprite blackBishop;
    public Sprite blackQueen;
    public Sprite blackKing;

    public Sprite GetSprite(Piece piece)
    {
        if (piece.Type == PieceType.None)
            return null;

        switch (piece.Type)
        {
            case PieceType.Pawn:
                return piece.Color == PieceColor.White ? whitePawn : blackPawn;

            case PieceType.Rook:
                return piece.Color == PieceColor.White ? whiteRook : blackRook;

            case PieceType.Knight:
                return piece.Color == PieceColor.White ? whiteKnight : blackKnight;

            case PieceType.Bishop:
                return piece.Color == PieceColor.White ? whiteBishop : blackBishop;

            case PieceType.Queen:
                return piece.Color == PieceColor.White ? whiteQueen : blackQueen;

            case PieceType.King:
                return piece.Color == PieceColor.White ? whiteKing : blackKing;
        }

        return null;
    }
}