/*
  Performance measurements on the seeded 100,000 rows (docs/PERFORMANCE.md).
  Run in SSMS / Azure Data Studio with "Include Actual Execution Plan" (Ctrl+M) enabled,
  or with sqlcmd:  sqlcmd -S localhost -U sa -P "Local_Dev_Passw0rd!" -C -d RequestsManagement -i docs/perf/measure.sql
  The queries are the SQL that EF Core generates for the API (copied from the EF log, parameters inlined as variables).
*/
SET NOCOUNT ON;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

DECLARE @status tinyint = 2;          -- InProgress
DECLARE @skip int = 0, @take int = 20;

PRINT '=== Q1: GET /api/requests?status=InProgress (default sort CreatedAt desc), page 1 ===';
SELECT COUNT(*) FROM Requests WHERE Status = @status;
SELECT Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion
FROM Requests
WHERE Status = @status
ORDER BY CreatedAt DESC, Id DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;

PRINT '=== Q2: same filter, deep page (page 1000) ===';
SET @skip = 999 * 20;
SELECT Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion
FROM Requests
WHERE Status = @status
ORDER BY CreatedAt DESC, Id DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;

PRINT '=== Q3: GET /api/requests?assignedTo=agent07&createdFrom=... (date range + assignee) ===';
DECLARE @assignee nvarchar(100) = N'agent07', @from datetime2(3) = DATEADD(MONTH, -3, SYSUTCDATETIME());
SET @skip = 0;
SELECT COUNT(*) FROM Requests WHERE AssignedTo = @assignee AND CreatedAt >= @from;
SELECT Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion
FROM Requests
WHERE AssignedTo = @assignee AND CreatedAt >= @from
ORDER BY CreatedAt DESC, Id DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;

PRINT '=== Q4: GET /api/requests?search=Refund (LIKE %x% - expected bottleneck) ===';
DECLARE @term nvarchar(100) = N'%Refund%';
SELECT COUNT(*) FROM Requests WHERE Title LIKE @term OR OrganizationName LIKE @term;
SELECT Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion
FROM Requests
WHERE Title LIKE @term OR OrganizationName LIKE @term
ORDER BY CreatedAt DESC, Id DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;

PRINT '=== Q5: GET /api/requests/summary (aggregations, uncached path) ===';
SELECT Status, COUNT(*) FROM Requests GROUP BY Status;
SELECT Priority, COUNT(*) FROM Requests GROUP BY Priority;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
