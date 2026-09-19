# Hướng dẫn chạy ứng dụng minh hoạ

Ứng dụng web trên lược đồ University, dùng cho phần demo của tiểu luận. Bốn
chức năng tương ứng bốn kỹ thuật: truy vấn đệ quy, thủ tục và hàm, SQL động,
SQL nhúng.

## Chạy nhanh

```powershell
git clone https://github.com/ThuanNP/database-system-concepts-demo.git
cd database-system-concepts-demo
pwsh chay.ps1 -Nap
```

Tham số `-Nap` dựng cơ sở dữ liệu rồi chạy ứng dụng. Lần đầu mất vài phút vì
tệp dữ liệu mẫu hơn ba vạn dòng. Khi màn hình hiện dòng
`Mở http://localhost:5080`, mở địa chỉ đó trong trình duyệt. Dừng bằng `Ctrl+C`.

Những lần sau bỏ `-Nap`:

```powershell
pwsh chay.ps1
```

Máy không có `pwsh` thì thay bằng `powershell`. Kịch bản chạy được trên cả
Windows PowerShell 5.1 lẫn PowerShell 7.

## Thành phần cần cài trước

| Thành phần | Kiểm tra bằng | Nguồn cài |
|---|---|---|
| .NET SDK 10 | `dotnet --version` | `winget install --id Microsoft.DotNet.SDK.10 --exact` |
| Microsoft SQL Server | `Get-Service MSSQL*` | bản Express đủ dùng, tải tại microsoft.com/sql-server |
| sqlcmd | `sqlcmd -?` | đi kèm bản cài SQL Server, nằm trong `Client SDK\ODBC\180\Tools\Binn` |

Sau khi cài phải mở lại cửa sổ PowerShell thì lệnh mới vào `PATH`.

Kịch bản kiểm `dotnet` và `sqlcmd` trước khi chạy, thiếu thành phần nào thì
dừng lại và nêu tên thành phần đó.

Ứng dụng đăng nhập SQL Server bằng tài khoản Windows đang mở phiên làm việc
(`Integrated Security=True`), không cần mật khẩu. Tài khoản đó cần quyền tạo
cơ sở dữ liệu trên máy chủ.

## Máy chủ SQL Server mang tên khác

Mặc định kịch bản nối tới instance `.\SQLEXPRESS`. Liệt kê các instance đang
chạy trên máy:

```powershell
Get-Service MSSQL* | Where-Object Status -eq 'Running'
```

| Dịch vụ hiện ra | Giá trị của -May |
|---|---|
| `MSSQLSERVER` | `'localhost'` |
| `MSSQL$SQLEXPRESS` | `'.\SQLEXPRESS'`, tức giá trị mặc định |
| `MSSQL$TEN_KHAC` | `'.\TEN_KHAC'` |

Chạy với instance mặc định:

```powershell
pwsh chay.ps1 -Nap -May 'localhost'
```

Tham số `-May` áp cho cả bước dựng cơ sở dữ liệu lẫn chuỗi kết nối của ứng
dụng, nên không phải sửa `appsettings.json`.

Trên máy có nhiều instance, `localhost` trỏ tới instance mặc định chứ không
phải `SQLEXPRESS`. Chọn nhầm thì ứng dụng nối được tới máy chủ nhưng báo không
mở được cơ sở dữ liệu `demo`.

## Tham số của chay.ps1

| Tham số | Tác dụng |
|---|---|
| `-Nap` | dựng lại cơ sở dữ liệu `demo` từ đầu rồi chạy |
| `-DatLai` | đưa dữ liệu học kỳ về trạng thái đầu rồi chạy |
| `-May <máy chủ>` | chỉ định máy chủ SQL Server |
| `-Cong <số>` | đổi cổng, chẳng hạn `-Cong 5090` khi 5080 đang bận |

Trước mỗi lần trình chiếu, chạy `pwsh chay.ps1 -DatLai`. Các lượt đăng ký và
sinh viên `SV###` phát sinh ở buổi trước còn lại sẽ làm kịch bản lệch.

## Kịch bản trình chiếu

Ba tab đầu chỉ đọc dữ liệu. Tab thứ tư có ghi dữ liệu nên cần theo đúng thứ tự.

### Tab 1. Cây môn tiên quyết

Thủ tục lưu trữ với CTE đệ quy và tham số đầu ra.

Mở lên đã sẵn môn `353 Operating Systems`, bấm *Tra cứu*. Kết quả: 7 môn tiên
quyết, cây sâu 5 mức, 8 nút.

Đổi *Mức tối đa* xuống 3 rồi tra cứu lại: còn 4 môn, sâu 3 mức, 5 nút, cây bị
cắt ngắn.

### Tab 2. Khối lượng tín chỉ

Hàm vô hướng gọi ngay trong mệnh đề `select`, dùng lại hàm trả về bảng.

Chọn `353`, bấm *Tính tín chỉ*. Kết quả: 27 tín chỉ, gồm 3 tín chỉ của môn và
24 tín chỉ của bảy môn tiên quyết.

### Tab 3. Tra cứu sinh viên

SQL động dựng mệnh đề `where` lúc chạy, thực thi bằng `sp_executesql`.

Gõ `an` vào ô tên, `60` vào ô tín chỉ tối thiểu, **để trống ô khoa**, bấm
*Tra cứu*. Khối mã bên dưới chỉ sinh hai điều kiện.

