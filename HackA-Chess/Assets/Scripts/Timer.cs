using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public float timeRemaining = 300f; // 5 phút
    public TMP_Text timerText;
    public bool isRunning = true;

    void Update()
    {
        if (!isRunning) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateDisplay();
        }
        else
        {
            timeRemaining = 0;
            isRunning = false;
            OnTimeUp();
        }
    }

    void UpdateDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void OnTimeUp()
    {
        Debug.Log("Time up!");
        // gọi GameOver hoặc xử lý thua
    }
}
