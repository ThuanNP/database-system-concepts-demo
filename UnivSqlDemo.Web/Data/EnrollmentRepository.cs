using System.Data;
using Microsoft.Data.SqlClient;

namespace UnivSqlDemo.Web.Data;

/// <summary>
/// Đăng ký lớp học phần, với ba cách đặt logic nghiệp vụ khác nhau:
///   - Đối chiếu điều kiện: câu lệnh tham số hoá viết trong mã C#, đọc kết quả
///     bằng vòng lặp trên SqlDataReader, tương ứng con trỏ với open, fetch,
///     close của SQL nhúng.
///   - Ghi nhận đăng ký: gọi usp_EnrollStudent, giao dịch và logic nghiệp vụ
///     nằm trong CSDL.
///   - Huỷ đăng ký: giao dịch do tầng ứng dụng mở và kết thúc, tương ứng
///     EXEC SQL COMMIT và EXEC SQL ROLLBACK.
/// </summary>
public sealed class EnrollmentRepository(Db db)
{
    private const int NamHocKyDemo = 2026;
    private const string HocKyDemo = "Fall";

    /// <summary>Các lớp học phần của học kỳ đang mở đăng ký.</summary>
    public async Task<IReadOnlyList<LopHocPhan>> LayLopDangMoAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT   s.course_id, s.sec_id, s.semester, s.year, c.title,
                     s.building, s.room_number, r.capacity,
                     (SELECT COUNT(*) FROM dbo.takes AS t
                      WHERE  t.course_id = s.course_id AND t.sec_id   = s.sec_id
                        AND  t.semester  = s.semester  AND t.year     = s.year) AS da_dang_ky
            FROM     dbo.section   AS s
                     JOIN dbo.course    AS c ON c.course_id   = s.course_id
                     JOIN dbo.classroom AS r ON r.building    = s.building
                                            AND r.room_number = s.room_number
            WHERE    s.semester = @semester AND s.year = @year
            ORDER BY s.course_id, s.sec_id;
            """;

        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand(sql, ketNoi);
        lenh.Parameters.Add("@semester", SqlDbType.VarChar, 6).Value = HocKyDemo;
        lenh.Parameters.Add("@year", SqlDbType.Decimal).Value = NamHocKyDemo;

        await using var doc = await lenh.ExecuteReaderAsync(ct);

        var ds = new List<LopHocPhan>();
        while (await doc.ReadAsync(ct))
        {
            ds.Add(new LopHocPhan(
                CourseId:   doc.GetString(0),
                SecId:      doc.GetString(1),
                Semester:   doc.GetString(2),
                Year:       (int)doc.GetDecimal(3),
                Title:      doc.GetString(4),
                Building:   doc.GetString(5),
                RoomNumber: doc.GetString(6),
                SucChua:    (int)doc.GetDecimal(7),
                DaDangKy:   doc.GetInt32(8)));
        }
        return ds;
    }

    /// <summary>
    /// Đối chiếu điều kiện trước khi đăng ký, không ghi gì vào CSDL.
    ///
    /// Một lô hai câu truy vấn gửi đi cùng lúc; đọc xong tập kết quả thứ nhất
    /// thì gọi NextResult để sang tập thứ hai.
    /// </summary>
    public async Task<KetQuaKiemTra> KiemTraDieuKienAsync(
        string id, string courseId, string secId, CancellationToken ct = default)
    {
        const string sql = """
            -- Tình trạng từng môn tiên quyết bắc cầu đối với sinh viên
            SELECT   bd.prereq_id,
                     c.title,
                     bd.muc,
                     CASE WHEN dh.grade IS NULL THEN 0 ELSE 1 END AS da_hoan_thanh,
                     dh.grade
            FROM     dbo.fn_PrereqClosure(@course_id) AS bd
                     JOIN dbo.course AS c ON c.course_id = bd.prereq_id
                     OUTER APPLY (
                         SELECT TOP (1) t.grade
                         FROM   dbo.takes AS t
                         WHERE  t.ID        = @ID
                           AND  t.course_id = bd.prereq_id
                           AND  t.grade IS NOT NULL
                           AND  t.grade <> 'F'
                         ORDER BY t.year DESC
                     ) AS dh
            ORDER BY bd.muc, bd.prereq_id;

