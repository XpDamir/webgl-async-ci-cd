using UnityEngine;

public class BoardCoordinates : MonoBehaviour
{
    [Header("Отступ от доски")]
    [SerializeField] private float letterOffsetY = 0.65f;  // Буквы: отступ снизу
    [SerializeField] private float numberOffsetX = 0.65f;  // Цифры: отступ слева

    [Header("Тонкая настройка")]
    [SerializeField] private float letterShiftX = 0f;      // Буквы: смещение влево/вправо
    [SerializeField] private float letterShiftY = 0f;      // Буквы: смещение вверх/вниз
    [SerializeField] private float numberShiftX = 0f;      // Цифры: смещение влево/вправо
    [SerializeField] private float numberShiftY = 0f;      // Цифры: смещение вверх/вниз

    [Header("Шрифт")]
    [SerializeField] private int fontSize = 36;
    [SerializeField] private float characterSize = 0.15f;
    [SerializeField] private Font font;

    private GameObject coordinatesContainer;

    public void Generate()
    {
        if (coordinatesContainer != null)
        {
            if (Application.isPlaying) Destroy(coordinatesContainer);
            else DestroyImmediate(coordinatesContainer);
        }

        coordinatesContainer = new GameObject("Coordinates");
        coordinatesContainer.transform.SetParent(transform);
        coordinatesContainer.transform.localPosition = Vector3.zero;

        Transform tilesContainer = transform.Find("TilesContainer");
        float tileSize = 1f;
        float boardOffsetX = 0f;
        float boardOffsetY = 0f;

        if (tilesContainer != null && tilesContainer.childCount > 0)
        {
            Transform firstTile = tilesContainer.GetChild(0);
            SpriteRenderer rend = firstTile.GetComponent<SpriteRenderer>();
            if (rend != null)
            {
                tileSize = rend.bounds.size.x;
                boardOffsetX = firstTile.localPosition.x;
                boardOffsetY = firstTile.localPosition.y;
            }
        }

        string[] letters = { "a", "b", "c", "d", "e", "f", "g", "h" };
        float halfTile = tileSize / 2f;

        // Буквы a-h снизу
        for (int i = 0; i < 8; i++)
        {
            float x = boardOffsetX + i * tileSize + halfTile + letterShiftX;
            float y = boardOffsetY - letterOffsetY + letterShiftY;
            Color color = (i % 2 == 0) ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            CreateText(letters[i], new Vector3(x, y, -0.1f), color);
        }

        // Цифры 1-8 слева
        for (int i = 0; i < 8; i++)
        {
            float x = boardOffsetX - numberOffsetX + numberShiftX;
            float y = boardOffsetY + i * tileSize + halfTile + numberShiftY;
            Color color = (i % 2 == 0) ? new Color(0.4f, 0.4f, 0.4f) : Color.white;
            CreateText((i + 1).ToString(), new Vector3(x, y, -0.1f), color);
        }
    }

    private void CreateText(string text, Vector3 position, Color color)
    {
        GameObject obj = new GameObject($"Coord_{text}");
        obj.transform.SetParent(coordinatesContainer.transform);
        obj.transform.localPosition = position;

        TextMesh textMesh = obj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        if (font != null) textMesh.font = font;

        obj.GetComponent<MeshRenderer>().sortingOrder = 10;
    }

    public void Show()
    {
        if (coordinatesContainer != null) coordinatesContainer.SetActive(true);
    }

    public void Hide()
    {
        if (coordinatesContainer != null) coordinatesContainer.SetActive(false);
    }
}