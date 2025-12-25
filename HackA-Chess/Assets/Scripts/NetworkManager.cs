using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        NetworkClient.Instance.OnLine += HandleLine;
    }

    void OnDestroy()
    {
        NetworkClient.Instance.OnLine -= HandleLine;
    }

    void HandleLine(string line)
    {
        if (line.StartsWith("CHAT|")) { 

        }
    }
}
