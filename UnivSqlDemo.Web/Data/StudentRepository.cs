using System.Data;
using Microsoft.Data.SqlClient;

namespace UnivSqlDemo.Web.Data;

/// <summary>
/// Tra cứu sinh viên theo nhiều tiêu chí, thực hiện bằng SQL động phía máy chủ.
/// </summary>
public sealed class StudentRepository(Db db)
{
    /// <summary>
    /// Gọi usp_SearchStudentsDynamic. Tiêu chí nào bỏ trống thì truyền
    /// <see cref="DBNull"/> để thủ tục không sinh điều kiện tương ứng.
    /// Tham số đầu ra @SqlSinhRa mang về đúng chuỗi lệnh đã dựng, để giao
    /// diện hiển thị.
    /// </summary>
    public async Task<KetQuaTraCuu> TraCuuAsync(
        string? ten,
        string? khoa,
        int? tinChiToiThieu,
        string sapXep,
        CancellationToken ct = default)
    {
        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand("dbo.usp_SearchStudentsDynamic", ketNoi)
        {
            CommandType = CommandType.StoredProcedure
        };

        lenh.Parameters.Add("@Ten", SqlDbType.NVarChar, 50).Value =
            string.IsNullOrWhiteSpace(ten) ? DBNull.Value : ten.Trim();
        // Kích thước phải khớp @Khoa NVARCHAR(40) của thủ tục và cột dept_name,
        // nếu khai báo ngắn hơn thì tên khoa dài bị cắt trước khi gửi đi.
        lenh.Parameters.Add("@Khoa", SqlDbType.NVarChar, 40).Value =
            string.IsNullOrWhiteSpace(khoa) ? DBNull.Value : khoa.Trim();
        lenh.Parameters.Add("@TinChiToiThieu", SqlDbType.Decimal).Value =
            tinChiToiThieu.HasValue ? tinChiToiThieu.Value : DBNull.Value;
        lenh.Parameters.Add("@SapXep", SqlDbType.VarChar, 20).Value = sapXep;

        var thamSoSql = lenh.Parameters.Add("@SqlSinhRa", SqlDbType.NVarChar, -1);
        thamSoSql.Direction = ParameterDirection.Output;

        var ds = new List<SinhVien>();

        await using (var doc = await lenh.ExecuteReaderAsync(ct))
        {
            while (await doc.ReadAsync(ct))
            {
                ds.Add(new SinhVien(
                    Id:          doc.GetString(0),
                    Ten:         doc.GetString(1),
                    Khoa:        doc.IsDBNull(2) ? string.Empty : doc.GetString(2),
                    TongTinChi:  doc.IsDBNull(3) ? 0 : (int)doc.GetDecimal(3)));
            }
        }

        var sqlSinhRa = thamSoSql.Value as string ?? string.Empty;
        return new KetQuaTraCuu(ds, sqlSinhRa);
    }

    /// <summary>
    /// Thêm một sinh viên mới.
    ///
    /// Tham số khai báo <see cref="SqlDbType.NVarChar"/> nên trình điều khiển
    /// gửi chuỗi dưới dạng Unicode, giữ được dấu tiếng Việt mà không cần tiền
    /// tố N của chuỗi hằng T-SQL.
    ///
    /// Mã sinh viên cấp trong cùng giao dịch với phép chèn, để hai người thêm
    /// cùng lúc không nhận trùng mã.
    /// </summary>
    public async Task<SinhVien> ThemSinhVienAsync(
        string ten, string khoa, int tongTinChi, CancellationToken ct = default)
    {
        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var giaoDich = (SqlTransaction)await ketNoi.BeginTransactionAsync(ct);

        try
        {
            const string sql = """
                DECLARE @ID VARCHAR(5);

                /* Mã sinh viên trong giáo trình là số, rải khắp dải 1000-99999,
                   nên không có khoảng số nào chắc chắn còn trống. Sinh viên do
                   ứng dụng tạo vì vậy mang tiền tố SV. */
                SELECT @ID = 'SV' + RIGHT('000' + CAST(
                                 ISNULL(MAX(TRY_CAST(RIGHT(s.ID, 3) AS INT)), 0) + 1
                             AS VARCHAR(3)), 3)
                FROM   dbo.student AS s WITH (UPDLOCK, HOLDLOCK)
                WHERE  s.ID LIKE 'SV[0-9][0-9][0-9]';

                INSERT INTO dbo.student (ID, name, dept_name, tot_cred)
                VALUES (@ID, @Ten, @Khoa, @TongTinChi);

                SELECT @ID AS ID;
                """;

            await using var lenh = new SqlCommand(sql, ketNoi, giaoDich);
            lenh.Parameters.Add("@Ten", SqlDbType.NVarChar, 50).Value = ten.Trim();
            lenh.Parameters.Add("@Khoa", SqlDbType.NVarChar, 40).Value = khoa;
            lenh.Parameters.Add("@TongTinChi", SqlDbType.Decimal).Value = tongTinChi;

            var id = (string)(await lenh.ExecuteScalarAsync(ct))!;

            await giaoDich.CommitAsync(ct);
            return new SinhVien(id, ten.Trim(), khoa, tongTinChi);
        }
        catch
        {
            await giaoDich.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Sinh viên thuộc dải mã dành cho phần demo, dùng cho ô chọn ở tab đăng ký.
    /// </summary>
    public async Task<IReadOnlyList<SinhVien>> LaySinhVienDemoAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT   s.ID, s.name, s.dept_name, s.tot_cred
            FROM     dbo.student AS s
            WHERE    s.ID LIKE 'SV[0-9][0-9][0-9]'
            ORDER BY s.ID;
            """;

        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand(sql, ketNoi);
        await using var doc = await lenh.ExecuteReaderAsync(ct);

        var ds = new List<SinhVien>();
        while (await doc.ReadAsync(ct))
        {
            ds.Add(new SinhVien(
                doc.GetString(0),
                doc.GetString(1),
                doc.IsDBNull(2) ? string.Empty : doc.GetString(2),
                doc.IsDBNull(3) ? 0 : (int)doc.GetDecimal(3)));
        }
        return ds;
    }

    /// <summary>Danh sách khoa, dùng cho ô chọn trên giao diện.</summary>
    public async Task<IReadOnlyList<string>> LayDanhSachKhoaAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT dept_name FROM dbo.department ORDER BY dept_name;";

        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand(sql, ketNoi);
        await using var doc = await lenh.ExecuteReaderAsync(ct);

        var ds = new List<string>();
        while (await doc.ReadAsync(ct)) ds.Add(doc.GetString(0));
        return ds;
    }
}
