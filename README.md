# CS-Winform-Project-nd-Lab
## Ứng dụng Cờ Vua (Chess Game) – Client / Server

## Thành viên thực hiện:



## Mục tiêu đồ án: 
Xây dựng một ứng dụng Cờ vua (Chess Game) nhằm mô phỏng đầy đủ luật chơi cờ vua quốc tế, đồng thời áp dụng các kiến thức đã học về lập trình hướng đối tượng, lập trình giao diện và lập trình mạng.


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

== 

## Các chức năng chính:

### 1. Chức năng giao diện (Client)
- Hiển thị bàn cờ 8×8
- Hiển thị các quân cờ
- Giao diện đăng nhập / đăng ký
- Hiển thị thông tin người chơi (username, ELO, thống kê)
- Thông báo trạng thái trận đấu

### 2. Chức năng xử lý logic cờ vua
- Di chuyển quân theo đúng luật
- Ăn quân hợp lệ
- Phân biệt lượt chơi Trắng – Đen
- Đồng bộ nước đi giữa hai người chơi

### 3️. Chức năng gameplay
- Tạo phòng chơi (public / private)
- Tham gia phòng chơi
- Cơ chế ready trước khi bắt đầu
- Bắt đầu trận đấu khi đủ người
- Kết thúc trận đấu và thông báo kết quả

### 4️. Chức năng lưu trữ và thống kê
- Quản lý tài khoản người chơi
- Lưu ELO
- Thống kê số trận thắng, hòa, thua
- Hiển thị bảng xếp hạng

### 5. Chức năng mở rộng (định hướng)
- AI đánh cờ
- Lưu và phát lại lịch sử ván đấu
- Mở rộng hệ thống xếp hạng
- Cải thiện UI/UX



