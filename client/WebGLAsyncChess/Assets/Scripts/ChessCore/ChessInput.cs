using UnityEngine;
using System.Collections.Generic;
using ChessCore;

public class ChessInput : MonoBehaviour
{
    public static ChessInput Instance;
    public ChessGameController2D controller;
    private List<BoardPosition> activeHighlights = new List<BoardPosition>();

    void Awake()
    {
        Instance = this;
    }

    public void OnSquareClicked(int x, int y)
    {
        if (controller == null || controller.Game == null) return;

        if (controller.Game.TryMove(x, y))
        {
            controller.spawner.SpawnAll();
            ClearHighlights();
        }
        else
        {
            ShowMovesFor(x, y);
        }
    }

    public void ShowMovesFor(int x, int y)
    {
        ClearHighlights();
        var moves = controller.Game.SelectPiece(x, y);
        if (moves != null)
        {
            foreach (var pos in moves)
            {
                activeHighlights.Add(pos);
                controller.boardView.HighlightTile(pos.X, pos.Y, true);
            }
        }
    }

    public void ClearHighlights()
    {
        if (controller.boardView == null) return;
        foreach (var pos in activeHighlights)
            controller.boardView.HighlightTile(pos.X, pos.Y, false);
        activeHighlights.Clear();
    }
}