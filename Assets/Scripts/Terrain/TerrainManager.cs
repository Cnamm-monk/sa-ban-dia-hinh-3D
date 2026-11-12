// TerrainManager.cs — dựng địa hình 3D từ dữ liệu: heightmap, ảnh phủ (Tự nhiên / Bản đồ), mặt nước, địa vật.
// Lưới tọa độ, ký hiệu và nhãn chữ thuộc về LayerManager.
// Gọi Tao(duLieu) để dựng; gọi lại lần nữa thì xóa địa hình cũ và dựng mới.
using System.Collections.Generic;
using UnityEngine;

namespace SaBan3D
{
    public class TerrainManager : MonoBehaviour
    {
        [Tooltip("Độ phân giải heightmap: 257, 513 hoặc 1025. Máy yếu thì dùng 257.")]
        public int doPhanGiaiHeightmap = 513;
        [Tooltip("Độ phân giải ảnh phủ bề mặt; 2048 nét hơn nhưng dựng chậm hơn")]
        public int doPhanGiaiTexture = 1024;

        // ----- Những thứ các module khác được dùng -----
        public Terrain DiaHinh { get; private set; }
        public DuLieuDiaHinh DuLieu { get; private set; }
        public bool CheDoBanDo { get; private set; }
        public Transform NhomDiaVat { get; private set; }
        public List<List<Vector2>> SongMin { get; private set; }     // sông suối đã làm mềm
        public List<List<Vector2>> DuongMin { get; private set; }    // đường giao thông đã làm mềm

        Texture2D texTuNhien, texBanDo;
        TerrainLayer lopTuNhien, lopBanDo;
        GameObject matNuoc;
        Vector2[][] songArr, duongArr;     // bản mảng của SongMin, DuongMin để tính nhanh
        float[,] doCaoChuan;               // heightmap đã chuẩn hóa 0..1, chỉ số [z, x]

        // ======================= DỰNG =======================

        public void Tao(DuLieuDiaHinh duLieu)
        {
            float t0 = Time.realtimeSinceStartup;
            XoaCu();
            DuLieu = duLieu;
            transform.position = Vector3.zero;   // mọi tọa độ trong dữ liệu tính từ gốc (0,0,0)

            SongMin = new List<List<Vector2>>();
            foreach (var s in duLieu.songSuoi) SongMin.Add(HinhHoc.LamMin(s.diem, 8));
            DuongMin = new List<List<Vector2>>();
            foreach (var r in duLieu.duong) DuongMin.Add(HinhHoc.LamMin(r.diem, 8));
            songArr = SongMin.ConvertAll(l => l.ToArray()).ToArray();
            duongArr = DuongMin.ConvertAll(l => l.ToArray()).ToArray();

            TaoTerrain();
            TaoTexture();
            TaoMatNuoc();
            TaoDiaVat();
            DatCheDo(CheDoBanDo);

            Debug.Log($"[TerrainManager] Dựng '{duLieu.tenKhuVuc}' trong {Time.realtimeSinceStartup - t0:0.0} s");
        }

        void XoaCu()
        {
            if (DiaHinh != null && DiaHinh.terrainData != null) Destroy(DiaHinh.terrainData);
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            if (texTuNhien != null) Destroy(texTuNhien);
            if (texBanDo != null) Destroy(texBanDo);
            if (lopTuNhien != null) Destroy(lopTuNhien);
            if (lopBanDo != null) Destroy(lopBanDo);
            DiaHinh = null; matNuoc = null; NhomDiaVat = null;
        }

        // ---------- 1. Heightmap ----------
        void TaoTerrain()
        {
            var d = DuLieu;
            var td = new TerrainData();
            td.heightmapResolution = doPhanGiaiHeightmap;          // phải đặt trước size
            td.size = new Vector3(d.rong, d.doCaoToiDa, d.dai);
            int n = td.heightmapResolution;

            var h = new float[n, n];                                 // chỉ số [hàng = z, cột = x]
            for (int iz = 0; iz < n; iz++)
                for (int ix = 0; ix < n; ix++)
                {
                    float x = ix / (float)(n - 1) * d.rong;
                    float z = iz / (float)(n - 1) * d.dai;
                    h[iz, ix] = Mathf.Clamp01(TinhDoCao(x, z) / d.doCaoToiDa);
                }
            td.SetHeights(0, 0, h);
            doCaoChuan = h;

            var go = Terrain.CreateTerrainGameObject(td);            // tự gắn Terrain + TerrainCollider
            go.name = "DiaHinh";
            go.transform.SetParent(transform, false);
            DiaHinh = go.GetComponent<Terrain>();
            var shTerrain = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (shTerrain != null) DiaHinh.materialTemplate = new Material(shTerrain);
            DiaHinh.heightmapPixelError = 2;
            DiaHinh.basemapDistance = 10000;                         // luôn dùng ảnh phủ độ phân giải đầy đủ
        }

