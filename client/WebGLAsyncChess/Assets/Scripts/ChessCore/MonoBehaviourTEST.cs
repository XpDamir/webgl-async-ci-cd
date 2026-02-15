using ChessCore;
using UnityEngine;
using System.Collections.Generic;

public class MonoBehaviourTEST : MonoBehaviour
{
    void Start()
    {
        GameState game = new GameState();

        List<Move> moves = MoveValidator.GenerateMoves(game);

        Debug.Log("Current turn: " + game.CurrentTurn);
        Debug.Log("Total moves: " + moves.Count);

        // Выведем сами ходы
        foreach (var move in moves)
        {
            Debug.Log(
                $"Move: {move.From.X},{move.From.Y} -> {move.To.X},{move.To.Y}"
            );
        }
    }
}