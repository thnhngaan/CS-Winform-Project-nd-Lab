using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;

public class FriendCardUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text fullNameText;
    [SerializeField] private TMP_Text eloText;
    [SerializeField] private TMP_Text wldText;
    [SerializeField] private Image statusDot;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button btnRemove;
    private string _usernameKey;

    public string UsernameKey => _usernameKey;

    private static string Key(string s) => (s ?? "").Trim().ToLowerInvariant();

    public void Bind(FriendManager.FriendCardData data, Sprite avatar, Sprite onlineDot = null, Sprite offlineDot = null)
    {
        _usernameKey = Key(data.username);

        if (fullNameText) fullNameText.text = string.IsNullOrWhiteSpace(data.fullName) ? data.username : data.fullName;
        if (eloText) eloText.text = $"ELO: {data.elo}";
        if (wldText) wldText.text = $"W:{data.win}  D:{data.draw}  L:{data.loss}";
        if (btnRemove)
        {
            btnRemove.onClick.RemoveAllListeners();
            btnRemove.onClick.AddListener(() => _ = OnRemove());
        }
        if (avatarImage) avatarImage.sprite = avatar;

        SetOnline(data.online, onlineDot, offlineDot);
    }

    public void SetOnline(bool online, Sprite onlineDot = null, Sprite offlineDot = null)
    {
        if (!statusDot) return;

        statusDot.gameObject.SetActive(true);

        // Nếu bạn có 2 sprite (xanh/đỏ) thì dùng sprite, khỏi set color
        if (onlineDot != null && offlineDot != null)
        {
            statusDot.sprite = online ? onlineDot : offlineDot;
            statusDot.color = Color.white;
        }
        else
        {
            statusDot.color = online ? Color.green : Color.gray;
        }
        if (statusText) statusText.text = online ? "ONLINE" : "OFFLINE";
    }
    private async Task OnRemove()
    {
        string msg = await FriendManager.Instance.RemoveFriendAsync(_usernameKey);
        Debug.Log("[REMOVE] " + msg);
    }
}
