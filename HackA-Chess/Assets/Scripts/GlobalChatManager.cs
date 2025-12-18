using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Assets.Scripts
{
    public class GlobalChatManager : MonoBehaviour
    {
        [SerializeField] Button sendmsg;
        [SerializeField] TMP_InputField inputmsg;
        [SerializeField] TMP_Text username;

        [SerializeField] TMP_Text chatlogtext;
        [SerializeField] ScrollRect scrollrect;

        [SerializeField] private int MaxLines = 200;

        private CancellationTokenSource cts;
        private void Awake()
        {
            sendmsg.onClick.AddListener(() => _ = SendMsgChatGlobal());
        }
        private void OnEnable()
        {
            DontDestroyOnLoad(this);
            NetworkClient.Instance.OnLine -= HandleServerMessage;
            NetworkClient.Instance.OnLine += HandleServerMessage;
        }

        private void OnDisable()
        {
            DontDestroyOnLoad(this);
            NetworkClient.Instance.OnLine -= HandleServerMessage;
        }
        private async System.Threading.Tasks.Task SendMsgChatGlobal()
        {
            string user = username.text.Trim();
            string msg = inputmsg.text.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            string msgchat = $"CHATGLOBAL|{user}|{msg}";
            await NetworkClient.Instance.SendAsync(msgchat);
            inputmsg.text = "";
        }

        private void HandleServerMessage(string line)
        {
            var parts = line.Split(new[] { '|' }, 3);
            if (parts.Length >= 3 && parts[0].Equals("CHATGLOBAL", StringComparison.OrdinalIgnoreCase))
            {
                AppendToChat(parts[1], parts[2]);
            }
        }

        private void AppendToChat(string user, string msg)
        {
            if (user == username.text) {
                chatlogtext.text += $"<align=\"right\"><color=#666666>{user}: </color><color=#1A1A1A>{msg}</color></align>";
            }
            else { 
                chatlogtext.text+= $"<align=\"left\"><color=#FF8C00>{user}: </color><color=#1A1A1A>{msg}</color></align>"; }
            chatlogtext.text += "\n";
            chatlogtext.text += "<align=\"left\"></align>";

            chatlogtext.ForceMeshUpdate();
           
        }


        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}