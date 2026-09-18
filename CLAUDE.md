# Ứng dụng demo

Ứng dụng nằm ở `UnivSqlDemo.Web` (.NET 10). Ngày 2026-09-12 đã cài lại .NET SDK 10.0.401
bằng `winget install --id Microsoft.DotNet.SDK.10 --exact`, kèm runtime
`Microsoft.AspNetCore.App` 10.0.12; ứng dụng build không cảnh báo và chạy được với SQLEXPRESS
(`dotnet run --project UnivSqlDemo.Web --urls http://localhost:5080`).
`appsettings.json` trỏ `Server=localhost`; với SQLEXPRESS ghi đè bằng biến môi trường
`ConnectionStrings__Demo=Server=lpc:.\SQLEXPRESS;Database=demo;Integrated Security=True;TrustServerCertificate=True`.

Yêu cầu của đề: ứng dụng web ASP.NET Core + Microsoft SQL Server, truy cập dữ liệu bằng
**ADO.NET** (`Microsoft.Data.SqlClient`) — chính lớp ADO.NET này là hiện thân của "SQL nhúng /
call-level interface" trên slide, nên không thay bằng EF Core cho phần đó. Ứng dụng cần chạm đủ
bốn kỹ thuật: gọi stored procedure, gọi UDF, SQL động tham số hóa qua `sp_executesql`, và CTE
đệ quy.
