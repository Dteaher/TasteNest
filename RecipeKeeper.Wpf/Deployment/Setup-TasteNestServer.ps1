param(
    [string]$DatabaseName = "RecipeKeeperDb",
    [string]$LoginName = "tastenest_app",
    [string]$LoginPassword = ""
)


$ErrorActionPreference = "Stop"

$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Please run PowerShell as Administrator and start this script again." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

$instanceId = "MSSQL17.SQLEXPRESS"
$tcpRoot = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\MSSQLServer\SuperSocketNetLib\Tcp"
$serverRoot = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\MSSQLServer"
$serverName = "localhost\SQLEXPRESS"
$databaseName = $DatabaseName
$loginName = $LoginName

if ([string]::IsNullOrWhiteSpace($LoginPassword)) {
    $loginPassword = Read-Host "Enter SQL password for client login '$loginName'"
}

if (-not (Test-Path $tcpRoot)) {
    Write-Host "SQL Server Express registry path was not found: $tcpRoot" -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "Enabling TCP/IP on port 1433..." -ForegroundColor Cyan
Set-ItemProperty -Path $tcpRoot -Name Enabled -Value 1
Set-ItemProperty -Path $tcpRoot -Name ListenOnAllIPs -Value 1
Set-ItemProperty -Path (Join-Path $tcpRoot "IPAll") -Name TcpDynamicPorts -Value ""
Set-ItemProperty -Path (Join-Path $tcpRoot "IPAll") -Name TcpPort -Value "1433"

Write-Host "Enabling SQL Server mixed authentication..." -ForegroundColor Cyan
Set-ItemProperty -Path $serverRoot -Name LoginMode -Value 2

if (-not (Get-NetFirewallRule -DisplayName "TasteNest SQL Server 1433" -ErrorAction SilentlyContinue)) {
    Write-Host "Opening Windows Firewall port 1433..." -ForegroundColor Cyan
    New-NetFirewallRule -DisplayName "TasteNest SQL Server 1433" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow | Out-Null
}

Write-Host "Restarting SQL Server Express..." -ForegroundColor Cyan
Restart-Service -Name "MSSQL`$SQLEXPRESS" -Force
Start-Sleep -Seconds 5

Write-Host "Creating database and client login..." -ForegroundColor Cyan
Add-Type -AssemblyName System.Data

$connectionString = "Server=$serverName;Database=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;Connection Timeout=10"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

$sql = @"
DECLARE @DbNameLocal sysname = @DatabaseNameParam;
DECLARE @LoginNameLocal sysname = @LoginNameParam;
DECLARE @LoginPasswordLocal nvarchar(128) = @LoginPasswordParam;
DECLARE @sql nvarchar(max);

IF DB_ID(@DbNameLocal) IS NULL
BEGIN
    SET @sql = N'CREATE DATABASE ' + QUOTENAME(@DbNameLocal) + N';';
    EXEC (@sql);
END;

IF SUSER_ID(@LoginNameLocal) IS NULL
BEGIN
    SET @sql = N'CREATE LOGIN ' + QUOTENAME(@LoginNameLocal)
        + N' WITH PASSWORD = ' + QUOTENAME(@LoginPasswordLocal, '''')
        + N', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;';
    EXEC (@sql);
END
ELSE
BEGIN
    SET @sql = N'ALTER LOGIN ' + QUOTENAME(@LoginNameLocal)
        + N' WITH PASSWORD = ' + QUOTENAME(@LoginPasswordLocal, '''')
        + N', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;';
    EXEC (@sql);

    SET @sql = N'ALTER LOGIN ' + QUOTENAME(@LoginNameLocal) + N' ENABLE;';
    EXEC (@sql);
END;
"@

$command = $connection.CreateCommand()
$command.CommandText = $sql
$command.Parameters.Add("@DatabaseNameParam", [System.Data.SqlDbType]::NVarChar, 128).Value = $databaseName
$command.Parameters.Add("@LoginNameParam", [System.Data.SqlDbType]::NVarChar, 128).Value = $loginName
$command.Parameters.Add("@LoginPasswordParam", [System.Data.SqlDbType]::NVarChar, 128).Value = $loginPassword
$command.ExecuteNonQuery() | Out-Null
$connection.Close()

$dbConnectionString = "Server=$serverName;Database=$databaseName;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;Connection Timeout=10"
$dbConnection = New-Object System.Data.SqlClient.SqlConnection($dbConnectionString)
$dbConnection.Open()

$dbSql = @"
DECLARE @LoginNameLocal sysname = @LoginNameParam;
DECLARE @sql nvarchar(max);

IF USER_ID(@LoginNameLocal) IS NULL
BEGIN
    SET @sql = N'CREATE USER ' + QUOTENAME(@LoginNameLocal) + N' FOR LOGIN ' + QUOTENAME(@LoginNameLocal) + N';';
    EXEC (@sql);
END;

IF IS_ROLEMEMBER(N'db_datareader', @LoginNameLocal) <> 1
BEGIN
    SET @sql = N'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@LoginNameLocal) + N';';
    EXEC (@sql);
END;

IF IS_ROLEMEMBER(N'db_datawriter', @LoginNameLocal) <> 1
BEGIN
    SET @sql = N'ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@LoginNameLocal) + N';';
    EXEC (@sql);
END;
"@

$dbCommand = $dbConnection.CreateCommand()
$dbCommand.CommandText = $dbSql
$dbCommand.Parameters.Add("@LoginNameParam", [System.Data.SqlDbType]::NVarChar, 128).Value = $loginName
$dbCommand.ExecuteNonQuery() | Out-Null
$dbConnection.Close()

Write-Host "TasteNest server setup completed." -ForegroundColor Green
Write-Host "Client login is ready: $loginName / $loginPassword" -ForegroundColor Yellow
Write-Host "Now start TasteNest again." -ForegroundColor Yellow
Read-Host "Press Enter to close"
