using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Sockets;
using UnityEngine.SceneManagement;

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
            ShowMessage("Vui lòng nhập đầy đủ thông tin.");
            return;
        }

        // Kiểm tra định dạng
        if (!CheckFullName(fullname))
        {
            ShowMessage("Họ tên sai định dạng (ít nhất 2 từ).");
            return;
        }
        if (!CheckUsername(username))
        {
            ShowMessage("Tên tài khoản 6–20 ký tự, chỉ chữ và số.");
            return;
        }
        if (!CheckEmail(email))
        {
            ShowMessage("Email phải là Gmail hợp lệ.");
            return;
        }
        if (!CheckPhone(phone))
        {
            ShowMessage("Số điện thoại phải có 10 số, bắt đầu bằng 0.");
            return;
        }
        if (!CheckPasswordStrong(password))
        {
            ShowMessage("Mật khẩu yếu. Cần 1 chữ hoa, 1 chữ thường, 1 số (6–32 ký tự).");
            return;
        }
        if (password != confirm)
        {
            ShowMessage("Mật khẩu xác nhận không khớp.");
            return;
        }

        ShowMessage("Dữ liệu hợp lệ. Đang gửi yêu cầu...");

        string hashedPassword = HashToSHA256(password);

        try
        {
            string result = await SendRegisterRequestAsync(username, hashedPassword, email, fullname, phone);

            if (result == "Register success")
            {
                ShowMessage("Tạo tài khoản thành công.");
                await Task.Delay(1000);
                SceneManager.LoadScene("Login");
            }
            else
            {
                ShowMessage("Đăng ký thất bại. Tài khoản có thể đã tồn tại.");
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

    // Gửi dữ liệu TCP
    private async Task<string> SendRegisterRequestAsync(string username, string hashedPassword, string email, string fullname, string sdt)
    {
        const string serverIp = "127.0.0.1";
        const int serverPort = 8080;

        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(serverIp, serverPort);
            using (var stream = client.GetStream())
            {
                string request = $"REGISTER|{username}|{hashedPassword}|{email}|{fullname}|{sdt}";
                byte[] data = Encoding.UTF8.GetBytes(request);
                await stream.WriteAsync(data, 0, data.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            }
        }
    }
}
