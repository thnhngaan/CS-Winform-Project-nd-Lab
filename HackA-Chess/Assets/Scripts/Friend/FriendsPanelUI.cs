using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FriendsPanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;      // Content có GridLayoutGroup
    [SerializeField] private FriendCardUI cardPrefab;

    [Header("Avatars")]
    [SerializeField] private Sprite defaultAvatar;
    [SerializeField] private string avatarResourcesFolder = "Avatars";

    [Header("Status Dots (optional)")]
    [SerializeField] private Sprite dotOnline;
    [SerializeField] private Sprite dotOffline;

    private readonly Dictionary<string, FriendCardUI> _cards = new();
    private readonly Dictionary<string, Sprite> _avatarCache = new();

    private static string Key(string s) => (s ?? "").Trim().ToLowerInvariant();

    private async void OnEnable()
    {
        StartCoroutine(SetupNextFrame());
    }
    private System.Collections.IEnumerator SetupNextFrame()
    {
        yield return null; // đợi 1 frame

        var fm = FriendManager.Instance ?? FindObjectOfType<FriendManager>(true);
        if (fm == null)
        {
            Debug.LogError("[FriendsPanelUI] FriendManager not found.");
            yield break;
        }

        fm.FriendsChanged += RebuildFromManager;
        fm.FriendStatusPushed += OnStatusPush;

        // refresh...
        _ = SafeRefresh();
    }
    private void OnDisable()
    {
        if (FriendManager.Instance != null)
        {
            FriendManager.Instance.FriendsChanged -= RebuildFromManager;
            FriendManager.Instance.FriendStatusPushed -= OnStatusPush;
        }
    }

    private async Task SafeRefresh()
    {
        if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected)
        {
            Debug.LogWarning("[FriendsPanelUI] Not connected, skip FetchFriendListDetail.");
            return;
        }

        await FriendManager.Instance.FetchFriendListDetailAsync();
    }

    private void RebuildFromManager()
    {
        if (content == null || FriendManager.Instance == null) return;

        var list = FriendManager.Instance.GetFriendsSnapshot();

        foreach (Transform c in content) Destroy(c.gameObject);
        _cards.Clear();

        foreach (var f in list)
        {
            var card = Instantiate(cardPrefab, content);
            _cards[Key(f.username)] = card;

            Sprite ava = ResolveAvatar(f.avatarKey);
            card.Bind(f, ava, dotOnline, dotOffline);
        }
    }

    private void OnStatusPush(string username, bool online)
    {
        if (_cards.TryGetValue(Key(username), out var card))
            card.SetOnline(online, dotOnline, dotOffline);
    }

    private Sprite ResolveAvatar(string avatarKey)
    {
        string k = Key(avatarKey);
        if (string.IsNullOrEmpty(k)) return defaultAvatar;

        if (_avatarCache.TryGetValue(k, out var sp))
            return sp;

        sp = Resources.Load<Sprite>($"{avatarResourcesFolder}/{k}");
        if (sp == null) sp = defaultAvatar;

        _avatarCache[k] = sp;
        return sp;
    }
}
