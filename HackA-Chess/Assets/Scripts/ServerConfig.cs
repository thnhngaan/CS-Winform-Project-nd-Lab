using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class ServerConfig : MonoBehaviour
{
    [Header("UI Input")]
    public TMP_InputField ipInput;
    public TMP_InputField portInput;
    public TMP_Text statusText;
    public GameObject loadingIcon; // icon loading (optional)

    [Header("Next Scene")]
    public string NextScene = "Login";

    // IP & Port mặc định
    private const string DEFAULT_IP = "127.0.0.1";
    private const int DEFAULT_PORT = 8080;

    private bool isConnecting = false; // chặn bấm nhiều lần

    private void Start()
    {
        if (ipInput == null || portInput == null || statusText == null)
        {
            Debug.LogError("ServerConfig: Chưa gán UI reference trong Inspector!");
            return;
        }

        ipInput.text = PlayerPrefs.GetString("SERVER_IP", DEFAULT_IP);
        portInput.text = PlayerPrefs.GetInt("SERVER_PORT", DEFAULT_PORT).ToString();
        statusText.text = "";
    }


    // Event khi bấm nút CONNECT
    public async void OnConnectClicked()
    {
        if (isConnecting) return; // chặn spam click
        isConnecting = true;

        string ip = ipInput.text.Trim();
        string portStr = portInput.text.Trim();

        // Validate input
        if (string.IsNullOrEmpty(ip) || !int.TryParse(portStr, out int port))
        {
            ShowStatus("IP hoặc Port không hợp lệ!", Color.red);
            isConnecting = false;
            return;
        }

        ShowStatus("Đang kiểm tra kết nối tới server...", Color.yellow);
        SetLoading(true);

        // Test kết nối với timeout
        bool connected = await TestConnectionWithTimeout(ip, port, 3000);

        SetLoading(false);

        if (!connected)
        {
            ShowStatus("Không thể kết nối tới server!", Color.red);
            isConnecting = false;
            return;
        }

        // Kết nối OK → lưu cấu hình
        PlayerPrefs.SetString("SERVER_IP", ip);
        PlayerPrefs.SetInt("SERVER_PORT", port);
        PlayerPrefs.Save();

        // Ngắt kết nối test (Login sẽ connect lại)
        NetworkClient.Instance.Disconnect();

        ShowStatus("Kết nối thành công!", Color.green);

        await Task.Delay(500);
        SceneManager.LoadScene(NextScene);
    }

    // Test kết nối có timeout (ms)
    private async Task<bool> TestConnectionWithTimeout(string ip, int port, int timeoutMs)
    {
        Task<bool> connectTask = NetworkClient.Instance.ConnectAsync(ip, port);
        Task delayTask = Task.Delay(timeoutMs);

        Task finishedTask = await Task.WhenAny(connectTask, delayTask);

        if (finishedTask == delayTask)
        {
            Debug.LogWarning("Connect timeout");
            return false;
        }

        return await connectTask;
    }

    // Hiển thị trạng thái
    private void ShowStatus(string msg, Color color)
    {
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.color = color;
        }
        else
        {
            Debug.Log(msg);
        }
    }

    // Bật / tắt loading icon
    private void SetLoading(bool state)
    {
        if (loadingIcon != null)
            loadingIcon.SetActive(state);
    }
}
