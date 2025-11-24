using Unity.VisualScripting;
using UnityEngine;
using TMPro;
public class Timer : MonoBehaviour // hàm timer
{
    public TextMeshProUGUI whiteTimerText; 
    public TextMeshProUGUI blackTimerText;
    public float whiteTime = 900f;
    public float blackTime = 900f;
    public Game game;

    //Đếm ngược thời gian
    void Update()
    {
        if (game == null) return;
        if (game.IsGameOver()) return;

        string currentPlayer = game.GetCurrentPlayer();// tìm current user

        if (currentPlayer == "white")
        {
            whiteTime -= Time.deltaTime;
            if (whiteTime <= 0f)
            {
                whiteTime = 0f;// đếm ngược quân trắng về 0 thì quân đen thắng
                UpdateUI();
                if (!game.IsGameOver())
                {
                    game.Winner("black");
                }
                return;
            }
        }
        else if (currentPlayer == "black")
        {
            blackTime -= Time.deltaTime;
            if (blackTime <= 0f)
            {
                blackTime = 0f;// đếm ngược quân đen về 0 thì quân trắng thắng
                UpdateUI();
                if (!game.IsGameOver())
                {
                    game.Winner("white");
                }
                return;
            }
        }

        UpdateUI();
    }

    // Cập nhật timer
    void UpdateUI()
    {
        if (whiteTimerText != null)
            whiteTimerText.text = FormatTime(whiteTime);
        if (blackTimerText != null)
            blackTimerText.text = FormatTime(blackTime);
    }

    // Hàm tính thời gian
    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60);
        int seconds = Mathf.FloorToInt(t % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
