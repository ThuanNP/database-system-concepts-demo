# =============================================================================
# Khởi tạo lại CSDL demo, chạy được cả khi CSDL chưa tồn tại.
#
# Lược đồ và dữ liệu lấy thẳng từ database\ nên không nhân bản tệp; riêng
# largeRelationsInsertFile.sql có hơn ba vạn dòng, phải nạp bằng sqlcmd.
#
# Cách chạy:  pwsh sql\00_nap_csdl_demo.ps1 [-Server <máy chủ>]
#   Máy chỉ có instance SQLEXPRESS và SQL Browser tắt:  -Server 'lpc:.\SQLEXPRESS'
# =============================================================================

param([string]$Server = '.\SQLEXPRESS')

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$db   = 'demo'

# ODBC Driver 18 mặc định mã hoá kết nối; -C tin chứng chỉ tự ký của máy chủ cục bộ.
function Sql([string]$csdl, [string[]]$thamSo) {
    sqlcmd -S $Server -E -C -d $csdl -b @thamSo
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd lỗi trên CSDL $csdl" }
}

Write-Host "==> Tạo CSDL $db nếu chưa có"
Sql master @('-Q', "IF DB_ID('$db') IS NULL CREATE DATABASE $db;")

# Thủ tục và hàm tham chiếu tới bảng nên phải gỡ trước khi xoá bảng.
Write-Host '==> Gỡ thủ tục, hàm và bảng cũ'
Sql $db @('-Q', @"
IF OBJECT_ID('dbo.usp_EnrollStudent','P')         IS NOT NULL DROP PROCEDURE dbo.usp_EnrollStudent;
IF OBJECT_ID('dbo.usp_SearchStudentsDynamic','P') IS NOT NULL DROP PROCEDURE dbo.usp_SearchStudentsDynamic;
IF OBJECT_ID('dbo.usp_GetPrereqTree','P')         IS NOT NULL DROP PROCEDURE dbo.usp_GetPrereqTree;
IF OBJECT_ID('dbo.fn_GetTotalCredits','FN')       IS NOT NULL DROP FUNCTION  dbo.fn_GetTotalCredits;
IF OBJECT_ID('dbo.fn_PrereqClosure','IF')         IS NOT NULL DROP FUNCTION  dbo.fn_PrereqClosure;
-- Bảng tham chiếu đứng trước bảng được tham chiếu, như trong DDL+drop.sql
DROP TABLE IF EXISTS prereq, time_slot, advisor, takes, student, teaches,
                     section, instructor, course, department, classroom;
"@)

$buoc = @(
    @{ Ten = 'Dựng lược đồ';        Tep = "$root\database\DDL.sql" },
    @{ Ten = 'Dữ liệu mẫu bản lớn'; Tep = "$root\database\largeRelationsInsertFile.sql" },
    @{ Ten = 'Thủ tục và hàm';      Tep = "$root\sql\03_programmability.sql" },
    @{ Ten = 'Học kỳ Fall 2026';    Tep = "$root\sql\04_hoc_ky_moi.sql" }
)

foreach ($b in $buoc) {
    Write-Host "==> $($b.Ten)"
    Sql $db @('-f', '65001', '-i', $b.Tep)
}

Write-Host '==> Kiểm tra số bộ từng quan hệ'
Sql $db @('-W', '-Q', @"
SET NOCOUNT ON;
SELECT 'course' AS quan_he, COUNT(*) AS so_bo FROM course
UNION ALL SELECT 'prereq',  COUNT(*) FROM prereq
UNION ALL SELECT 'section', COUNT(*) FROM section
UNION ALL SELECT 'student', COUNT(*) FROM student
UNION ALL SELECT 'takes',   COUNT(*) FROM takes;
"@)
