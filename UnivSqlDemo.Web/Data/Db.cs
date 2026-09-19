using Microsoft.Data.SqlClient;

namespace UnivSqlDemo.Web.Data;

/// <summary>
/// Nguồn kết nối tới CSDL univdb. Mọi truy cập dữ liệu của ứng dụng đều đi qua
/// ADO.NET, tức giao diện mức lời gọi: chương trình gửi câu lệnh SQL tới máy
/// chủ qua một tập hàm thư viện, câu lệnh được diễn dịch lúc chạy.
/// </summary>
public sealed class Db(IConfiguration cauHinh)
{
    private readonly string _chuoiKetNoi =
        cauHinh.GetConnectionString("UnivDb")
        ?? throw new InvalidOperationException("Thiếu chuỗi kết nối 'UnivDb' trong appsettings.json");

    public async Task<SqlConnection> MoKetNoiAsync(CancellationToken ct = default)
    {
        var ketNoi = new SqlConnection(_chuoiKetNoi);
        await ketNoi.OpenAsync(ct);
        return ketNoi;
    }
}