        // Độ cao (m) tại điểm (x, z) tính từ dữ liệu tham số
        float TinhDoCao(float x, float z)
        {
            var d = DuLieu;

            // Nền đồng bằng hơi gồ ghề (2 tầng nhiễu Perlin)
            float s = d.hatGiong * 13.7f;
            float nhieu = (Mathf.PerlinNoise(x * 0.004f + s, z * 0.004f + s) - 0.5f) * 1.4f
                        + (Mathf.PerlinNoise(x * 0.015f + 2 * s, z * 0.015f + 2 * s) - 0.5f) * 0.6f;
            float h = d.doCaoNen + nhieu * d.bienDoNhieu;

            // Điểm cao / sống núi: hàm chuông Gauss, có thể kéo dài và lệch sườn
            foreach (var c in d.diemCao)
            {
                float a = c.huong * Mathf.Deg2Rad;
                float dx = x - c.viTri.x, dz = z - c.viTri.y;
                float u = dx * Mathf.Cos(a) + dz * Mathf.Sin(a);     // dọc trục dài
                float v = -dx * Mathf.Sin(a) + dz * Mathf.Cos(a);    // vuông góc trục dài
                float ru = c.banKinh * Mathf.Max(0.1f, c.keoDai);
                float rv = c.banKinh * (v < 0 ? Mathf.Max(0.1f, c.heSoSuon) : 1f);
                float q = u * u / (ru * ru) + v * v / (rv * rv);
                float hc = d.doCaoNen + (c.doCao - d.doCaoNen) * Mathf.Exp(-3f * q);
                h = HinhHoc.SmoothMax(h, hc, 12f);                   // ghép mềm, tự tạo yên ngựa giữa 2 đỉnh
            }

            // Sông suối: khoét thung lũng và lòng sông
            for (int i = 0; i < d.songSuoi.Count; i++)
            {
                float k = HinhHoc.KhoangCach(x, z, songArr[i]);
                float nuaRong = d.songSuoi[i].rong * 0.5f;
                float bo = d.mucNuoc + 1f + Mathf.Max(0f, k - nuaRong) * d.doDocBo;
                h = HinhHoc.SmoothMin(h, bo, 6f);
                if (k < nuaRong)
                {
                    float t = k / nuaRong;
                    h = Mathf.Min(h, d.mucNuoc - d.doSauLong * (1f - t * t));
                }
            }
            return h;
        }

        // ---------- 2. Ảnh phủ bề mặt: Tự nhiên và Bản đồ ----------
        void TaoTexture()
        {
            var d = DuLieu;
            var td = DiaHinh.terrainData;
            int T = doPhanGiaiTexture;
            float sx = d.rong / (T - 1f), sz = d.dai / (T - 1f);     // số mét giữa 2 pixel theo X và theo Z
            float mPx = Mathf.Max(sx, sz);
            float doCaoMax = d.doCaoNen;
            foreach (var c in d.diemCao) doCaoMax = Mathf.Max(doCaoMax, c.doCao);

            // Độ cao tại từng pixel: nội suy từ heightmap đã tính (không gọi lại Terrain từng điểm)
            int n = doCaoChuan.GetLength(0);
            var hs = new float[T * T];
            for (int y = 0; y < T; y++)
            {
                float fz = y / (T - 1f) * (n - 1);
                int z0 = Mathf.Min((int)fz, n - 2); float tz = fz - z0;
                for (int x = 0; x < T; x++)
                {
                    float fx = x / (T - 1f) * (n - 1);
                    int x0 = Mathf.Min((int)fx, n - 2); float tx = fx - x0;
                    float h0 = doCaoChuan[z0, x0] + (doCaoChuan[z0, x0 + 1] - doCaoChuan[z0, x0]) * tx;
                    float h1 = doCaoChuan[z0 + 1, x0] + (doCaoChuan[z0 + 1, x0 + 1] - doCaoChuan[z0 + 1, x0]) * tx;
                    hs[y * T + x] = (h0 + (h1 - h0) * tz) * d.doCaoToiDa;
                }
            }

            // Độ dốc (độ) tại từng pixel: tính từ chênh cao giữa các pixel lân cận
            var doc = new float[T * T];
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    int xa = Mathf.Max(x - 1, 0), xb = Mathf.Min(x + 1, T - 1);
                    int ya = Mathf.Max(y - 1, 0), yb = Mathf.Min(y + 1, T - 1);
                    float gx = (hs[y * T + xb] - hs[y * T + xa]) / ((xb - xa) * sx);
                    float gz = (hs[yb * T + x] - hs[ya * T + x]) / ((yb - ya) * sz);
                    doc[y * T + x] = Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
                }

