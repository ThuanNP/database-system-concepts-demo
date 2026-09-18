# =============================================================================
# Chạy ứng dụng minh hoạ.
#
#   pwsh chay.ps1            chạy ngay
#   pwsh chay.ps1 -DatLai    đưa dữ liệu học kỳ về trạng thái đầu rồi chạy
#
# Tham số -DatLai xoá các lượt đăng ký và sinh viên SV### phát sinh trong buổi
# trước, dùng trước mỗi lần trình chiếu.
# =============================================================================
param([switch]$DatLai)

$ErrorActionPreference = 'Stop'

$goc = $PSScriptRoot
$web = Join-Path $goc 'UnivSqlDemo.Web'

if ($DatLai) {
    Write-Host '==> Đặt lại dữ liệu học kỳ Fall 2026'
    sqlcmd -S localhost -E -d demo -f 65001 -b -i (Join-Path $goc 'sql\04_hoc_ky_moi.sql')
    if ($LASTEXITCODE -ne 0) { throw 'Không đặt lại được dữ liệu' }
}

Write-Host '==> Mở http://localhost:5080'
Set-Location $web
dotnet run
