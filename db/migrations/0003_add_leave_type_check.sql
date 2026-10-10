ALTER TABLE dbo.LeaveRequests
    ADD CONSTRAINT CK_LeaveRequests_LeaveType CHECK (LeaveType IN (N'Annual', N'Sick', N'Unpaid'));
