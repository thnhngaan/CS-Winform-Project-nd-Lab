
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
USE HackAChessDB
CREATE TABLE Friendship (
    UserA       VARCHAR(50) NOT NULL,  --luôn là username đã lower + sort
    UserB       VARCHAR(50) NOT NULL,  --luôn là username đã lower + sort
    Status      TINYINT NOT NULL DEFAULT 0,
    RequestedBy VARCHAR(50) NOT NULL,  --ai là người gửi lời mời (lower) A or B
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Friendship PRIMARY KEY (UserA, UserB),
    CONSTRAINT CK_Friendship_Pair CHECK (UserA < UserB),
    CONSTRAINT CK_Friendship_Status CHECK (Status IN (0,1))
);

CREATE INDEX IX_Friendship_UserA ON Friendship(UserA);
CREATE INDEX IX_Friendship_UserB ON Friendship(UserB);

USE HackAChessDB
GO
ALTER TABLE ROOM ADD Password CHAR(4)
ALTER TABLE ROOM
ADD CONSTRAINT CK_ROOM_Pass
CHECK (
    (IsPublic = 1 AND Password IS NULL)
 OR (IsPublic = 0 AND Password IS NOT NULL)
);

USE HackAChessDB
GO
CREATE TABLE MatchHistory
(
    MatchID        INT IDENTITY(1,1) PRIMARY KEY,
    RoomID         INT NOT NULL,
    PlayedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),

    Player1        NVARCHAR(50) NOT NULL,
    Player2        NVARCHAR(50) NOT NULL,

    EloBefore1     INT NOT NULL,
    EloAfter1      INT NOT NULL,
    EloBefore2     INT NOT NULL,
    EloAfter2      INT NOT NULL,

    WinnerUsername NVARCHAR(50) NULL,  --NULL => DRAW
    Result         NVARCHAR(10) NOT NULL -- 'WIN'/'LOSS'/'DRAW' theo góc nhìn Player1 
);
GO

CREATE INDEX IX_MH_Player1_PlayedAt ON dbo.MatchHistory(Player1, PlayedAt DESC);
CREATE INDEX IX_MH_Player2_PlayedAt ON dbo.MatchHistory(Player2, PlayedAt DESC);
GO