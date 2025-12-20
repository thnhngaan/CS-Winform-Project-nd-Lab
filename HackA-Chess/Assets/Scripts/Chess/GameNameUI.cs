using TMPro;
using UnityEngine;
using Assets.Scripts;

public class GameNameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameNear; // dưới (gần màn hình)
    [SerializeField] private TMP_Text nameFar;  // trên (xa)

    private static GameNameUI instance;

    private void Awake()
    {
        instance = this;
        Apply();
    }

    private void Apply()
    {
        string myName = string.IsNullOrEmpty(GameSession.MyFullName)
            ? UserSession.CurrentUsername
            : GameSession.MyFullName;

        string oppName = string.IsNullOrEmpty(GameSession.OpponentFullName)
            ? GameSession.OpponentName
            : GameSession.OpponentFullName;

        // ✅ POV: mình luôn ở dưới
        if (nameNear != null) nameNear.text = myName;
        if (nameFar != null) nameFar.text = oppName;
    }

    public static void Refresh()
    {
        if (instance != null) instance.Apply();
    }
}
