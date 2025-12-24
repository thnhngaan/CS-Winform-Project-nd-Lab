using System;
using System.Collections.Generic;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadRank : MonoBehaviour
{
    [Header("UI Panel Top 3")]
    [SerializeField] private TMP_Text FullnameTop1;
    [SerializeField] private TMP_Text EloTop1;
    [SerializeField] private TMP_Text WinTop1;
    [SerializeField] private TMP_Text DrawTop1;
    [SerializeField] private TMP_Text LossTop1;

    [SerializeField] private TMP_Text FullnameTop2;
    [SerializeField] private TMP_Text EloTop2;
    [SerializeField] private TMP_Text WinTop2;
    [SerializeField] private TMP_Text DrawTop2;
    [SerializeField] private TMP_Text LossTop2;

    [SerializeField] private TMP_Text FullnameTop3;
    [SerializeField] private TMP_Text EloTop3;
    [SerializeField] private TMP_Text WinTop3;
    [SerializeField] private TMP_Text DrawTop3;
    [SerializeField] private TMP_Text LossTop3;


    [Header("UI References")]
    [SerializeField] private TMP_Text PageText;             
    [SerializeField] private Button next;
    [SerializeField] private Button back;

    [Header("Content Panel")]
    [SerializeField] private Transform Leaderboard_Content; 

    [Header("Text Font")]
    [SerializeField] private TMP_FontAsset Font;        

    [Header("Page Settings")]
    [SerializeField] private int pageSize = 10; //mỗi trang 10 người

    private int currentpage = 1;
    private int totalpages = 1;
    private int totalcount = 0;
    private bool isLoading = false;

    private readonly List<GameObject> spawnedRankItems = new();

    // Model dữ liệu cho 1 player trên leaderboard
    [Serializable]
    private class LeaderboardEntry
    {
        public int Rank;
        public string Username;
        public string Fullname;
        public int Elo;
        public int TotalWin;
        public int TotalDraw;
        public int TotalLoss;
    }

    private void OnEnable()
    {
        if (next!= null)
        {
            next.onClick.AddListener(OnNextClicked);
        }

        if (back != null)
        {
            back.onClick.AddListener(OnBackClicked);
        }

        currentpage = 1;
        RequestRanking(currentpage);
    }

    private void OnDisable()
    {
        if (next != null)
        {
            next.onClick.RemoveListener(OnNextClicked);
        }

        if (back != null)
        {
            back.onClick.RemoveListener(OnBackClicked);
        }
    }


    private void OnNextClicked()
    {
        if (isLoading) return;
        if (currentpage < totalpages)
        {
            currentpage++;
            RequestRanking(currentpage);
        }
    }

    private void OnBackClicked()
    {
        if (isLoading) return;
        if (currentpage > 1)
        {
            currentpage--;
            RequestRanking(currentpage);
        }
    }

    //Gửi request lên server để lấy 1 trang leaderboard
    private async void RequestRanking(int page)
    {
        if (NetworkClient.Instance == null)
        {
            return;
        }

        isLoading = true;

        //Ví dụ format: GET_RANK|page|pageSize
        string msg = $"GET_RANK|{page}|{pageSize}";
        Debug.Log("Send: " + msg);
        await NetworkClient.Instance.SendAsync(msg);
        string result = await NetworkClient.Instance.WaitForPrefixAsync("RANK_PAGE|", 5000);
        HandleServerMessage(result);
    }

    //nhận message từ server. 
    //Định dạng: RANK_PAGE|page|totalCount|username,fullname,elo,win,draw,loss;...
    private void HandleServerMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        string[] parts = msg.Split('|');
        if (parts.Length < 4) return;
        if (parts[0] != "RANK_PAGE") return; 
        try
        {
            int page = int.Parse(parts[1]);
            int total = int.Parse(parts[2]);
            string dataPart = parts[3];

            totalcount = total;
            totalpages = (totalcount % pageSize == 0) ? totalcount / pageSize : totalcount / pageSize + 1;
            currentpage = Mathf.Clamp(page, 1, totalpages);

            List<LeaderboardEntry> entries = ParseEntries(dataPart);
            if (entries.Count > 0)
            {
                var top1 = entries[0];
                if (FullnameTop1 != null) FullnameTop1.text = top1.Fullname;
                if (EloTop1 != null) EloTop1.text = top1.Elo.ToString();
                if (WinTop1 != null) WinTop1.text = top1.TotalWin + "W";
                if (DrawTop1 != null) DrawTop1.text = top1.TotalDraw + "D";
                if (LossTop1 != null) LossTop1.text = top1.TotalLoss + "L";
            }
            else
            {
                if (FullnameTop1 != null) FullnameTop1.text = "-";
                if (EloTop1 != null) EloTop1.text = "-";
                if (WinTop1 != null) WinTop1.text = "-";
                if (DrawTop1 != null) DrawTop1.text = "-";
                if (LossTop1 != null) LossTop1.text = "-";
            }

            if (entries.Count > 1)
            {
                var top2 = entries[1];
                if (FullnameTop2 != null) FullnameTop2.text = top2.Fullname;
                if (EloTop2 != null) EloTop2.text = top2.Elo.ToString();
                if (WinTop2 != null) WinTop2.text = top2.TotalWin + "W";
                if (DrawTop2 != null) DrawTop2.text = top2.TotalDraw + "D";
                if (LossTop2 != null) LossTop2.text = top2.TotalLoss + "L";
            }
            else
            {
                if (FullnameTop2 != null) FullnameTop2.text = "-";
                if (EloTop2 != null) EloTop2.text = "-";
                if (WinTop2 != null) WinTop2.text = "-";
                if (DrawTop2 != null) DrawTop2.text = "-";
                if (LossTop2 != null) LossTop2.text = "-";
            }

            if (entries.Count > 2)
            {
                var top3 = entries[2];
                if (FullnameTop3 != null) FullnameTop3.text = top3.Fullname;
                if (EloTop3 != null) EloTop3.text = top3.Elo.ToString();
                if (WinTop3 != null) WinTop3.text = top3.TotalWin + "W";
                if (DrawTop3 != null) DrawTop3.text = top3.TotalDraw + "D";
                if (LossTop3 != null) LossTop3.text = top3.TotalLoss + "L";
            }
            else
            {
                if (FullnameTop3 != null) FullnameTop3.text = "-";
                if (EloTop3 != null) EloTop3.text = "-";
                if (WinTop3 != null) WinTop3.text = "-";
                if (DrawTop3 != null) DrawTop3.text = "-";
                if (LossTop3 != null) LossTop3.text = "-";
            }


            if (PageText != null)
            {
                PageText.text = $"{currentpage} / {totalpages}";
            }
            UpdateButtonsInteractable();
            PopulateLeaderboard(entries);
        }
        catch (Exception e)
        {
            Debug.LogError("Parse ranking không thành công: " + e);
        }
        finally
        {
            isLoading = false;
        }
    }

    //parse chuỗi data player từ server.
    //định dạng dataPart: "user1,Fullname1,1500,10,2,3;user2,Fullname2,1490,9,3,4;"
    private List<LeaderboardEntry> ParseEntries(string dataPart)
    {
        var listdata = new List<LeaderboardEntry>();
        if (string.IsNullOrWhiteSpace(dataPart))
            return listdata;

        string[] players = dataPart.Split(';', StringSplitOptions.RemoveEmptyEntries);
        int baseranking = (currentpage - 1) * pageSize;

        for (int i = 0; i < players.Length; i++)
        {
            string p = players[i];
            string[] fields = p.Split(',');

            if (fields.Length < 6)
                continue;

            var entry = new LeaderboardEntry
            {
                Rank = baseranking + i + 1,
                Username = fields[0],
                Fullname = fields[1],
                Elo = int.TryParse(fields[2], out int elo) ? elo : 0,
                TotalWin = int.TryParse(fields[3], out int w) ? w : 0,
                TotalDraw = int.TryParse(fields[4], out int d) ? d : 0,
                TotalLoss = int.TryParse(fields[5], out int l) ? l : 0
            };
            listdata.Add(entry);
        }
        return listdata;
    }
    private void UpdateButtonsInteractable()
    {
        if (back != null)
            back.interactable = currentpage > 1;

        if (next != null)
            next.interactable = currentpage < totalpages;
    }

    //tạo textmesspro bằng code và gắn các trường lên đó
    private TMP_Text CreateTmpText(Transform parent, string name, string text, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        if (parent == null)
        {
            Debug.LogError("[Leaderboard] CreateTmpText: parent == null");
            return null;
        }

        var goText = new GameObject(name, typeof(RectTransform));
        goText.transform.SetParent(parent, false);

        var tmp = goText.AddComponent<TextMeshProUGUI>();

        if (Font != null)
            tmp.font = Font;

        tmp.text = text ?? string.Empty;
        tmp.alignment = alignment;
        tmp.fontSize = 35;

        var rt = goText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 70); //set chiều cao (Height) trước còn witdh của text sẽ để AddLeaderboardItem quyết định

        return tmp;
    }

    //reset
    private void ClearLeaderboard()
    {
        for (int i = 0; i < spawnedRankItems.Count; i++)
        {
            if (spawnedRankItems[i] != null)
                Destroy(spawnedRankItems[i]);
        }
        spawnedRankItems.Clear();
    }

    //Vẽ lại 10 player cho trang hiện tại (khi load trang, khi ấn next, khi ấn back)
    private void PopulateLeaderboard(List<LeaderboardEntry> entries)
    {
        ClearLeaderboard();
        if (Leaderboard_Content == null)
        {
            Debug.LogWarning("[Leaderboard] Thiếu tham chiếu Leaderboard_Content.");
            return;
        }

        foreach (var e in entries)
        {
            AddLeaderboardItem(e.Rank, e.Username, e.Fullname, e.Elo, e.TotalWin, e.TotalDraw, e.TotalLoss);
        }
    }

    //Tạo 1 dòng player hoàn toàn bằng code
    private void AddLeaderboardItem(int rank, string username, string fullname, int elo, int win, int draw, int loss)
    {
        var go = new GameObject($"Rank_{rank}", typeof(RectTransform));
        go.transform.SetParent(Leaderboard_Content, false);
        spawnedRankItems.Add(go);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 90);

        var image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.05f);

        var hLayout = go.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = true;    
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = false;  
        hLayout.childForceExpandHeight = false;
        hLayout.spacing = 10;
        hLayout.padding = new RectOffset(10, 10, 5, 5);

        // Rank
        var Rank = CreateTmpText(go.transform, "Text_Rank", rank.ToString(), TextAlignmentOptions.Center);
        // Username
        var User = CreateTmpText(go.transform, "Text_Username", username, TextAlignmentOptions.Left);
        // Fullname
        var Fullname = CreateTmpText(go.transform, "Text_Fullname", fullname, TextAlignmentOptions.Center);
        // Elo
        var Elo = CreateTmpText(go.transform, "Text_Elo", elo.ToString(), TextAlignmentOptions.Left);

        var Win = CreateTmpText(go.transform,"Text_Win",win.ToString(),TextAlignmentOptions.Center);
        var Draw = CreateTmpText(go.transform,"Text_Draw",draw.ToString(), TextAlignmentOptions.Center);
        var Loss = CreateTmpText(go.transform,"Text_Loss", loss.ToString(), TextAlignmentOptions.Center);

        //Chia tỉ lệ bằng flexibleWidth
        if (Rank != null)
        {
            var le = Rank.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 120f;   // Rank luôn 80px
        }

        if (User != null)
        {
            var le = User.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 250f;  // Username 200px
        }

        if (Fullname != null)
        {
            var le = Fullname.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 600f;  // Fullname 320px
        }

        if (Elo != null)
        {
            var le = Elo.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 200f;  // Elo 100px
        }
        if(Win != null)
        {
            var le=Win.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 100f;
        }
        if (Loss != null)
        {
            var le = Loss.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 100f;
        }
        if (Draw != null)
        {
            var le = Draw.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 100f;
        }
    }

}
