using UnityEngine;

namespace Assets.Scripts
{
    // class static lưu trạng thái
    public static class GameSession
    {
        // màu của mình: "white" hoặc "black"
        public static string MyColor { get; set; } = "white";

        // room đang chơi
        public static string RoomId { get; set; } = "";

        // tên đối thủ
        public static string OpponentName { get; set; } = "";
    }
}
