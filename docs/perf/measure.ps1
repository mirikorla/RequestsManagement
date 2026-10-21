# Measures the API's main queries on the seeded data (docs/PERFORMANCE.md, "תוצאות").
# For each query: logical reads (SET STATISTICS IO), the actual execution plan (SET STATISTICS XML)
# and the average wall time of 20 runs. Queries are sent with parameters, the same way EF Core sends them.
#
#   powershell -ExecutionPolicy Bypass -File docs\perf\measure.ps1
#   powershell -ExecutionPolicy Bypass -File docs\perf\measure.ps1 -ConnectionString "Server=localhost,1433;Database=RequestsManagement;User Id=sa;Password=...;TrustServerCertificate=True"
param(
  [string]$ConnectionString = 'Server=.\SQLEXPRESS;Database=RequestsManagement;Integrated Security=True'
)

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$script:messages = New-Object System.Collections.Generic.List[string]
$conn.add_InfoMessage({ param($s, $e) $script:messages.Add($e.Message) })
$conn.Open()

$cols = 'Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion'
$page = 'ORDER BY CreatedAt DESC, Id DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY'
$search = "(Title LIKE @t ESCAPE N'\' OR OrganizationName LIKE @t ESCAPE N'\')"
$from = [DateTime]::UtcNow.AddMonths(-3)

$queries = @(
  @{ Name = 'Q0 default view, page 1';               Sql = "SELECT $cols FROM Requests $page"; P = @{ skip = 0; take = 20 } },
  @{ Name = 'Q1 status=InProgress, count';           Sql = 'SELECT COUNT(*) FROM Requests WHERE Status = @status'; P = @{ status = 2 } },
  @{ Name = 'Q1 status=InProgress, page 1';          Sql = "SELECT $cols FROM Requests WHERE Status = @status $page"; P = @{ status = 2; skip = 0; take = 20 } },
  @{ Name = 'Q1 same, index disabled (baseline)';    Sql = "SELECT $cols FROM Requests WITH (INDEX(1)) WHERE Status = @status $page"; P = @{ status = 2; skip = 0; take = 20 } },
  @{ Name = 'Q2 status=InProgress, page 1000';       Sql = "SELECT $cols FROM Requests WHERE Status = @status $page"; P = @{ status = 2; skip = 19980; take = 20 } },
  # Proposed fix for Q2 (not implemented in the API): page the narrow index first, then fetch only 20 full rows.
  @{ Name = 'Q2 page 1000, deferred join (proposal)'; Sql = "SELECT r.Id, r.Title, r.OrganizationName, r.Status, r.Priority, r.AssignedTo, r.CreatedAt, r.UpdatedAt, r.RowVersion FROM Requests r JOIN (SELECT Id FROM Requests WHERE Status = @status $page) p ON p.Id = r.Id ORDER BY r.CreatedAt DESC, r.Id DESC"; P = @{ status = 2; skip = 19980; take = 20 } },
  @{ Name = 'Q3 assignedTo + date range, page 1';    Sql = "SELECT $cols FROM Requests WHERE AssignedTo = @a AND CreatedAt >= @from $page"; P = @{ a = 'agent07'; from = $from; skip = 0; take = 20 } },
  @{ Name = 'Q4 search=Refund, count';               Sql = "SELECT COUNT(*) FROM Requests WHERE $search"; P = @{ t = '%Refund%' } },
  @{ Name = 'Q4 search=Refund, page 1';              Sql = "SELECT $cols FROM Requests WHERE $search $page"; P = @{ t = '%Refund%'; skip = 0; take = 20 } },
  @{ Name = 'Q4 search=rare term, page 1';           Sql = "SELECT $cols FROM Requests WHERE $search $page"; P = @{ t = '%#99999%'; skip = 0; take = 20 } },
  @{ Name = 'Q4 search=rare term, fresh plan';       Sql = "SELECT $cols FROM Requests WHERE $search $page OPTION (RECOMPILE)"; P = @{ t = '%#99999%'; skip = 0; take = 20 } },
  @{ Name = 'Q4 search=Refund + assignedTo, page 1'; Sql = "SELECT $cols FROM Requests WHERE AssignedTo = @a AND $search $page"; P = @{ a = 'agent07'; t = '%Refund%'; skip = 0; take = 20 } },
  @{ Name = 'Q5 summary by status';                  Sql = 'SELECT Status, COUNT(*) FROM Requests GROUP BY Status'; P = @{} },
  @{ Name = 'Q5 summary by priority';                Sql = 'SELECT Priority, COUNT(*) FROM Requests GROUP BY Priority'; P = @{} },
  @{ Name = 'Q6 sort by Title, page 1';              Sql = "SELECT $cols FROM Requests ORDER BY Title DESC, Id DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY"; P = @{ skip = 0; take = 20 } }
)

