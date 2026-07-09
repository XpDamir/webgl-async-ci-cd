using UnityEngine;
using ChessCore;

public class BoardView2D : MonoBehaviour
{
    public GameObject tilePrefab;
    public Color whiteColor = new Color(0.8f, 0.8f, 0.8f);
    public Color blackColor = new Color(0.3f, 0.3f, 0.3f);
    public Color highlightColor = Color.green;

    private ChessTile[,] tiles = new ChessTile[8, 8];
    private Transform tilesContainer;

    public void Generate()
    {
        // Создаем или очищаем контейнер клеток
        if (tilesContainer == null)
        {
            GameObject go = new GameObject("TilesContainer");
            tilesContainer = go.transform;
            tilesContainer.SetParent(this.transform);
            tilesContainer.localPosition = Vector3.zero;
        }

        // Очистка старых клеток (через Immediate)
        for (int i = tilesContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(tilesContainer.GetChild(i).gameObject);
        }

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                GameObject obj = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity, tilesContainer);
                obj.name = $"Tile_{x}_{y}";

                ChessTile tile = obj.GetComponent<ChessTile>();
                if (tile != null) tile.Init(x, y);

                SpriteRenderer rend = obj.GetComponent<SpriteRenderer>();
                if (rend != null)
                {
                    rend.sortingOrder = 0;
                    rend.color = (x + y) % 2 != 0 ? whiteColor : blackColor;
                }
                tiles[x, y] = tile;
            }
        }
    }

    public void HighlightTile(int x, int y, bool highlight)
    {
        if (x < 0 || x >= 8 || y < 0 || y >= 8 || tiles[x, y] == null) return;
        Color baseColor = (x + y) % 2 != 0 ? whiteColor : blackColor;
        tiles[x, y].SetColor(highlight ? highlightColor : baseColor);
    }
}