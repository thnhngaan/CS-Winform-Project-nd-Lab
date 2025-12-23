using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text winnerText;
    public GameObject user1;
    public GameObject user2;
    public GameObject message;
    public TMP_Text chat;
    public TMP_Text UserName;
    public TMP_Text EnemyName;

    // Gọi hàm này khi hết game
    public void ShowGameOver(string winner)
    {
        winnerText.text = winner.ToUpper() + " WINS!";
        panel.SetActive(true);
        user1.SetActive(false);
        user2.SetActive(false);
        message.SetActive(false);
        chat.gameObject.SetActive(false);
        UserName.gameObject.SetActive(false);
        EnemyName.gameObject.SetActive(false);
    }

    // Nút Rematch
    public async void OnRematchClick()
    {
        string roomId = GameSession.RoomId;
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogWarning("[REMATCH] RoomId empty");
            return;
        }

        try
        {
            if (NetworkClient.Instance != null && NetworkClient.Instance.IsConnected)
            {
                await NetworkClient.Instance.SendAsync($"REMATCH|{roomId}\n"); // nhớ \n nếu protocol line-based
            }
            else
            {
                Debug.LogWarning("[REMATCH] Not connected");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[REMATCH] Send failed: " + ex.Message);
        }

        // ✅ LoadScene đúng cách
        SceneManager.LoadScene("WaitingRoom");
    }
    // Nút Quit
    public async void OnQuitButton()
    {
        string roomId = GameSession.RoomId;

        try
        {
            if (NetworkClient.Instance != null && NetworkClient.Instance.IsConnected && !string.IsNullOrEmpty(roomId))
                await NetworkClient.Instance.SendAsync($"OUTROOM|{roomId}\n");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[QUIT] OUTROOM send failed: " + ex.Message);
        }

        // reset local session để khỏi dính room cũ
        GameSession.RoomId = "";
        GameSession.OpponentName = "";

        SceneManager.LoadScene("MainMenu");
    }
}