function New-Command($q, [string]$prefix = '', [string]$suffix = '') {
  $cmd = $conn.CreateCommand()
  $cmd.CommandText = "$prefix $($q.Sql); $suffix"
  foreach ($k in $q.P.Keys) { [void]$cmd.Parameters.AddWithValue("@$k", $q.P[$k]) }
  return $cmd
}

function Get-PlanSummary([string]$planXml) {
  $x = [xml]$planXml
  $ns = New-Object System.Xml.XmlNamespaceManager $x.NameTable
  $ns.AddNamespace('p', 'http://schemas.microsoft.com/sqlserver/2004/07/showplan')
  $ops = foreach ($op in $x.SelectNodes('//p:RelOp', $ns)) {
    if ($op.PhysicalOp -notmatch 'Seek|Scan|Lookup|Sort|Top|Aggregate') { continue }
    $index = $op.SelectSingleNode('./p:IndexScan/p:Object', $ns)
    $label = $op.PhysicalOp
    if ($index -and $index.Index) { $label += '[' + $index.Index.Trim('[', ']') + ']' }
    $counters = $op.SelectNodes('./p:RunTimeInformation/p:RunTimeCountersPerThread', $ns)
    $rows = ($counters | ForEach-Object { [long]$_.ActualRows } | Measure-Object -Sum).Sum
    $execs = ($counters | ForEach-Object { [long]$_.ActualExecutions } | Measure-Object -Sum).Sum
    if ($execs -gt 1) { "$label(rows=$rows, executions=$execs)" } else { "$label(rows=$rows)" }
  }
  return $ops -join ' > '
}

foreach ($q in $queries) {
  # Warm-up so every query is measured with data in memory, like a running system.
  1..2 | ForEach-Object { $r = (New-Command $q).ExecuteReader(); while ($r.Read()) {}; $r.Close() }

  # Reads + actual plan
  $script:messages.Clear()
  $reader = (New-Command $q 'SET STATISTICS IO ON; SET STATISTICS XML ON;' 'SET STATISTICS XML OFF; SET STATISTICS IO OFF;').ExecuteReader()
  $plan = $null
  do { while ($reader.Read()) { if ($reader.GetName(0) -like 'Microsoft SQL Server*') { $plan = $reader.GetString(0) } } } while ($reader.NextResult())
  $reader.Close()
  $reads = ($script:messages | Select-String "Table 'Requests'.*?logical reads (\d+)" -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { [int]$_.Groups[1].Value } | Measure-Object -Sum).Sum

  # Average wall time of 20 runs, without statistics overhead
  $sw = [Diagnostics.Stopwatch]::StartNew()
  1..20 | ForEach-Object { $r = (New-Command $q).ExecuteReader(); while ($r.Read()) {}; $r.Close() }
  $avg = [math]::Round($sw.Elapsed.TotalMilliseconds / 20, 2)

  '{0,-40} reads={1,-7} avg={2,7} ms   {3}' -f $q.Name, $reads, $avg, (Get-PlanSummary $plan)
}
$conn.Close()
