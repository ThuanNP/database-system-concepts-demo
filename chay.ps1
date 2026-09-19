# =============================================================================
# Chạy ứng dụng minh hoạ.
#
#   pwsh chay.ps1 -Nap            lần đầu: dựng CSDL rồi chạy
#   pwsh chay.ps1                 các lần sau: chạy ngay
#   pwsh chay.ps1 -DatLai         đặt lại dữ liệu học kỳ rồi chạy
#   pwsh chay.ps1 -May 'localhost'    dùng máy chủ SQL Server khác
#   pwsh chay.ps1 -Cong 5090      đổi cổng khi 5080 đang bận
#
# Tham số -May áp cho cả bước dựng CSDL lẫn chuỗi kết nối của ứng dụng, nên
# không phải sửa appsettings.json.
#
# Tham số -DatLai xoá các lượt đăng ký và sinh viên SV### phát sinh trong buổi
# trước. Dùng trước mỗi lần trình chiếu, nếu không kịch bản demo sẽ lệch.
#
# Chạy được bằng cả pwsh (PowerShell 7) lẫn powershell (Windows PowerShell 5.1).
# =============================================================================
param(
    [switch] $Nap,
    [switch] $DatLai,
    [string] $May  = '.\SQLEXPRESS',
    [int]    $Cong = 5080
)

$ErrorActionPreference = 'Stop'

$goc = $PSScriptRoot
$web = Join-Path $goc 'UnivSqlDemo.Web'

function CoLenh([string]$ten) {
    return [bool](Get-Command $ten -ErrorAction SilentlyContinue)
}

# In thông báo rồi dừng hẳn, thay cho khối lỗi nhiều dòng của PowerShell.
function Dung([string]$thongBao) {
    Write-Host ''
    Write-Host $thongBao -ForegroundColor Red
    exit 1
}

# sqlcmd ghi lỗi ra luồng stderr; với $ErrorActionPreference = 'Stop', PowerShell
# biến dòng đó thành lỗi kết thúc và nuốt mất thông báo bên dưới. Hàm này hạ mức
# xử lý lỗi trong lúc gọi, rồi trả về cả kết quả lẫn mã thoát.
function ChaySqlcmd([string[]]$thamSo) {
    $cu = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    # 5.1 gói lỗi của lệnh ngoài thành ErrorRecord kèm cả vị trí gọi; chỉ giữ phần thông báo.
    $ketQua = & sqlcmd -S $May -E -C @thamSo 2>&1 | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" }
    }
    $ma = $LASTEXITCODE
    $ErrorActionPreference = $cu
    return [pscustomobject]@{ KetQua = ($ketQua | Out-String); Ma = $ma }
}

if (-not (CoLenh 'dotnet')) {
    Dung @"
Không thấy lệnh dotnet. Cài .NET SDK 10 rồi mở lại cửa sổ PowerShell:
    winget install --id Microsoft.DotNet.SDK.10 --exact
"@
}

if (-not (CoLenh 'sqlcmd')) {
    Dung @"
Không thấy lệnh sqlcmd. Công cụ này đi kèm bản cài Microsoft SQL Server; cài
SQL Server rồi mở lại cửa sổ PowerShell.
"@
}

if ($Nap) {
    Write-Host "==> Dựng CSDL univdb trên $May (mất vài phút vì tệp dữ liệu hơn ba vạn dòng)"
    & (Join-Path $goc 'sql\00_nap_univdb.ps1') -Server $May
}

# Soát trước khi chạy: nối được CSDL và đủ năm thủ tục, hàm. Thiếu thì báo ngay,
# thay vì để ứng dụng mở lên rồi mọi trang đều báo lỗi.
$soat = ChaySqlcmd @('-d', 'univdb', '-h', '-1', '-W', '-Q',
    "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.objects WHERE type IN ('P','FN','IF') AND is_ms_shipped = 0;")
$dem = $soat.KetQua

if ($soat.Ma -ne 0) {
    Dung @"
Không mở được CSDL univdb trên máy chủ '$May'.
    $($dem.Trim())

Lần đầu chạy trên máy này thì dựng CSDL trước:
    pwsh chay.ps1 -Nap

Máy chủ SQL Server mang tên khác thì chỉ rõ bằng -May. Liệt kê các instance đang chạy:
    Get-Service MSSQL* | Where-Object Status -eq 'Running'
Tên 'MSSQLSERVER' ứng với -May 'localhost'; tên 'MSSQL`$ABC' ứng với -May '.\ABC'.
"@
}

if ($dem.Trim() -ne '5') {
    Dung @"
CSDL univdb có nhưng thiếu thủ tục và hàm (đếm được $($dem.Trim()), cần 5).
Dựng lại bằng:
    pwsh chay.ps1 -Nap -May '$May'
"@
}

if ($DatLai) {
    Write-Host '==> Đặt lại dữ liệu học kỳ Fall 2026'
    $dat = ChaySqlcmd @('-d', 'univdb', '-f', '65001', '-b', '-i', (Join-Path $goc 'sql\04_hoc_ky_moi.sql'))
    if ($dat.Ma -ne 0) { Dung "Không đặt lại được dữ liệu.`n$($dat.KetQua.Trim())" }
}

# Biến môi trường này đè lên ConnectionStrings:UnivDb trong appsettings.json, nên
# tham số -May áp được cho ứng dụng mà không phải sửa tệp cấu hình.
$env:ConnectionStrings__UnivDb =
    "Server=$May;Database=univdb;Integrated Security=True;TrustServerCertificate=True;Application Name=UnivSqlDemo"

Write-Host ''
Write-Host "==> Mở http://localhost:$Cong trong trình duyệt. Dừng bằng Ctrl+C."
Set-Location $web
dotnet run --urls "http://localhost:$Cong"
