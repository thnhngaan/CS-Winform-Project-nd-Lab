using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class AvatarPickerUI : MonoBehaviour
{
    [SerializeField] private Button btnUpdate;
    [SerializeField] private Button avatarButton;
    [SerializeField] private Image avt;
    private string SelectedAvatarKey = "";
    private void Awake()
    {
        btnUpdate.onClick.AddListener(() => _ = OnClickUpdate());
        btnUpdate.interactable = false;
    }
    public void SelectAvatar(string avatarKey)
    {
        SelectedAvatarKey = (avatarKey ?? "").Trim();
        btnUpdate.interactable = !string.IsNullOrWhiteSpace(SelectedAvatarKey);
    }

    private async Task OnClickUpdate()
    {
        if (string.IsNullOrWhiteSpace(SelectedAvatarKey)) return;

        string username = UserSession.CurrentUsername;
        string updateMsg = $"UPDATEAVATAR|{username}|{SelectedAvatarKey}";
        string updateResp = await SendMessageAsync(updateMsg, "UPDATEAVATAR|", 5000);

        if (string.IsNullOrEmpty(updateResp))
        {
            Debug.LogWarning("Update avatar timeout");
            return;
        }

        var updateParts = updateResp.Split('|');
        if (updateParts.Length < 2 || updateParts[1] != "SUCCESS")
        {
            Debug.LogWarning("Update avatar FAIL: " + updateResp);
            return;
        }
        var waitGetInfo = NetworkClient.Instance.WaitForPrefixAsync("GETINFO|", 5000);
        await NetworkClient.Instance.SendAsync($"GETINFO|{username}");
        string getInfoResp = await waitGetInfo;

        if (string.IsNullOrEmpty(getInfoResp))
        {
            Debug.LogError("GetInfo: Không nhận được phản hồi từ server.");
            return;
        }
        MessageBoxManager.Instance.ShowMessageBox("THÔNG BÁO", "Bạn đã thay đổi avatar thành công!");
        Debug.Log("Server trả GETINFO: " + getInfoResp);

        var parts1 = getInfoResp.Split('|');
        if (parts1.Length < 2 || parts1[0] != "GETINFO")
        {
            Debug.LogError("GetInfo: Gói tin sai format: " + getInfoResp);
            return;
        }

        string avatarPath = parts1[parts1.Length - 1].Trim();
        UserSession.avatar = avatarPath;

        var loaded = Resources.Load<Sprite>(avatarPath);
        if (loaded == null)
        {
            Debug.LogWarning("GetInfo: Không load được avatar từ path: " + avatarPath);
            return;
        }

        if (avatarButton != null) avatarButton.image.sprite = loaded;
        if (avt != null) avt.sprite = loaded;
        
    }

    private async Task<string> SendMessageAsync(string message, string waitPrefix, int timeoutMs)
    {
        var waitTask = NetworkClient.Instance.WaitForPrefixAsync(waitPrefix, timeoutMs);
        await NetworkClient.Instance.SendAsync(message);
        return await waitTask;
    }
}
