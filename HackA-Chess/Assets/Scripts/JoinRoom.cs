
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

namespace Assets.Scripts
{
    public class JoinRoom : MonoBehaviour
    {
        //  JOIN ROOM PANEL (Panel gốc)
        [Header("JoinRoom_Panel")]
        [SerializeField] private GameObject JoinRoom_Panel;
        [SerializeField] private Button Back_button;
        [SerializeField] private Button Join_button;
        [SerializeField] private Button Create_button;
        [SerializeField] private Button JoinID_button;

        //  LIST ROOM PANEL
        [Header("ListRoom")]
        [SerializeField] private GameObject ListRoom_Panel;
        [SerializeField] private Transform ListRoom_Content;
        [SerializeField] private GameObject ListRoomItem_prefab;
        [SerializeField] private Button Load_ListRoom_button; 

        [SerializeField] private TMP_InputField ID_ListRoom_inputfield;


        //  ENTER ID PANEL
        [Header("EnterID_Panel")]
        [SerializeField] private GameObject EnterID_Panel;
        [SerializeField] private TMP_InputField ID_EnterID_inputfield;
        [SerializeField] private Button Join_EnterID_button;
        [SerializeField] private Button Back_EnterID_button;

        // CREATE ID PANEL
        [Header("Create ID")]
        [SerializeField] private GameObject CreateID_Panel;

        //Status (ToggleGroup - Toggles)
        [SerializeField] private ToggleGroup Status_CreateID_togglegroup;
        [SerializeField] private Toggle StatusPublic_CreateID_toggle;
        [SerializeField] private Toggle StatusPrivate_CreateID_toggle;

        //Controls
        [SerializeField] private TMP_InputField ID_CreateID_inputfield;
        [SerializeField] private Button Create_CreateID_button;
        [SerializeField] private Button Back_CreateID_button;
        [SerializeField] private Button Join_CreateID_button;



        // MESSAGES
        [Header("Messages")]
        [SerializeField] private TMP_Text messageText;

        //  TCP SOCKET
        private string serverIP = "10.32.87.236";
        private int Port_JoinID = 8081;  
        private int Port_Create = 8082; 
        private int Port_ListRoom = 8080;
        private int Port_Join = 8083; 

        // SCENES
        private const string Waiting_Scene = "WaitingRoom";
        [Header("Back Scene")]
        [SerializeField] private string Back_scene = "MainMenu";

        // TRẠNG THÁI & VALIDATION
        private string status = "public";
        private static readonly Regex SixDigits = new(@"^\d{6}$");

        private readonly List<GameObject> _spawnedListItems = new();
        private string _selectedRoomId = null;
        [SerializeField] private bool autoListOnStart = true;

        private void Awake()
        {
            // JoinRoom buttons
            if (Back_button != null) Back_button.onClick.AddListener(OnBack_buttonClicked); 
            if (Join_button != null) Join_button.onClick.AddListener(OnJoin_buttonClicked); 
            if (Create_button != null) Create_button.onClick.AddListener(OpenCreateIDPanel);
            if (JoinID_button != null) JoinID_button.onClick.AddListener(OpenEnterIDPanel);

            // List refresh
            if (Load_ListRoom_button != null) Load_ListRoom_button.onClick.AddListener(() => _ = OpenListRoomPanelAsync(true)); // để ý

            // EnterID
            if (Join_EnterID_button != null) Join_EnterID_button.onClick.AddListener(OnJoin_EnterID_buttonClicked); 
            if (Back_EnterID_button != null) Back_EnterID_button.onClick.AddListener(CloseEnterIDPanel); 

            // CreateID
            if (Back_CreateID_button != null) Back_CreateID_button.onClick.AddListener(OnBack_CreateID_buttonClicked); 
            if (Create_CreateID_button != null) Create_CreateID_button.onClick.AddListener(OnCreate_CreatID_buttonClicked);
            if (Join_CreateID_button != null) Join_CreateID_button.onClick.AddListener(OnJoin_CreateID_buttonClicked); 

            // Status toggles (Create_ID)
            if (StatusPublic_CreateID_toggle != null)
                StatusPublic_CreateID_toggle.onValueChanged.AddListener(isOn => { if (isOn) status = "public"; });
            if (StatusPrivate_CreateID_toggle != null)
                StatusPrivate_CreateID_toggle.onValueChanged.AddListener(isOn => { if (isOn) status = "private"; });

            // Đóng cả 3 panel
            CloseAllPanels();

            if (StatusPublic_CreateID_toggle != null) StatusPublic_CreateID_toggle.isOn = true;

            // Input settings
            if (ID_CreateID_inputfield != null)
            {
                ID_CreateID_inputfield.characterLimit = 6;
                ID_CreateID_inputfield.contentType = TMP_InputField.ContentType.IntegerNumber;
            }
            if (ID_EnterID_inputfield != null)
            {
                ID_EnterID_inputfield.characterLimit = 6;
                ID_EnterID_inputfield.contentType = TMP_InputField.ContentType.IntegerNumber;
            }
            if (ID_ListRoom_inputfield != null)
            {
                ID_ListRoom_inputfield.characterLimit = 6;
                ID_ListRoom_inputfield.contentType = TMP_InputField.ContentType.IntegerNumber;
            }
        }

