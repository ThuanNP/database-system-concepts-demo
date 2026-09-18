using System.Data;
using Microsoft.Data.SqlClient;

namespace UnivSqlDemo.Web.Data;

/// <summary>
/// Truy cập dữ liệu môn học: cây môn tiên quyết và khối lượng tín chỉ tích luỹ.
/// </summary>
public sealed class CourseRepository(Db db)
{
    /// <summary>
    /// Gọi thủ tục usp_GetPrereqTree. Thủ tục trả về đồng thời hai luồng dữ
    /// liệu: tập kết quả dạng bảng, và tổng số môn tiên quyết qua tham số đầu
    /// ra. Tham số đầu ra chỉ có giá trị sau khi đã đọc hết tập kết quả, nên
    /// vòng lặp đọc phải chạy xong rồi mới lấy giá trị.
    /// </summary>
    public async Task<KetQuaCayTienQuyet> LayCayTienQuyetAsync(
        string courseId, int mucToiDa, CancellationToken ct = default)
    {
        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand("dbo.usp_GetPrereqTree", ketNoi)
        {
            CommandType = CommandType.StoredProcedure
        };

        lenh.Parameters.Add("@course_id", SqlDbType.VarChar, 8).Value = courseId;
        lenh.Parameters.Add("@MucToiDa", SqlDbType.Int).Value = mucToiDa;

        var thamSoRa = lenh.Parameters.Add("@TongSoMon", SqlDbType.Int);
        thamSoRa.Direction = ParameterDirection.Output;

        var cay = new List<NutCayTienQuyet>();

        await using (var doc = await lenh.ExecuteReaderAsync(ct))
        {
            while (await doc.ReadAsync(ct))
            {
                cay.Add(new NutCayTienQuyet(
                    CourseId:     doc.GetString(0),
                    CourseIdCha:  doc.IsDBNull(1) ? null : doc.GetString(1),
                    Muc:          doc.GetInt32(2),
                    DuongDi:      doc.GetString(3),
                    Title:        doc.GetString(4),
                    DeptName:     doc.IsDBNull(5) ? string.Empty : doc.GetString(5),
                    Credits:      (int)doc.GetDecimal(6)));
            }
        }

        var tongSoMon = thamSoRa.Value is int n ? n : 0;
        var mucSauNhat = cay.Count == 0 ? 0 : cay.Max(x => x.Muc);

        var cauLenh =
            $"DECLARE @TongSoMon INT;{Environment.NewLine}" +
            $"EXEC dbo.usp_GetPrereqTree @course_id = '{courseId}', " +
            $"@MucToiDa = {mucToiDa}, @TongSoMon = @TongSoMon OUTPUT;";

        return new KetQuaCayTienQuyet(cay, tongSoMon, mucSauNhat, cauLenh);
    }

    /// <summary>
    /// Gọi hàm vô hướng fn_GetTotalCredits ngay trong mệnh đề SELECT của một
    /// câu truy vấn.
    /// </summary>
    public async Task<KhoiLuongTinChi?> LayKhoiLuongTinChiAsync(
        string courseId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT c.course_id,
                   c.title,
                   c.credits                              AS tin_chi_rieng,
                   dbo.fn_GetTotalCredits(c.course_id)    AS tong_tin_chi,
                   (SELECT COUNT(*) FROM dbo.fn_PrereqClosure(c.course_id)) AS so_mon_tien_quyet
            FROM   dbo.course AS c
            WHERE  c.course_id = @course_id;
            """;

        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand(sql, ketNoi);
        lenh.Parameters.Add("@course_id", SqlDbType.VarChar, 8).Value = courseId;

        await using var doc = await lenh.ExecuteReaderAsync(ct);
        if (!await doc.ReadAsync(ct)) return null;

        return new KhoiLuongTinChi(
            CourseId:       doc.GetString(0),
            Title:          doc.GetString(1),
            TinChiRieng:    (int)doc.GetDecimal(2),
            TongTinChi:     (int)doc.GetDecimal(3),
            SoMonTienQuyet: doc.GetInt32(4),
            CauLenh:        sql.Replace("@course_id", $"'{courseId}'"));
    }

    /// <summary>
    /// Danh sách môn học có môn tiên quyết, xếp theo số môn tiên quyết bắc
    /// cầu giảm dần.
    /// </summary>
    public async Task<IReadOnlyList<MonHoc>> LayMonCoTienQuyetAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT   c.course_id, c.title, c.dept_name, c.credits
            FROM     dbo.course AS c
            WHERE    EXISTS (SELECT 1 FROM dbo.prereq AS p WHERE p.course_id = c.course_id)
            ORDER BY (SELECT COUNT(*) FROM dbo.fn_PrereqClosure(c.course_id)) DESC,
                     c.course_id;
            """;

        await using var ketNoi = await db.MoKetNoiAsync(ct);
        await using var lenh = new SqlCommand(sql, ketNoi);
        await using var doc = await lenh.ExecuteReaderAsync(ct);

        var ds = new List<MonHoc>();
        while (await doc.ReadAsync(ct))
        {
            ds.Add(new MonHoc(
                doc.GetString(0),
                doc.GetString(1),
                doc.IsDBNull(2) ? string.Empty : doc.GetString(2),
                (int)doc.GetDecimal(3)));
        }
        return ds;
    }
}
