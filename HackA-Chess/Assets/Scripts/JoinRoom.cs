
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class JoinRoom : MonoBehaviour
    {
       
        [SerializeField] private TMP_FontAsset roomItemFont;


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

        [Header("Password (Private)")]
        [SerializeField] private GameObject Password_CreateID_Panel;
        [SerializeField] private TMP_InputField Password_CreateID_input;


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
            //reload list room
            if (Load_ListRoom_button != null)
            {
                Load_ListRoom_button.onClick.RemoveAllListeners();  
                Load_ListRoom_button.onClick.AddListener(() => _ = OpenListRoomPanelAsync(true));
            }
            // JoinRoom buttons
            if (Back_button != null) Back_button.onClick.AddListener(OnBack_buttonClicked); 
            if (Join_button != null) Join_button.onClick.AddListener(OnJoin_buttonClicked); 
            if (Create_button != null) Create_button.onClick.AddListener(OpenCreateIDPanel);
            if (JoinID_button != null)
            {
                JoinID_button.onClick.RemoveAllListeners();
                JoinID_button.onClick.AddListener(OpenEnterIDPanel);
            }

            // EnterID
            if (Join_EnterID_button != null)
            {
                Join_EnterID_button.onClick.RemoveAllListeners();
                Join_EnterID_button.onClick.AddListener(OnJoin_EnterID_buttonClicked);
            }
            if (Back_EnterID_button != null)
            {
                Back_EnterID_button.onClick.RemoveAllListeners();
                Back_EnterID_button.onClick.AddListener(CloseEnterIDPanel);
            }

            // CreateID
            if (Back_CreateID_button != null)
            {
                Back_CreateID_button.onClick.RemoveAllListeners();
                Back_CreateID_button.onClick.AddListener(OnBack_CreateID_buttonClicked);
            }

            if (Create_CreateID_button != null)
            {
                Create_CreateID_button.onClick.RemoveAllListeners();
                Create_CreateID_button.onClick.AddListener(OnCreate_CreatID_buttonClicked);
            }

            if (Join_CreateID_button != null) Join_CreateID_button.onClick.AddListener(OnJoin_CreateID_buttonClicked);

            // Status toggles (Create_ID)
            if (StatusPublic_CreateID_toggle != null)
                StatusPublic_CreateID_toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        status = "public";
                        RefreshPasswordUI();
                    }
                });

            if (StatusPrivate_CreateID_toggle != null)
                StatusPrivate_CreateID_toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        status = "private";
                        RefreshPasswordUI();
                    }
                });

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
            if (refresh)                                              
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
        //  Gửi "Join|{ID}
        //  Nhận "Join success"/"Join fail"
        private async void OnJoin_buttonClicked() 
        {
            if (!string.IsNullOrEmpty(_selectedRoomId) && SixDigits.IsMatch(_selectedRoomId))
            {
                ShowMessage($"Đang gửi JOIN|{_selectedRoomId} ...");
                string result = await SendMessageAsync($"JOIN|{_selectedRoomId}", "JOIN|");
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
            ShowMessage("Đang gửi yêu cầu JOINID...");

            string result = await SendMessageAsync($"JOINID|{id}", "JOINID|");
            HandleResponse_JoinID(result); 
        }

        // CreateID_Panel (MỞ - Create_button)
        private void OpenCreateIDPanel()
        {
            // Đóng 3 panel
            CreateID_Panel.transform.SetAsLastSibling();           
            CreateID_Panel.SetActive(true);
            if (StatusPublic_CreateID_toggle != null) StatusPublic_CreateID_toggle.isOn = true;
            status = GetStatusFromGroup();
            RefreshPasswordUI();

        }

        // CreateID_Panel (ĐÓNG - Back_CreateID_button)
        private async void OnBack_CreateID_buttonClicked() 
        {
            if (CreateID_Panel != null) CreateID_Panel.SetActive(false);
            if (JoinRoom_Panel != null) JoinRoom_Panel.SetActive(true);

            await OpenListRoomPanelAsync(true);
        }

        //  CreateID_Panel (Create_CreateID_button)
        //  Gửi "CREATE|{status}"
        //  Nhận: "{ID 6 chữ số}" / "Create fail"
        private async void OnCreate_CreatID_buttonClicked()
        {
            status = GetStatusFromGroup();
            string result;

            string pass = (Password_CreateID_input.text ?? "").Trim();

            // đúng yêu cầu: 4 chữ số
            if (pass.Length != 4 || !pass.All(char.IsDigit))
            {
                MessageBoxManager.Instance.ShowMessageBox("BÁO LỖI", "Password phải đúng 4 chữ số.");
                Password_CreateID_input.ActivateInputField();
                return;
            }

            ShowMessage("Đang gửi yêu cầu CREATE (private)...");
            result = await SendMessageAsync($"CREATE|private|{pass}", "CREATE|");
            HandleResponse_Create(result);
            return;


            ShowMessage($"Đang gửi yêu cầu CREATE ({status})...");
            result = await SendMessageAsync($"CREATE|{status}", "CREATE|");
            HandleResponse_Create(result);
        }

        //  CreateID_Panel (Join_CreateID_button)
        //  Nhận: "JoinID success"/"JoinID fail"
        //JOINID
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

            string result = await SendMessageAsync($"JOINID|{id}", "JOINID|");
            HandleResponse_JoinID(result); 
        }

        //  Xử lý phản hồi "JoinID|{ID}"
        //  (Join_EnterID_button / Join_CreateID_button).
        private void HandleResponse_JoinID(string result)
        {

            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server.");
                return;
            }

            // Lấy ID mà user vừa nhập (hoặc từ ô CreateID)
            string roomId = null;
            if (ID_EnterID_inputfield != null && !string.IsNullOrWhiteSpace(ID_EnterID_inputfield.text))
                roomId = ID_EnterID_inputfield.text.Trim();
            else if (ID_CreateID_inputfield != null && !string.IsNullOrWhiteSpace(ID_CreateID_inputfield.text))
                roomId = ID_CreateID_inputfield.text.Trim();

            // Trim & tách dòng (phòng khi server gửi nhiều message dính nhau)
            result = result.Trim();
            
            Debug.Log($"[JoinRoom] JoinID Room {roomId}: {result}");

            // ───────── JOIN SUCCESS ─────────
            if (result.Equals("JOINID|SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                // LƯU ROOM ID CHO SESSION
                if (!string.IsNullOrEmpty(roomId))
                    GameSession.RoomId = roomId;

                ShowMessage("Gia nhập thành công! Đang chuyển tới phòng chờ...");
                SceneManager.LoadScene(Waiting_Scene, LoadSceneMode.Single);
                return;
            }

            // ───────── JOIN FAIL ─────────
            if (result.Equals("JOINID|FAILED", StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Gia nhập phòng thất bại.");
                return;
            }
            // ───────── Phản hồi không hiểu ─────────
            ShowMessage($"Phản hồi lạ từ server (JoinID): {result}");
        }

        //  Xử lý phản hồi "Join|{ID}"
        //  (Join_button).
        private void HandleResponse_Join(string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server (Join).");
                return;
            }

            result = result.Trim();
            if (result.Equals("JOIN|SUCCESS", System.StringComparison.OrdinalIgnoreCase))
            {
                // LƯU ROOM ID CHO SESSION (dùng phòng đã chọn trong list)
                if (!string.IsNullOrEmpty(_selectedRoomId))
                    GameSession.RoomId = _selectedRoomId;
                Debug.Log("[JoinRoom] Join success. Set GameSession.RoomId = " + GameSession.RoomId);
                ShowMessage("Gia nhập thành công! Đang chuyển tới phòng chờ...");
                SceneManager.LoadScene(Waiting_Scene, LoadSceneMode.Single);
            }
            else if (result.Equals("JOIN|FAILED", System.StringComparison.OrdinalIgnoreCase))
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
        //  Xử lý phản hồi "CREATE|{status}"
        //  (Create_CreateID_button).
        private void HandleResponse_Create(string result) 
        {
            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server.");
                return;
            }

            // Trim trước
            result = result.Trim();
            string[] parts = result.Split('|');
            if (result.Equals("CREATE|FAILED", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Gia nhập phòng thất bại.");
            }
            // Nếu sau khi Trim mà rỗng -> coi như không nhận được gì
            if (string.IsNullOrEmpty(result))
            {
                ShowMessage("Không nhận được phản hồi từ server.");
                return;
            }

            if (SixDigits.IsMatch(parts[1].Trim()))
            {
                // LƯU ROOM ID CHO SESSION
                GameSession.RoomId = parts[1].Trim();

                if (ID_CreateID_inputfield != null)
                {
                    ID_CreateID_inputfield.text = parts[1].Trim();
                    ID_CreateID_inputfield.textComponent.color = Color.green;
                }

                ShowMessage($"Tạo phòng thành công! ID: {parts[1].Trim()}. Đang chuyển tới phòng chờ...");
                SceneManager.LoadScene(Waiting_Scene, LoadSceneMode.Single);
            }
            else
            {
                ShowMessage($"Phản hồi lạ từ server: {result}");
            }
        }

        //Gửi/nhận thông điệp
        private async Task<string> SendMessageAsync(string message, string waitPrefix = null, int timeoutMs = 5000)
        {
            waitPrefix.Trim(); 
            try
            {
                if (!NetworkClient.Instance.IsConnected)
                    return "Lỗi: Chưa kết nối tới server";

                await NetworkClient.Instance.SendAsync(message);

                string prefix = waitPrefix ?? string.Empty;

                string response = await NetworkClient.Instance.WaitForPrefixAsync(prefix, timeoutMs);

                if (string.IsNullOrEmpty(response))
                    return "Lỗi: Không nhận được dữ liệu (timeout hoặc mất kết nối).";

                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"TCP Error: {ex.Message}");
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

            string result = await SendMessageAsync("ListRoom","ListRoom|");
            string[]rooms=result.Split('|');
            for (int i = 1; i < rooms.Length; i++)
            {
                string[] room = rooms[i].Split(",");
                AddListRoomItem(room[0], int.Parse(room[1]), room[2], int.Parse(room[3]));
            }
        }
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
        private bool TryParseRoom(string line, out string id, out int count, out string hostName, out int hostElo)
        {
            id = null;
            count = -1;
            hostName = null;
            hostElo = 0;

            if (string.IsNullOrEmpty(line)) return false;

            var parts = line.Split(',');
            if (parts.Length < 4) return false;   // RoomID,NumberPlayer,HostUsername,HostElo

            var pid = parts[0].Trim();
            var pcount = parts[1].Trim();
            var phost = parts[2].Trim();
            var phostElo = parts[3].Trim();

            if (!SixDigits.IsMatch(pid)) return false;
            if (!int.TryParse(pcount, out int c)) return false;
            if (c != 0 && c != 1) return false;
            if (!int.TryParse(phostElo, out int elo)) elo = 1200;   // fallback

            id = pid;
            count = c;
            hostName = string.IsNullOrEmpty(phost) ? "Unknown" : phost;
            hostElo = elo;
            return true;
        }

        // Tạo nhanh 1 TMP_Text con
        private TMP_Text CreateTmpText(Transform parent, string name, string text, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            if (parent == null)
            {
                Debug.LogError("[JoinRoom] CreateTmpText: parent == null");
                return null;
            }

            var goText = new GameObject(name, typeof(RectTransform));
            goText.transform.SetParent(parent, false);

            var tmp = goText.AddComponent<TextMeshProUGUI>();

            if (roomItemFont != null)
                tmp.font = roomItemFont;   //sử dụng font đã gắn vào ở trên

            tmp.text = text ?? string.Empty;
            tmp.alignment = alignment;
            tmp.fontSize = 35;

            var rt = goText.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 70);

            return tmp;
        }

        // Thêm 1 item phòng lên ListRoom_Panel HOÀN TOÀN BẰNG CODE
        private void AddListRoomItem(string id, int count, string hostName, int hostElo)
        {
            if (ListRoom_Content == null)
            {
                Debug.LogWarning("[JoinRoom] Thiếu tham chiếu ListRoom_Content.");
                return;
            }

            Debug.Log($"[JoinRoom] AddListRoomItem: id={id}, count={count}, host={hostName}, elo={hostElo}");

            //Tạo GameObject gốc cho 1 dòng phòng
            var go = new GameObject($"Room_{id}", typeof(RectTransform));
            go.transform.SetParent(ListRoom_Content, false);
            _spawnedListItems.Add(go);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 90); //height dòng, width để 0 cho layout parent lo

            //Thêm Image + Button để click chọn phòng
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.05f); // hơi xám nhẹ cho có nền

            var btn = go.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnListRoomSelected(id));

            //layout ngang cho các cột: ID | Host | EloHost | Status | Count
            var hLayout = go.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlHeight = true;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.spacing = 10;
            hLayout.padding = new RectOffset(10, 10, 5, 5);

            //ID
            var tID = CreateTmpText(go.transform, "Text_ID", id, TextAlignmentOptions.Left);

            //hostName
            var tHost = CreateTmpText(go.transform, "Text_Host", hostName, TextAlignmentOptions.Left);

            //elo host
            var tElo = CreateTmpText(go.transform, "Text_Elo", hostElo.ToString(), TextAlignmentOptions.Left);

            //trạng thái
            var sStatus = CreateTmpText(go.transform, "Text_Status", "PUBLIC", TextAlignmentOptions.Center);

            //số player (ví dụ: "1/2")
            var tCount = CreateTmpText(go.transform, "Text_Count", $"{count}/2", TextAlignmentOptions.Center);

            //LayoutElement cho từng cột
            if (tID != null)
            {
                var leID = tID.gameObject.AddComponent<LayoutElement>();
                leID.preferredWidth = 300f;
            }
            if (tHost != null)
            {
                var leHost = tHost.gameObject.AddComponent<LayoutElement>();
                leHost.preferredWidth = 500f;
            }
            if (tElo != null)
            {
                var leElo = tElo.gameObject.AddComponent<LayoutElement>();
                leElo.preferredWidth = 300f;
            }
            if (sStatus != null)
            {
                var leStatus = sStatus.gameObject.AddComponent<LayoutElement>();
                leStatus.preferredWidth = 400f;
            }
            if (tCount != null)
            {
                var leCount = tCount.gameObject.AddComponent<LayoutElement>();
                leCount.preferredWidth= 400f;
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
            if (ListRoom_Content != null)
            {
                for (int i = ListRoom_Content.childCount - 1; i >= 0; i--)
                {
                    var child = ListRoom_Content.GetChild(i);
                    Destroy(child.gameObject);
                }
            }
            _spawnedListItems.Clear();
        }

        private void RefreshPasswordUI()
        {
            string s = GetStatusFromGroup();
            bool isPrivate = s.Equals("private", StringComparison.OrdinalIgnoreCase);

            if (Password_CreateID_Panel != null)
                Password_CreateID_Panel.SetActive(isPrivate);

            if (isPrivate && Password_CreateID_input != null)
            {
                // setup cho pass 4 số
                Password_CreateID_input.characterLimit = 4;
                Password_CreateID_input.contentType = TMP_InputField.ContentType.IntegerNumber;

                Password_CreateID_input.text = "";
                Password_CreateID_input.ActivateInputField(); 
            }
            if (!isPrivate && Password_CreateID_input != null)
                Password_CreateID_input.text = ""; //public thì xoá
        }

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


