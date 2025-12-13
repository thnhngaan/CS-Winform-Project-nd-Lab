# CS-Winform-Project-nd-Lab
## Ứng dụng Cờ Vua (Chess Game) – Client / Server

## Thành viên thực hiện:
- Trương Thị Thanh Ngân - 24521137 (Nhóm trưởng).
- Tạ Đức Long - 24521013
- Nguyễn Hữu Nghĩa - 24521144
- Lê Văn Lộc - 24520982
- Nguyễn Hiểu Lam - 24520933

## Mục tiêu đồ án: 
Xây dựng một ứng dụng Cờ vua (Chess Game) nhằm mô phỏng đầy đủ luật chơi cờ vua quốc tế bao gồm những tính năng cơ bản như:
- Đăng ký và đăng nhập tài khoản để sử dụng hệ thống.
- Xem thông tin cá nhân tài khoản người chơi.
- Tạo phòng, mời người chơi khác tham gia.
- Tham gia phòng chơi thông qua danh sách phòng công khai hoặc bằng mã phòng, random.
- Thực hiện thao tác sẵn sàng (ready) trước khi bắt đầu trận đấu.
- Thi đấu trực tuyến với người chơi thông qua mạng LAN, đấu với AI.
- Quy định di chuyển quân cờ tuân thủ đúng luật chơi quốc tế.
- Theo dõi trạng thái trận đấu và kết quả sau khi ván cờ kết thúc.
- Xem bảng xếp hạng người chơi dựa trên chỉ số ELO.
  
## Tổng quan hệ thống
Hệ thống bao gồm 3 thành phần chính:

**Client (Unity):**
  - Hiển thị giao diện trò chơi.
  - Xử lý thao tác người chơi.
  - Gửi và nhận dữ liệu từ server.

**Server (C# WinForms):**
  - Xử lý đăng nhập, đăng ký.
  - Quản lý người chơi online.
  - Quản lý phòng chơi.
  - Điều phối trận đấu.
  - Giao tiếp với cơ sở dữ liệu.

**Database (SQL Server):**
  - Lưu thông tin người dùng.
  - Lưu thống kê ELO, thắng/thua.
  - Lưu thông tin phòng chơi.

==

### Sơ đồ kiến trúc:
Unity Client <-- TCP Socket --> C# WinForms Server <-- SQL --> Database

- Hệ thống hoạt động theo mô hình client–server, trong đó Unity Client đóng vai trò là ứng dụng phía người chơi, còn C# WinForms Server là trung tâm xử lý và điều phối. 
- Khi người chơi khởi động ứng dụng Unity, client sẽ thiết lập kết nối TCP Socket đến server. Mọi thao tác của người chơi như đăng nhập, đăng ký, tạo phòng, tham gia phòng và thực hiện nước đi đều được client gửi lên server dưới dạng các gói tin.
- Server tiếp nhận các yêu cầu này, thực hiện kiểm tra tính hợp lệ, xử lý logic tương ứng và truy vấn hoặc cập nhật dữ liệu trong cơ sở dữ liệu SQL Server khi cần thiết. Sau khi xử lý xong, server gửi kết quả phản hồi về cho client để cập nhật giao diện và trạng thái trò chơi.
- Trong quá trình thi đấu, mỗi nước đi của một người chơi sẽ được gửi lên server, sau đó server chuyển tiếp nước đi này đến đối thủ nhằm đảm bảo trạng thái bàn cờ giữa hai phía luôn được đồng bộ.

== 

## Các chức năng chính:

### 1. Chức năng giao diện
- Hiển thị bàn cờ 8×8.
- Hiển thị các quân cờ và trạng thái quân.
- Giao diện đăng nhập / đăng ký tài khoản.
- Hiển thị thông tin người chơi (username, ELO, số trận thắng/hòa/thua).
- Hiển thị danh sách phòng chơi công khai.
- Thông báo trạng thái trận đấu (đang chờ, bắt đầu, kết thúc).

### 2. Chức năng xử lý logic cờ vua
- Phân biệt lượt đi của bên Trắng và bên Đen.
- Gửi và nhận nước đi giữa hai người chơi.
- Đồng bộ trạng thái bàn cờ thông qua server.
- Xác định kết thúc trận đấu và bên thắng cuộc.

### 3. Chức năng gameplay
- Có 2 chế độ: Đấu với AI hoặc với người chơi.
- Tạo phòng chơi (public / private).
- Tham gia phòng chơi bằng danh sách phòng hoặc ID phòng.
- Cơ chế sẵn sàng (ready) trước khi bắt đầu trận.
- Tự động bắt đầu trận đấu khi đủ 2 người chơi.
- Chơi cờ trực tuyến theo mô hình client–server.

### 4. Chức năng lưu trữ / lịch sử
- Lưu thông tin tài khoản người chơi vào cơ sở dữ liệu.
- Lưu ELO và thống kê số trận thắng, hòa, thua.
- Quản lý danh sách phòng chơi và trạng thái phòng.
- Hiển thị bảng xếp hạng người chơi theo ELO.

### 5. Chức năng mở rộng
- Lưu lịch sử ván đấu.
- AI đánh cờ.


