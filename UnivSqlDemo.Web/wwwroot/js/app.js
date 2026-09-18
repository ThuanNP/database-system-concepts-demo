'use strict';

// Toàn bộ dữ liệu lấy về qua các điểm cuối /api/..., trang không dựng sẵn gì
// từ máy chủ. Mỗi tab tương ứng một kỹ thuật SQL nâng cao.

const $ = (id) => document.getElementById(id);

const MAU_MUC = ['#6ea8ff', '#a97bff', '#f0883e', '#3fb950', '#d29922', '#f85149'];

async function goi(duongDan, tuyChon) {
    const dap = await fetch(duongDan, tuyChon);
    if (!dap.ok) {
        const loi = await dap.text();
        throw new Error(loi || `Máy chủ trả về ${dap.status}`);
    }
    return dap.json();
}

function thoat(s) {
    return String(s ?? '').replace(/[&<>"']/g, (c) => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[c]);
}

function oSoLieu(nhan, giaTri) {
    return `<div class="o-so-lieu"><span class="nhan-o">${thoat(nhan)}</span>
            <span class="gia-tri">${thoat(giaTri)}</span></div>`;
}

function thongBao(loai, noiDung) {
    return `<div class="thong-bao ${loai}">${noiDung}</div>`;
}

// Hộp thoại alert chặn toàn trang và phải bấm mới đóng, nên lỗi được hiện
// ngay trong khối thông báo của tab đang mở.
function baoLoi(nut, loi) {
    const bang = nut.closest('.bang');
    const o = bang && bang.querySelector('.o-loi');
    if (o) o.innerHTML = loi ? thongBao('sai', thoat(loi)) : '';
}

async function chay(nut, viec) {
    nut.disabled = true;
    baoLoi(nut, '');
    try {
        await viec();
    } catch (e) {
        console.error(e);
        baoLoi(nut, e.message);
    } finally {
        nut.disabled = false;
    }
}

// ======================================================================
// Chuyển tab
// ======================================================================
document.querySelectorAll('.the').forEach((the) => {
    the.addEventListener('click', () => {
        document.querySelectorAll('.the').forEach((t) =>
            t.setAttribute('aria-selected', String(t === the)));
        document.querySelectorAll('.bang').forEach((b) => b.classList.add('an'));
        $('tab-' + the.dataset.tab).classList.remove('an');
    });
});

// ======================================================================
// Tab 1. Cây môn tiên quyết
// ======================================================================
function veCay(cay) {
    if (cay.length === 0) return '<p class="ghi-chu">Môn này không có môn tiên quyết.</p>';

    return cay.map((n) => {
        const mau = MAU_MUC[(n.muc - 1) % MAU_MUC.length];
        const thut = (n.muc - 1) * 26;
        const cha = n.courseIdCha
            ? `<span class="the-nho">tiên quyết của ${thoat(n.courseIdCha)}</span>`
            : '<span class="the-nho">môn đang xét</span>';
        return `<div class="nut-cay" style="margin-left:${thut}px">
                  <span class="huy-muc" style="background:${mau}">M${n.muc}</span>
                  <span class="ma-mon">${thoat(n.courseId)}</span>
                  <strong>${thoat(n.title)}</strong>
                  <span class="the-nho">${thoat(n.deptName)}</span>
                  <span class="the-nho">${n.credits} tín chỉ</span>
                  ${cha}
                </div>`;
    }).join('');
}

$('cay-chay').addEventListener('click', (e) => chay(e.target, async () => {
    const courseId = $('cay-mon').value;
    const muc = $('cay-muc').value;
    const kq = await goi(`/api/cay-tien-quyet?courseId=${encodeURIComponent(courseId)}&mucToiDa=${muc}`);

    $('cay-sql').textContent = kq.cauLenh;
    $('cay-so-lieu').innerHTML =
        oSoLieu('Môn tiên quyết', kq.tongSoMon) +
        oSoLieu('Mức sâu nhất', kq.mucSauNhat) +
        oSoLieu('Số nút trong cây', kq.cay.length);
    $('cay-ket-qua').innerHTML = veCay(kq.cay);
}));

// ======================================================================
// Tab 2. Khối lượng tín chỉ
// ======================================================================
$('tc-chay').addEventListener('click', (e) => chay(e.target, async () => {
    const courseId = $('tc-mon').value;
    const kq = await goi(`/api/khoi-luong-tin-chi?courseId=${encodeURIComponent(courseId)}`);

    $('tc-sql').textContent = kq.cauLenh;
    $('tc-ket-qua').innerHTML =
        `<div class="hang-so-lieu">
            ${oSoLieu('Tín chỉ của môn', kq.tinChiRieng)}
            ${oSoLieu('Môn tiên quyết', kq.soMonTienQuyet)}
            ${oSoLieu('Tổng phải tích luỹ', kq.tongTinChi)}
         </div>
         <p class="ghi-chu">${thoat(kq.courseId)}, ${thoat(kq.title)}:
            sinh viên phải tích luỹ <strong>${kq.tongTinChi}</strong> tín chỉ,
            gồm ${kq.tinChiRieng} tín chỉ của chính môn này và
            ${kq.tongTinChi - kq.tinChiRieng} tín chỉ của ${kq.soMonTienQuyet}
            môn tiên quyết bắc cầu.</p>`;
}));

// ======================================================================
// Tab 3. Tra cứu sinh viên
// ======================================================================
$('tracuu-chay').addEventListener('click', (e) => chay(e.target, async () => {
    const ts = new URLSearchParams();
    if ($('tc-ten').value.trim()) ts.set('ten', $('tc-ten').value.trim());
    if ($('tc-khoa').value) ts.set('khoa', $('tc-khoa').value);
    if ($('tc-tinchi').value) ts.set('tinChiToiThieu', $('tc-tinchi').value);
    ts.set('sapXep', $('tc-sapxep').value);

    const kq = await goi('/api/tra-cuu-sinh-vien?' + ts.toString());

    $('tracuu-sql').textContent = kq.sqlSinhRa;
    $('tracuu-ket-qua').innerHTML = kq.danhSach.length === 0
        ? '<p class="ghi-chu">Không có sinh viên nào khớp tiêu chí.</p>'
        : `<table>
             <thead><tr><th>Mã số</th><th>Họ tên</th><th>Khoa</th>
                        <th class="so">Tín chỉ</th></tr></thead>
             <tbody>${kq.danhSach.map((s) => `<tr>
                 <td class="ma-mon">${thoat(s.id)}</td>
                 <td>${thoat(s.ten)}</td>
                 <td>${thoat(s.khoa)}</td>
                 <td class="so">${s.tongTinChi}</td></tr>`).join('')}</tbody>
           </table>
           <p class="ghi-chu">Tìm thấy ${kq.danhSach.length} sinh viên.</p>`;
}));

// ======================================================================
// Tab 4. Đăng ký lớp học phần
// ======================================================================
function khoaLopDangChon() {
    const [courseId, secId] = $('dk-lop').value.split('|');
    return { courseId, secId };
}

// Thông báo và bảng đối chiếu chỉ đúng với đúng một cặp sinh viên và lớp học
// phần. Đổi lựa chọn hoặc thao tác lần mới thì phải dọn, nếu không kết quả cũ
// đứng lẫn với kết quả mới.
function donKetQuaCu() {
    $('dk-thong-bao').innerHTML = '';
    $('dk-dieu-kien').innerHTML = '';
    $('dk-sql').textContent = '';
}

async function napLopDaDangKy() {
    const id = $('dk-sinhvien').value;
    const ds = await goi(`/api/lop-da-dang-ky?id=${encodeURIComponent(id)}`);

    $('dk-da-dang-ky').innerHTML = ds.length === 0
        ? '<p class="ghi-chu">Sinh viên chưa đăng ký lớp nào trong học kỳ này.</p>'
        : `<table>
             <thead><tr><th>Môn</th><th>Tên môn</th><th>Nhóm</th>
                        <th>Điểm</th><th></th></tr></thead>
             <tbody>${ds.map((l) => `<tr>
                 <td class="ma-mon">${thoat(l.courseId)}</td>
                 <td>${thoat(l.title)}</td>
                 <td>${thoat(l.secId)}</td>
                 <td>${l.diem ? thoat(l.diem) : 'chưa có'}</td>
                 <td><button class="nut-phu nut-huy"
                        data-mon="${thoat(l.courseId)}"
                        data-nhom="${thoat(l.secId)}">Huỷ đăng ký</button></td>
               </tr>`).join('')}</tbody>
           </table>`;

    document.querySelectorAll('.nut-huy').forEach((nut) => {
        nut.addEventListener('click', (e) => chay(e.target, async () => {
            const kq = await goi('/api/huy-dang-ky', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    id: $('dk-sinhvien').value,
                    courseId: nut.dataset.mon,
                    secId: nut.dataset.nhom
                })
            });
            $('dk-thong-bao').innerHTML =
                thongBao(kq.thanhCong ? 'dung' : 'sai', thoat(kq.thongBao));
            await napLopDaDangKy();
        }));
    });
}

