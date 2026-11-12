// DuLieuDiaHinh.cs — cấu trúc dữ liệu của một khu vực địa hình.
// Tên trường ở đây chính là tên khóa trong tệp StreamingAssets/Data/diahinh.json (đọc bằng JsonUtility).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaBan3D
{
    [Serializable]
    public class DiemCao
    {
        public string ten;
        public Vector2 viTri;          // x = hướng Đông, y = hướng Bắc (mét)
        public float doCao;            // độ cao đỉnh (m)
        public float banKinh;          // bán kính chân đồi (m)
        public float keoDai = 1f;      // 1 = đồi tròn; > 1 = kéo dài thành sống núi
        public float huong;            // hướng trục dài, độ (0 = theo trục Đông)
        public float heSoSuon = 1f;    // < 1 = sườn bên phải trục dài dốc hơn
    }

    [Serializable]
    public class DuongNet              // dùng chung cho sông, suối, đường giao thông
    {
        public string ten;
        public float rong;             // bề rộng (m)
        public List<Vector2> diem = new List<Vector2>();   // các điểm khống chế
    }

    [Serializable]
    public class DuLieuDiaHinh
    {
        public int phienBan = 1;
        public string tenKhuVuc = "Khu vực mẫu 1 - Đồi núi ven sông";

        // Kích thước
        public float rong = 2000f;          // theo hướng Đông (trục X của Unity)
        public float dai = 1200f;           // theo hướng Bắc (trục Z của Unity)
        public float doCaoToiDa = 260f;     // trần độ cao của Terrain

        // Nền và thủy văn
        public float doCaoNen = 22f;
        public float bienDoNhieu = 3f;      // độ gồ ghề của vùng đồng bằng
        public int hatGiong = 7;            // đổi số này để có nền gồ ghề khác
        public float mucNuoc = 17f;
        public float doSauLong = 2.5f;
        public float doDocBo = 1f;          // độ dốc bờ thung lũng sông: 1 = 45 độ

        // Bản đồ
        public float khoangCaoDeu = 10f;    // đường bình độ con; cứ 5 đường có 1 đường cái
        public float buocLuoi = 250f;       // lưới tọa độ giả lập
        public float gocToaDoX = 2300000f;  // X (hướng Bắc) giả lập tại mép Nam
        public float gocToaDoY = 500000f;   // Y (hướng Đông) giả lập tại mép Tây

        // Yếu tố địa hình
        public List<DiemCao> diemCao = new List<DiemCao>();
        public List<DuongNet> songSuoi = new List<DuongNet>();
        public List<DuongNet> duong = new List<DuongNet>();

        // Dân cư và vật chuẩn
        public string tenKhuDanCu = "Thôn Đông";
        public Vector2 tamKhuDanCu = new Vector2(760, 300);
        public float banKinhKhuDanCu = 80f;
        public int soNha = 14;
        public string tenVatChuan = "Tháp nước";
        public Vector2 viTriVatChuan = new Vector2(1300, 480);

        // Kiểm tra dữ liệu đọc từ tệp có dùng được không; trả về null nếu ổn, ngược lại là mô tả lỗi
        public string KiemTra()
        {
            if (rong <= 0 || dai <= 0) return "rong và dai phải lớn hơn 0";
            if (doCaoToiDa <= 0) return "doCaoToiDa phải lớn hơn 0";
            if (diemCao == null || songSuoi == null || duong == null) return "thiếu danh sách diemCao / songSuoi / duong";
            foreach (var c in diemCao)
            {
                if (c.banKinh <= 0) return $"điểm cao '{c.ten}' có banKinh <= 0";
                if (c.doCao > doCaoToiDa) return $"điểm cao '{c.ten}' cao hơn doCaoToiDa";
            }
            foreach (var s in songSuoi) if (s.diem == null || s.diem.Count < 2) return $"sông suối '{s.ten}' cần ít nhất 2 điểm";
            foreach (var r in duong) if (r.diem == null || r.diem.Count < 2) return $"đường '{r.ten}' cần ít nhất 2 điểm";
            return null;
        }

        // Dữ liệu dự phòng, dùng khi không có hoặc không đọc được tệp JSON
        public static DuLieuDiaHinh MacDinh()
        {
            var d = new DuLieuDiaHinh();
            d.diemCao.Add(new DiemCao { ten = "Sống núi Bắc", viTri = new Vector2(520, 820), doCao = 215, banKinh = 300, keoDai = 1.8f, huong = 25, heSoSuon = 0.5f });
            d.diemCao.Add(new DiemCao { ten = "Đồi Tròn",     viTri = new Vector2(1000, 930), doCao = 178, banKinh = 220 });
            d.diemCao.Add(new DiemCao { ten = "Đồi Đông Nam", viTri = new Vector2(1600, 360), doCao = 142, banKinh = 260 });
            d.diemCao.Add(new DiemCao { ten = "Đồi Tây Nam",  viTri = new Vector2(230, 260),  doCao = 96,  banKinh = 200 });

            d.songSuoi.Add(new DuongNet { ten = "Sông A", rong = 24, diem = new List<Vector2> {
                new Vector2(0, 560), new Vector2(300, 520), new Vector2(620, 600), new Vector2(900, 640),
                new Vector2(1150, 660), new Vector2(1400, 620), new Vector2(1700, 700), new Vector2(2000, 660) } });
            d.songSuoi.Add(new DuongNet { ten = "Suối B", rong = 8, diem = new List<Vector2> {
                new Vector2(1450, 1200), new Vector2(1380, 1000), new Vector2(1260, 820), new Vector2(1180, 665) } });

            d.duong.Add(new DuongNet { ten = "Đường 1", rong = 8, diem = new List<Vector2> {
                new Vector2(0, 200), new Vector2(450, 330), new Vector2(900, 420), new Vector2(1080, 560),
                new Vector2(1110, 700), new Vector2(1150, 900), new Vector2(1250, 1200) } });
            d.duong.Add(new DuongNet { ten = "Đường 2", rong = 6, diem = new List<Vector2> {
                new Vector2(900, 420), new Vector2(1300, 330), new Vector2(1700, 160), new Vector2(2000, 120) } });
            return d;
        }
    }
}
