using UnityEngine;

public class BoardView2D : MonoBehaviour
{
    public GameObject tilePrefab;
    public Color whiteColor = new Color(0.8f, 0.8f, 0.8f);
    public Color blackColor = new Color(0.3f, 0.3f, 0.3f);
    public Color highlightColor = Color.green;

    private ChessTile[,] tiles = new ChessTile[8, 8];

    public void Generate()
    {
        Debug.Log("BoardView: Попытка создания 64 клеток...");

        if (tilePrefab == null)
        {
            Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА: Tile Prefab не назначен в Инспекторе!");
            return;
        }

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                try
                {
                    GameObject obj = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                    obj.name = $"Tile_{x}_{y}";

                    ChessTile tile = obj.GetComponent<ChessTile>();
                    if (tile != null) tile.Init(x, y);

                    SpriteRenderer rend = obj.GetComponent<SpriteRenderer>();
                    if (rend != null)
                    {
                        rend.sortingOrder = 0;
                        rend.color = (x + y) % 2 != 0 ? whiteColor : blackColor;
                        if (rend.sprite == null) Debug.LogWarning($"У Tile_{x}_{y} нет спрайта!");
                    }

                    tiles[x, y] = tile;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Ошибка на клетке {x}:{y}: " + e.Message);
                }
            }
        }
        Debug.Log("BoardView: Цикл генерации завершен.");
    }

    public void HighlightTile(int x, int y, bool highlight)
    {
        if (x < 0 || x >= 8 || y < 0 || y >= 8 || tiles[x, y] == null) return;
        Color baseColor = (x + y) % 2 != 0 ? whiteColor : blackColor;
        tiles[x, y].SetColor(highlight ? highlightColor : baseColor);
    }
}