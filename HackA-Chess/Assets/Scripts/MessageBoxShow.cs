using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

namespace Assets.Scripts
{
    public class MessageBoxManager : MonoBehaviour
    {
        //singleton pattern
        public static MessageBoxManager Instance { get; set; }

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
                
            }
           
            messageBoxPanel.SetActive(false);
        }
        public void ShowMessageBox(string title, string message, Action okCallback = null, Action cancelCallback = null)
        {
            titleText.text = title;
            messageText.text = message;

            okButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();

            onOkAction = okCallback;
            onCancelAction = cancelCallback;
                
            okButton.onClick.AddListener(OnOkClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);

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
}