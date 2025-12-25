using TMPro;
using UnityEngine;

public class MatchHistoryItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text txtMyName;
    [SerializeField] private TMP_Text txtOppName;
    [SerializeField] private TMP_Text txtMyDelta;   //+xx hoặc -xx
    [SerializeField] private TMP_Text txtOppDelta;  //+xx hoặc -xx
    [SerializeField] private TMP_Text txtMyAfter;   //elo sau
    [SerializeField] private TMP_Text txtOppAfter;  //elo sau
    [SerializeField] private TMP_Text txtResult;
    [SerializeField] private TMP_Text time;

    public void Bind(string myFullName, string oppFullName,int myDelta, int oppDelta, int myAfter, int oppAfter, string myResult, string timeplay)
    {
        if (txtMyName) txtMyName.text = myFullName;
        if (txtOppName) txtOppName.text = oppFullName;

        if (txtMyDelta)
            txtMyDelta.text = myDelta >= 0 ? $"<color=#2ECC71>+{myDelta}</color>" : $"<color=#E74C3C>{myDelta}</color>";

        if (txtOppDelta)
            txtOppDelta.text = oppDelta >= 0 ? $"<color=#2ECC71>+{oppDelta}</color>": $"<color=#E74C3C>{oppDelta}</color>";

        if (txtMyAfter) txtMyAfter.text = myAfter.ToString();
        if (txtOppAfter) txtOppAfter.text = oppAfter.ToString();
        if (time) time.text = timeplay;
        if (txtResult) txtResult.text = myResult;
    }
}