            -- Sức chứa phòng và số chỗ đã dùng của lớp học phần
            SELECT   r.capacity,
                     (SELECT COUNT(*) FROM dbo.takes AS t
                      WHERE  t.course_id = s.course_id AND t.sec_id   = s.sec_id
                        AND  t.semester  = s.semester  AND t.year     = s.year)
            FROM     dbo.section   AS s
                     JOIN dbo.classroom AS r ON r.building    = s.building
                                            AND r.room_number = s.room_number
            WHERE    s.course_id = @course_id AND s.sec_id = @sec_id
              AND    s.semester  = @semester  AND s.year   = @year;
            """;

        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand(sql, ketNoi);
        lenh.Parameters.Add("@ID", SqlDbType.VarChar, 5).Value = id;
        lenh.Parameters.Add("@course_id", SqlDbType.VarChar, 8).Value = courseId;
        lenh.Parameters.Add("@sec_id", SqlDbType.VarChar, 8).Value = secId;
        lenh.Parameters.Add("@semester", SqlDbType.VarChar, 6).Value = HocKyDemo;
        lenh.Parameters.Add("@year", SqlDbType.Decimal).Value = NamHocKyDemo;

        var danhSach = new List<TinhTrangTienQuyet>();
        int sucChua = 0, daDangKy = 0;

        await using var doc = await lenh.ExecuteReaderAsync(ct);

        while (await doc.ReadAsync(ct))
        {
            danhSach.Add(new TinhTrangTienQuyet(
                CourseId:     doc.GetString(0),
                Title:        doc.GetString(1),
                Muc:          doc.GetInt32(2),
                DaHoanThanh:  doc.GetInt32(3) == 1,
                Diem:         doc.IsDBNull(4) ? null : doc.GetString(4)));
        }

        if (await doc.NextResultAsync(ct) && await doc.ReadAsync(ct))
        {
            sucChua  = (int)doc.GetDecimal(0);
            daDangKy = doc.GetInt32(1);
        }

        return new KetQuaKiemTra(
            DanhSachTienQuyet: danhSach,
            SoMonConThieu:     danhSach.Count(x => !x.DaHoanThanh),
            SucChua:           sucChua,
            DaDangKy:          daDangKy,
            CauLenh:           sql);
    }

    /// <summary>
    /// Gọi thủ tục usp_EnrollStudent. Thủ tục tự mở và kết thúc giao dịch, và
    /// báo lỗi bằng RAISERROR nên mọi vi phạm điều kiện đều tới đây dưới dạng
    /// <see cref="SqlException"/>. Mức nghiêm trọng 16 là lỗi nghiệp vụ do
    /// thủ tục chủ động phát ra, phân biệt với lỗi hệ thống.
    /// </summary>
    public async Task<KetQuaDangKy> DangKyAsync(
        string id, string courseId, string secId, CancellationToken ct = default)
    {
        var cauLenh =
            $"EXEC dbo.usp_EnrollStudent @ID = '{id}', @course_id = '{courseId}', " +
            $"@sec_id = '{secId}', @semester = '{HocKyDemo}', @year = {NamHocKyDemo};";

        try
        {
            await using var ketNoi = await db.MoKetNoiAsync(ct);
            await using var lenh = new SqlCommand("dbo.usp_EnrollStudent", ketNoi)
            {
                CommandType = CommandType.StoredProcedure
            };

            lenh.Parameters.Add("@ID", SqlDbType.VarChar, 5).Value = id;
            lenh.Parameters.Add("@course_id", SqlDbType.VarChar, 8).Value = courseId;
            lenh.Parameters.Add("@sec_id", SqlDbType.VarChar, 8).Value = secId;
            lenh.Parameters.Add("@semester", SqlDbType.VarChar, 6).Value = HocKyDemo;
            lenh.Parameters.Add("@year", SqlDbType.Decimal).Value = NamHocKyDemo;

            await using var doc = await lenh.ExecuteReaderAsync(ct);

            if (await doc.ReadAsync(ct))
            {
                return new KetQuaDangKy(
                    ThanhCong: true,
                    ThongBao:  doc.GetString(0),
                    DaDangKy:  doc.GetInt32(1),
                    SucChua:   doc.GetInt32(2),
                    CauLenh:   cauLenh);
            }

            return new KetQuaDangKy(true, "Đăng ký thành công", null, null, cauLenh);
        }
        catch (SqlException ex) when (ex.Class == 16)
        {
            return new KetQuaDangKy(false, ex.Message, null, null, cauLenh);
        }
    }

    /// <summary>Các lớp học phần sinh viên đang theo học trong học kỳ demo.</summary>
    public async Task<IReadOnlyList<LopDaDangKy>> LayLopDaDangKyAsync(
        string id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT   t.course_id, t.sec_id, t.semester, t.year, c.title, t.grade
            FROM     dbo.takes  AS t
                     JOIN dbo.course AS c ON c.course_id = t.course_id
            WHERE    t.ID = @ID AND t.semester = @semester AND t.year = @year
            ORDER BY t.course_id, t.sec_id;
            """;

        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand(sql, ketNoi);
        lenh.Parameters.Add("@ID", SqlDbType.VarChar, 5).Value = id;
        lenh.Parameters.Add("@semester", SqlDbType.VarChar, 6).Value = HocKyDemo;
        lenh.Parameters.Add("@year", SqlDbType.Decimal).Value = NamHocKyDemo;

