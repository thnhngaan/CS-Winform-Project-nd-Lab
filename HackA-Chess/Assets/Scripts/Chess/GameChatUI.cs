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

    private void Awake()
    {
        if (chatRoot != null)
            chatRoot.SetActive(false);

        if (inputField != null)
        {
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(OnSubmitChat);
        }
    }

    private void OnEnable()
    {
        NetworkClient.Instance.OnLine -= OnServerLine;
        NetworkClient.Instance.OnLine += OnServerLine;
    }

    private void OnDisable()
    {
        if (NetworkClient.Instance != null)
            NetworkClient.Instance.OnLine -= OnServerLine;
    }

    public void ToggleChat()
    {
        if (chatRoot == null) return;
        bool next = !chatRoot.activeSelf;
        chatRoot.SetActive(next);
        if (next && inputField != null)
            inputField.ActivateInputField();
    }

    // TMP_InputField callback: void(string) - FIXED
    private async void OnSubmitChat(string _)
    {
        try
        {
            await SendChatAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Chat error: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task SendChatAsync()
    {
        if (inputField == null) return;

        string msg = inputField.text.Trim();

        // clear input ngay
        inputField.text = "";
        inputField.ActivateInputField();

        if (string.IsNullOrEmpty(msg)) return;

        msg = msg.Replace("\n", " ")
                 .Replace("\r", " ")
                 .Replace("|", "/");

        string roomId = GameSession.RoomId;
        string user = UserSession.CurrentUsername;

        await NetworkClient.Instance.SendAsync($"CHAT|{roomId}|{user}|{msg}");

        // nếu server CHƯA broadcast, bạn có thể tạm show local:
        // AddItem($"{user}: {msg}");
    }

    private void OnServerLine(string line)
    {
        if (!line.StartsWith("CHAT|")) return;

        var parts = line.Split('|');
        if (parts.Length < 4) return;

        string roomId = parts[1];
        if (roomId != GameSession.RoomId) return;

        string user = parts[2];
        string msg = parts[3];

        AddItem($"{user}: {msg}");
    }

    private void AddItem(string text)
    {
        if (content == null || chatItemTemplate == null) return;

        // clone 1 dòng chat
        var item = Instantiate(chatItemTemplate, content);
        item.gameObject.SetActive(true);
        item.text = text;
        _items.Add(item);

        // giới hạn số dòng
        if (_items.Count > maxItems)
        {
            var first = _items[0];
            _items.RemoveAt(0);
            if (first != null) Destroy(first.gameObject);
        }

        // auto scroll xuống cuối
        if (chatScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
        }
    }
}
