# Hướng dẫn chạy ứng dụng minh hoạ

Ứng dụng web trên lược đồ University, dùng cho phần demo của tiểu luận. Bốn
chức năng tương ứng bốn kỹ thuật: truy vấn đệ quy, thủ tục và hàm, SQL động,
SQL nhúng.

## Yêu cầu

| Thành phần | Bản đang dùng |
|---|---|
| Microsoft SQL Server | 2025, tại `localhost` |
| .NET SDK | 10.0 |
| CSDL | `demo`, đăng nhập Windows |

Chuỗi kết nối trong `appsettings.json` dùng `Integrated Security=True`, tức
đăng nhập Windows của người đang chạy, nên không cần nhập mật khẩu.

## Chạy

```powershell
cd <thư mục kho>\UnivSqlDemo.Web
dotnet run
```

Mở trình duyệt tới `http://localhost:5080`. Dừng bằng `Ctrl+C`.

Cách gọn hơn:

```powershell
cd <thư mục kho>
pwsh chay.ps1 -DatLai
```

Tham số `-DatLai` đưa dữ liệu học kỳ về trạng thái đầu trước khi chạy, xoá các
lượt đăng ký và sinh viên `SV###` phát sinh ở buổi trước. **Dùng tham số này
trước mỗi lần trình chiếu**, nếu không kịch bản demo sẽ lệch.

Cổng 5080 bận thì đổi bằng `dotnet run --urls http://localhost:5090`.

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

Bốn ca theo thứ tự:

| Sinh viên | Lớp học phần | Kết quả |
|---|---|---|
| SV001 Lê Hoàn Vũ | 353 nhóm 1, Operating Systems | đăng ký thành công |
| SV002 Phan Bảo Ân | 353 nhóm 1 | bị chặn, còn thiếu 5 môn tiên quyết |
| SV002 Phan Bảo Ân | 584 nhóm 2, phòng 10 chỗ đã kín | bị chặn, hết chỗ |
| SV002 Phan Bảo Ân | 584 nhóm 1, phòng 115 chỗ | đăng ký thành công |

Với hai ca bị chặn, bấm *Đối chiếu điều kiện* trước để hiện bảng từng môn tiên
quyết kèm tình trạng, rồi mới bấm *Đăng ký*. Thủ tục huỷ giao dịch và trả về lý
do.

Khối *Thêm sinh viên mới* ở đầu tab nhận họ tên tiếng Việt đầy đủ. Sinh viên
tạo ra nhận mã `SV003` và hiện ngay trong ô chọn, đăng ký được như hai sinh
viên mẫu.

## Chuẩn bị dữ liệu

Chỉ cần khi trang báo *Không kết nối được CSDL demo*, hoặc khi dựng lại từ đầu
trên một máy khác.

```powershell
cd <thư mục kho>
pwsh sql\00_nap_csdl_demo.ps1
```

Kịch bản gỡ thủ tục, hàm và bảng cũ, dựng lược đồ bằng `database\DDL.sql`, nạp
`database\largeRelationsInsertFile.sql`, dựng lại năm đối tượng T-SQL, rồi mở
học kỳ Fall 2026. Mất vài phút vì tệp dữ liệu hơn ba vạn dòng.

Chạy từng bước:

```powershell
sqlcmd -S localhost -E -d demo -f 65001 -i database\DDL+drop.sql
sqlcmd -S localhost -E -d demo -f 65001 -i database\largeRelationsInsertFile.sql
sqlcmd -S localhost -E -d demo -f 65001 -i sql\03_programmability.sql
sqlcmd -S localhost -E -d demo -f 65001 -i sql\04_hoc_ky_moi.sql
```

`DDL+drop.sql` chỉ xoá bảng, không xoá thủ tục và hàm, nên phải gỡ chúng trước.

## Đối chiếu trạng thái đúng

Chạy trên CSDL `demo` để kiểm tra trước buổi trình chiếu:

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

Ba lớp học phần Fall 2026 phải có sức chứa 120, 115 và 10 chỗ, trong đó lớp 584
nhóm 2 đã dùng đủ 10 chỗ.

## Xử lý sự cố

**Trang báo không kết nối được CSDL.** Kiểm tra dịch vụ SQL Server đang chạy, và
CSDL `demo` tồn tại. Sau đó chạy lại phần chuẩn bị dữ liệu ở trên.

**Lỗi build báo tệp bị khoá.** Ứng dụng còn chạy ở một cửa sổ khác và giữ tệp
`UnivSqlDemo.Web.exe`. Đóng cửa sổ đó, hoặc:

```powershell
Get-Process dotnet, UnivSqlDemo.Web -ErrorAction SilentlyContinue | Stop-Process -Force
```

**Đăng ký báo sinh viên đã đăng ký lớp này rồi.** Dữ liệu còn lại từ buổi trước.
Chạy `pwsh chay.ps1 -DatLai`.

**Tên tiếng Việt mất dấu.** Cột văn bản phải là `nvarchar` và chuỗi hằng trong
script phải có tiền tố `N`. Chạy lại `sql\04_hoc_ky_moi.sql`.
