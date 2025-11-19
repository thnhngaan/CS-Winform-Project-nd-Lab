using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts;


namespace Assets.Scripts
{
    internal class Client_message
    {
        private string serverIP = "192.168.153.1";
        private int loginPort = 8080;
        private int registerPort = 8081;

        // Kết nối TCP đến server
        private async Task<string> SendMessageAsync(string message, string IP, int Port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(IP, Port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Gửi thông điệp
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(data, 0, data.Length);

                        // Nhận phản hồi
                        byte[] buffer = new byte[2048];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"TCP Error (IP {IP}) (Port {Port}): {ex.Message}"); // Thông báo lỗi trong Unity Console

                return $"Lỗi: {ex.Message}";
            }
        }

        // LOGIN (port 8080)
        public Task<string> LoginAsync(string username, string hashedpassword)
        {
            string message = $"LOGIN|{username}|{hashedpassword}";
            return SendMessageAsync(message, serverIP, loginPort);
        }

        // REGISTER (port 8081)
        public Task<string> RegisterAsync(string username, string hashedpassword, string email, string fullname, string phonenumber)
        {
            string message = $"REGISTER|{username}|{hashedpassword}|{email}|{fullname}|{phonenumber}";
            return SendMessageAsync(message, serverIP, registerPort);
        }

        // LOGOUT (port 8082)
        public Task<string> LogoutAsync(string username)
        {
            string message = $"LOGOUT|{username}";
            return SendMessageAsync(message, serverIP, loginPort);
        }
    }
}
