using UnityEngine;
using UnityEngine.EventSystems;

public class PieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int X, Y;
    private Vector3 startPos;
    private SpriteRenderer rend;

    private void Awake() => rend = GetComponent<SpriteRenderer>();

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = transform.position;
        if (ChessInput.Instance != null)
            ChessInput.Instance.ShowMovesFor(X, Y);

        if (rend != null) rend.sortingOrder = 10;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = -1;
        transform.position = mousePos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (rend != null) rend.sortingOrder = 1;

        int targetX = Mathf.RoundToInt(transform.position.x);
        int targetY = Mathf.RoundToInt(transform.position.y);

        if (ChessInput.Instance != null && ChessInput.Instance.controller != null)
        {
            if (ChessInput.Instance.controller.Game.TryMove(targetX, targetY))
            {
                ChessInput.Instance.controller.spawner.SpawnAll();
                ChessInput.Instance.ClearHighlights();
                return;
            }
        }

        transform.position = startPos;
        if (ChessInput.Instance != null) ChessInput.Instance.ClearHighlights();
    }
}