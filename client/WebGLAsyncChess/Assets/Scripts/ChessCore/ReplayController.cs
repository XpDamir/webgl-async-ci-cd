using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChessCore;

public class ReplayController : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private ChessGameController2D controller;
    [SerializeField] private Text bestTimeText;
    [SerializeField] private Vector3 replayPosition = new Vector3(5, -3, 0);

    [Header("Настройки")]
    [SerializeField] private float moveDelay = 1.5f;
    [SerializeField] private string serverUrl = "https://webgl-async-ci-cd-production.up.railway.app";
    [SerializeField] private float replayScale = 0.35f;
    [SerializeField] private Vector2 cornerOffset = new Vector2(50, 50);

    private bool isReplaying = false;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (controller != null) controller.InitializeBoard();
        HideAllRenderers();
    }

    public void StartReplay()
    {
        gameObject.SetActive(true);
        if (!isReplaying) StartCoroutine(FetchAndReplayCoroutine());
    }

    private IEnumerator FetchAndReplayCoroutine()
    {
        transform.position = replayPosition;
        isReplaying = true;

        string url = $"{serverUrl}/api/sessions/best";
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                isReplaying = false;
                yield break;
            }

            var response = JsonUtility.FromJson<BestSessionResponse>(request.downloadHandler.text);
            if (response.session == null || response.session.moves == null || response.session.moves.Length == 0)
            {
                if (bestTimeText != null) bestTimeText.text = "Нет записей";
                isReplaying = false;
                yield break;
            }

            // Позиционируем в правый нижний угол
            //SnapToCorner();

            // Генерируем доску
            controller.InitializeBoard();

            // Масштабируем дочерние контейнеры
            foreach (Transform child in transform)
            {
                child.localScale = new Vector3(replayScale, replayScale, 1f);
            }

            ShowAllRenderers();

            if (bestTimeText != null && response.session.duration > 0)
            {
                int mins = response.session.duration / 60;
                int secs = response.session.duration % 60;
                bestTimeText.text = $"Лучшая: {mins:D2}:{secs:D2}";
            }

            foreach (string moveStr in response.session.moves)
            {
                yield return new WaitForSeconds(moveDelay);
                ApplyMove(moveStr);
            }
        }

        isReplaying = false;
    }

    //private void SnapToCorner()
    //{
    //    if (mainCamera == null) mainCamera = Camera.main;
    //    if (mainCamera == null) return;

    //    // Размер доски в мировых единицах (8 клеток * масштаб)
    //    float boardWorldSize = 8f * replayScale;

    //    // Правый нижний угол экрана в мировых координатах
    //    Vector3 bottomRightScreen = new Vector3(Screen.width - cornerOffset.x, cornerOffset.y, 10);
    //    Vector3 worldPos = mainCamera.ScreenToWorldPoint(bottomRightScreen);

    //    // Смещаем на половину размера доски, чтобы она была внутри экрана
    //    worldPos.x -= boardWorldSize / 2f;
    //    worldPos.y += boardWorldSize / 2f;
    //    worldPos.z = 0;

    //    transform.position = worldPos;
    //    transform.localScale = Vector3.one;
    //}

    private void ApplyMove(string moveStr)
    {
        string cleanMove = moveStr;
        string promotion = null;

        if (moveStr.Length > 5)
        {
            char lastChar = moveStr[moveStr.Length - 1];
            if (lastChar >= 'a' && lastChar <= 'z')
            {
                promotion = lastChar.ToString();
                cleanMove = moveStr.Substring(0, moveStr.Length - 1);
            }
        }

        string[] parts = cleanMove.Split('-');
        if (parts.Length != 2) return;

        int fromX = parts[0][0] - 'a';
        int fromY = parts[0][1] - '1';
        int toX = parts[1][0] - 'a';
        int toY = parts[1][1] - '1';

        PieceType? promoType = null;
        if (promotion != null)
        {
            switch (promotion)
            {
                case "q": promoType = PieceType.Queen; break;
                case "r": promoType = PieceType.Rook; break;
                case "b": promoType = PieceType.Bishop; break;
                case "n": promoType = PieceType.Knight; break;
            }
        }

        var move = new Move(new BoardPosition(fromX, fromY), new BoardPosition(toX, toY), promoType);
        controller.Game.ExecuteLocalMove(move);
        controller.spawner.SpawnAll();

        foreach (Transform child in transform)
        {
            child.localScale = new Vector3(replayScale, replayScale, 1f);
        }
    }

    private void HideAllRenderers()
    {
        foreach (var rend in GetComponentsInChildren<Renderer>(true)) rend.enabled = false;
    }

    private void ShowAllRenderers()
    {
        foreach (var rend in GetComponentsInChildren<Renderer>(true)) rend.enabled = true;
    }

    public void Clear()
    {
        StopAllCoroutines();
        isReplaying = false;
        HideAllRenderers();
        transform.position = Vector3.zero;
        if (bestTimeText != null) bestTimeText.text = "Лучшая: --:--";
        gameObject.SetActive(false);
    }

    [System.Serializable] private class BestSessionResponse { public string message; public SessionData session; }
    [System.Serializable] private class SessionData { public int id; public string status; public string result; public string[] moves; public int duration; }
}