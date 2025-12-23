
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