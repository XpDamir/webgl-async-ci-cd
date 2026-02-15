using UnityEngine;
using ChessCore;
using System.Linq;
using System.Collections.Generic;

public class ChessCoreTester : MonoBehaviour
{
    private GameState game;

    void Start()
    {
        game = new GameState();

        Debug.Log("Game started.");

        TestInitialMoves();
        TestMakeMove();
    }

    void TestInitialMoves()
    {
        List<Move> moves = MoveValidator.GenerateMoves(game);

        Debug.Log("Initial move count: " + moves.Count);

        foreach (var move in moves.Take(10))
        {
            Debug.Log($"Move: ({move.From.X},{move.From.Y}) -> ({move.To.X},{move.To.Y})");
        }
    }

    void TestMakeMove()
    {
        var selectable = game.SelectPiece(0, 1);

        if (selectable == null || selectable.Count == 0)
        {
            Debug.Log("No moves!");
            return;
        }

        var target = selectable[0];

        game.TryMove(target.X, target.Y);

        Debug.Log("Recorded moves: " + game.Record.Moves.Count);
        Debug.Log("Move executed.");

        var newMoves = MoveValidator.GenerateMoves(game);
        Debug.Log("New move count: " + newMoves.Count);
    }
}