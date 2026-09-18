# Ứng dụng minh hoạ SQL nâng cao

Ứng dụng web trên lược đồ University, dùng cho phần demo của tiểu luận.

Cách chạy, kịch bản trình chiếu và xử lý sự cố: xem
[HUONG_DAN_CHAY.md](HUONG_DAN_CHAY.md).

## Bắt đầu nhanh

Cần .NET SDK 10 và một máy chủ Microsoft SQL Server cho đăng nhập Windows.

```powershell
pwsh sql\00_nap_csdl_demo.ps1   # dựng CSDL demo: lược đồ, dữ liệu, thủ tục, hàm
pwsh chay.ps1                   # chạy ứng dụng, mở http://localhost:5080
```

Máy chỉ có instance SQLEXPRESS và tắt SQL Browser thì thêm tham số
`-Server 'lpc:.\SQLEXPRESS'` cho bước dựng CSDL.

## Bốn chức năng

| Chức năng | Kỹ thuật | Đối tượng trong CSDL |
|---|---|---|
| Cây môn tiên quyết | Thủ tục lưu trữ, CTE đệ quy, tham số đầu ra | `usp_GetPrereqTree` |
| Khối lượng tín chỉ tích luỹ | Hàm vô hướng, hàm trả về bảng dạng nội tuyến | `fn_GetTotalCredits`, `fn_PrereqClosure` |
| Tra cứu sinh viên đa tiêu chí | SQL động qua `sp_executesql` | `usp_SearchStudentsDynamic` |
| Đăng ký lớp học phần | SQL nhúng qua ADO.NET, giao dịch | `usp_EnrollStudent` |

## Lược đồ

Lược đồ trong `database\DDL.sql` và `database\DDL+drop.sql` dùng `nvarchar` cho
các cột chứa văn bản (`name` 50, `dept_name` 40, `building` 30, `title` 100) để
lưu được tiếng Việt có dấu. Cột mang mã định danh vẫn là `varchar`.

Chuỗi hằng tiếng Việt trong script phải mang tiền tố `N`. Thiếu tiền tố đó,
chuỗi bị diễn dịch theo collation mặc định của CSDL trước khi ghi vào cột và
mất dấu, dù cột đã là `nvarchar`. Phía ứng dụng thì tham số khai báo
`SqlDbType.NVarChar` gửi Unicode nên không vướng.

## Cấu trúc

```
UnivSqlDemo.Web/
  Program.cs                    các điểm cuối /api/...
  Data/Db.cs                    nguồn kết nối
  Data/CourseRepository.cs      usp_GetPrereqTree, fn_GetTotalCredits
  Data/StudentRepository.cs     usp_SearchStudentsDynamic, thêm sinh viên
  Data/EnrollmentRepository.cs  đối chiếu điều kiện, đăng ký, huỷ đăng ký
  Data/Models.cs                các kiểu dữ liệu trả về
  Pages/Index.cshtml            giao diện bốn tab
  wwwroot/css/site.css
  wwwroot/js/app.js
database/
  DDL.sql, DDL+drop.sql         lược đồ University
  largeRelationsInsertFile.sql  dữ liệu lớn: 200 môn, 2000 sinh viên, 30000 lượt học
  smallRelationsInsertFile.sql  dữ liệu nhỏ, dễ đọc trên màn chiếu
sql/
  00_nap_csdl_demo.ps1          dựng lại toàn bộ CSDL demo
  03_programmability.sql        năm thủ tục và hàm của ứng dụng
  04_hoc_ky_moi.sql             học kỳ Fall 2026 và ba lớp học phần
chay.ps1                        chạy ứng dụng, kèm tham số -DatLai
RelativeDiagrams/               lược đồ quan hệ
screenshots/                    ảnh chụp bốn chức năng
```

Truy cập dữ liệu chỉ dùng `Microsoft.Data.SqlClient`, không dùng Entity
Framework. Chính tầng ADO.NET này là phần minh hoạ cho SQL nhúng trên slide.

## Nguồn dữ liệu

Lược đồ University và hai tệp dữ liệu mẫu trong `database/` lấy từ tài nguyên
kèm giáo trình *Database System Concepts* (https://www.db-book.com/). Bản trong
kho này đổi các cột văn bản sang `nvarchar` và nới rộng (`name` 50, `dept_name`
40, `building` 30, `title` 100) để lưu được họ tên tiếng Việt đầy đủ.
