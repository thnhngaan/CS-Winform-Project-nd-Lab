using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts;

public class GameChatUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject chatRoot;
    [SerializeField] private ScrollRect chatScroll;
    [SerializeField] private Transform content;
    [SerializeField] private TMP_Text chatItemTemplate;
    [SerializeField] private TMP_InputField inputField;

    [Header("Config")]
    [SerializeField] private int maxItems = 60;

    private readonly List<TMP_Text> _items = new List<TMP_Text>();

    // ✅ nhận line ở thread nào cũng được, UI update ở Update() (main thread)
    private readonly ConcurrentQueue<string> _pendingLines = new ConcurrentQueue<string>();

    private void Awake()
    {
        if (chatRoot != null) chatRoot.SetActive(false);

        if (inputField != null)
        {
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(OnSubmitChat);
        }
    }

    private void OnEnable()
    {
        if (NetworkClient.Instance == null) return;
        NetworkClient.Instance.OnLine -= OnServerLine;
        NetworkClient.Instance.OnLine += OnServerLine;
    }

    private void OnDisable()
    {
        if (NetworkClient.Instance != null)
            NetworkClient.Instance.OnLine -= OnServerLine;
    }

    private void Update()
    {
        while (_pendingLines.TryDequeue(out var line))
        {
            HandleChatLineOnMainThread(line);
        }
    }

    public void ToggleChat()
    {
        if (chatRoot == null) return;
        bool next = !chatRoot.activeSelf;
        chatRoot.SetActive(next);
        if (next && inputField != null)
            inputField.ActivateInputField();
    }

    private async void OnSubmitChat(string _)
    {
        await SendChatAsync();
    }

    private async System.Threading.Tasks.Task SendChatAsync()
    {
        if (inputField == null) return;

        string msg = inputField.text.Trim();
        inputField.text = "";
        inputField.ActivateInputField();

        if (string.IsNullOrEmpty(msg)) return;

        msg = msg.Replace("\n", " ").Replace("\r", " ").Replace("|", "/");

        string roomId = GameSession.RoomId;
        string user = UserSession.CurrentUsername;

        await NetworkClient.Instance.SendAsync($"CHAT|{roomId}|{user}|{msg}");
    }

    // ⚠️ Có thể được gọi từ thread nền
    private void OnServerLine(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return;

        // đề phòng raw có \n chứa nhiều line
        var lines = raw.Split('\n');
        foreach (var l in lines)
        {
            var line = l.Trim();
            if (line.Length > 0)
                _pendingLines.Enqueue(line);
        }
    }

    // ✅ chạy ở main thread (Update)
    private void HandleChatLineOnMainThread(string line)
    {
        if (!line.StartsWith("CHAT|")) return;

        var parts = line.Split('|');
        if (parts.Length < 4) return;

        string roomId = parts[1]?.Trim();
        if (!string.Equals(roomId, GameSession.RoomId, System.StringComparison.OrdinalIgnoreCase))
            return;

        string user = parts[2];
        string msg = parts[3];

        AddItem($"{user}: {msg}");
    }

    private void AddItem(string text)
    {
        if (content == null || chatItemTemplate == null) return;

        var item = Instantiate(chatItemTemplate, content);
        item.gameObject.SetActive(true);
        item.text = text;
        _items.Add(item);

        if (_items.Count > maxItems)
        {
            var first = _items[0];
            _items.RemoveAt(0);
            if (first != null) Destroy(first.gameObject);
        }

        if (chatScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
        }
    }
}

