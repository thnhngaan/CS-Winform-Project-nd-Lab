
-- Tạo Database
CREATE DATABASE HackAChessDB;
GO

USE HackAChessDB;
GO

-- USERDB (DB Login - Register)
CREATE TABLE UserDB
(
    Username     VARCHAR(50)  NOT NULL PRIMARY KEY,
    Fullname     NVARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Email        VARCHAR(100) NOT NULL,
    Phone        VARCHAR(10)  NOT NULL,
    Elo          INT          NOT NULL DEFAULT 1000,
    TotalWin     INT          NOT NULL DEFAULT 0,
    TotalDraw    INT          NOT NULL DEFAULT 0,
    TotalLoss    INT          NOT NULL DEFAULT 0,
    Avatar       NVARCHAR(255)
);
GO
    
-- ROOM (DB các phòng chơi)
USE HackAChessDB
CREATE TABLE ROOM
(
    RoomID           CHAR(6)     NOT NULL PRIMARY KEY,
    UsernameHost     VARCHAR(50) NOT NULL,
    UsernameClient   VARCHAR(50) NULL,
    NumberPlayer     TINYINT     NOT NULL DEFAULT 1,
    RoomIsFull       BIT         NOT NULL DEFAULT 0,
    IsPublic         BIT         NOT NULL DEFAULT 1,
    CreatedAt        DATETIME    NOT NULL DEFAULT GETDATE(),
    IsClosed         BIT         NOT NULL DEFAULT 0,
    Password         CHAR(4)     NULL,

    CONSTRAINT FK_ROOM_Host
        FOREIGN KEY (UsernameHost) REFERENCES UserDB(Username),

    CONSTRAINT FK_ROOM_Client
        FOREIGN KEY (UsernameClient) REFERENCES UserDB(Username),

    CONSTRAINT CK_ROOM_Password
        CHECK (
            (IsPublic = 1 AND Password IS NULL)
         OR (IsPublic = 0 AND Password IS NOT NULL)
        )
);
GO

-- Friend (Danh sách bạn bè)
USE HackAChessDB
CREATE TABLE Friendship
(
    UserA        VARCHAR(50) NOT NULL,
    UserB        VARCHAR(50) NOT NULL,
    Status       TINYINT     NOT NULL DEFAULT 0, -- 0: pending, 1: accepted
    RequestedBy  VARCHAR(50) NOT NULL,
    CreatedAt    DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt    DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Friendship
        PRIMARY KEY (UserA, UserB),

    CONSTRAINT CK_Friendship_Pair
        CHECK (UserA < UserB),

    CONSTRAINT CK_Friendship_Status
        CHECK (Status IN (0,1)),

    CONSTRAINT FK_Friendship_UserA
        FOREIGN KEY (UserA) REFERENCES UserDB(Username),

    CONSTRAINT FK_Friendship_UserB
        FOREIGN KEY (UserB) REFERENCES UserDB(Username)
);
GO

CREATE INDEX IX_Friendship_UserA ON Friendship(UserA);
CREATE INDEX IX_Friendship_UserB ON Friendship(UserB);
GO

-- Match
USE HackAChessDB
CREATE TABLE MatchHistory
(
    MatchID         INT IDENTITY(1,1) PRIMARY KEY,
    RoomID          CHAR(6)     NOT NULL,
    PlayedAt        DATETIME    NOT NULL DEFAULT GETDATE(),

    Player1         VARCHAR(50) NOT NULL,
    Player2         VARCHAR(50) NOT NULL,

    EloBefore1      INT         NOT NULL,
    EloAfter1       INT         NOT NULL,
    EloBefore2      INT         NOT NULL,
    EloAfter2       INT         NOT NULL,

    WinnerUsername  VARCHAR(50) NULL,
    Result          VARCHAR(20) NOT NULL,

    CONSTRAINT FK_Match_Room
        FOREIGN KEY (RoomID) REFERENCES ROOM(RoomID),

    CONSTRAINT FK_Match_Player1
        FOREIGN KEY (Player1) REFERENCES UserDB(Username),

    CONSTRAINT FK_Match_Player2
        FOREIGN KEY (Player2) REFERENCES UserDB(Username)
);
GO