        private async void Start()
        {
            //  Khi vào giao diện, đóng 3 panel sau đó ListRoom_Panel trước (và load list)
            CloseAllPanels();
            if (autoListOnStart)
            {
                await OpenListRoomPanelAsync(true);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (EnterID_Panel != null && EnterID_Panel.activeSelf)
                    CloseEnterIDPanel();                            // note: chỉnh lại
                else if (CreateID_Panel != null && CreateID_Panel.activeSelf)
                    OnBack_CreateID_buttonClicked();
                else
                    OnBack_buttonClicked(); 
            }
        }

        //  Đóng tất cả Panel trước khi mở panel khác
        private void CloseAllPanels()
        {
            if (ListRoom_Panel != null) ListRoom_Panel.SetActive(false);
            if (EnterID_Panel != null) EnterID_Panel.SetActive(false);
            if (CreateID_Panel != null) CreateID_Panel.SetActive(false);
        }

        //
        private async Task OpenListRoomPanelAsync(bool refresh)
        {
            CloseAllPanels(); // đảm bảo không chồng panel
            if (JoinRoom_Panel != null) JoinRoom_Panel.SetActive(true);
            if (ListRoom_Panel != null) ListRoom_Panel.SetActive(true);
            if (refresh)                                                    // để ý
                await LoadListRoomAsync(); 
        }

        //  JoinRoom_Panel (Back_button)
        //  Quay lại Giao diện trước (MainMenu)
        private void OnBack_buttonClicked() 
        {
            var current = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(Back_scene) && current != Back_scene)
                SceneManager.LoadScene(Back_scene, LoadSceneMode.Single);
        }

        //  ListRoom_Panel (Join_button - JoinRoom_Panel) 
        //  Gửi "Join|{ID}" Port: 8083
        //  Nhận "Join success"/"Join fail"
        private async void OnJoin_buttonClicked() 
        {
            if (!string.IsNullOrEmpty(_selectedRoomId) && SixDigits.IsMatch(_selectedRoomId))
            {
                ShowMessage($"Đang gửi Join|{_selectedRoomId} (8083)...");
                string result = await SendMessageAsync($"Join|{_selectedRoomId}", serverIP, Port_Join);
                HandleResponse_Join(result); 
            }
            else
            {
                ShowMessage("Chưa chọn phòng trong danh sách.");
            }
        }

        // EnterID_Panel (MỞ - JoinID_button)
        private void OpenEnterIDPanel() 
        {
            CloseAllPanels(); // Đóng 3 panel
            EnterID_Panel.transform.SetAsLastSibling();
            EnterID_Panel.SetActive(true);

            if (ID_EnterID_inputfield != null)
            {
                ID_EnterID_inputfield.text = _selectedRoomId ?? string.Empty;
                ID_EnterID_inputfield.textComponent.color = Color.black;
                ID_EnterID_inputfield.ActivateInputField();
            }
        }

        // EnterID_Panel (ĐÓNG - Back_JoinID_button)
        private async void CloseEnterIDPanel() 
        {
            if (EnterID_Panel != null) EnterID_Panel.SetActive(false);

            await OpenListRoomPanelAsync(true);
        }

