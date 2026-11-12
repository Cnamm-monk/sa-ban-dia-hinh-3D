// LayerManager.cs — quản lý 5 lớp thông tin bản đồ phủ trên sa bàn. Mỗi lớp là một GameObject cha, bật/tắt bằng SetActive.
//   KyHieu    : ký hiệu địa vật (khu dân cư, vật chuẩn, cầu)
//   ChuThich  : tên điểm cao, sông suối, đường, khu dân cư, vật chuẩn
//   DoCao     : nhãn độ cao tại các điểm cao (đọc từ terrain thật, không lấy số trong dữ liệu)
//   LuoiToaDo : lưới tọa độ giả lập và số ghi lưới ở mép Nam, mép Tây
//   DiemMoc   : ký hiệu tam giác tại các điểm cao
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SaBan3D
{
    public enum LopThongTin { KyHieu = 0, ChuThich = 1, DoCao = 2, LuoiToaDo = 3, DiemMoc = 4 }

    public class LayerManager : MonoBehaviour
    {
        // Tên hiển thị trên giao diện, đúng thứ tự của enum LopThongTin
        public static readonly string[] TenLop = { "Ký hiệu địa vật", "Chú thích tên", "Nhãn độ cao", "Lưới tọa độ", "Điểm mốc địa hình" };

        [Tooltip("Chiều cao chữ khi nhìn gần (m)")]
        public float coChu = 14f;
        [Tooltip("Kích thước ký hiệu khi nhìn gần (m)")]
        public float coKyHieu = 16f;
        [Tooltip("Bề rộng nét lưới tọa độ (m)")]
        public float rongNetLuoi = 2.5f;

        GameObject goc;                                             // cha của cả 5 lớp
        readonly GameObject[] nhom = new GameObject[5];
        readonly bool[] dangBat = { true, true, true, true, true };

        // Các vật luôn quay mặt về camera, kèm tỉ lệ gốc của từng vật
        readonly List<Transform> bangQuay = new List<Transform>();
        readonly List<float> tiLeGoc = new List<float>();

        TerrainManager diaHinh;
        Font font;
        Camera cam;
        Material vlLuoi;
        Sprite spTamGiac, spVuong, spVatChuan, spCau;

        static readonly Color mauNau = new Color(0.33f, 0.12f, 0.03f), mauXanh = new Color(0.03f, 0.18f, 0.60f);
        static readonly Color mauDo = new Color(0.62f, 0.10f, 0.03f), mauDen = new Color(0.08f, 0.08f, 0.08f);
        static readonly Color mauLuoi = new Color(0.15f, 0.25f, 0.60f);

        // ======================= DÙNG TỪ BÊN NGOÀI =======================

        public void Bat(LopThongTin lop, bool hien)
        {
            dangBat[(int)lop] = hien;
            if (nhom[(int)lop] != null) nhom[(int)lop].SetActive(hien);
        }

        public bool DangBat(LopThongTin lop) => dangBat[(int)lop];

        // Dựng lại toàn bộ các lớp theo địa hình hiện tại; giữ nguyên trạng thái bật/tắt
        public void Tao(TerrainManager terrain)
        {
            XoaCu();
            diaHinh = terrain;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (spTamGiac == null) TaoBoKyHieu();

            goc = new GameObject("LopThongTin");
            for (int i = 0; i < nhom.Length; i++)
            {
                nhom[i] = new GameObject("Lop_" + (LopThongTin)i);
                nhom[i].transform.SetParent(goc.transform, false);
            }

            TaoDiemCao();
            TaoSongVaDuong();
            TaoDanCuVaVatChuan();
            TaoCau();
            TaoLuoi();

            for (int i = 0; i < nhom.Length; i++) nhom[i].SetActive(dangBat[i]);
        }

        void XoaCu()
        {
            if (goc != null) Destroy(goc);
            bangQuay.Clear();
            tiLeGoc.Clear();
        }

        // ======================= NỘI DUNG TỪNG LỚP =======================

        void TaoDiemCao()
        {
            foreach (var c in diaHinh.DuLieu.diemCao)
            {
                Vector3 p = diaHinh.TrenMatDat(c.viTri) + Vector3.up * 2f;
                TaoKyHieu(LopThongTin.DiemMoc, spTamGiac, p, "DiemMoc_" + c.ten);
                // Số độ cao nằm bên phải ký hiệu: các dấu cách đầu dòng đẩy chữ sang phải
                TaoChu(LopThongTin.DoCao, "    " + Mathf.RoundToInt(p.y - 2f), p, mauNau, TextAnchor.LowerLeft);
                // Tên nằm phía trên ký hiệu: hai dòng trống cuối đẩy chữ lên cao
                TaoChu(LopThongTin.ChuThich, c.ten + "\n \n ", p, mauNau, TextAnchor.LowerCenter);
            }
        }

        void TaoSongVaDuong()
        {
            var d = diaHinh.DuLieu;
            for (int i = 0; i < d.songSuoi.Count; i++)
            {
                var l = diaHinh.SongMin[i];
                Vector3 p = diaHinh.TrenMatDat(l[l.Count / 4]);       // 1/4 chiều dài, tránh chỗ cầu và ngã ba sông
                p.y = Mathf.Max(p.y, d.mucNuoc) + 6f;
                TaoChu(LopThongTin.ChuThich, d.songSuoi[i].ten, p, mauXanh, TextAnchor.MiddleCenter);
            }
            for (int i = 0; i < d.duong.Count; i++)
            {
                var l = diaHinh.DuongMin[i];
                Vector3 p = diaHinh.TrenMatDat(l[l.Count * 3 / 5]) + Vector3.up * 6f;
                TaoChu(LopThongTin.ChuThich, d.duong[i].ten, p, mauDo, TextAnchor.MiddleCenter);
            }
        }

        void TaoDanCuVaVatChuan()
        {
            var d = diaHinh.DuLieu;
            if (d.soNha > 0)
            {
                Vector3 p = diaHinh.TrenMatDat(d.tamKhuDanCu) + Vector3.up * 12f;      // cao hơn mái nhà
                TaoKyHieu(LopThongTin.KyHieu, spVuong, p, "KyHieu_DanCu");
                TaoChu(LopThongTin.ChuThich, "Khu dân cư " + d.tenKhuDanCu + "\n \n ", p, mauDen, TextAnchor.LowerCenter);
            }
            if (!string.IsNullOrEmpty(d.tenVatChuan))
            {
                Vector3 p = diaHinh.TrenMatDat(d.viTriVatChuan) + Vector3.up * 26f;    // trên đỉnh cột 24 m
                TaoKyHieu(LopThongTin.KyHieu, spVatChuan, p, "KyHieu_VatChuan");
                TaoChu(LopThongTin.ChuThich, d.tenVatChuan + "\n \n ", p, mauDen, TextAnchor.LowerCenter);
            }
        }

        // Cầu: đặt ký hiệu tại mọi chỗ đường giao thông cắt sông suối
        void TaoCau()
        {
            var d = diaHinh.DuLieu;
            foreach (var duong in diaHinh.DuongMin)
                foreach (var song in diaHinh.SongMin)
                    for (int i = 0; i < duong.Count - 1; i++)
                        for (int j = 0; j < song.Count - 1; j++)
                        {
                            if (!HinhHoc.GiaoDoan(duong[i], duong[i + 1], song[j], song[j + 1], out Vector2 g)) continue;
                            Vector3 p = diaHinh.TrenMatDat(g);
                            p.y = Mathf.Max(p.y, d.mucNuoc) + 4f;
                            TaoKyHieu(LopThongTin.KyHieu, spCau, p, "KyHieu_Cau");
                            TaoChu(LopThongTin.ChuThich, "Cầu\n \n ", p, mauDen, TextAnchor.LowerCenter);
                        }
        }

        void TaoLuoi()
        {
            var d = diaHinh.DuLieu;
            if (d.buocLuoi <= 0) return;
            if (vlLuoi == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                vlLuoi = new Material(sh) { color = mauLuoi };
            }
            const float buocMau = 10f;      // cứ 10 m lấy một điểm để đường lưới ôm theo mặt đất

            // Đường dọc (Bắc - Nam): mỗi đường ứng với một giá trị Y (hướng Đông)
            for (float gx = 0f; gx <= d.rong + 0.01f; gx += d.buocLuoi)
            {
                var diem = new List<Vector3>();
                for (float z = 0f; z <= d.dai + 0.01f; z += buocMau) diem.Add(DiemLuoi(Mathf.Min(gx, d.rong), Mathf.Min(z, d.dai)));
                TaoDuongKe(diem, "Luoi_Y_" + gx);
                Vector3 p = DiemLuoi(Mathf.Min(gx, d.rong), 0f) + new Vector3(0, 2f, -12f);
                TaoChu(LopThongTin.LuoiToaDo, SoKm(d.gocToaDoY + gx), p, mauLuoi, TextAnchor.UpperCenter, 0.75f);
            }
            // Đường ngang (Đông - Tây): mỗi đường ứng với một giá trị X (hướng Bắc)
            for (float gz = 0f; gz <= d.dai + 0.01f; gz += d.buocLuoi)
            {
                var diem = new List<Vector3>();
                for (float x = 0f; x <= d.rong + 0.01f; x += buocMau) diem.Add(DiemLuoi(Mathf.Min(x, d.rong), Mathf.Min(gz, d.dai)));
                TaoDuongKe(diem, "Luoi_X_" + gz);
                Vector3 p = DiemLuoi(0f, Mathf.Min(gz, d.dai)) + new Vector3(-12f, 2f, 0);
                TaoChu(LopThongTin.LuoiToaDo, SoKm(d.gocToaDoX + gz), p, mauLuoi, TextAnchor.MiddleRight, 0.75f);
            }
        }

        // Điểm của đường lưới: nằm trên mặt đất 1,5 m; qua sông thì nằm trên mặt nước
        Vector3 DiemLuoi(float x, float z)
        {
            Vector3 p = diaHinh.TrenMatDat(new Vector2(x, z));
            p.y = Mathf.Max(p.y, diaHinh.DuLieu.mucNuoc) + 1.5f;
            return p;
        }

        // Đổi mét sang km, 2 chữ số thập phân, dấu phẩy kiểu Việt Nam: 500250 m -> "500,25"
        static string SoKm(float met) => (met / 1000f).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

        // ======================= HÀM TẠO VẬT THỂ =======================

        void TaoDuongKe(List<Vector3> diem, string ten)
        {
            var g = new GameObject(ten);
            g.transform.SetParent(nhom[(int)LopThongTin.LuoiToaDo].transform, false);
            var lr = g.AddComponent<LineRenderer>();
            lr.sharedMaterial = vlLuoi;
            lr.useWorldSpace = true;
            lr.widthMultiplier = rongNetLuoi;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.positionCount = diem.Count;
            lr.SetPositions(diem.ToArray());
        }

        void TaoKyHieu(LopThongTin lop, Sprite hinh, Vector3 viTri, string ten)
        {
            var g = new GameObject(ten);
            g.transform.SetParent(nhom[(int)lop].transform, false);
            g.transform.position = viTri;
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = hinh;
            ThemBangQuay(g.transform, coKyHieu);
        }

        // tiLe: 1 = cỡ chữ chuẩn (coChu); nhỏ hơn 1 để chữ phụ nhỏ hơn
        void TaoChu(LopThongTin lop, string noiDung, Vector3 viTri, Color mau, TextAnchor neo, float tiLe = 1f)
        {
            var g = new GameObject("Chu_" + noiDung.Trim().Split('\n')[0]);
            g.transform.SetParent(nhom[(int)lop].transform, false);
            g.transform.position = viTri;
            DatChu(g, noiDung, mau, neo, 1);

            // Viền trắng quanh chữ: 4 bản chữ trắng lệch về 4 góc, vẽ trước bản chữ màu
            float lech = coChu * 0.035f;
            for (int i = 0; i < 4; i++)
            {
                var vien = new GameObject("Vien");
                vien.transform.SetParent(g.transform, false);
                vien.transform.localPosition = new Vector3(i < 2 ? lech : -lech, i % 2 == 0 ? lech : -lech, 0.01f);
                DatChu(vien, noiDung, Color.white, neo, 0);
            }

            ThemBangQuay(g.transform, tiLe);
        }

        void DatChu(GameObject g, string noiDung, Color mau, TextAnchor neo, int thuTuVe)
        {
            var chu = g.AddComponent<TextMesh>();
            chu.text = noiDung;
            chu.font = font;
            chu.fontSize = 100;                 // cỡ lớn cho nét, kích thước thật do characterSize quyết định
            chu.characterSize = coChu / 10f;
            chu.anchor = neo;
            chu.alignment = TextAlignment.Center;
            chu.color = mau;
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = font.material;
            r.sortingOrder = thuTuVe;
        }

        void ThemBangQuay(Transform t, float tiLe)
        {
            t.localScale = Vector3.one * tiLe;
            bangQuay.Add(t);
            tiLeGoc.Add(tiLe);
        }

        // Chữ và ký hiệu luôn quay về phía camera; ở xa thì phóng to để vẫn đọc được
        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            Quaternion q = cam.transform.rotation;
            Vector3 pc = cam.transform.position;
            for (int i = 0; i < bangQuay.Count; i++)
            {
                var t = bangQuay[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                t.rotation = q;
                t.localScale = Vector3.one * (tiLeGoc[i] * Mathf.Max(1f, Vector3.Distance(pc, t.position) / 500f));
            }
        }

        // ======================= VẼ KÝ HIỆU =======================
        // Mỗi ký hiệu là một ảnh 64 x 64 vẽ bằng công thức; (u, v) chạy từ -1 đến 1, gốc ở giữa ảnh.

        void TaoBoKyHieu()
        {
            Color den = mauDen, trang = Color.white, rong = new Color(0, 0, 0, 0);

            // Điểm mốc: tam giác viền đen, nền trắng, chấm đen ở giữa
            Vector2 a = new Vector2(0f, 0.9f), b = new Vector2(-0.85f, -0.7f), c = new Vector2(0.85f, -0.7f);
            spTamGiac = VeKyHieu((u, v) =>
            {
                var p = new Vector2(u, v);
                float k = Mathf.Min(KcCanh(a, b, p), Mathf.Min(KcCanh(b, c, p), KcCanh(c, a, p)));   // > 0 là nằm trong tam giác
                if (k < 0f) return rong;
                if (k < 0.16f) return den;
                return u * u + (v + 0.15f) * (v + 0.15f) < 0.03f ? den : trang;
            });

            // Khu dân cư: ô vuông đen viền trắng
            spVuong = VeKyHieu((u, v) =>
            {
                float m = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                if (m > 0.85f) return rong;
                return m > 0.68f ? trang : den;
            });

            // Vật chuẩn: vòng tròn đen, nền trắng, chấm đen ở tâm
            spVatChuan = VeKyHieu((u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v);
                if (r > 0.88f) return rong;
                if (r > 0.66f) return den;
                return r < 0.25f ? den : trang;
            });

            // Cầu: hai vạch đen song song, giữa là nền trắng
            spCau = VeKyHieu((u, v) =>
            {
                if (Mathf.Abs(u) > 0.9f || Mathf.Abs(v) > 0.55f) return rong;
                return Mathf.Abs(v) > 0.32f ? den : trang;
            });
        }

        // Khoảng cách có dấu từ P tới cạnh A-B (dương khi P nằm bên trái hướng A -> B)
        static float KcCanh(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a, ap = p - a;
            return (ab.x * ap.y - ab.y * ap.x) / ab.magnitude;
        }

        static Sprite VeKyHieu(System.Func<float, float, Color> mauTai)
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    px[y * S + x] = mauTai((x + 0.5f) / S * 2f - 1f, (y + 0.5f) / S * 2f - 1f);
            tex.SetPixels32(px);
            tex.Apply();
            // Tâm xoay ở đáy giữa: ký hiệu "đứng" trên điểm của nó, không cắm xuống đất khi phóng to.
            // pixelsPerUnit = S nên ký hiệu rộng đúng 1 đơn vị, kích thước thật do coKyHieu quyết định.
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0f), S);
        }
    }
}
