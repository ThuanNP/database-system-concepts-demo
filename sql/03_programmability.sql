/* Thủ tục và hàm cho ứng dụng minh hoạ, trên lược đồ University.
   Chạy trên CSDL demo, Microsoft SQL Server. */

USE demo;
GO

IF OBJECT_ID('dbo.usp_EnrollStudent',         'P')  IS NOT NULL DROP PROCEDURE dbo.usp_EnrollStudent;
IF OBJECT_ID('dbo.usp_SearchStudentsDynamic', 'P')  IS NOT NULL DROP PROCEDURE dbo.usp_SearchStudentsDynamic;
IF OBJECT_ID('dbo.usp_GetPrereqTree',         'P')  IS NOT NULL DROP PROCEDURE dbo.usp_GetPrereqTree;
IF OBJECT_ID('dbo.fn_GetTotalCredits',        'FN') IS NOT NULL DROP FUNCTION  dbo.fn_GetTotalCredits;
IF OBJECT_ID('dbo.fn_PrereqClosure',          'IF') IS NOT NULL DROP FUNCTION  dbo.fn_PrereqClosure;
GO

/* =========================================================================
   fn_PrereqClosure. Hàm trả về bảng dạng nội tuyến.

   Bao đóng bắc cầu của quan hệ prereq cho một môn học: tập mọi môn là tiên
   quyết trực tiếp hoặc gián tiếp của môn đó.

   T-SQL chỉ hỗ trợ UNION ALL trong CTE đệ quy, tức không có phép loại bộ trùng
   để đưa truy vấn tới điểm bất động. Quan hệ prereq của bộ dữ liệu mẫu bản lớn
   chứa chu trình: bốn môn 133, 634, 852, 864 là tiên quyết của chính mình qua
   một đường vòng. Với dữ liệu như vậy, truy vấn lặp cho tới khi chạm ngưỡng
   MAXRECURSION và phát sinh lỗi 530.

   Cột duong_di ghi lại các môn đã đi qua, và điều kiện NOT LIKE loại những
   bước quay lại môn cũ.

   Hàm nội tuyến không nhận mệnh đề OPTION (MAXRECURSION n). Giới hạn mặc định
   là 100 mức, còn độ sâu thực tế của prereq là 4 mức.
   ========================================================================= */
CREATE FUNCTION dbo.fn_PrereqClosure (@course_id VARCHAR(8))
RETURNS TABLE
AS
RETURN
(
    WITH c_prereq AS
    (
        /* Bộ khởi đầu: các môn tiên quyết trực tiếp. */
        SELECT  p.prereq_id,
                1 AS muc,
                CAST('/' + p.course_id + '/' + p.prereq_id + '/' AS VARCHAR(400)) AS duong_di
        FROM    dbo.prereq AS p
        WHERE   p.course_id = @course_id

        UNION ALL

        /* Bộ đệ quy: tiên quyết của những môn vừa tìm được. */
        SELECT  p.prereq_id,
                c.muc + 1,
                CAST(c.duong_di + p.prereq_id + '/' AS VARCHAR(400))
        FROM    c_prereq   AS c
                JOIN dbo.prereq AS p ON p.course_id = c.prereq_id
        WHERE   c.duong_di NOT LIKE '%/' + p.prereq_id + '/%'
    )
    /* Một môn tới được qua nhiều đường; giữ mức nông nhất. */
    SELECT   prereq_id,
             MIN(muc) AS muc
    FROM     c_prereq
    GROUP BY prereq_id
);
GO

/* =========================================================================
   fn_GetTotalCredits. Hàm vô hướng.

   Tổng số tín chỉ phải tích luỹ để hoàn thành một môn học, gồm tín chỉ của
   chính môn đó cộng tín chỉ của toàn bộ môn tiên quyết bắc cầu.
   ========================================================================= */
CREATE FUNCTION dbo.fn_GetTotalCredits (@course_id VARCHAR(8))
RETURNS NUMERIC(6, 0)
AS
BEGIN
    DECLARE @tong NUMERIC(6, 0);

    SELECT  @tong = SUM(c.credits)
    FROM    dbo.course AS c
    WHERE   c.course_id = @course_id
       OR   c.course_id IN ( SELECT bd.prereq_id
                             FROM   dbo.fn_PrereqClosure(@course_id) AS bd );

    /* Môn không tồn tại thì trả 0. */
    RETURN ISNULL(@tong, 0);
END;
GO

