using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ChessCore;

public class NetworkChessManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string serverUrl = "https://webgl-async-ci-cd-production.up.railway.app";

    [Header("UI Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("UI References")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button botMoveButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text resultText;

    [Header("Coordinates")]
    [SerializeField] private BoardCoordinates boardCoordinates;

    [Header("Таймер")]
    [SerializeField] private GameTimer gameTimer;

    [Header("Повтор")]
    [SerializeField] private ReplayController replayController;



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
        ShowMenuPanel();

        if (newGameButton != null) newGameButton.onClick.AddListener(CreateNewGame);
        if (botMoveButton != null) botMoveButton.onClick.AddListener(RequestBotMove);
        if (menuButton != null) menuButton.onClick.AddListener(ReturnToMenu);
        if (gameTimer != null) gameTimer.OnTimeUp += OnTimeUp;

        UpdateUIState();
    }

    private void OnDestroy()
    {
        if (controller != null && controller.Game != null)
        {
            controller.Game.OnMoveExecuted -= HandleOnMoveExecuted;
        }
    }

    #region Panel Management

    private void ShowMenuPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (boardCoordinates != null) boardCoordinates.Hide();
        if (gameTimer != null) gameTimer.StopTimer();
        if (replayController != null) replayController.Clear();
    }

    private void OnTimeUp()
    {
        UpdateStatusText("Время вышло! Поражение.");
        ShowResultPanel("black_win");
    }

    private void ShowGamePanel()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void ShowResultPanel(string result)
    {
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultText != null)
        {
            switch (result)
            {
                case "white_win":
                case "checkmate":
                    resultText.text = "Победа!";
                    resultText.color = Color.green;
                    break;
                case "black_win":
                    resultText.text = "Поражение!";
                    resultText.color = Color.red;
                    break;
                case "draw":
                    resultText.text = "Ничья!";
                    resultText.color = Color.yellow;
                    break;
                default:
                    resultText.text = "Игра завершена";
                    resultText.color = Color.white;
                    break;
            }
        }
    }

    public void ReturnToMenu()
    {
        currentSessionId = null;
        localMovesCount = 0;
        isWaitingForBot = false;
        if (boardCoordinates != null) boardCoordinates.Hide();
        if (gameTimer != null) gameTimer.StopTimer();
        if (replayController != null) replayController.Clear();
        ShowMenuPanel();
    }

    #endregion

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
                if (gameTimer != null) gameTimer.StartTimer();
                if (replayController != null) replayController.StartReplay();
                if (boardCoordinates != null)
                {
                    boardCoordinates.Generate();
                    boardCoordinates.Show();
                }
                ShowGamePanel();

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
            controller.ResetGame();
            controller.Game.OnMoveExecuted += HandleOnMoveExecuted;

            if (controller.spawner != null)
            {
                controller.spawner.Game = controller.Game;
                controller.spawner.SpawnAll();
            }
        }
    }

    #endregion

    #region Player Move Handling

    private void HandleOnMoveExecuted(Move move)
    {
        // Отправляем только ходы белых (игрока)
        var piece = controller.Game.Board.GetPiece(move.To.X, move.To.Y);
        if (piece.Color != PieceColor.White) return;

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
                localMovesCount++;
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
            yield return new WaitForSeconds(2f);

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);

                    if (response.session.moves != null && response.session.moves.Length > localMovesCount)
                    {
                        moveFound = true;
                        string botMoveStr = response.session.moves[response.session.moves.Length - 1];
                        ApplyServerMove(botMoveStr);

                        if (response.session.status == "completed")
                        {
                            if (gameTimer != null) gameTimer.StopTimer();
                            ShowResultPanel(response.session.result);
                            isWaitingForBot = false;
                            yield break;
                        }
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

        if (!pieceAtStart.IsEmpty && pieceAtStart.Color == PieceColor.Black)
        {
            controller.Game.SelectPiece(move.From.X, move.From.Y);
            bool moveSucceeded = controller.Game.TryMove(move.To.X, move.To.Y);

            if (moveSucceeded)
            {
                localMovesCount++;
                UpdateStatusText($"Бот походил: {moveStr}");
            }
            else
            {
                localMovesCount++;
                UpdateStatusText("Бот сделал невозможный ход.");
            }
        }
        else
        {
            localMovesCount++;
            UpdateStatusText("Бот ошибся (не черная фигура).");
        }

        if (controller.spawner != null) controller.spawner.SpawnAll();
        if (ChessInput.Instance != null) ChessInput.Instance.ClearHighlights();
    }

    #endregion

    #region Helpers

    private void UpdateUIState()
    {
        if (newGameButton != null)
            newGameButton.interactable = !isWaitingForBot;

        if (botMoveButton != null)
        {
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

    #endregion

    #region JSON

    [Serializable] public class SessionData { public int id; public string status; public string result; public string[] moves; }
    [Serializable] public class SessionResponse { public string message; public SessionData session; }
    [Serializable] public class PutMoveRequest { public string move; }

    #endregion
}