Chọn thêm một khoa rồi tra cứu lại để thấy điều kiện thứ ba xuất hiện.

### Tab 4. Đăng ký học phần

SQL nhúng qua ADO.NET, giao dịch, và thủ tục kiểm tra điều kiện.

Năm lượt theo thứ tự, cho đủ bốn loại kết quả của thủ tục:

| Sinh viên | Lớp học phần | Kết quả |
|---|---|---|
| SV001 Lê Hoàn Vũ | 353 nhóm 1, Operating Systems | đăng ký thành công |
| SV001 Lê Hoàn Vũ | 353 nhóm 1, bấm lần thứ hai | bị chặn, đã đăng ký lớp này rồi |
| SV002 Phan Bảo Ân | 353 nhóm 1 | bị chặn, còn thiếu 5 môn tiên quyết |
| SV002 Phan Bảo Ân | 584 nhóm 2, phòng 10 chỗ đã kín | bị chặn, lớp đủ 10/10 chỗ |
| SV002 Phan Bảo Ân | 584 nhóm 1, phòng 115 chỗ | đăng ký thành công |

Với các lượt bị chặn, bấm *Đối chiếu điều kiện* trước để hiện bảng từng môn
tiên quyết kèm điểm và tình trạng hoàn thành, kèm số chỗ còn lại của lớp, rồi
mới bấm *Đăng ký*. Thủ tục huỷ giao dịch và trả về lý do.

Khối *Thêm sinh viên mới* ở đầu tab nhận họ tên tiếng Việt đầy đủ. Sinh viên
tạo ra nhận mã `SV003` và hiện ngay trong ô chọn, đăng ký được như hai sinh
viên mẫu.

## Đối chiếu trạng thái đúng

Chạy trên cơ sở dữ liệu `demo` để kiểm tra trước buổi trình chiếu:

```sql
SELECT (SELECT COUNT(*) FROM course)  AS mon,
       (SELECT COUNT(*) FROM student) AS sinh_vien,
       (SELECT COUNT(*) FROM takes)   AS luot_dang_ky,
       (SELECT COUNT(*) FROM sys.objects
        WHERE type IN ('P','FN','IF') AND is_ms_shipped = 0) AS thu_tuc_va_ham;
```

| Giá trị | Đúng khi bằng |
|---|---|
| mon | 200 |
| sinh_vien | 2002 |
| luot_dang_ky | 30019 |
| thu_tuc_va_ham | 5 |

Ba lớp học phần Fall 2026 có sức chứa 120, 115 và 10 chỗ, trong đó lớp 584
nhóm 2 đã dùng đủ 10 chỗ.

## Dựng cơ sở dữ liệu riêng lẻ

`pwsh chay.ps1 -Nap` đã gọi sẵn bước này. Chạy riêng khi cần dựng dữ liệu mà
chưa mở ứng dụng:

```powershell
pwsh sql\00_nap_csdl_demo.ps1                       # instance .\SQLEXPRESS
pwsh sql\00_nap_csdl_demo.ps1 -Server 'localhost'   # instance mặc định
```

Kịch bản gỡ thủ tục, hàm và bảng cũ, dựng lược đồ bằng `database\DDL.sql`, nạp
`database\largeRelationsInsertFile.sql`, dựng lại năm đối tượng T-SQL, rồi mở
học kỳ Fall 2026. Chạy được cả trên cơ sở dữ liệu trống lẫn trên bản đã có.

## Xử lý sự cố

**`dotnet` hoặc `sqlcmd` không phải là lệnh.** Chưa cài, hoặc đã cài nhưng chưa
mở lại cửa sổ PowerShell. Xem mục *Thành phần cần cài trước*.

**Không mở được CSDL demo trên máy chủ ...** Cơ sở dữ liệu chưa dựng, chạy
`pwsh chay.ps1 -Nap`. Nếu máy chủ mang tên khác, xem mục *Máy chủ SQL Server
mang tên khác*.

**CSDL demo có nhưng thiếu thủ tục và hàm.** Lần dựng trước dừng giữa chừng.
Chạy lại `pwsh chay.ps1 -Nap`.

**sqlcmd báo không nhận tham số `-C`.** Tham số này bảo sqlcmd tin chứng chỉ tự
ký của máy chủ cục bộ, cần cho ODBC Driver 18 vì driver này mặc định mã hoá kết
nối. Bản sqlcmd không có tham số đó thì bỏ nó khỏi lệnh.

**Cổng 5080 đang bận.** Chạy `pwsh chay.ps1 -Cong 5090`.

**Lỗi build báo tệp bị khoá.** Ứng dụng còn chạy ở một cửa sổ khác và giữ tệp
`UnivSqlDemo.Web.exe`. Đóng cửa sổ đó, hoặc:

```powershell
Get-Process dotnet, UnivSqlDemo.Web -ErrorAction SilentlyContinue | Stop-Process -Force
```

**Đăng ký báo sinh viên đã đăng ký lớp này rồi.** Dữ liệu còn lại từ buổi trước.
Chạy `pwsh chay.ps1 -DatLai`.

**Tên tiếng Việt mất dấu.** Cột văn bản phải là `nvarchar` và chuỗi hằng trong
script phải mang tiền tố `N`. Chạy lại `pwsh chay.ps1 -DatLai`.