/* =========================================================================
   usp_GetPrereqTree. Thủ tục lưu trữ với CTE đệ quy và tham số đầu ra.

   Trả về cây môn tiên quyết của một môn học: mỗi bộ gồm môn, môn cha trong
   cây, mức sâu và đường đi. Tham số @TongSoMon nhận tổng số môn tiên quyết.

   T-SQL chỉ cho phép dùng CTE cho đúng một câu lệnh liền sau nó, nên kết quả
   đệ quy được đổ vào bảng tạm #cay để vừa đếm vừa trả về.
   ========================================================================= */
CREATE PROCEDURE dbo.usp_GetPrereqTree
    @course_id  VARCHAR(8),
    @MucToiDa   INT = 5,
    @TongSoMon  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    WITH cay AS
    (
        /* Bộ khởi đầu: chính môn được hỏi, đặt ở mức 1. */
        SELECT  c.course_id,
                CAST(NULL AS VARCHAR(8)) AS course_id_cha,
                1 AS muc,
                CAST('/' + c.course_id + '/' AS VARCHAR(400)) AS duong_di
        FROM    dbo.course AS c
        WHERE   c.course_id = @course_id

        UNION ALL

        /* Bộ đệ quy: các môn tiên quyết của mức liền trước. */
        SELECT  p.prereq_id,
                cay.course_id,
                cay.muc + 1,
                CAST(cay.duong_di + p.prereq_id + '/' AS VARCHAR(400))
        FROM    cay
                JOIN dbo.prereq AS p ON p.course_id = cay.course_id
        WHERE   cay.muc < @MucToiDa
          AND   cay.duong_di NOT LIKE '%/' + p.prereq_id + '/%'
    )
    SELECT  cay.course_id,
            cay.course_id_cha,
            cay.muc,
            cay.duong_di,
            c.title,
            c.dept_name,
            c.credits
    INTO    #cay
    FROM    cay
            JOIN dbo.course AS c ON c.course_id = cay.course_id
    OPTION  (MAXRECURSION 100);

    /* Trừ đi môn gốc. Môn tới được qua nhiều đường chỉ đếm một lần. */
    SELECT @TongSoMon = COUNT(DISTINCT course_id) - 1 FROM #cay;

    SELECT   course_id, course_id_cha, muc, duong_di, title, dept_name, credits
    FROM     #cay
    ORDER BY muc, course_id;

    DROP TABLE #cay;
END;
GO

/* =========================================================================
   usp_SearchStudentsDynamic. SQL động.

   Tra cứu sinh viên theo nhiều tiêu chí, mỗi tiêu chí đều có thể bỏ trống.
   Mệnh đề where chỉ sinh ra những điều kiện được nhập.

   sp_executesql nhận danh sách tham số tách rời chuỗi lệnh, nên giá trị người
   dùng nhập không được ghép vào chuỗi SQL.

   Cột dùng để sắp xếp là một định danh, mà định danh không tham số hoá được.
   Trường hợp này đối chiếu với một danh sách trắng.

   Tham số @SqlSinhRa trả lại chuỗi lệnh đã dựng để giao diện hiển thị.
   ========================================================================= */
CREATE PROCEDURE dbo.usp_SearchStudentsDynamic
    @Ten             NVARCHAR(50)  = NULL,
    @Khoa            NVARCHAR(40)  = NULL,
    @TinChiToiThieu  NUMERIC(3, 0) = NULL,
    @SapXep          VARCHAR(20)   = 'ID',
    @SqlSinhRa       NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @sql      NVARCHAR(MAX),
            @danh_sach_tham_so NVARCHAR(MAX);

    /* Điều kiện 1 = 1 để mọi điều kiện sau đều nối bằng AND. */
    SET @sql = N'SELECT TOP (200) s.ID, s.name, s.dept_name, s.tot_cred' + NCHAR(13) + NCHAR(10)
             + N'FROM   dbo.student AS s'                                + NCHAR(13) + NCHAR(10)
             + N'WHERE  1 = 1';

    SET @danh_sach_tham_so = N'@p_Ten NVARCHAR(50), @p_Khoa NVARCHAR(40), @p_TinChi NUMERIC(3,0)';

    IF @Ten IS NOT NULL
        SET @sql += NCHAR(13) + NCHAR(10) + N'   AND s.name LIKE ''%'' + @p_Ten + ''%''';

    IF @Khoa IS NOT NULL
        SET @sql += NCHAR(13) + NCHAR(10) + N'   AND s.dept_name = @p_Khoa';

    IF @TinChiToiThieu IS NOT NULL
        SET @sql += NCHAR(13) + NCHAR(10) + N'   AND s.tot_cred >= @p_TinChi';

    /* Danh sách trắng cho cột sắp xếp. */
    SET @sql += NCHAR(13) + NCHAR(10) + N'ORDER BY '
             + CASE @SapXep
                   WHEN 'name'     THEN N's.name'
                   WHEN 'tot_cred' THEN N's.tot_cred DESC'
                   WHEN 'dept'     THEN N's.dept_name, s.name'
                   ELSE                 N's.ID'
               END;

    SET @SqlSinhRa = @sql;

    EXEC sp_executesql @stmt      = @sql,
                       @params    = @danh_sach_tham_so,
                       @p_Ten     = @Ten,
                       @p_Khoa    = @Khoa,
                       @p_TinChi  = @TinChiToiThieu;
