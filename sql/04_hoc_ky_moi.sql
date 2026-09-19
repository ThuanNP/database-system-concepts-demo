/* Học kỳ Fall 2026 đang mở đăng ký, dùng cho phần demo.

   Bộ dữ liệu mẫu của giáo trình ghi 30000 lượt đăng ký cho 100 lớp học phần,
   trong khi phòng học lớn nhất có 120 chỗ, nên mọi lớp đều vượt sức chứa. Ba
   lớp học phần dựng ở đây cho phép tái hiện đủ ba kết cục: đăng ký thành công,
   bị chặn vì chưa học đủ môn tiên quyết, bị chặn vì phòng hết chỗ.

   Chạy lại được nhiều lần: phần xoá ở đầu dọn dữ liệu do chính nó sinh ra và
   các sinh viên do ứng dụng tạo.

   Mọi chuỗi tiếng Việt mang tiền tố N. Thiếu tiền tố đó, chuỗi bị diễn dịch
   theo collation mặc định của CSDL trước khi ghi vào cột và mất dấu. */

USE univdb;
GO

/* Thứ tự xoá đi ngược với thứ tự tham chiếu khoá ngoại. */
DELETE FROM takes   WHERE year IN (2025, 2026) OR ID LIKE 'SV[0-9][0-9][0-9]';
DELETE FROM section WHERE year IN (2025, 2026);
DELETE FROM advisor WHERE s_ID LIKE 'SV[0-9][0-9][0-9]';
DELETE FROM student WHERE ID LIKE 'SV[0-9][0-9][0-9]';
GO

/* Hai sinh viên mẫu, khác nhau ở chỗ đã học hay chưa học môn tiên quyết. Mã
   mang tiền tố SV để tách khỏi dải mã số của dữ liệu giáo trình, vốn rải khắp
   khoảng 1000 tới 99999. */
INSERT INTO student (ID, name, dept_name, tot_cred) VALUES
    ('SV001', N'Lê Hoàn Vũ',  N'Comp. Sci.', 24),
    ('SV002', N'Phan Bảo Ân', N'Comp. Sci.',  8);
GO

/* Lớp học phần các học kỳ trước, dùng để ghi nhận kết quả học tập.

   Môn 353 Operating Systems có bảy môn tiên quyết bắc cầu: 254, 599, 647, 694,
   792, 814, 877. Dữ liệu gốc chỉ mở lớp cho 599 và 694. */
INSERT INTO section (course_id, sec_id, semester, year, building, room_number, time_slot_id) VALUES
    ('254', '1', 'Fall', 2025, 'Whitman', '134', 'A'),
    ('599', '1', 'Fall', 2025, 'Whitman', '134', 'B'),
    ('647', '1', 'Fall', 2025, 'Whitman', '134', 'C'),
    ('694', '1', 'Fall', 2025, 'Whitman', '134', 'D'),
    ('792', '1', 'Fall', 2025, 'Whitman', '134', 'E'),
    ('814', '1', 'Fall', 2025, 'Whitman', '134', 'F'),
    ('877', '1', 'Fall', 2025, 'Whitman', '134', 'G');
GO

/* SV001 đã hoàn thành cả bảy môn tiên quyết của 353. */
INSERT INTO takes (ID, course_id, sec_id, semester, year, grade) VALUES
    ('SV001', '254', '1', 'Fall', 2025, 'A' ),
    ('SV001', '599', '1', 'Fall', 2025, 'B+'),
    ('SV001', '647', '1', 'Fall', 2025, 'A-'),
    ('SV001', '694', '1', 'Fall', 2025, 'B' ),
    ('SV001', '792', '1', 'Fall', 2025, 'A' ),
    ('SV001', '814', '1', 'Fall', 2025, 'B+'),
    ('SV001', '877', '1', 'Fall', 2025, 'A-');

/* SV002 mới học hai môn, còn thiếu năm môn. */
INSERT INTO takes (ID, course_id, sec_id, semester, year, grade) VALUES
    ('SV002', '254', '1', 'Fall', 2025, 'B' ),
    ('SV002', '599', '1', 'Fall', 2025, 'C+');
GO

/* Ba lớp học phần của học kỳ Fall 2026:
     353 nhóm 1  phòng 120 chỗ còn trống, cổng chặn là môn tiên quyết
     584 nhóm 1  không có môn tiên quyết, phòng 115 chỗ còn trống
     584 nhóm 2  cùng môn nhưng xếp phòng 10 chỗ và đã kín */
INSERT INTO section (course_id, sec_id, semester, year, building, room_number, time_slot_id) VALUES
    ('353', '1', 'Fall', 2026, 'Whitman',  '134', 'A'),
    ('584', '1', 'Fall', 2026, 'Taylor',   '812', 'B'),
    ('584', '2', 'Fall', 2026, 'Chandler', '375', 'C');
GO

/* Lấp đầy đúng 10 chỗ của phòng Chandler 375. */
INSERT INTO takes (ID, course_id, sec_id, semester, year, grade)
SELECT TOP (10) s.ID, '584', '2', 'Fall', 2026, NULL
FROM   student AS s
WHERE  s.ID NOT LIKE 'SV[0-9][0-9][0-9]'
ORDER BY s.ID;
GO

SELECT  s.course_id,
        s.sec_id,
        c.title,
        r.capacity               AS suc_chua,
        COUNT(t.ID)              AS da_dang_ky,
        r.capacity - COUNT(t.ID) AS con_trong
FROM    section   AS s
        JOIN course    AS c ON c.course_id   = s.course_id
        JOIN classroom AS r ON r.building    = s.building
                           AND r.room_number = s.room_number
        LEFT JOIN takes AS t ON t.course_id = s.course_id
                            AND t.sec_id    = s.sec_id
                            AND t.semester  = s.semester
                            AND t.year      = s.year
WHERE   s.year = 2026
GROUP BY s.course_id, s.sec_id, c.title, r.capacity
ORDER BY s.course_id, s.sec_id;
GO
