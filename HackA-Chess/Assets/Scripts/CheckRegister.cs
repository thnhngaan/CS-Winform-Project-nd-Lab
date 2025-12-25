using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RegisterUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmInput;
    public TMP_InputField emailInput;
    public TMP_InputField fullnameInput;
    public TMP_InputField phoneInput;

    [Header("Buttons")]
    public Button registerButton;
    public Button eyePassword;
    public Button eyeConfirm;

    [Header("Message Text")]
    public TMP_Text messageText;

    private void Start()
    {
        // Gán sự kiện nút
        registerButton.onClick.AddListener(OnRegisterClicked);
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        confirmInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();
        eyePassword.onClick.AddListener(() => TogglePassword(passwordInput));
        eyeConfirm.onClick.AddListener(() => TogglePassword(confirmInput));
    }
    //hiện, ẩn pass
    private void TogglePassword(TMP_InputField field)
    {
        if (field == null) return;

        if (field.contentType == TMP_InputField.ContentType.Password) //ẩn => hiện
        {
            field.contentType = TMP_InputField.ContentType.Standard;
            field.inputType = TMP_InputField.InputType.Standard;
        }
        else //hiện => ẩn
        {
            field.contentType = TMP_InputField.ContentType.Password;
            field.inputType = TMP_InputField.InputType.Password;
        }
        string temp = field.text;
        field.text = "";
        field.text = temp;
        field.ForceLabelUpdate();
    }

    // Nút Đăng ký
    private async void OnRegisterClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();
        string confirm = confirmInput.text.Trim();
        string email = emailInput.text.Trim();
        string fullname = fullnameInput.text.Trim();
        string phone = phoneInput.text.Trim();

        // Kiểm tra rỗng
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
           string.IsNullOrEmpty(confirm) || string.IsNullOrEmpty(email) ||
           string.IsNullOrEmpty(fullname) || string.IsNullOrEmpty(phone))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Vui lòng nhập đầy đủ thông tin.");
            return;
        }

        // Kiểm tra định dạng
        if (!CheckFullName(fullname))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Họ tên sai định dạng (ít nhất 2 từ).");
            return;
        }
        if (!CheckUsername(username))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Tên tài khoản 6–20 ký tự, chỉ chữ và số.");
            return;
        }
        if (!CheckEmail(email))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Email phải là Gmail hợp lệ.");
            return;
        }
        if (!CheckPhone(phone))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Số điện thoại phải có 10 số, bắt đầu bằng 0.");
            return;
        }
        if (!CheckPasswordStrong(password))
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Mật khẩu yếu. Cần 1 chữ hoa, 1 chữ thường, 1 số (6–32 ký tự).");
            return;
        }
        if (password != confirm)
        {
            MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Mật khẩu xác nhận không khớp.");
            return;
        }

        ShowMessage("Dữ liệu hợp lệ. Đang gửi yêu cầu...");

        string hashedPassword = HashToSHA256(password);

        try
        {
            string result = await SendRegisterRequestAsync(username, hashedPassword, email, fullname, phone);

            if (result == "REGISTER|SUCCESS")
            {
                MessageBoxManager.Instance.ShowMessageBox("THÔNG BÁO", "Tạo tài khoản thành công");
                await Task.Delay(1000);
                SceneManager.LoadScene("Login");
            }
            else
            {
                MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Đăng ký thất bại. Tài khoản có thể đã tồn tại.");
            }
        }
        catch
        {
            ShowMessage("Không thể kết nối đến server.");
        }
    }

    // Hiển thị thông báo
    private void ShowMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
        else
            Debug.Log(msg);
    }

    #region Kiểm tra dữ liệu
    bool CheckFullName(string name) =>
        Regex.IsMatch(name, @"^[A-Za-zÀ-ỹ]+(\s[A-Za-zÀ-ỹ]+)+$");
    bool CheckUsername(string user) =>
        Regex.IsMatch(user, @"^[a-zA-Z0-9]{6,20}$");
    bool CheckEmail(string email) =>
        Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@gmail\.com(\.vn)?$") || Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@yahoo\.com(\.vn)?$");
    bool CheckPhone(string sdt) =>
        Regex.IsMatch(sdt, @"^0\d{9}$");
    bool CheckPasswordStrong(string pass) =>
        Regex.IsMatch(pass, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,32}$");
    #endregion

    // Hàm hash SHA256
    private string HashToSHA256(string input)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
    const string IPServer = "127.0.0.1";
    const int PortServer = 8080;
    // Gửi dữ liệu TCP
    private async Task<string> SendRegisterRequestAsync(string username, string hashedPassword, string email, string fullname, string sdt)
    {
        string message = $"REGISTER|{username}|{hashedPassword}|{email}|{fullname}|{sdt}";
        try
        {
            bool ok = await NetworkClient.Instance.ConnectAsync(IPServer, PortServer);
            if (!ok)
                return "Lỗi: Không kết nối được tới server.";
            await NetworkClient.Instance.SendAsync(message);
            string response = await NetworkClient.Instance.WaitForPrefixAsync("REGISTER|", 5000);
            if (response == null)
                return "Lỗi: Không nhận được dữ liệu từ server.";
            return response;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TCP Error (IP {IPServer}) (Port {PortServer}): {ex.Message}");
            return $"Lỗi: {ex.Message}";
        }
    }
}
