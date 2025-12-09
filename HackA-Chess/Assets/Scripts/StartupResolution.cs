using UnityEngine;

public class StartupResolution : MonoBehaviour
{
    void Start()
    {
        // Chạy ở chế độ cửa sổ 1280x720
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        // hoặc dòng cũ:
        // Screen.SetResolution(1280, 720, false);
    }
}
