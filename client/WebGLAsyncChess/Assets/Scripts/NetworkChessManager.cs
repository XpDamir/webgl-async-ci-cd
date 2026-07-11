using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ChessCore;
using System.Reflection;

public class NetworkChessManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string serverUrl = "https://webgl-async-ci-cd-production.up.railway.app";

    [Header("UI Roots")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameUIPanel;

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
    }

    private void Start()
    {
        // Начальное состояние интерфейса
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        if (newGameButton != null) newGameButton.onClick.AddListener(CreateNewGame);
        if (botMoveButton != null) botMoveButton.onClick.AddListener(RequestBotMove);

        UpdateUIState();
    }

    private void OnDestroy()
    {
        if (controller != null && controller.Game != null)
        {
            controller.Game.OnMoveExecuted -= HandleOnMoveExecuted;
        }
    }

    #region Game Initialization

    public void CreateNewGame()
    {
        StartCoroutine(CreateSessionCoroutine());
    }

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

                if (menuPanel != null) menuPanel.SetActive(false);
                if (gameUIPanel != null) gameUIPanel.SetActive(true);

                UpdateStatusText($"Игра началась! ID: {currentSessionId}");
            }
            else
            {
                UpdateStatusText($"Ошибка подключения: {request.error}");
            }
        }
        UpdateUIState();
    }

    private void RestartLocalGame()
    {
        if (controller != null)
        {
            if (controller.Game != null)
                controller.Game.OnMoveExecuted -= HandleOnMoveExecuted;

            // Создаем новый GameState
            GameState newGame = new GameState();

            // Записываем его в контроллер через рефлексию (так как сеттер приватный)
            var field = typeof(ChessGameController2D).GetField("<Game>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) field.SetValue(controller, newGame);

            newGame.OnMoveExecuted += HandleOnMoveExecuted;
            if (controller.spawner != null) controller.spawner.Game = newGame;
        }
    }

    #endregion

    #region Player Move Handling

    private void HandleOnMoveExecuted(Move move)
    {
        // ВНИМАНИЕ: localMovesCount НЕ увеличиваем здесь. 
        // Ждем подтверждения от сервера в SendMoveCoroutine.
        if (currentSessionId.HasValue)
        {
            string moveString = ConvertMoveToString(move);
            StartCoroutine(SendMoveCoroutine(currentSessionId.Value, moveString));
        }
    }

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

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Сервер подтвердил ход, теперь синхронизируем счетчик
                localMovesCount++;
                Debug.Log($"[NetworkChess] Ход {moveStr} подтвержден сервером. localMovesCount: {localMovesCount}");
                UpdateUIState();
            }
            else
            {
                UpdateStatusText("Ошибка синхронизации хода.");
            }
        }
    }

    #endregion

    #region Bot Move Handling

    public void RequestBotMove()
    {
        if (currentSessionId.HasValue && !isWaitingForBot)
        {
            StartCoroutine(RequestBotMoveCoroutine(currentSessionId.Value));
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
            {
                StartCoroutine(PollSessionStateCoroutine(sessionId));
            }
            else
            {
                UpdateStatusText("Бот недоступен.");
                isWaitingForBot = false;
                UpdateUIState();
            }
        }
    }

    private IEnumerator PollSessionStateCoroutine(int sessionId)
    {
        string url = $"{serverUrl}/api/sessions/{sessionId}";
        bool moveFound = false;

        while (!moveFound)
        {
            yield return new WaitForSeconds(2f); // Опрос раз в 2 секунды

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);

                    // Если на сервере ходов больше, чем у нас локально - бот ответил
                    if (response.session.moves != null && response.session.moves.Length > localMovesCount)
                    {
                        moveFound = true;
                        string botMoveStr = response.session.moves[response.session.moves.Length - 1];
                        ApplyServerMove(botMoveStr);
                    }
                }
                else
                {
                    UpdateStatusText("Потеря связи...");
                    break;
                }
            }
        }

        isWaitingForBot = false;
        UpdateUIState();
    }

    private void ApplyServerMove(string moveStr)
    {
        if (string.IsNullOrEmpty(moveStr) || moveStr == "null")
        {
            UpdateStatusText("Бот пропустил ход.");
            localMovesCount++;
            return;
        }

        Move move = ConvertStringToMove(moveStr);
        var pieceAtStart = controller.Game.Board.GetPiece(move.From.X, move.From.Y);

        // Бот должен ходить только черными
        if (!pieceAtStart.IsEmpty && pieceAtStart.Color == PieceColor.Black)
        {
            // КРИТИЧЕСКИЙ ШАГ: Программный выбор фигуры перед ходом
            controller.Game.SelectPiece(move.From.X, move.From.Y);

            // Выполняем ход
            bool moveSucceeded = controller.Game.TryMove(move.To.X, move.To.Y);

            if (moveSucceeded)
            {
                localMovesCount++; // Ход выполнен успешно
                UpdateStatusText($"Бот походил: {moveStr}");
            }
            else
            {
                localMovesCount++; // Ход невозможен по правилам, но мы его "пропускаем"
                UpdateStatusText("Бот сделал невозможный ход.");
            }
        }
        else
        {
            localMovesCount++; // Пропускаем некорректный ход из базы
            UpdateStatusText("Бот ошибся (не черная фигура).");
        }

        // Обновляем визуал
        if (controller.spawner != null) controller.spawner.SpawnAll();
        if (ChessInput.Instance != null) ChessInput.Instance.ClearHighlights();
    }

    #endregion

    #region Helpers & Data Structures

    private void UpdateUIState()
    {
        if (newGameButton != null)
            newGameButton.interactable = !isWaitingForBot;

        if (botMoveButton != null)
        {
            // Кнопка активна только в ход черных
            bool isBlackTurn = controller.Game != null && controller.Game.CurrentTurn == PieceColor.Black;
            botMoveButton.interactable = currentSessionId.HasValue && !isWaitingForBot && isBlackTurn;
        }
    }

    private void UpdateStatusText(string text)
    {
        if (statusText != null) statusText.text = text;
        Debug.Log($"[NetworkChess] {text}");
    }

    private string ConvertMoveToString(Move move)
    {
        return $"{(char)('a' + move.From.X)}{(char)('1' + move.From.Y)}-{(char)('a' + move.To.X)}{(char)('1' + move.To.Y)}";
    }

    private Move ConvertStringToMove(string moveStr)
    {
        string[] p = moveStr.Split('-');
        if (p.Length != 2 || p[0].Length < 2 || p[1].Length < 2)
        {
            Debug.LogError($"Неверный формат хода: {moveStr}");
            return new Move(new BoardPosition(0, 0), new BoardPosition(0, 0));
        }

        int fromX = p[0][0] - 'a';
        int fromY = p[0][1] - '1';
        int toX = p[1][0] - 'a';
        int toY = p[1][1] - '1';

        return new Move(new BoardPosition(fromX, fromY), new BoardPosition(toX, toY));
    }

    [Serializable] public class SessionData { public int id; public string status; public string[] moves; }
    [Serializable] public class SessionResponse { public string message; public SessionData session; }
    [Serializable] public class PutMoveRequest { public string move; }
    #endregion
}