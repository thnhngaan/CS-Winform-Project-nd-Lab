using UnityEngine;

namespace Assets.Scripts
{
    // class static lưu trạng thái
    public static class GameSession
    {
        public static string MyColor { get; set; } = "white";
        public static string RoomId { get; set; } = "";

        // username đối thủ (server gửi)
        public static string OpponentName { get; set; } = "";

        // ⬇️ THÊM
        public static string MyFullName { get; set; } = "";
        public static string OpponentFullName { get; set; } = "";

    }
}