END;
GO

/* =========================================================================
   usp_EnrollStudent. Thủ tục ghi dữ liệu trong một giao dịch.

   Đăng ký một sinh viên vào lớp học phần sau khi kiểm tra hai điều kiện: sinh
   viên đã hoàn thành các môn tiên quyết bắc cầu, và phòng học còn chỗ.

   Vi phạm điều kiện nào thì RAISERROR ở mức nghiêm trọng 16, giao dịch bị huỷ
   và tầng ứng dụng nhận được ngoại lệ kèm lý do.
   ========================================================================= */
CREATE PROCEDURE dbo.usp_EnrollStudent
    @ID         VARCHAR(5),
    @course_id  VARCHAR(8),
    @sec_id     VARCHAR(8),
    @semester   VARCHAR(6),
    @year       NUMERIC(4, 0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @thong_bao NVARCHAR(400);

        IF NOT EXISTS ( SELECT 1
                        FROM   dbo.section AS s
                        WHERE  s.course_id = @course_id
                          AND  s.sec_id    = @sec_id
                          AND  s.semester  = @semester
                          AND  s.year      = @year )
        BEGIN
            SET @thong_bao = N'Không tìm thấy lớp học phần ' + @course_id + N' nhóm ' + @sec_id;
            RAISERROR(@thong_bao, 16, 1);
        END;

        IF EXISTS ( SELECT 1
                    FROM   dbo.takes AS t
                    WHERE  t.ID        = @ID
                      AND  t.course_id = @course_id
                      AND  t.sec_id    = @sec_id
                      AND  t.semester  = @semester
                      AND  t.year      = @year )
        BEGIN
            SET @thong_bao = N'Sinh viên ' + @ID + N' đã đăng ký lớp này rồi';
            RAISERROR(@thong_bao, 16, 1);
        END;

        /* Một môn coi là đã hoàn thành khi đã có điểm và điểm khác F. Bộ chưa
           có điểm là lớp đang học dở. */
        DECLARE @con_thieu NVARCHAR(400);

        SELECT  @con_thieu = STRING_AGG(bd.prereq_id + N' ' + c.title, N'; ')
                             WITHIN GROUP (ORDER BY bd.muc, bd.prereq_id)
        FROM    dbo.fn_PrereqClosure(@course_id) AS bd
                JOIN dbo.course AS c ON c.course_id = bd.prereq_id
        WHERE   NOT EXISTS ( SELECT 1
                             FROM   dbo.takes AS t
                             WHERE  t.ID        = @ID
                               AND  t.course_id = bd.prereq_id
                               AND  t.grade IS NOT NULL
                               AND  t.grade <> 'F' );

        IF @con_thieu IS NOT NULL
        BEGIN
            SET @thong_bao = N'Chưa hoàn thành môn tiên quyết: ' + @con_thieu;
            RAISERROR(@thong_bao, 16, 1);
        END;

        DECLARE @dang_hoc INT,
                @suc_chua INT;

        SELECT  @dang_hoc = COUNT(*)
        FROM    dbo.takes AS t
        WHERE   t.course_id = @course_id
          AND   t.sec_id    = @sec_id
          AND   t.semester  = @semester
          AND   t.year      = @year;

        SELECT  @suc_chua = r.capacity
        FROM    dbo.section   AS s
                JOIN dbo.classroom AS r ON r.building    = s.building
                                       AND r.room_number = s.room_number
        WHERE   s.course_id = @course_id
          AND   s.sec_id    = @sec_id
          AND   s.semester  = @semester
          AND   s.year      = @year;

        IF @dang_hoc >= @suc_chua
        BEGIN
            SET @thong_bao = N'Lớp đã đủ sĩ số: ' + CAST(@dang_hoc AS NVARCHAR(10))
                           + N'/' + CAST(@suc_chua AS NVARCHAR(10)) + N' chỗ';
            RAISERROR(@thong_bao, 16, 1);
        END;

        INSERT INTO dbo.takes (ID, course_id, sec_id, semester, year, grade)
        VALUES (@ID, @course_id, @sec_id, @semester, @year, NULL);

        COMMIT TRANSACTION;

        SELECT  N'Đăng ký thành công' AS thong_bao,
                @dang_hoc + 1         AS da_dang_ky,
                @suc_chua             AS suc_chua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
