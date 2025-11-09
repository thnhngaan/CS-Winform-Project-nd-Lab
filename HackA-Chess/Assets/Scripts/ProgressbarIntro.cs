using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public Image progressBar;        // Thanh tiến trình
    public Image progressBar_Background;
    public TMP_Text progressText;    // Text hiển thị %
    public Button joinButton;        // Nút "CLICK TO JOIN GAME"
    public TMP_Text loading;

    [Header("Settings")]
    public string nextScene;         // Tên scene tiếp theo
    public float fakeLoadTime = 30f; // Thời gian load giả (giây)

    void Start()
    {
        // Ẩn nút JOIN khi bắt đầu
        joinButton.gameObject.SetActive(false);   // Đặt thanh tiến trình về 0
        progressBar.fillAmount = 0f;
        progressText.text = "0%";

        // Bắt đầu giả lập load
        StartCoroutine(FakeLoading());
    }

    IEnumerator FakeLoading()
    {
        float elapsed = 0f;

        while (elapsed < fakeLoadTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fakeLoadTime);

            progressBar.fillAmount = progress;
            progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        // Khi hoàn thành load 100%
        progressText.text = "100%";

        // Ẩn progress bar + text
        progressBar.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);
        progressBar_Background.gameObject.SetActive(false);

        // Hiện nút "CLICK TO JOIN GAME"
        joinButton.gameObject.SetActive(true);
        loading.gameObject.SetActive(false);
    }

    public void OnJoinButtonClicked()
    {
        SceneManager.LoadScene(nextScene);
    }
}