            // Khoảng cách tới đường và sông: chỉ tính cho các pixel nằm gần từng đoạn (nhanh hơn nhiều so với tính mọi pixel)
            var dDuong = new float[T * T]; var rDuong = new float[T * T];
            var dSong = new float[T * T]; var rSong = new float[T * T];
            for (int i = 0; i < dDuong.Length; i++) { dDuong[i] = 1e9f; dSong[i] = 1e9f; }
            for (int r = 0; r < duongArr.Length; r++)
                VeKhoangCach(duongArr[r], d.duong[r].rong * 0.5f, 3f * mPx + 3f, dDuong, rDuong, T, sx, sz);
            for (int r = 0; r < songArr.Length; r++)
                VeKhoangCach(songArr[r], d.songSuoi[r].rong * 0.5f, 3f * mPx + 3f, dSong, rSong, T, sx, sz);

            // Đánh dấu đường bình độ: 1 = đường con, 2 = đường cái, 3 = phần làm đậm đường cái
            var bd = new byte[T * T];
            float ci = Mathf.Max(1f, d.khoangCaoDeu);
            for (int y = 0; y < T - 1; y++)
                for (int x = 0; x < T - 1; x++)
                {
                    int i = y * T + x;
                    if (hs[i] < d.mucNuoc) continue;
                    int l0 = Mathf.FloorToInt(hs[i] / ci);
                    int l1 = Mathf.FloorToInt(hs[i + 1] / ci);
                    int l2 = Mathf.FloorToInt(hs[i + T] / ci);
                    if (l0 == l1 && l0 == l2) continue;
                    int L = Mathf.Max(l0, Mathf.Max(l1, l2));
                    bd[i] = (byte)(L % 5 == 0 ? 2 : 1);
                }
            for (int y = T - 2; y >= 1; y--)
                for (int x = T - 2; x >= 1; x--)
                {
                    int i = y * T + x;
                    if (bd[i] == 2) { if (bd[i - 1] == 0) bd[i - 1] = 3; if (bd[i - T] == 0) bd[i - T] = 3; }
                }

            var cTN = new Color32[T * T];
            var cBD = new Color32[T * T];

            // Bảng màu
            Color coThap = new Color(0.36f, 0.52f, 0.24f), rung = new Color(0.20f, 0.36f, 0.17f);
            Color datTrong = new Color(0.47f, 0.40f, 0.30f), da = new Color(0.46f, 0.44f, 0.41f);
            Color cat = new Color(0.76f, 0.70f, 0.50f), dayNuoc = new Color(0.33f, 0.31f, 0.24f);
            Color mauDuongTN = new Color(0.58f, 0.46f, 0.33f), mauDanCuTN = new Color(0.50f, 0.48f, 0.42f);
            Color bdNuoc = new Color(0.55f, 0.78f, 0.93f), bdSong = new Color(0.20f, 0.50f, 0.80f);
            Color bdBinhDo = new Color(0.62f, 0.38f, 0.16f), bdDuong = new Color(0.86f, 0.30f, 0.12f);
            Color bdDanCu = new Color(0.30f, 0.30f, 0.30f);
            Color[] phanTang = {
                new Color(0.78f, 0.89f, 0.68f), new Color(0.95f, 0.94f, 0.70f),
                new Color(0.92f, 0.80f, 0.56f), new Color(0.80f, 0.64f, 0.46f) };

            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    int i = y * T + x;
                    float wx = x * sx, wz = y * sz;
                    float h = hs[i], dc = doc[i];
                    float kDuong = dDuong[i], rongDuong = rDuong[i];
                    float kSong = dSong[i], rongSong = rSong[i];
                    float ex = wx - d.tamKhuDanCu.x, ez = wz - d.tamKhuDanCu.y;
                    float dDanCu = Mathf.Sqrt(ex * ex + ez * ez);
                    float bien = 0.92f + 0.16f * Mathf.PerlinNoise(wx * 0.05f, wz * 0.05f);

