using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class AddFriendUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private Button btnAddFriend;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        if (btnAddFriend != null)
            btnAddFriend.onClick.AddListener(() => _ = OnClickAdd());
    }

    private async Task OnClickAdd()
    {
        if (statusText) statusText.text = "";

        string target = inputUsername != null ? inputUsername.text : "";
        string msg = await FriendManager.Instance.SendFriendInviteAsync(target);

        if (statusText) statusText.text = msg;
        if (inputUsername) inputUsername.text = "";
    }
}