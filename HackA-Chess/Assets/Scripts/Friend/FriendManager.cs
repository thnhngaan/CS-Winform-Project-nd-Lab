using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class FriendManager : MonoBehaviour
{
    public static FriendManager Instance { get; private set; }

    // ====== Models ======
    [Serializable]
    public class FriendCardData
    {
        public string username;   // key (lower)
        public string fullName;
        public int elo;
        public int win;
        public int draw;
        public int loss;
        public string avatarKey;
        public bool online;
    }

    // ====== State ======
    private readonly Dictionary<string, FriendCardData> _friends = new(); // key=username lower
    private readonly HashSet<string> _incomingInvites = new(); // usernames lower

    public event Action FriendsChanged;
    public event Action IncomingInvitesChanged;
    public event Action<string, bool> FriendStatusPushed; // (username, online) for UI quick update
    public event Action<string> IncomingInvitePushed;      // username who invited
    public event Action<string, string> InviteResultPushed; // (who, ACCEPT/DECLINE)

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkClient.Instance != null)
        {
            NetworkClient.Instance.OnLine -= HandleServerLine;
            NetworkClient.Instance.OnLine += HandleServerLine;
        }
    }

    private void OnDisable()
    {
        if (NetworkClient.Instance != null)
            NetworkClient.Instance.OnLine -= HandleServerLine;
    }

    private static string Key(string s) => (s ?? "").Trim().ToLowerInvariant();

    // ===================== Public APIs (UI calls) =====================

    // Send invite: FRIEND_INVITE|target
    public async Task<string> SendFriendInviteAsync(string targetUsername, int timeoutMs = 5000)
    {
        string target = Key(targetUsername);
        if (string.IsNullOrEmpty(target)) return "Username trống.";

        await NetworkClient.Instance.SendAsync($"FRIEND_INVITE|{target}\n");
        string resp = await NetworkClient.Instance.WaitForPrefixAsync("FRIEND_INVITE|", timeoutMs);

        return ParseFriendInviteResponse(resp);
    }

    // Accept/Decline invite: FRIEND_RESP|fromUser|ACCEPT/DECLINE
    public async Task<string> RespondInviteAsync(string fromUser, bool accept, int timeoutMs = 5000)
    {
        string from = Key(fromUser);
        if (string.IsNullOrEmpty(from)) return "FromUser trống.";

        string decision = accept ? "ACCEPT" : "DECLINE";
        await NetworkClient.Instance.SendAsync($"FRIEND_RESP|{from}|{decision}\n");

        // Server trả: FRIEND_RESP_ACK|OK|fromUser|ACCEPT/DECLINE  (hoặc NOT_FOUND/INVALID)
        string resp = await NetworkClient.Instance.WaitForPrefixAsync("FRIEND_RESP_ACK|", timeoutMs);
        return ParseFriendRespAck(resp, from, accept);
    }

    // Load full friend cards: FRIEND_LIST_DETAIL
    public async Task<List<FriendCardData>> FetchFriendListDetailAsync(int timeoutMs = 5000)
    {
        await NetworkClient.Instance.SendAsync("FRIEND_LIST_DETAIL\n");
        string resp = await NetworkClient.Instance.WaitForPrefixAsync("FRIEND_LIST_DETAIL|", timeoutMs);

        var list = ParseFriendListDetail(resp);
        ApplyFriendList(list);
        return list;
    }

    public List<FriendCardData> GetFriendsSnapshot()
        => _friends.Values
                   .OrderByDescending(x => x.online)
                   .ThenByDescending(x => x.elo)
                   .ThenBy(x => x.username)
                   .ToList();

    public List<string> GetIncomingInvitesSnapshot()
        => _incomingInvites.OrderBy(x => x).ToList();

    // ===================== Parsing & Applying =====================

    private string ParseFriendInviteResponse(string line)
    {
        // Examples:
        // FRIEND_INVITE|OK|target
        // FRIEND_INVITE|PENDING|target
        // FRIEND_INVITE|ALREADY|target
        // FRIEND_INVITE|NOT_FOUND|target
        // FRIEND_INVITE|SELF
        // FRIEND_INVITE|AUTO_ACCEPT|target
        if (string.IsNullOrEmpty(line)) return "Timeout / không nhận được phản hồi.";

        var parts = line.Trim().Split('|');
        if (parts.Length < 2) return "Phản hồi FRIEND_INVITE không hợp lệ.";

        string code = parts[1].Trim().ToUpperInvariant();
        return code switch
        {
            "OK" => "Đã gửi lời mời kết bạn.",
            "PENDING" => "Đang có lời mời chờ xử lý.",
            "ALREADY" => "Hai bạn đã là bạn bè.",
            "NOT_FOUND" => "Không tìm thấy user.",
            "SELF" => "Không thể tự kết bạn.",
            "AUTO_ACCEPT" => "Đối phương đã mời bạn trước đó → auto kết bạn thành công.",
            "NOLOGIN" => "Bạn chưa đăng nhập.",
            "INVALID" => "Dữ liệu không hợp lệ.",
            _ => $"Lỗi: {code}"
        };
    }

    private string ParseFriendRespAck(string line, string fromUser, bool accept)
    {
        // FRIEND_RESP_ACK|OK|fromUser|ACCEPT
        // FRIEND_RESP_ACK|NOT_FOUND|fromUser
        // FRIEND_RESP_ACK|INVALID|fromUser
        if (string.IsNullOrEmpty(line)) return "Timeout / không nhận được phản hồi.";

        var parts = line.Trim().Split('|');
        if (parts.Length < 2) return "Phản hồi FRIEND_RESP_ACK không hợp lệ.";

        string code = parts[1].Trim().ToUpperInvariant();
        if (code == "OK")
        {
            // update local state
            _incomingInvites.Remove(Key(fromUser));
            IncomingInvitesChanged?.Invoke();

            if (accept)
            {
                // sau accept: thường server sẽ push/hoặc bạn fetch lại list
                // ở đây mình add tạm nếu chưa có
                if (!_friends.ContainsKey(Key(fromUser)))
                {
                    _friends[Key(fromUser)] = new FriendCardData
                    {
                        username = Key(fromUser),
                        fullName = "",
                        elo = 0,
                        win = 0,
                        draw = 0,
                        loss = 0,
                        avatarKey = "",
                        online = false
                    };
                    FriendsChanged?.Invoke();
                }
            }
            return accept ? "Đã chấp nhận lời mời." : "Đã từ chối lời mời.";
        }

        return $"Lỗi: {code}";
    }

    private List<FriendCardData> ParseFriendListDetail(string line)
    {
        // FRIEND_LIST_DETAIL|count|u,fullname,elo,win,draw,loss,avatar,online;...
        var list = new List<FriendCardData>();
        if (string.IsNullOrEmpty(line)) return list;

        var parts = line.Trim().Split('|');
        if (parts.Length < 2) return list;

        // parts[1] = count (không bắt buộc phải tin tuyệt đối)
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[2]))
            return list;

        var items = parts[2].Split(';');
        foreach (var it in items)
        {
            if (string.IsNullOrWhiteSpace(it)) continue;

            var f = it.Split(',');
            if (f.Length < 8) continue;

            string u = Key(f[0]);
            if (string.IsNullOrEmpty(u)) continue;

            list.Add(new FriendCardData
            {
                username = u,
                fullName = f[1], // server đã SafeField() nên không còn dấu phân cách phá format
                elo = SafeInt(f[2]),
                win = SafeInt(f[3]),
                draw = SafeInt(f[4]),
                loss = SafeInt(f[5]),
                avatarKey = f[6],
                online = f[7] == "1"
            });
        }
        return list;
    }

    private void ApplyFriendList(List<FriendCardData> list)
    {
        _friends.Clear();
        foreach (var f in list)
            _friends[f.username] = f;

        FriendsChanged?.Invoke();
    }

    private int SafeInt(string s) => int.TryParse(s, out var v) ? v : 0;

    // ===================== Push handler =====================

    private void HandleServerLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        line = line.Trim();

        // 1) Incoming invite push
        if (line.StartsWith("FRIEND_INVITE_PUSH|", StringComparison.Ordinal))
        {
            var p = line.Split('|');
            if (p.Length >= 2)
            {
                string from = Key(p[1]);
                if (!string.IsNullOrEmpty(from))
                {
                    _incomingInvites.Add(from);
                    IncomingInvitesChanged?.Invoke();
                    IncomingInvitePushed?.Invoke(from);
                }
            }
            return;
        }

        // 2) Online/offline status push
        if (line.StartsWith("FRIEND_STATUS|", StringComparison.Ordinal))
        {
            var p = line.Split('|');
            if (p.Length >= 3)
            {
                string u = Key(p[1]);
                bool on = p[2] == "1";

                if (_friends.TryGetValue(u, out var f))
                {
                    f.online = on;
                    FriendsChanged?.Invoke();
                }

                FriendStatusPushed?.Invoke(u, on);
            }
            return;
        }

        // 3) Result push when someone accepts/declines your invite
        if (line.StartsWith("FRIEND_RESP_PUSH|", StringComparison.Ordinal))
        {
            var p = line.Split('|');
            if (p.Length >= 3)
            {
                string who = Key(p[1]);
                string decision = p[2].Trim().ToUpperInvariant();

                InviteResultPushed?.Invoke(who, decision);

                if (decision == "ACCEPT")
                {
                    if (!_friends.ContainsKey(who))
                    {
                        _friends[who] = new FriendCardData
                        {
                            username = who,
                            fullName = "",
                            elo = 0,
                            win = 0,
                            draw = 0,
                            loss = 0,
                            avatarKey = "",
                            online = false
                        };
                        FriendsChanged?.Invoke();
                    }
                }
            }
            return;
        }
        if (line.StartsWith("FRIEND_REMOVED_PUSH|", StringComparison.Ordinal))
        {
            var p = line.Split('|');
            if (p.Length >= 2)
            {
                string by = Key(p[1]);
                _friends.Remove(by);
                FriendsChanged?.Invoke();
            }
            return;
        }

    }
    public async Task RefreshIncomingInvitesAsync(int timeoutMs = 5000)
    {
        await NetworkClient.Instance.SendAsync("FRIEND_INBOX\n");
        string resp = await NetworkClient.Instance.WaitForPrefixAsync("FRIEND_INBOX|", timeoutMs);
        ApplyIncomingInvites(resp);
    }

    private void ApplyIncomingInvites(string line)
    {
        // FRIEND_INBOX|count|userA;userB;...
        _incomingInvites.Clear();

        if (string.IsNullOrEmpty(line)) { IncomingInvitesChanged?.Invoke(); return; }

        var parts = line.Trim().Split('|');
        if (parts.Length < 2) { IncomingInvitesChanged?.Invoke(); return; }

        if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
        {
            foreach (var u in parts[2].Split(';'))
            {
                var k = Key(u);
                if (!string.IsNullOrEmpty(k))
                    _incomingInvites.Add(k);
            }
        }

        IncomingInvitesChanged?.Invoke();
    }
    public async Task<string> RemoveFriendAsync(string targetUser, int timeoutMs = 5000)
    {
        string target = Key(targetUser);
        if (string.IsNullOrEmpty(target)) return "Username trống.";

        await NetworkClient.Instance.SendAsync($"FRIEND_REMOVE|{target}\n");
        string resp = await NetworkClient.Instance.WaitForPrefixAsync("FRIEND_REMOVE|", timeoutMs);

        if (string.IsNullOrEmpty(resp)) return "Timeout.";

        var p = resp.Trim().Split('|');

        string code = p[1].Trim().ToUpperInvariant();
        if (code == "OK" && p.Length >= 3)
        {
            string t = Key(p[2]);
            _friends.Remove(t);
            FriendsChanged?.Invoke();
            return "Đã xóa bạn.";
        }

        return $"Lỗi: {code}";
    }

}
