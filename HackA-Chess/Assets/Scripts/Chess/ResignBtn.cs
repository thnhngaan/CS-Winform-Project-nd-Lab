using UnityEngine;
using Assets.Scripts;


public class ResignBtn : MonoBehaviour
{
    public void OnResignClick()
    {
        if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected)
            return;

        string roomId = GameSession.RoomId;
        if (string.IsNullOrEmpty(roomId))
            return;

        _ = NetworkClient.Instance.SendAsync($"RESIGN|{roomId}\n");
    }
}