                    // --- Tự nhiên ---
                    Color c = Color.Lerp(coThap, rung, HinhHoc.Buoc(60f, 110f, h));
                    c = Color.Lerp(c, datTrong, HinhHoc.Buoc(18f, 28f, dc));
                    c = Color.Lerp(c, da, HinhHoc.Buoc(30f, 40f, dc));
                    c *= bien;
                    if (d.soNha > 0 && dDanCu < d.banKinhKhuDanCu)
                        c = Color.Lerp(c, mauDanCuTN, 0.5f * (1f - HinhHoc.Buoc(d.banKinhKhuDanCu * 0.8f, d.banKinhKhuDanCu, dDanCu)));
                    c = Color.Lerp(cat, c, HinhHoc.Buoc(d.mucNuoc + 0.3f, d.mucNuoc + 1.6f, h));
                    if (h < d.mucNuoc) c = dayNuoc;
                    if (kDuong < rongDuong + 1.5f)
                        c = Color.Lerp(c, mauDuongTN, 1f - HinhHoc.Buoc(rongDuong, rongDuong + 1.5f, kDuong));
                    cTN[i] = c;

                    // --- Bản đồ ---
                    float t = Mathf.InverseLerp(d.doCaoNen - 5f, doCaoMax, h) * (phanTang.Length - 1);
                    int k0 = Mathf.Clamp(Mathf.FloorToInt(t), 0, phanTang.Length - 2);
                    Color b = Color.Lerp(phanTang[k0], phanTang[k0 + 1], t - k0);
                    if (bd[i] == 1) b = Color.Lerp(b, bdBinhDo, 0.7f);
                    else if (bd[i] >= 2) b = bdBinhDo * 0.85f;
                    if (h < d.mucNuoc) b = bdNuoc;
                    if (kSong < rongSong + mPx) b = bdSong;
                    if (d.soNha > 0 && dDanCu < d.banKinhKhuDanCu && dDanCu > d.banKinhKhuDanCu - 2.5f * mPx) b = bdDanCu;
                    if (kDuong < rongDuong + mPx) b = bdDuong;
                    cBD[i] = b;
                }

            texTuNhien = TaoAnh(T, cTN, "TuNhien");
            texBanDo = TaoAnh(T, cBD, "BanDo");

