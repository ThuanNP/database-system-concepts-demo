using UnivSqlDemo.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<Db>();
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<StudentRepository>();
builder.Services.AddScoped<EnrollmentRepository>();

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();

// =====================================================================
// Chức năng 1. Cây môn tiên quyết
// Thủ tục lưu trữ với CTE đệ quy và tham số đầu ra.
// =====================================================================
app.MapGet("/api/cay-tien-quyet", async (
    CourseRepository kho, string courseId, int? mucToiDa, CancellationToken ct) =>
    Results.Ok(await kho.LayCayTienQuyetAsync(courseId, mucToiDa ?? 5, ct)));

app.MapGet("/api/mon-co-tien-quyet", async (
    CourseRepository kho, CancellationToken ct) =>
    Results.Ok(await kho.LayMonCoTienQuyetAsync(ct)));

// =====================================================================
// Chức năng 2. Khối lượng tín chỉ tích luỹ
// Hàm vô hướng gọi ngay trong mệnh đề SELECT.
// =====================================================================
app.MapGet("/api/khoi-luong-tin-chi", async (
    CourseRepository kho, string courseId, CancellationToken ct) =>
{
    var kq = await kho.LayKhoiLuongTinChiAsync(courseId, ct);
    return kq is null ? Results.NotFound(new { thongBao = "Không tìm thấy môn học" })
                      : Results.Ok(kq);
});

// =====================================================================
// Chức năng 3. Tra cứu sinh viên đa tiêu chí
// SQL động dựng mệnh đề WHERE lúc chạy, thực thi bằng sp_executesql.
// =====================================================================
app.MapGet("/api/tra-cuu-sinh-vien", async (
    StudentRepository kho, string? ten, string? khoa, int? tinChiToiThieu,
    string? sapXep, CancellationToken ct) =>
    Results.Ok(await kho.TraCuuAsync(ten, khoa, tinChiToiThieu, sapXep ?? "ID", ct)));

app.MapGet("/api/khoa", async (
    StudentRepository kho, CancellationToken ct) =>
    Results.Ok(await kho.LayDanhSachKhoaAsync(ct)));

app.MapGet("/api/sinh-vien-demo", async (
    StudentRepository kho, CancellationToken ct) =>
    Results.Ok(await kho.LaySinhVienDemoAsync(ct)));

app.MapPost("/api/them-sinh-vien", async (
    StudentRepository kho, YeuCauThemSinhVien yc, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(yc.Ten))
        return Results.BadRequest(new { thongBao = "Chưa nhập họ tên" });

    return Results.Ok(await kho.ThemSinhVienAsync(yc.Ten, yc.Khoa, yc.TongTinChi, ct));
});

// =====================================================================
// Chức năng 4. Đăng ký lớp học phần
// SQL nhúng qua ADO.NET, giao dịch, và thủ tục kiểm tra logic nghiệp vụ.
// =====================================================================
app.MapGet("/api/lop-dang-mo", async (
    EnrollmentRepository kho, CancellationToken ct) =>
    Results.Ok(await kho.LayLopDangMoAsync(ct)));

app.MapGet("/api/kiem-tra-dieu-kien", async (
    EnrollmentRepository kho, string id, string courseId, string secId,
    CancellationToken ct) =>
    Results.Ok(await kho.KiemTraDieuKienAsync(id, courseId, secId, ct)));

app.MapPost("/api/dang-ky", async (
    EnrollmentRepository kho, YeuCauDangKy yc, CancellationToken ct) =>
    Results.Ok(await kho.DangKyAsync(yc.Id, yc.CourseId, yc.SecId, ct)));

app.MapGet("/api/lop-da-dang-ky", async (
    EnrollmentRepository kho, string id, CancellationToken ct) =>
    Results.Ok(await kho.LayLopDaDangKyAsync(id, ct)));

app.MapPost("/api/huy-dang-ky", async (
    EnrollmentRepository kho, YeuCauDangKy yc, CancellationToken ct) =>
{
    var (thanhCong, thongBao) = await kho.HuyDangKyAsync(yc.Id, yc.CourseId, yc.SecId, ct);
    return Results.Ok(new { thanhCong, thongBao });
});

app.Run();

/// <summary>Dữ liệu một yêu cầu đăng ký hoặc huỷ đăng ký.</summary>
internal sealed record YeuCauDangKy(string Id, string CourseId, string SecId);

/// <summary>Dữ liệu một yêu cầu thêm sinh viên.</summary>
internal sealed record YeuCauThemSinhVien(string Ten, string Khoa, int TongTinChi);