        await using var doc = await lenh.ExecuteReaderAsync(ct);

        var ds = new List<LopDaDangKy>();
        while (await doc.ReadAsync(ct))
        {
            ds.Add(new LopDaDangKy(
                doc.GetString(0), doc.GetString(1), doc.GetString(2),
                (int)doc.GetDecimal(3), doc.GetString(4),
                doc.IsDBNull(5) ? null : doc.GetString(5)));
        }
        return ds;
    }

    /// <summary>
    /// Huỷ đăng ký, với giao dịch do tầng ứng dụng điều khiển.
    ///
    /// Bộ đã có điểm thì không cho xoá, vì đó là kết quả học tập chứ không còn
    /// là một lượt đăng ký. Phép kiểm tra và phép xoá nằm trong cùng một giao
    /// dịch nên không có khe hở giữa hai bước.
    /// </summary>
    public async Task<(bool ThanhCong, string ThongBao)> HuyDangKyAsync(
        string id, string courseId, string secId, CancellationToken ct = default)
    {
        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var giaoDich = (SqlTransaction)await ketNoi.BeginTransactionAsync(ct);

        try
        {
            await using (var lenhDoc = new SqlCommand(
                """
                SELECT t.grade
                FROM   dbo.takes AS t WITH (UPDLOCK)
                WHERE  t.ID = @ID AND t.course_id = @course_id AND t.sec_id = @sec_id
                  AND  t.semester = @semester AND t.year = @year;
                """, ketNoi, giaoDich))
            {
                ThemThamSo(lenhDoc, id, courseId, secId);

                var ketQua = await lenhDoc.ExecuteScalarAsync(ct);

                if (ketQua is null)
                {
                    await giaoDich.RollbackAsync(ct);
                    return (false, "Sinh viên chưa đăng ký lớp này");
                }

                if (ketQua is not DBNull)
                {
                    await giaoDich.RollbackAsync(ct);
                    return (false, $"Lớp đã có điểm {ketQua}, không huỷ đăng ký được");
                }
            }

            await using (var lenhXoa = new SqlCommand(
                """
                DELETE FROM dbo.takes
                WHERE  ID = @ID AND course_id = @course_id AND sec_id = @sec_id
                  AND  semester = @semester AND year = @year;
                """, ketNoi, giaoDich))
            {
                ThemThamSo(lenhXoa, id, courseId, secId);
                await lenhXoa.ExecuteNonQueryAsync(ct);
            }

            await giaoDich.CommitAsync(ct);
            return (true, $"Đã huỷ đăng ký lớp {courseId} nhóm {secId}");
        }
        catch
        {
            await giaoDich.RollbackAsync(ct);
            throw;
        }
    }

    private static void ThemThamSo(SqlCommand lenh, string id, string courseId, string secId)
    {
        lenh.Parameters.Add("@ID", SqlDbType.VarChar, 5).Value = id;
        lenh.Parameters.Add("@course_id", SqlDbType.VarChar, 8).Value = courseId;
        lenh.Parameters.Add("@sec_id", SqlDbType.VarChar, 8).Value = secId;
        lenh.Parameters.Add("@semester", SqlDbType.VarChar, 6).Value = HocKyDemo;
        lenh.Parameters.Add("@year", SqlDbType.Decimal).Value = NamHocKyDemo;
    }
}