            // Mỗi chế độ là 1 lớp ảnh phủ trọn khu vực
            lopTuNhien = TaoLop(texTuNhien, d);
            lopBanDo = TaoLop(texBanDo, d);
            td.terrainLayers = new[] { lopTuNhien };
            td.alphamapResolution = 32;
            var a = new float[32, 32, 1];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) a[y, x, 0] = 1f;
            td.SetAlphamaps(0, 0, a);
        }

        // Ghi vào dArr khoảng cách nhỏ nhất tới đường gấp khúc pts, chỉ xét các pixel cách mỗi đoạn không quá (nuaRong + le)
        static void VeKhoangCach(Vector2[] pts, float nuaRong, float le, float[] dArr, float[] rArr, int T, float sx, float sz)
        {
            float bien = nuaRong + le;
            for (int s = 0; s < pts.Length - 1; s++)
            {
                float ax = pts[s].x, az = pts[s].y, bx = pts[s + 1].x, bz = pts[s + 1].y;
                int x0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(ax, bx) - bien) / sx), 0, T - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(ax, bx) + bien) / sx), 0, T - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(az, bz) - bien) / sz), 0, T - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(az, bz) + bien) / sz), 0, T - 1);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float k = Mathf.Sqrt(HinhHoc.BinhPhuongKhoangCachDoan(x * sx, y * sz, ax, az, bx, bz));
                        int i = y * T + x;
                        if (k < dArr[i]) { dArr[i] = k; rArr[i] = nuaRong; }
                    }
            }
        }

        // Ảnh không có kênh alpha: với Terrain của URP, kênh alpha bị hiểu là độ bóng, có alpha thì mặt đất bóng loáng
        static Texture2D TaoAnh(int T, Color32[] px, string ten)
        {
            var tex = new Texture2D(T, T, TextureFormat.RGB24, true) { name = ten, wrapMode = TextureWrapMode.Clamp, anisoLevel = 8 };
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        static TerrainLayer TaoLop(Texture2D anh, DuLieuDiaHinh d)
        {
            return new TerrainLayer
            {
                diffuseTexture = anh,
                tileSize = new Vector2(d.rong, d.dai),               // 1 ảnh phủ trọn khu vực
                tileOffset = Vector2.zero,
                smoothness = 0f,                                     // mặt đất nhám, không phản chiếu
                metallic = 0f,
                specular = Color.black
            };
        }

        // ---------- 3. Mặt nước ----------
        void TaoMatNuoc()
        {
            var d = DuLieu;
            matNuoc = GameObject.CreatePrimitive(PrimitiveType.Plane);  // Plane gốc rộng 10 x 10
            matNuoc.name = "MatNuoc";
            matNuoc.transform.SetParent(transform, false);
            matNuoc.transform.position = new Vector3(d.rong / 2, d.mucNuoc, d.dai / 2);
            matNuoc.transform.localScale = new Vector3(d.rong / 10f, 1f, d.dai / 10f);
            Destroy(matNuoc.GetComponent<Collider>());                    // để tia Raycast trúng lòng sông
            var m = TaoVatLieu(new Color(0.20f, 0.42f, 0.58f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.9f);
            matNuoc.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        // ---------- 4. Địa vật: nhà, vật chuẩn ----------
        void TaoDiaVat()
        {
            var d = DuLieu;
            var nhom = new GameObject("DiaVat");
            nhom.transform.SetParent(transform, false);
            NhomDiaVat = nhom.transform;

            // Khu dân cư: rải nhà ngẫu nhiên (cố định theo hạt giống), tránh đường và sông
            var rng = new System.Random(d.hatGiong);
            var vlTuong = TaoVatLieu(new Color(0.85f, 0.82f, 0.74f));
            var vlMai = TaoVatLieu(new Color(0.62f, 0.25f, 0.18f));
            int dem = 0;
            for (int thu = 0; thu < 1000 && dem < d.soNha; thu++)
            {
                float goc = (float)(rng.NextDouble() * Mathf.PI * 2);
                float bk = Mathf.Sqrt((float)rng.NextDouble()) * d.banKinhKhuDanCu * 0.85f;
                var p2 = d.tamKhuDanCu + new Vector2(Mathf.Cos(goc), Mathf.Sin(goc)) * bk;
                if (HinhHoc.KhoangCachMin(p2, DuongMin) < 14f || HinhHoc.KhoangCachMin(p2, SongMin) < 35f) continue;

                float xoay = (float)rng.NextDouble() * 90f;
                Vector3 p = TrenMatDat(p2);
                TaoKhoi(PrimitiveType.Cube, "Nha", p + Vector3.up * 2.5f, new Vector3(8, 5, 10), xoay, vlTuong);
                TaoKhoi(PrimitiveType.Cube, "Nha", p + Vector3.up * 5.5f, new Vector3(9, 1.2f, 11), xoay, vlMai);
                dem++;
            }

            // Vật chuẩn: cột cao 24 m (Cylinder gốc cao 2 nên scale y = 12)
            if (!string.IsNullOrEmpty(d.tenVatChuan))
            {
                Vector3 pv = TrenMatDat(d.viTriVatChuan);
                TaoKhoi(PrimitiveType.Cylinder, "VatChuan_" + d.tenVatChuan, pv + Vector3.up * 12f, new Vector3(5, 12, 5), 0f,
                        TaoVatLieu(new Color(0.75f, 0.75f, 0.78f)));
            }
        }

        void TaoKhoi(PrimitiveType loai, string ten, Vector3 viTri, Vector3 kichThuoc, float xoayY, Material vl)
        {
            var g = GameObject.CreatePrimitive(loai);
            g.name = ten;
            g.transform.SetParent(NhomDiaVat, false);
            g.transform.SetPositionAndRotation(viTri, Quaternion.Euler(0, xoayY, 0));
            g.transform.localScale = kichThuoc;
            g.GetComponent<MeshRenderer>().sharedMaterial = vl;
        }

        // ======================= DÙNG CHUNG =======================

        // Đổi chế độ hiển thị: false = Tự nhiên, true = Bản đồ (màu phân tầng + đường bình độ)
        public void DatCheDo(bool banDo)
        {
            CheDoBanDo = banDo;
            if (DiaHinh == null || lopTuNhien == null) return;
            DiaHinh.terrainData.terrainLayers = new[] { banDo ? lopBanDo : lopTuNhien };
            if (matNuoc != null) matNuoc.SetActive(!banDo);
        }

        // Điểm trên mặt đất ứng với tọa độ phẳng p (x = Đông, y = Bắc)
        public Vector3 TrenMatDat(Vector2 p)
        {
            var w = new Vector3(p.x, 0, p.y);
            w.y = DiaHinh.SampleHeight(w) + DiaHinh.transform.position.y;
            return w;
        }

        public static Material TaoVatLieu(Color mau)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            return new Material(sh) { color = mau };
        }
    }
}
