using ChessCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square
{
    public int X;
    public int Y;

    public Piece Piece = Piece.Empty;

    public bool IsWhite;

    public Square(int x, int y)
    {
        X = x;
        Y = y;
        IsWhite = (x + y) % 2 == 0;
    }
}