        //  EnterID_Panel (Join_EnterID_buton)
        //  Gửi : JoinID|{ID} Port: 8081
        //  Nhận: "JoinID success" / "JoinID fail"
        private async void OnJoin_EnterID_buttonClicked() 
        {
            if (ID_EnterID_inputfield == null) return;

            string id = ID_EnterID_inputfield.text.Trim();
            if (!SixDigits.IsMatch(id)) // kiểm tra ID trước khi gửi đi
            {
                ID_EnterID_inputfield.textComponent.color = Color.red;
                ShowMessage("ID phải gồm 6 chữ số.");
                return;
            }

            ID_EnterID_inputfield.textComponent.color = Color.black;
            ShowMessage("Đang gửi yêu cầu JoinID...");

            string result = await SendMessageAsync($"JoinID|{id}", serverIP, Port_JoinID);
            HandleResponse_JoinID(result); 
        }

        // CreateID_Panel (MỞ - Create_button)
        private void OpenCreateIDPanel()
        {
            CloseAllPanels(); // Đóng 3 panel

            CreateID_Panel.transform.SetAsLastSibling();            //!
            CreateID_Panel.SetActive(true);

            if (ID_CreateID_inputfield != null)
            {
                ID_CreateID_inputfield.text = string.Empty;
                ID_CreateID_inputfield.textComponent.color = Color.black;
                ID_CreateID_inputfield.ActivateInputField();
            }

            if (StatusPublic_CreateID_toggle != null) StatusPublic_CreateID_toggle.isOn = true;
            status = GetStatusFromGroup();


        }

        // CreateID_Panel (ĐÓNG - Back_CreateID_button)
        private async void OnBack_CreateID_buttonClicked() 
        {
            if (CreateID_Panel != null) CreateID_Panel.SetActive(false);
            if (JoinRoom_Panel != null) JoinRoom_Panel.SetActive(true);

            await OpenListRoomPanelAsync(true);
        }

        //  CreateID_Panel (Create_CreateID_button)
        //  Gửi "CREATE|{status}" Port: 8082
        //  Nhận: "{ID 6 chữ số}" / "Create fail"
        private async void OnCreate_CreatID_buttonClicked()
        {
            status = GetStatusFromGroup();
            ShowMessage($"Đang gửi yêu cầu CREATE ({status})...");

            string result = await SendMessageAsync($"CREATE|{status}", serverIP, Port_Create);
            HandleResponse_Create(result); 
        }

        //  CreateID_Panel (Join_CreateID_button)
        //  Gửi: "JoinID|{ID}" Port: 8081
        //  Nhận: "JoinID success"/"JoinID fail"
        private async void OnJoin_CreateID_buttonClicked() 
        {
            if (ID_CreateID_inputfield == null) return;

            string id = ID_CreateID_inputfield.text.Trim();
            if (!SixDigits.IsMatch(id))
            {
                ID_CreateID_inputfield.textComponent.color = Color.red;
                ShowMessage("ID phải gồm 6 chữ số.");
                return;
            }

            ID_CreateID_inputfield.textComponent.color = Color.black;
            ShowMessage("Đang gửi yêu cầu JoinID...");

            string result = await SendMessageAsync($"JoinID|{id}", serverIP, Port_JoinID);
            HandleResponse_JoinID(result); 
        }

        //  Xử lý phản hồi "JoinID|{ID}" Port: 8081
        //  (Join_EnterID_button / Join_CreateID_button).
        private void HandleResponse_JoinID(string result) 
        {
            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server.");
                return;
            }

