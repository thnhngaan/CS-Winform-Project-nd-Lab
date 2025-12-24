using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class InviteItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text fromText;
    [SerializeField] private Button btnAccept;
    [SerializeField] private Button btnDecline;

    private string _fromUser;

    public void Bind(string fromUser)
    {
        _fromUser = (fromUser ?? "").Trim().ToLowerInvariant();
        if (fromText) fromText.text = _fromUser + " đã gửi cho bạn 1 lời mời kết bạn!";

        if (btnAccept)
        {
            btnAccept.onClick.RemoveAllListeners();
            btnAccept.onClick.AddListener(() => _ = OnAccept());
        }

        if (btnDecline)
        {
            btnDecline.onClick.RemoveAllListeners();
            btnDecline.onClick.AddListener(() => _ = OnDecline());
        }
    }

    private async Task OnAccept()
    {
        SetInteractable(false);
        await FriendManager.Instance.RespondInviteAsync(_fromUser, true);
        Destroy(gameObject); // UI remove ngay
    }

    private async Task OnDecline()
    {
        SetInteractable(false);
        await FriendManager.Instance.RespondInviteAsync(_fromUser, false);
        Destroy(gameObject);
    }

    private void SetInteractable(bool on)
    {
        if (btnAccept) btnAccept.interactable = on;
        if (btnDecline) btnDecline.interactable = on;
    }
}
