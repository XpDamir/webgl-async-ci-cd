using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ChessCore;

public class NetworkChessManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string serverUrl = "https://webgl-async-ci-cd-production.up.railway.app";

    [Header("UI Roots")]
    [SerializeField] private GameObject menuPanel;   // Панель с кнопкой "Играть"
    [SerializeField] private GameObject gameUIPanel; // Панель с кнопкой бота и статусом

    [Header("UI References")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button botMoveButton;
    [SerializeField] private Text statusText;

    private ChessGameController2D controller;
    private int? currentSessionId = null;
    private int localMovesCount = 0;
    private bool isWaitingForBot = false;

    private void Awake()
    {
        controller = GetComponent<ChessGameController2D>();

        // Автоопределение: если запущено в браузере не на localhost — используем продакшен
#if UNITY_WEBGL && !UNITY_EDITOR
        serverUrl = "https://webgl-async-ci-cd-production.up.railway.app"; // Замените на реальный URL после деплоя
#endif
    }

    private void Start()
    {
        // Изначальное состояние: меню включено, интерфейс игры выключен
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        // Скрываем доску до начала игры (если она уже создана контроллером)
        //SetBoardVisibility(false);

        // Подписка на кнопки
        if (newGameButton != null) newGameButton.onClick.AddListener(CreateNewGame);
        if (botMoveButton != null) botMoveButton.onClick.AddListener(RequestBotMove);

        UpdateUIState();

        if (controller != null && controller.Game != null)
        {
            controller.Game.OnMoveExecuted += HandleOnMoveExecuted;
        }
    }

    private void OnDestroy()
    {
        if (controller != null && controller.Game != null)
        {
            controller.Game.OnMoveExecuted -= HandleOnMoveExecuted;
        }
    }

    #region Visibility Logic

    private void SetBoardVisibility(bool visible)
    {
        // Находим все префабы клеток и фигур на сцене и скрываем/показываем их
        // Это костыль, так как контроллер уже запустил генерацию в своем Start()
        var tiles = GameObject.FindObjectsOfType<ChessTile>(true);
        foreach (var t in tiles) t.gameObject.SetActive(visible);

        var pieces = GameObject.FindObjectsOfType<PieceView>(true);
        foreach (var p in pieces) p.gameObject.SetActive(visible);
    }

    private void SwitchToGameUI()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(true);
        SetBoardVisibility(true);
    }

    #endregion

    #region API Logic

    public void CreateNewGame()
    {
        StartCoroutine(CreateSessionCoroutine());
    }

    public void RequestBotMove()
    {
        if (currentSessionId.HasValue && !isWaitingForBot)
        {
            StartCoroutine(RequestBotMoveCoroutine(currentSessionId.Value));
        }
    }

    private void HandleOnMoveExecuted(Move move)
    {
        localMovesCount++;
        if (currentSessionId.HasValue)
        {
            string moveString = ConvertMoveToString(move);
            StartCoroutine(SendMoveCoroutine(currentSessionId.Value, moveString));
        }
        UpdateUIState(); // Обновляем кнопки сразу после хода игрока
    }

    #endregion

    #region Coroutines

    private IEnumerator CreateSessionCoroutine()
    {
        string url = $"{serverUrl}/api/sessions";
        byte[] bodyData = System.Text.Encoding.UTF8.GetBytes("{}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
                currentSessionId = response.session.id;
                localMovesCount = 0;

                RestartLocalGame();
                controller.InitializeBoard();
                SwitchToGameUI(); // ПЕРЕКЛЮЧАЕМ ЭКРАН ТУТ
                UpdateStatusText($"Сессия {currentSessionId} начата.");
            }
            else
            {
                UpdateStatusText($"Ошибка: {request.error}");
            }
        }
        UpdateUIState();
    }

    // Остальные корутины (SendMove, RequestBotMove, PollSession) остаются такими же
    private IEnumerator SendMoveCoroutine(int sessionId, string moveStr)
    {
        string url = $"{serverUrl}/api/sessions/{sessionId}";
        string jsonPayload = JsonUtility.ToJson(new PutMoveRequest { move = moveStr });
        byte[] bodyData = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
        }
    }

    private IEnumerator RequestBotMoveCoroutine(int sessionId)
    {
        isWaitingForBot = true;
        UpdateUIState();
        UpdateStatusText("Бот думает...");

        string url = $"{serverUrl}/api/sessions/{sessionId}/bot-move";
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                StartCoroutine(PollSessionStateCoroutine(sessionId));
            else { isWaitingForBot = false; UpdateUIState(); }
        }
    }

    private IEnumerator PollSessionStateCoroutine(int sessionId)
    {
        string url = $"{serverUrl}/api/sessions/{sessionId}";
        bool responseReceived = false;

        while (!responseReceived)
        {
            yield return new WaitForSeconds(2f);

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);

                    // Если количество ходов на сервере увеличилось - значит бот что-то ответил
                    if (response.session.moves != null && response.session.moves.Length > localMovesCount)
                    {
                        responseReceived = true; // Выходим из цикла
                        string botMoveStr = response.session.moves[response.session.moves.Length - 1];
                        ApplyServerMove(botMoveStr);
                    }
                }
                else
                {
                    UpdateStatusText("Ошибка связи с сервером.");
                    responseReceived = true; // Прекращаем попытки при ошибке сети
                }
            }
        }

        isWaitingForBot = false;
        UpdateUIState();
    }


    #endregion

    #region Helpers

    private void RestartLocalGame()
    {
        if (controller != null)
        {
            controller.ResetGame();
            controller.Game.OnMoveExecuted += HandleOnMoveExecuted;

            if (controller.spawner != null)
            {
                controller.spawner.Game = controller.Game;
                controller.spawner.SpawnAll();
            }
        }
    }

    private void ApplyServerMove(string moveStr)
    {
        Move move = ConvertStringToMove(moveStr);
        var pieceAtStart = controller.Game.Board.GetPiece(move.From.X, move.From.Y);

        // Если фигура черная и ход валидный — выполняем
        if (!pieceAtStart.IsEmpty && pieceAtStart.Color == PieceColor.Black)
        {
            bool moveSucceeded = controller.Game.TryMove(move.To.X, move.To.Y);

            if (moveSucceeded)
            {
                // Увеличиваем счетчик ТОЛЬКО при успешном ходе
                localMovesCount++;
                UpdateStatusText($"Бот походил: {moveStr}");
            }
            else
            {
                // Ход бота невалиден — всё равно увеличиваем счетчик,
                // чтобы не зациклиться в опросе
                localMovesCount++;
                UpdateStatusText("Бот совершил неверный ход.");
            }
        }
        else
        {
            // Нет фигуры для хода — пропускаем
            localMovesCount++;
            UpdateStatusText("Бот попытался походить пустой клеткой.");
        }

        if (controller.spawner != null) controller.spawner.SpawnAll();
        if (ChessInput.Instance != null) ChessInput.Instance.ClearHighlights();
    }

    private void UpdateUIState()
    {
        if (newGameButton != null)
            newGameButton.interactable = !isWaitingForBot;

        if (botMoveButton != null)
        {
            // Кнопка активна, если:
            // 1. Сессия создана
            // 2. Бот сейчас не "думает"
            // 3. Сейчас ХОД ЧЕРНЫХ (бота)
            bool isBlackTurn = controller.Game != null && controller.Game.CurrentTurn == PieceColor.Black;
            botMoveButton.interactable = currentSessionId.HasValue && !isWaitingForBot && isBlackTurn;
        }
    }

    private void UpdateStatusText(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private string ConvertMoveToString(Move move)
    {
        return $"{(char)('a' + move.From.X)}{(char)('1' + move.From.Y)}-{(char)('a' + move.To.X)}{(char)('1' + move.To.Y)}";
    }

    private Move ConvertStringToMove(string moveStr)
    {
        string[] p = moveStr.Split('-');
        return new Move(new BoardPosition(p[0][0] - 'a', p[0][1] - '1'), new BoardPosition(p[1][0] - 'a', p[1][1] - '1'));
    }

    #endregion

    #region JSON
    [Serializable] public class SessionData { public int id; public string status; public string[] moves; }
    [Serializable] public class SessionResponse { public string message; public SessionData session; }
    [Serializable] public class PutMoveRequest { public string move; }
    #endregion
}