using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class MessageBoxManager : MonoBehaviour
{
    //singleton pattern
    public static MessageBoxManager Instance;

    [Header("UI References")]
    public GameObject messageBoxPanel; //panel chính bao gồm cả nền mờ
    public TMP_Text titleText;
    public TMP_Text messageText;
    public Button okButton;
    public Button cancelButton;

    private Action onOkAction;
    private Action onCancelAction;

    void Awake()
    {
        //khởi tạo Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
        messageBoxPanel.SetActive(false);
    }
    public void ShowMessageBox(string title, string message, Action okCallback = null, bool isConfirm = false)
    {
        //gán nội dung
        titleText.text = title;
        messageText.text = message;
        //cài đặt các hành động (Clear Listener trước để tránh lỗi)
        okButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        onOkAction = okCallback;
        onCancelAction = null; //hiện tại không dùng, có thể mở rộng

        //cài đặt nút OK
        okButton.onClick.AddListener(OnOkClicked);

        //cài đặt nút Cancel/Confirm
        if (isConfirm)
        {
            cancelButton.gameObject.SetActive(true);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        else
        {
            cancelButton.gameObject.SetActive(false);
        }

        //hiển thị Panel
        messageBoxPanel.SetActive(true);
    }

    //xử lý khi nhấn OK
    private void OnOkClicked()
    {
        messageBoxPanel.SetActive(false); //ẩn hộp thoại
        onOkAction?.Invoke(); //gọi hành động được truyền vào (nếu có)
    }

    // Xử lý khi nhấn Cancel
    private void OnCancelClicked()
    {
        messageBoxPanel.SetActive(false); // Ẩn hộp thoại
        onCancelAction?.Invoke(); // Gọi hành động Cancel (nếu có, bạn có thể truyền nó vào hàm ShowMessageBox)
    }
}