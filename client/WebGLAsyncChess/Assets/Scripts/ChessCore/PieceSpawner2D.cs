using UnityEngine;
using ChessCore;

public class PieceSpawner2D : MonoBehaviour
{
    public GameObject piecePrefab;
    public GameState Game;
    public PieceSprites sprites;

    private Transform piecesContainer;

    public void SpawnAll()
    {
        if (piecesContainer == null)
        {
            GameObject go = new GameObject("PiecesContainer");
            piecesContainer = go.transform;
            piecesContainer.SetParent(this.transform);
            piecesContainer.localPosition = Vector3.zero;
        }

        ClearAll();

        if (Game == null || Game.Board == null) return;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var piece = Game.Board.GetPiece(x, y);
                if (piece.IsEmpty) continue;

                var obj = Instantiate(piecePrefab, new Vector3(x, y, -1), Quaternion.identity, piecesContainer);

                var view = obj.GetComponent<PieceView>();
                if (view != null)
                {
                    view.X = x;
                    view.Y = y;
                    view.SetPiece(piece);
                }
            }
        }
    }

    public void ClearAll()
    {
        if (piecesContainer == null) return;

        for (int i = piecesContainer.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(piecesContainer.GetChild(i).gameObject);
            else
                DestroyImmediate(piecesContainer.GetChild(i).gameObject);
        }
    }
}