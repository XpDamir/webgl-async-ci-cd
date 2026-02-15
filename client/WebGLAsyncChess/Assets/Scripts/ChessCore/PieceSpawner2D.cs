using UnityEngine;
using ChessCore;

public class PieceSpawner2D : MonoBehaviour
{
    public GameObject piecePrefab;
    public GameState Game;

    public void SpawnAll()
    {
        ClearAll();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var piece = Game.Board.GetPiece(x, y);
                if (piece.IsEmpty) continue;

                var obj = Instantiate(piecePrefab, new Vector3(x, y, -1), Quaternion.identity, transform);
                var view = obj.GetComponent<PieceView>();
                view.X = x;
                view.Y = y;

                var rend = obj.GetComponent<SpriteRenderer>();
                if (rend != null)
                {
                    rend.sortingOrder = 1;
                    rend.color = (piece.Color == PieceColor.White) ? Color.white : new Color(0.15f, 0.15f, 0.15f);
                }
            }
        }
    }

    public void ClearAll()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
    }
}