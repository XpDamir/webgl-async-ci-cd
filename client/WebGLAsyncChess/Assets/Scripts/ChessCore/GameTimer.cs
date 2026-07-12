using System;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Настройки")]
    [SerializeField] private float totalTime = 900f;

    private float remainingTime;
    private bool isRunning = false;

    public float RemainingTime => remainingTime;
    public bool IsRunning => isRunning;
    public event Action OnTimeUp;

    public void StartTimer()
    {
        remainingTime = totalTime;
        isRunning = true;
        UpdateDisplay();
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public float GetElapsedTime()
    {
        return totalTime - remainingTime;
    }

    private void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0)
        {
            remainingTime = 0;
            isRunning = false;
            OnTimeUp?.Invoke();
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timerText == null) return;
        int mins = Mathf.FloorToInt(remainingTime / 60);
        int secs = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{mins:D2}:{secs:D2}";
    }
}