using UnityEngine;
using ChessCore;

public class ChessGameController2D : MonoBehaviour
{
    public BoardView2D boardView;
    public PieceSpawner2D spawner;
    public GameState Game { get; private set; }

    void Awake() 
    {
        Game = new GameState();
    }

    void Start()
    {
        Game = new GameState();

        if (boardView != null) boardView.Generate();

        if (spawner != null)
        {
            spawner.Game = Game;
            spawner.SpawnAll();
        }
    }
}