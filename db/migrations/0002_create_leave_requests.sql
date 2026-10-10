CREATE TABLE dbo.LeaveRequests
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeaveRequests PRIMARY KEY,
    EmployeeId  INT               NOT NULL CONSTRAINT FK_LeaveRequests_Employees REFERENCES dbo.Employees (Id),
    StartDate   DATE              NOT NULL,
    EndDate     DATE              NOT NULL,
    LeaveType   NVARCHAR(20)      NOT NULL,
    Status      NVARCHAR(20)      NOT NULL CONSTRAINT DF_LeaveRequests_Status DEFAULT N'Pending',
    Reason      NVARCHAR(500)     NULL,
    CreatedAt   DATETIME2(3)      NOT NULL CONSTRAINT DF_LeaveRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_LeaveRequests_DateRange CHECK (EndDate >= StartDate),
    CONSTRAINT CK_LeaveRequests_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected', N'Cancelled'))
);

CREATE INDEX IX_LeaveRequests_EmployeeId ON dbo.LeaveRequests (EmployeeId);
