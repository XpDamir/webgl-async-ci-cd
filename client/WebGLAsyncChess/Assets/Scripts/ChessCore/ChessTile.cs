using UnityEngine;
using UnityEngine.EventSystems;

public class ChessTile : MonoBehaviour, IPointerClickHandler
{
    public int X;
    public int Y;
    private SpriteRenderer rend;

    public void Init(int x, int y)
    {
        X = x;
        Y = y;
        rend = GetComponent<SpriteRenderer>();
    }

    public void SetColor(Color color)
    {
        if (rend == null) rend = GetComponent<SpriteRenderer>();
        rend.color = color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Clicked on {X}:{Y}");
        ChessInput.Instance.OnSquareClicked(X, Y);
    }
}