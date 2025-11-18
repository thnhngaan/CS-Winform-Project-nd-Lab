
-- Tạo Database
CREATE DATABASE UserDB;
GO

USE UserDB;
GO

-- USERDB (DB Login - Register)
CREATE TABLE UserDB 
(
    Username VARCHAR(50) PRIMARY KEY NOT NULL,
    Fullname VARCHAR(100),
    PasswordHash VARCHAR(255),
    Email VARCHAR(100),
    Phone VARCHAR(20)
);
GO

-- SESSION (DB các phiên đăng nhập - đăng xuất)
CREATE TABLE SessionDB 
(
    SessionID INT IDENTITY PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Token VARCHAR(255) NOT NULL,
    LoginTime DATETIME NOT NULL DEFAULT GETDATE(),
    LogoutTime DATETIME NULL,
    FOREIGN KEY (Username) REFERENCES UserDB(Username)
);

-- ROOM (DB các phòng chơi)
CREATE TABLE RoomDB 
(
    RoomID INT IDENTITY PRIMARY KEY,
    RoomName VARCHAR(50),
    MaxPlayers INT DEFAULT 2,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- ROOMPARTICIPANT (DB user tham gia phòng)
CREATE TABLE RoomParticipantDB 
(
    ParticipantID INT IDENTITY PRIMARY KEY,
    RoomID INT NOT NULL,
    Username VARCHAR(50) NOT NULL,
    JoinTime DATETIME NOT NULL DEFAULT GETDATE(),
    LeaveTime DATETIME NULL,
    FOREIGN KEY (RoomID) REFERENCES RoomDB(RoomID),
    FOREIGN KEY (Username) REFERENCES UserDB(Username)
);

-- MATCH (DB trận đấu)
CREATE TABLE MatchDB 
(
    MatchID INT IDENTITY PRIMARY KEY,
    RoomID INT NOT NULL,
    Player1 VARCHAR(50),
    Player2 VARCHAR(50),
    Winner VARCHAR(50),
    StartTime DATETIME DEFAULT GETDATE(),
    EndTime DATETIME NULL,
    FOREIGN KEY (RoomID) REFERENCES RoomDB(RoomID)
);
GO

-- MATCHMOVEDB (tuỳ chọn - DB lưu nước đi)
CREATE TABLE MatchMoveDB (
    MoveID INT IDENTITY PRIMARY KEY,
    MatchID INT NOT NULL,
    MoveNumber INT NOT NULL,
    Player VARCHAR(50),
    MoveData VARCHAR(255),
    TimeStamp DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MatchID) REFERENCES MatchDB(MatchID)
);

-- User mẫu
INSERT INTO UserDB (Username, Fullname, PasswordHash, Email, Phone)
VALUES 
('lam','Hieu Lam','pass123','lam@test.com','0123456789'),
('long','Duc Long','long123','long@test.com','0987654321');

-- Phiên đăng nhập
INSERT INTO SessionDB (Username, Token)
VALUES
('lam','token_lam_001'),
('long','token_long_001');

-- Phòng mẫu
INSERT INTO RoomDB (RoomName, MaxPlayers)
VALUES
('Room 01',2),
('Room 02',2);

-- Tham gia phòng
INSERT INTO RoomParticipantDB (RoomID, Username)
VALUES
(1,'lam'),
(1,'long');

-- Trận đấu mẫu
INSERT INTO MatchDB (RoomID, Player1, Player2, Winner)
VALUES
(1,'lam','long',NULL);

-- Nước đi mẫu
INSERT INTO MatchMoveDB (MatchID, MoveNumber, Player, MoveData)
VALUES
(1,1,'lam','e2e4');

SELECT * FROM UserDB;

SELECT * FROM SessionDB;

SELECT * FROM RoomDB;

SELECT * FROM RoomParticipantDB;

SELECT * FROM MatchDB;

SELECT * FROM MatchMoveDB;