$('dk-kiemtra').addEventListener('click', (e) => chay(e.target, async () => {
    const { courseId, secId } = khoaLopDangChon();
    const id = $('dk-sinhvien').value;

    donKetQuaCu();
    const kq = await goi(`/api/kiem-tra-dieu-kien?id=${encodeURIComponent(id)}` +
                         `&courseId=${encodeURIComponent(courseId)}` +
                         `&secId=${encodeURIComponent(secId)}`);

    $('dk-sql').textContent = kq.cauLenh;

    const conCho = kq.sucChua - kq.daDangKy;
    const dat = kq.soMonConThieu === 0 && conCho > 0;

    const bangTienQuyet = kq.danhSachTienQuyet.length === 0
        ? '<p class="ghi-chu">Môn này không có môn tiên quyết.</p>'
        : `<table>
             <thead><tr><th>Môn tiên quyết</th><th>Tên môn</th><th>Mức</th>
                        <th>Tình trạng</th></tr></thead>
             <tbody>${kq.danhSachTienQuyet.map((m) => `<tr>
                 <td class="ma-mon">${thoat(m.courseId)}</td>
                 <td>${thoat(m.title)}</td>
                 <td class="so">${m.muc}</td>
                 <td>${m.daHoanThanh
                        ? `<span class="dau-dung">đã học, điểm ${thoat(m.diem)}</span>`
                        : '<span class="dau-sai">chưa học</span>'}</td>
               </tr>`).join('')}</tbody>
           </table>`;

    $('dk-dieu-kien').innerHTML =
        `<div class="hang-so-lieu">
            ${oSoLieu('Môn tiên quyết còn thiếu', kq.soMonConThieu)}
            ${oSoLieu('Đã đăng ký', kq.daDangKy + '/' + kq.sucChua)}
            ${oSoLieu('Chỗ còn trống', conCho)}
         </div>
         ${thongBao(dat ? 'dung' : 'sai', dat
            ? 'Đủ điều kiện đăng ký.'
            : (kq.soMonConThieu > 0
                ? `Còn thiếu ${kq.soMonConThieu} môn tiên quyết.`
                : 'Lớp đã hết chỗ.'))}
         ${bangTienQuyet}`;
}));

