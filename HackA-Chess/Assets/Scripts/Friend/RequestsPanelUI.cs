using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class RequestsPanelUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private InviteItemUI itemPrefab;

    private FriendManager _fm;

    private void OnEnable()
    {
        StartCoroutine(SetupNextFrame());
    }

    private void OnDisable()
    {
        if (_fm != null)
            _fm.IncomingInvitesChanged -= Rebuild;
    }

    private IEnumerator SetupNextFrame()
    {
        // Nếu panel vừa bật, đợi 1 frame để các Awake() khác chạy xong
        yield return null;

        // Check Inspector refs
        if (content == null || itemPrefab == null)
        {
            Debug.LogError("[RequestsPanelUI] Missing references: content or itemPrefab is NULL. Drag them in Inspector.");
            yield break;
        }

        // Lấy FriendManager (ưu tiên Instance, fallback FindObjectOfType)
        _fm = FriendManager.Instance != null
            ? FriendManager.Instance
            : FindObjectOfType<FriendManager>(true);

        if (_fm == null)
        {
            Debug.LogError("[RequestsPanelUI] FriendManager not found in scene.");
            yield break;
        }

        // Subscribe event (nhớ unsubscribe ở OnDisable)
        _fm.IncomingInvitesChanged -= Rebuild;
        _fm.IncomingInvitesChanged += Rebuild;

        // Refresh inbox từ server (nếu đã connect)
        _ = SafeRefreshInbox();

        // Build UI lần đầu (dù chưa refresh xong vẫn hiện snapshot hiện có)
        Rebuild();
    }

    private async Task SafeRefreshInbox()
    {
        if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected)
        {
            Debug.LogWarning("[RequestsPanelUI] Not connected, skip FRIEND_INBOX refresh.");
            return;
        }

        await _fm.RefreshIncomingInvitesAsync();
        // Khi refresh xong, IncomingInvitesChanged sẽ bắn và gọi Rebuild() rồi.
    }

    private void Rebuild()
    {
        if (content == null || itemPrefab == null || _fm == null) return;

        foreach (Transform c in content) Destroy(c.gameObject);

        var invites = _fm.GetIncomingInvitesSnapshot();
        foreach (var from in invites)
        {
            var item = Instantiate(itemPrefab, content, false);
            item.Bind(from);
        }
    }
}
