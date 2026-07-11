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

    public void InitializeBoard()
    {
        if (boardView != null) boardView.Generate();

        if (spawner != null)
        {
            spawner.Game = this.Game;
            spawner.SpawnAll();
        }
    }

    public void ResetGame()
    {
        Game = new GameState();
    }

    //private void OnMoveExecutedHandler(Move move)
    //{
    //    // Здесь будет вызов из NetworkChessManager
    //    // Оставьте пустым или перенесите логику
    //}
}