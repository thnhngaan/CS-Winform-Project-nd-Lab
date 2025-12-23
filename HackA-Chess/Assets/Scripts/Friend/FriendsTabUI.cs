using UnityEngine;
using UnityEngine.UI;

public class FriendsTabsUI : MonoBehaviour
{
    [SerializeField] private GameObject friendsTab;
    [SerializeField] private GameObject requestsTab;
    [SerializeField] private GameObject addFriendTab;

    [SerializeField] private Button btnRequests;

    private void Awake()
    {
        //Luôn bật 2 tab chính
        if (friendsTab) friendsTab.SetActive(true);
        if (addFriendTab) addFriendTab.SetActive(true);
        if (requestsTab) requestsTab.SetActive(false);

        if (btnRequests) btnRequests.onClick.AddListener(() =>
        {
            if (requestsTab) requestsTab.SetActive(!requestsTab.activeSelf);
        });
    }
private void Show(string tab)
    {
        if (friendsTab) friendsTab.SetActive(true);

        bool showRequests = tab == "requests";
        bool showAdd = tab == "add";

        if (requestsTab) requestsTab.SetActive(showRequests);
        if (addFriendTab) addFriendTab.SetActive(showAdd);

        if (tab == "friends")
        {
            if (requestsTab) requestsTab.SetActive(false);
            if (addFriendTab) addFriendTab.SetActive(false);
        }
    }
}
