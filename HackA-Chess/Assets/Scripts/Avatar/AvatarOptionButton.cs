using UnityEngine;
using UnityEngine.UI;

public class AvatarOptionButton : MonoBehaviour
{
    [TextArea] public string AvatarKey;     //"Avatar/icons8-avatar-50"
    [SerializeField] private AvatarPickerUI picker;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (picker == null) picker = FindFirstObjectByType<AvatarPickerUI>();
            picker.SelectAvatar(AvatarKey);
        });
    }
}
