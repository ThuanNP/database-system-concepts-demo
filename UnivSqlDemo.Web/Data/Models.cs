namespace UnivSqlDemo.Web.Data;

/// <summary>Một nút trong cây môn tiên quyết.</summary>
public sealed record NutCayTienQuyet(
    string CourseId,
    string? CourseIdCha,
    int Muc,
    string DuongDi,
    string Title,
    string DeptName,
    int Credits);

/// <summary>Cây môn tiên quyết kèm số liệu tổng hợp lấy từ tham số đầu ra.</summary>
public sealed record KetQuaCayTienQuyet(
    IReadOnlyList<NutCayTienQuyet> Cay,
    int TongSoMon,
    int MucSauNhat,
    string CauLenh);

/// <summary>Khối lượng tín chỉ tích luỹ của một môn học.</summary>
public sealed record KhoiLuongTinChi(
    string CourseId,
    string Title,
    int TinChiRieng,
    int TongTinChi,
    int SoMonTienQuyet,
    string CauLenh);

/// <summary>Một môn học trong danh sách chọn.</summary>
public sealed record MonHoc(string CourseId, string Title, string DeptName, int Credits);

/// <summary>Một sinh viên trong kết quả tra cứu.</summary>
public sealed record SinhVien(string Id, string Ten, string Khoa, int TongTinChi);

/// <summary>Kết quả tra cứu kèm chuỗi lệnh mà SQL động đã dựng.</summary>
public sealed record KetQuaTraCuu(
    IReadOnlyList<SinhVien> DanhSach,
    string SqlSinhRa);

/// <summary>Một lớp học phần đang mở đăng ký.</summary>
public sealed record LopHocPhan(
    string CourseId,
    string SecId,
    string Semester,
    int Year,
    string Title,
    string Building,
    string RoomNumber,
    int SucChua,
    int DaDangKy);

/// <summary>Tình trạng một môn tiên quyết đối với một sinh viên.</summary>
public sealed record TinhTrangTienQuyet(
    string CourseId,
    string Title,
    int Muc,
    bool DaHoanThanh,
    string? Diem);

/// <summary>Kết quả đối chiếu điều kiện trước khi đăng ký.</summary>
public sealed record KetQuaKiemTra(
    IReadOnlyList<TinhTrangTienQuyet> DanhSachTienQuyet,
    int SoMonConThieu,
    int SucChua,
    int DaDangKy,
    string CauLenh);

/// <summary>Kết quả một lượt đăng ký.</summary>
public sealed record KetQuaDangKy(
    bool ThanhCong,
    string ThongBao,
    int? DaDangKy,
    int? SucChua,
    string CauLenh);

/// <summary>Một lớp học phần mà sinh viên đang theo học.</summary>
public sealed record LopDaDangKy(
    string CourseId,
    string SecId,
    string Semester,
    int Year,
    string Title,
    string? Diem);