$('dk-dangky').addEventListener('click', (e) => chay(e.target, async () => {
    const { courseId, secId } = khoaLopDangChon();

    donKetQuaCu();
    const kq = await goi('/api/dang-ky', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: $('dk-sinhvien').value, courseId, secId })
    });

    $('dk-sql').textContent = kq.cauLenh;
    $('dk-thong-bao').innerHTML = thongBao(
        kq.thanhCong ? 'dung' : 'sai',
        thoat(kq.thongBao) + (kq.thanhCong && kq.sucChua
            ? `. Lớp còn ${kq.sucChua - kq.daDangKy} chỗ trống.` : ''));

    await napLopDaDangKy();
    await napLop();
}));

$('dk-sinhvien').addEventListener('change', () => {
    donKetQuaCu();
    napLopDaDangKy();
});

$('dk-lop').addEventListener('change', donKetQuaCu);

async function napSinhVien(chonId) {
    const ds = await goi('/api/sinh-vien-demo');
    const dangChon = chonId || $('dk-sinhvien').value;

    $('dk-sinhvien').innerHTML = ds.map((s) =>
        `<option value="${thoat(s.id)}">${thoat(s.id)}: ${thoat(s.ten)}
         (${thoat(s.khoa)}, ${s.tongTinChi} tín chỉ)</option>`).join('');

    if (dangChon && ds.some((s) => s.id === dangChon)) $('dk-sinhvien').value = dangChon;
    await napLopDaDangKy();
}

