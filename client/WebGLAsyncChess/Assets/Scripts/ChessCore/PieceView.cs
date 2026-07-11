using UnityEngine;
using UnityEngine.EventSystems;
using ChessCore;

public class PieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int X, Y;
    private Vector3 startPos;
    private SpriteRenderer rend;
    private bool isDraggingAllowed = false;

    private void Awake()
    {
        rend = GetComponent<SpriteRenderer>();
    }

    public void SetPiece(Piece piece)
    {
        var sprites = FindObjectOfType<PieceSprites>();
        if (sprites != null && rend != null)
        {
            rend.sprite = sprites.GetSprite(piece);
        }
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = transform.position;

        var game = ChessInput.Instance.controller.Game;
        var piece = game.Board.GetPiece(X, Y);

        if (!piece.IsEmpty && game.CurrentTurn == PieceColor.White && piece.Color == PieceColor.White)
        {
            isDraggingAllowed = true;

            if (ChessInput.Instance != null)
                ChessInput.Instance.ShowMovesFor(X, Y);

            if (rend != null)
                rend.sortingOrder = 10;
        }
        else
        {
            isDraggingAllowed = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingAllowed) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = -1;
        transform.position = mousePos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (rend != null) rend.sortingOrder = 1;

        if (!isDraggingAllowed)
        {
            transform.position = startPos;
            return;
        }

        int targetX = Mathf.RoundToInt(transform.position.x);
        int targetY = Mathf.RoundToInt(transform.position.y);

        var controller = ChessInput.Instance.controller;

        if (controller.Game.TryMove(targetX, targetY))
        {
            controller.spawner.SpawnAll();
            ChessInput.Instance.ClearHighlights();
        }
        else
        {
            transform.position = startPos;
            ChessInput.Instance.ClearHighlights();
        }

        isDraggingAllowed = false;
    }
}