            result = result.Trim();
            if (result.Equals("JoinID success", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Gia nhập thành công! Đang chuyển tới phòng chờ...");
                SceneManager.LoadScene(Waiting_Scene, LoadSceneMode.Single);
            }
            else if (result.Equals("JoinID fail", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Gia nhập phòng thất bại.");
                //_ = OpenListRoomPanelAsync(true); // quay lại ListRoom (đã đóng các panel khác)
            }
            else
            {
                ShowMessage($"Phản hồi lạ từ server: {result}");
            }
        }

        //  Xử lý phản hồi "Join|{ID}" Port: 8083
        //  (Join_button).
        private void HandleResponse_Join(string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server (Join).");
                return;
            }

            result = result.Trim();
            if (result.Equals("Join success", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Gia nhập thành công! Đang chuyển tới phòng chờ...");
                SceneManager.LoadScene(Waiting_Scene, LoadSceneMode.Single);
            }
            else if (result.Equals("Join fail", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Gia nhập phòng thất bại.");
                _ = OpenListRoomPanelAsync(true);
            }
            else
            {
                ShowMessage($"Phản hồi lạ từ server (Join): {result}");
            }
        }

        //  XỬ LÝ PHẢN HỒI TỪ SERVER (CREATE)
        //  Xử lý phản hồi "CREATE|{status}" Port: 8082
        //  (Create_CreateID_button).
        private void HandleResponse_Create(string result) 
        {
            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server.");
                return;
            }

            result = result.Trim();
            if (SixDigits.IsMatch(result))
            {
                if (ID_CreateID_inputfield != null)
                {
                    ID_CreateID_inputfield.text = result;
                    ID_CreateID_inputfield.textComponent.color = Color.green;   // chỉnh màu xanh
                }

                ShowMessage($"Tạo phòng thành công! ID: {result}. Đang chuyển tới phòng chờ...");
                SceneManager.LoadScene(Waiting_Scene, LoadSceneMode.Single);
            }
            else if (result.Equals("Create fail", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Gia nhập phòng thất bại.");
            }
            else
            {
                ShowMessage($"Phản hồi lạ từ server: {result}");
            }
        }

        //  TCP: Gửi/nhận thông điệp
        private async Task<string> SendMessageAsync(string message, string IP, int Port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;

                    await client.ConnectAsync(IP, Port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[4096];
                        int bytesRead = 0;
                        var sb = new StringBuilder();

                        do
                        {
                            if (stream.DataAvailable)
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                                }
                            }
                            else
                            {
                                await Task.Delay(50);
                            }
                        }
                        while (stream.DataAvailable);

                        string response = sb.ToString().Trim();
                        if (string.IsNullOrEmpty(response))
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                                response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        }

                        return response;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TCP Error (IP {IP}) (Port {Port}): {ex.Message}");
                return $"Lỗi: {ex.Message}";
            }
        }

        //  LIST ROOM: gọi server & render
        private async Task LoadListRoomAsync() 
        {
            ClearListRoomUI();
            _selectedRoomId = null;

            if (ListRoom_Panel != null) ListRoom_Panel.SetActive(true);
            if (JoinRoom_Panel != null) JoinRoom_Panel.SetActive(true);

            ShowMessage("Đang tải danh sách phòng public...");

            string result = await SendMessageAsync("ListRoom", serverIP, Port_ListRoom);
            var lines = SplitLines(result);
            int added = 0;
            foreach (var line in lines)
            {
                if (TryParseRoom(line, out var id, out var count))
                {
                    AddListRoomItem(id, count);
                    added++;
                }
            }

            if (added == 0)
                ShowMessage("Không có phòng trống (public, 0/1).");
            else
                ShowMessage("");
        }

        //  Tách chuỗi nhiều dòng thành danh sách các dòng riêng biệt: ListRoomItem
        private List<string> SplitLines(string block)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(block)) return list;
            var raw = block.Replace("\r", "").Split('\n');
            foreach (var s in raw)
            {
                var t = s.Trim();
                if (!string.IsNullOrEmpty(t))
                    list.Add(t);
            }
            return list;
        }

        //  Phân tích cú pháp dòng thành: ID, Count
        private bool TryParseRoom(string line, out string id, out int count)
        {
            id = null; count = -1;
            if (string.IsNullOrEmpty(line)) return false;

            var parts = line.Split(',');
            if (parts.Length != 2) return false;

            var pid = parts[0].Trim();
            var pcount = parts[1].Trim();

            if (SixDigits.IsMatch(pid) && int.TryParse(pcount, out int c) && (c == 0 || c == 1))
            {
                id = pid; count = c; return true;
            }
            return false;
        }

        // Thêm danh sách phòng lên ListRoom_Panel
        private void AddListRoomItem(string id, int count)
        {
            if (ListRoomItem_prefab == null || ListRoom_Content == null)
            {
                Debug.LogWarning("[JoinRoom] Thiếu tham chiếu ListRoomItem_prefab hoặc ListRoom_Content.");
                return;
            }

            var go = Instantiate(ListRoomItem_prefab, ListRoom_Content);
            _spawnedListItems.Add(go);

            TMP_Text tID, tStatus, tCount;
            Button btn;
            BindListItemTexts(go.transform, out tID, out tStatus, out tCount, out btn);

            if (tID) tID.text = id;
            if (tStatus) tStatus.text = "Public";
            if (tCount) tCount.text = $"{count}/2";

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnListRoomSelected(id));
            }
        }

        private void BindListItemTexts(Transform root, out TMP_Text tID, out TMP_Text tStatus, out TMP_Text tCount, out Button btn)
        {
            tID = root.Find("Text_ID")?.GetComponent<TMP_Text>();
            tStatus = root.Find("Text_Status")?.GetComponent<TMP_Text>();
            tCount = root.Find("Text_Count")?.GetComponent<TMP_Text>();
            btn = root.GetComponent<Button>();

            if (tID == null || tStatus == null || tCount == null || btn == null)
            {
                Debug.LogWarning("[JoinRoom] ListRoomItem_prefab thiếu thành phần. Hãy chỉnh BindListItemTexts cho khớp cấu trúc của bạn.");
            }
        }

        //  Xử lý khi chọn một phòng từ danh sách
        private void OnListRoomSelected(string roomId)
        {
            _selectedRoomId = roomId;

            if (ID_ListRoom_inputfield != null)
            {
                ID_ListRoom_inputfield.text = _selectedRoomId;
                ID_ListRoom_inputfield.textComponent.color = Color.black;
            }

            if (ID_EnterID_inputfield != null)
            {
                ID_EnterID_inputfield.text = _selectedRoomId;
                ID_EnterID_inputfield.textComponent.color = Color.black;
            }

            ShowMessage($"Đã chọn phòng {roomId}");
        }

        //  Xóa tất cả mục trong danh sách phòng
        private void ClearListRoomUI()
        {
            for (int i = 0; i < _spawnedListItems.Count; i++)
            {
                var go = _spawnedListItems[i];
                if (go) Destroy(go);
            }
            _spawnedListItems.Clear();
        }

        //  
        private string GetStatusFromGroup()
        {
            if (Status_CreateID_togglegroup != null)
            {
                var active = Status_CreateID_togglegroup.GetFirstActiveToggle();
                if (active != null && StatusPrivate_CreateID_toggle != null && active == StatusPrivate_CreateID_toggle)
                    return "private";
                return "public";
            }

            if (StatusPrivate_CreateID_toggle != null && StatusPrivate_CreateID_toggle.isOn) return "private";
            return "public";
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null)
            {
                messageText.text = msg;
                messageText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
            }
            else
            {
                Debug.Log(msg);
            }
        }

        //  Cleanup
        private void OnDestroy()
        {
            if (Back_button != null) Back_button.onClick.RemoveAllListeners();
            if (Join_button != null) Join_button.onClick.RemoveAllListeners();
            if (Create_button != null) Create_button.onClick.RemoveAllListeners();
            if (JoinID_button != null) JoinID_button.onClick.RemoveAllListeners();

            if (Load_ListRoom_button != null) Load_ListRoom_button.onClick.RemoveAllListeners(); // was: RefreshList_button

            if (Join_EnterID_button != null) Join_EnterID_button.onClick.RemoveAllListeners();
            if (Back_EnterID_button != null) Back_EnterID_button.onClick.RemoveAllListeners();

            if (Back_CreateID_button != null) Back_CreateID_button.onClick.RemoveAllListeners();
            if (Create_CreateID_button != null) Create_CreateID_button.onClick.RemoveAllListeners();
            if (Join_CreateID_button != null) Join_CreateID_button.onClick.RemoveAllListeners();

            if (StatusPublic_CreateID_toggle != null) StatusPublic_CreateID_toggle.onValueChanged.RemoveAllListeners();
            if (StatusPrivate_CreateID_toggle != null) StatusPrivate_CreateID_toggle.onValueChanged.RemoveAllListeners();
        }
    }
}