$('sv-them').addEventListener('click', (e) => chay(e.target, async () => {
    const ten = $('sv-ten').value.trim();
    if (!ten) {
        $('sv-thong-bao').innerHTML = thongBao('sai', 'Chưa nhập họ tên.');
        return;
    }

    const sv = await goi('/api/them-sinh-vien', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            ten,
            khoa: $('sv-khoa').value,
            tongTinChi: Number($('sv-tinchi').value) || 0
        })
    });

    $('sv-thong-bao').innerHTML = thongBao('dung',
        `Đã thêm sinh viên <strong>${thoat(sv.id)}: ${thoat(sv.ten)}</strong>.`);
    $('sv-ten').value = '';

    await napSinhVien(sv.id);
}));

async function napLop() {
    const ds = await goi('/api/lop-dang-mo');
    const dangChon = $('dk-lop').value;

    $('dk-lop').innerHTML = ds.map((l) => {
        const conCho = l.sucChua - l.daDangKy;
        return `<option value="${thoat(l.courseId)}|${thoat(l.secId)}">
                  ${thoat(l.courseId)} nhóm ${thoat(l.secId)}: ${thoat(l.title)}
                  (${l.daDangKy}/${l.sucChua} chỗ${conCho <= 0 ? ', đã đầy' : ''})
                </option>`;
    }).join('');

    if (dangChon) $('dk-lop').value = dangChon;
}

// ======================================================================
// Nạp dữ liệu ban đầu
// ======================================================================
(async function khoiDong() {
    try {
        const [monHoc, khoa] = await Promise.all([
            goi('/api/mon-co-tien-quyet'),
            goi('/api/khoa')
        ]);

        const chonMon = monHoc.map((m) =>
            `<option value="${thoat(m.courseId)}">${thoat(m.courseId)}: ${thoat(m.title)}</option>`
        ).join('');

        $('cay-mon').innerHTML = chonMon;
        $('tc-mon').innerHTML = chonMon;

        const chonKhoa = khoa.map((k) =>
            `<option value="${thoat(k)}">${thoat(k)}</option>`).join('');

        $('tc-khoa').innerHTML = '<option value="">(tất cả)</option>' + chonKhoa;
        $('sv-khoa').innerHTML = chonKhoa;
        $('sv-khoa').value = 'Comp. Sci.';

        await napLop();
        await napSinhVien();

        // Dựng sẵn cây của môn đầu danh sách để mở ứng dụng là thấy ngay kết quả.
        $('cay-chay').click();
    } catch (e) {
        console.error(e);
        document.querySelector('main').insertAdjacentHTML('afterbegin',
            thongBao('sai', 'Không kết nối được CSDL demo: ' + thoat(e.message)));
    }
})();
