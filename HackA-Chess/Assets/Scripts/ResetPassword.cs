using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResetPassword : MonoBehaviour
{
    private string serverIP = "127.0.0.1";// chỉnh sửa IP server nha, để người chơi nhập
    private int Port = 8080;
    [Header("Inputs")]
    [SerializeField] private TMP_InputField InputEmail;
    [SerializeField] private TMP_InputField InputOtp;
    [SerializeField] private TMP_InputField InputNewPassword;

    [Header("Buttons")]
    [SerializeField] private Button btnSendOtp;
    [SerializeField] private Button btnConfirm;

    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text otptext;
    [SerializeField] private TMP_Text passtext;

    private async Task Start()
    {
        InputOtp.gameObject.SetActive(false);
        InputNewPassword.gameObject.SetActive(false);
        btnConfirm.gameObject.SetActive(false); 
        otptext.gameObject.SetActive(false);
        passtext.gameObject.SetActive(false);
    }
    private void Awake()
    {
        if (btnSendOtp) btnSendOtp.onClick.AddListener(() => _ = OnClickSendOtp());
        if (btnConfirm) btnConfirm.onClick.AddListener(() => _ = OnClickConfirm());
    }

    private async Task OnClickSendOtp()
    {
        string email = InputEmail.text.Trim();
        await NetworkClient.Instance.ConnectAsync(serverIP, Port);
        if (string.IsNullOrEmpty(email))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Bạn phải nhập email trước đã");
            return;
        }
        btnSendOtp.interactable = false;

        //chờ trước để không bị miss
        var wait = NetworkClient.Instance.WaitForPrefixAsync("RESET_REQ|", 5000);
        await NetworkClient.Instance.SendAsync($"RESET_REQ|{email}");
        string resp = await wait;
        Debug.LogWarning(resp);
        btnSendOtp.interactable = true;

        if (string.IsNullOrEmpty(resp))
        {
            Debug.LogWarning("Mất kết nối server");
            return;
        }
        var p = resp.Trim().Split('|');
        if (p.Length >= 2 && p[1] == "OK")
        {
            status.text="Đã gửi OTP. Kiểm tra email và nhập OTP trong 60 giây.";
            btnConfirm.gameObject.SetActive(true);
            btnSendOtp.gameObject.SetActive(false);
            otptext.gameObject.SetActive(true);
            passtext.gameObject.SetActive(true);
            InputOtp.gameObject.SetActive(true);
            InputNewPassword.gameObject.SetActive(true);
        }
        else
        {
            string reason = (p.Length >= 3) ? p[2] : "UNKNOWN";
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI","Gửi OTP thất bại: " + reason);
        }
        NetworkClient.Instance.Disconnect();
    }

    private async Task OnClickConfirm()
    {
        string email = InputEmail.text.Trim();
        string otp = InputOtp.text.Trim();
        string newPass = InputNewPassword.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(newPass))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Nhập đủ Email / OTP / Mật khẩu mới.");
            return;
        }
        btnConfirm.interactable = false;

        try
        {
            await NetworkClient.Instance.ConnectAsync(serverIP, Port);

            string newPassHash = Sha256Hex(newPass);

            var wait = NetworkClient.Instance.WaitForPrefixAsync("RESET_VERIFY|", 5000);
            await NetworkClient.Instance.SendAsync($"RESET_VERIFY|{email}|{otp}|{newPassHash}\n"); 

            string resp = await wait;
            Debug.Log("RESET_VERIFY resp = " + resp);

            if (string.IsNullOrEmpty(resp))
            {
                MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "RESET_VERIFY timeout");
                return;
            }

            var p = resp.Trim().Split('|');
            if (p.Length >= 2 && p[1] == "OK")
            {
                MessageBoxManager.Instance.ShowMessageBox("THÔNG BÁO", "Đổi mật khẩu thành công! Bạn có thể đăng nhập lại.");
                SceneManager.LoadScene("Login");
            }
            else
            {
                string reason = (p.Length >= 3) ? p[2] : "UNKNOWN";
                MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Xác thực thất bại: " + reason);
            }
        }
        finally
        {
            btnConfirm.interactable = true;
            NetworkClient.Instance.Disconnect(); 
        }
    }

    // Hash password (OTP thì bạn không hash, nhưng password hash là nên)
    private static string Sha256Hex(string s)
    {
        using var sha = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        byte[] hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
