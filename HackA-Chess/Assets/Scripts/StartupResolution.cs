using UnityEngine;

public class StartupResolution : MonoBehaviour // hàm tạo tab windw như winform
{
    void Start()
    {
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
    }
}
