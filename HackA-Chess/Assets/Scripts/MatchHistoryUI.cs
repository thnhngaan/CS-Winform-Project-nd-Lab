using System;
using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine;

public class MatchHistoryUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private int topN = 50;

    private void OnEnable()
    {
        _ = Refresh();
    }

    public async Task Refresh()
    {
        Clear();

        string u = UserSession.CurrentUsername;
        if (string.IsNullOrWhiteSpace(u))
        {
            Debug.LogWarning("No CurrentUsername");
            return;
        }

        var waitBatch = NetworkClient.Instance.WaitForPrefixAsync("MATCHHISTORY|", 5000);
        await NetworkClient.Instance.SendAsync($"MATCHHISTORY|{u}|{topN}|{7}\n");

        string batch = await waitBatch;
        if (string.IsNullOrEmpty(batch))
        {
            Debug.LogWarning("MATCHHISTORY timeout");
            return;
        }
        Debug.LogWarning($"{batch}");
        // tách các dòng
        string[] parts = batch.Split('|');
        if (parts[1] == "FAIL")
        {
            Debug.LogWarning("MATCHHISTORY fail");
            return;
        }
        if (parts[1] != "OK" || !int.TryParse(parts[2], out int count))
        {
            Debug.LogWarning("Bad MATCHHISTORY header:");
            return;
        }

        for (int idx = 3; idx < parts.Length; idx++)
        {
            string[] matchhistory = parts[idx].Split(",");

            string playedAt = matchhistory[0];
            string myName = matchhistory[1];
            string oppName = matchhistory[2];
            int myDelta = SafeInt(matchhistory[3]);
            int oppDelta = SafeInt(matchhistory[4]);
            int myAfter = SafeInt(matchhistory[5]);
            int oppAfter = SafeInt(matchhistory[6]);

            string result = (myDelta >= 0) ? "WIN" : "LOSS";

            var go = Instantiate(itemPrefab, content);
            var ui = go.GetComponent<MatchHistoryItemUI>();
            ui.Bind(myName, oppName, myDelta, oppDelta, myAfter, oppAfter, result, playedAt);
        }
    }

    private int SafeInt(string s)
        => int.TryParse(s, out int v) ? v : 0;

    private void Clear()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }
}
