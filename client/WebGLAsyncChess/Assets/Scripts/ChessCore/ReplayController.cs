using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReplayController : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private ChessGameController2D controller;
    [SerializeField] private Text bestTimeText;

    [Header("Настройки")]
    [SerializeField] private float moveDelay = 1.5f;
    [SerializeField] private string serverUrl = "https://webgl-async-ci-cd-production.up.railway.app";
    [SerializeField] private float replayScale = 0.35f;

    private bool isReplaying = false;
    private bool isInitialized = false;

    private void Start()
    {
        if (controller != null)
        {
            controller.InitializeBoard();
        }
        HideAllRenderers();
        isInitialized = true;
    }

    public void StartReplay()
    {
        if (!isReplaying)
            StartCoroutine(FetchAndReplayCoroutine());
    }

    private IEnumerator FetchAndReplayCoroutine()
    {
        isReplaying = true;

        // Уменьшаем доску
        transform.localScale = new Vector3(replayScale, replayScale, 1f);
        ShowAllRenderers();

        string url = $"{serverUrl}/api/sessions/best";
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Не удалось загрузить лучшую партию");
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

            if (bestTimeText != null && response.session.duration > 0)
            {
                int mins = response.session.duration / 60;
                int secs = response.session.duration % 60;
                bestTimeText.text = $"Лучшая: {mins:D2}:{secs:D2}";
            }

            // Пересоздаём доску в уменьшенном масштабе
            controller.InitializeBoard();

            foreach (string moveStr in response.session.moves)
            {
                yield return new WaitForSeconds(moveDelay);
                ApplyMove(moveStr);
            }
        }

        isReplaying = false;
    }

    private void ApplyMove(string moveStr)
    {
        string[] parts = moveStr.Split('-');
        if (parts.Length != 2) return;

        int fromX = parts[0][0] - 'a';
        int fromY = parts[0][1] - '1';
        int toX = parts[1][0] - 'a';
        int toY = parts[1][1] - '1';

        controller.Game.SelectPiece(fromX, fromY);
        if (controller.Game.TryMove(toX, toY))
        {
            controller.spawner.SpawnAll();
        }
    }

    private void HideAllRenderers()
    {
        foreach (var rend in GetComponentsInChildren<Renderer>(true))
        {
            rend.enabled = false;
        }
    }

    private void ShowAllRenderers()
    {
        foreach (var rend in GetComponentsInChildren<Renderer>(true))
        {
            rend.enabled = true;
        }
    }

    public void StopReplay()
    {
        StopAllCoroutines();
        isReplaying = false;
    }

    public void Clear()
    {
        StopReplay();
        HideAllRenderers();
        transform.localScale = Vector3.one;
        if (bestTimeText != null) bestTimeText.text = "Лучшая: --:--";
    }

    [System.Serializable]
    private class BestSessionResponse
    {
        public string message;
        public SessionData session;
    }

    [System.Serializable]
    private class SessionData
    {
        public int id;
        public string status;
        public string result;
        public string[] moves;
        public int duration;
    }
}