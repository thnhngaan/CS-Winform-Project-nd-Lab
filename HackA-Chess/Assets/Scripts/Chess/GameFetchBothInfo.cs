using UnityEngine;
using Assets.Scripts;
using System.Threading.Tasks;

public class GameFetchBothInfo : MonoBehaviour
{
    private async void Start()
    {
        await Fetch(UserSession.CurrentUsername, isMe: true);
        await Fetch(GameSession.OpponentName, isMe: false);

        GameNameUI.Refresh();
    }

    private async Task Fetch(string username, bool isMe)
    {
        if (string.IsNullOrEmpty(username)) return;

        await NetworkClient.Instance.SendAsync($"GETINFO|{username}");
        string resp = await NetworkClient.Instance.WaitForPrefixAsync("GETINFO|", 5000);
        if (string.IsNullOrEmpty(resp)) return;

        var parts = resp.Split('|');
        if (parts.Length < 7 || parts[0] != "GETINFO") return;

        string fullName = parts[1];

        if (isMe) GameSession.MyFullName = fullName;
        else GameSession.OpponentFullName = fullName;
    }
}
