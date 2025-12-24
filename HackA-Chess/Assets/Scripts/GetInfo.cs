using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public static class UserSession
    {
        public static string CurrentUsername;
        public static string avatar;
        public static int elopoint;
        public static string fullname;
        public static int totalwin;
        public static int totaldraw;
        public static int totalloss;

    }
}
public class GetInfo : MonoBehaviour
{
    public Button avatarButton;      // Button chứa hình avatar
    public Sprite defaultAvatar;
    public TMP_Text fullname;
    public TMP_Text point;
    public TMP_Text win;
    public TMP_Text draw;
    public TMP_Text loss;
    public TMP_Text elopoint;
    public TMP_Text name;

    private async void Start()
    {
        string currentUsername = UserSession.CurrentUsername;
        

        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.LogError("GetInfo: CurrentUsername trống (chưa login hoặc chưa gán UserSession).");
            return;
        }
        //Giả sử NetworkClient đã ConnectAsync ở scene trước
        string msg = $"GETINFO|{currentUsername}";
        await NetworkClient.Instance.SendAsync(msg);
        Debug.Log("Đã gửi: " + msg);

        string resp = await NetworkClient.Instance.WaitForPrefixAsync("GETINFO|",5000);

        if (string.IsNullOrEmpty(resp))
        {
            Debug.LogError("GetInfo: Không nhận được phản hồi từ server (resp null hoặc rỗng).");
            return;
        }

        Debug.Log("Server trả: " + resp);

        //format: GETINFO|fullName|elo|totalWin|totalDraw|totalLoss|avatarPath
        string[] parts = resp.Split('|');
        if (parts.Length < 8 || parts[0] != "GETINFO")
        {
            Debug.LogError($"GetInfo: Gói tin sai format. parts.Length = {parts.Length}, parts[0] = {parts[0]}");
            return;
        }
        string usernamestr = parts[1];
        string fullNameStr = parts[2];
        int elo = int.Parse(parts[3]);
        int totalWin = int.Parse(parts[4]);
        int totalDraw = int.Parse(parts[5]);
        int totalLoss = int.Parse(parts[6]);
        string avatarPath = parts[7]; 

        UserSession.avatar= avatarPath;
        UserSession.elopoint = elo;
        UserSession.totalwin = totalWin;
        UserSession.totaldraw = totalDraw;
        UserSession.totalloss = totalLoss;
        UserSession.fullname = fullNameStr;
        //set avt cho button, nếu có thì sẽ dùng avatarPath, nếu không có thì sẽ dùng default avatar
        Sprite finalAvatar = defaultAvatar;

        if (!string.IsNullOrEmpty(avatarPath))
        {
            Sprite loaded = Resources.Load<Sprite>(avatarPath);
            if (loaded != null)
            {
                finalAvatar = loaded;
            }
            else
            {
                Debug.LogWarning("GetInfo: Không load được avatar từ path: " + avatarPath);
            }
        }

        if (avatarButton != null && finalAvatar != null)
        {
            avatarButton.image.sprite = finalAvatar;
        }

        if (fullname != null) fullname.text = fullNameStr;
        if (name != null) name.text = fullNameStr;

        if (point != null) point.text = elo.ToString();
        if (elopoint != null) elopoint.text = elo.ToString();

        if (win != null) win.text = totalWin.ToString();
        if (draw != null) draw.text = totalDraw.ToString();
        if (loss != null) loss.text = totalLoss.ToString();
    }
}
