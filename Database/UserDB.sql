
-- Tạo Database
CREATE DATABASE HackAChessDB;
GO

USE HackAChessDB;
GO

-- USERDB (DB Login - Register)
CREATE TABLE UserDB 
(
    Username VARCHAR(50) PRIMARY KEY NOT NULL,
    Fullname NVARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(10) NOT NULL,
    Elo INT,
    TotalWin INT,
    TotalDraw INT,
    TotalLoss INT,
    Avatar NVARCHAR(255)
);

GO

-- SESSION (DB các phiên đăng nhập - đăng xuất)
CREATE TABLE SessionDB 
(
    SessionID INT IDENTITY PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    LoginTime DATETIME NOT NULL DEFAULT GETDATE(),
    LogoutTime DATETIME NULL,
    FOREIGN KEY (Username) REFERENCES UserDB(Username)
);

-- ROOM (DB các phòng chơi)
USE HackAChessDB
CREATE TABLE ROOM (
    RoomID       CHAR(6)      NOT NULL PRIMARY KEY,         
    UsernameHost VARCHAR(50)  NOT NULL,                    
    UsernameClient VARCHAR(50) NULL,                        
    NumberPlayer TINYINT      NOT NULL DEFAULT 1,          
    RoomIsFull   BIT          NOT NULL DEFAULT 0,           
    IsPublic     BIT          NOT NULL DEFAULT 1,           
    CreatedAt    DATETIME     NOT NULL DEFAULT GETDATE(),
    IsClosed     BIT          NOT NULL DEFAULT 0,           
    FOREIGN KEY (UsernameHost) REFERENCES UserDB(Username),
    FOREIGN KEY (UsernameClient) REFERENCES UserDB(Username)
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

-- MATCHMOVE (tuỳ chọn - DB lưu nước đi)
CREATE TABLE MatchMoveDB (
    MoveID INT IDENTITY PRIMARY KEY,
    MatchID INT NOT NULL,
    MoveNumber INT NOT NULL,
    Player VARCHAR(50),
    MoveData VARCHAR(255),
    TimeStamp DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MatchID) REFERENCES MatchDB(MatchID)
);