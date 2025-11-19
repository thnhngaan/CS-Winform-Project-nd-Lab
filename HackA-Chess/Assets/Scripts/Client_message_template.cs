using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Assets.Scripts;



namespace Assets.Scripts
{
    internal class Client_message_template : MonoBehaviour
    {
        public static async Task RunExamples()
        {
            Client_message client = new Client_message();

            // LOGIN
            string username1 = "user1";
            string hashedpassword1 = "hashedpassword2";

            string loginResponse = await client.LoginAsync(username1, hashedpassword1); // template gửi thông điệp LOGIN     
            Debug.Log("[LOGIN] Server response: " + loginResponse);                     // thông báo phản hồi từ server: "Login success" / "Login failed"


            // REGISTER
            string username2 = "user2";
            string hashedpassword2 = "hashedpassword2";
            string email2 = "2@gmail.com";
            string fullname2 = "Full Name";
            string phonenumber2 = "0123456789";

            string registerResponse = await client.RegisterAsync(username2, hashedpassword2, email2, fullname2, phonenumber2);  // template gửi thông điệp REGISTER  
            Debug.Log("[REGISTER] Server response: " + registerResponse);                                                       // thông báo phản hồi từ server: "Register success" / "Register failed"


            // LOGOUT
            string username3 = "user3";

            string logoutResponse = await client.LogoutAsync(username3);    // template gửi thông điệp LOGOUT
            Debug.Log("[LOGOUT] Server response: " + logoutResponse);       // thông báo phản hồi từ server: "..."
        }
    }
}
