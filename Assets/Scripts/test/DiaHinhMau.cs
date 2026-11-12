// DiaHinhMau.cs — sinh một dải địa hình mẫu 2 km x 1,2 km từ dữ liệu giả lập.
// Gắn vào một GameObject rỗng rồi bấm Play. Mọi yếu tố (điểm cao, sông, đường...) sửa được trong Inspector
// hoặc trong tệp StreamingAssets/DiaHinhMau.json (chuột phải component -> "Xuất dữ liệu ra JSON").
//
// Phím tắt khi chạy:  M = đổi chế độ Tự nhiên / Bản đồ (màu phân tầng + đường bình độ + lưới tọa độ)
//                     L = bật/tắt lớp nhãn
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SaBan3D.ViDu
{
    // ======================= DỮ LIỆU =======================

    [Serializable]
    public class DiemCao
    {
        public string ten;
        [Tooltip("Vị trí đỉnh (x = hướng Đông, y = hướng Bắc), đơn vị mét")]
        public Vector2 viTri;
        [Tooltip("Độ cao đỉnh (m)")]
        public float doCao;
        [Tooltip("Bán kính chân đồi (m)")]
        public float banKinh;
        [Tooltip("1 = đồi tròn; > 1 = kéo dài thành sống núi")]
        public float keoDai = 1f;
        [Tooltip("Hướng trục dài, tính bằng độ (0 = theo trục Đông)")]
        public float huong;
        [Tooltip("< 1 = sườn bên phải trục dài dốc hơn (đường bình độ dày hơn)")]
        public float heSoSuon = 1f;
    }

    [Serializable]
    public class DuongNet   // dùng chung cho sông, suối, đường giao thông
    {
        public string ten;
        [Tooltip("Bề rộng (m)")]
        public float rong;
        [Tooltip("Các điểm khống chế; script tự làm mềm thành đường cong")]
        public List<Vector2> diem = new List<Vector2>();
    }

    [Serializable]
    public class DuLieuDiaHinh
    {
        [Header("Kích thước")]
        public float rong = 2000f;          // theo hướng Đông (trục X của Unity)
        public float dai = 1200f;           // theo hướng Bắc (trục Z của Unity)
        public float doCaoToiDa = 260f;     // trần độ cao của Terrain

        [Header("Nền và thủy văn")]
        public float doCaoNen = 22f;
        public float bienDoNhieu = 3f;      // độ gồ ghề của vùng đồng bằng
        public int hatGiong = 7;            // đổi số này để có nền gồ ghề khác
        public float mucNuoc = 17f;
        public float doSauLong = 2.5f;
        [Tooltip("Độ dốc bờ thung lũng sông: 1 = 45 độ")]
        public float doDocBo = 1f;

        [Header("Bản đồ")]
        public float khoangCaoDeu = 10f;    // đường bình độ con; cứ 5 đường thì có 1 đường cái
        public float buocLuoi = 250f;       // lưới tọa độ giả lập
        public float gocToaDoX = 2300000f;  // X (hướng Bắc) giả lập tại mép Nam
        public float gocToaDoY = 500000f;   // Y (hướng Đông) giả lập tại mép Tây

        [Header("Yếu tố địa hình")]
        public List<DiemCao> diemCao = new List<DiemCao>();
        public List<DuongNet> songSuoi = new List<DuongNet>();
        public List<DuongNet> duong = new List<DuongNet>();

        [Header("Dân cư và vật chuẩn")]
        public string tenKhuDanCu = "Thôn Đông";
        public Vector2 tamKhuDanCu = new Vector2(760, 300);
        public float banKinhKhuDanCu = 80f;
        public int soNha = 14;
        public string tenVatChuan = "Tháp nước";
        public Vector2 viTriVatChuan = new Vector2(1300, 480);

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

    // ======================= SINH ĐỊA HÌNH =======================

    public class DiaHinhMau : MonoBehaviour
    {
        public DuLieuDiaHinh duLieu = DuLieuDiaHinh.MacDinh();

        [Header("Tùy chọn")]
        [Tooltip("Nếu có tệp StreamingAssets/DiaHinhMau.json thì đọc dữ liệu từ tệp (bỏ qua giá trị trong Inspector)")]
        public bool docTuJsonNeuCo = true;
        [Tooltip("Độ phân giải heightmap: 257, 513 hoặc 1025")]
        public int doPhanGiaiHeightmap = 513;
        [Tooltip("Độ phân giải ảnh phủ bề mặt; 2048 nét hơn nhưng sinh chậm hơn")]
        public int doPhanGiaiTexture = 1024;
        [Tooltip("Chiều cao chữ của nhãn khi nhìn gần (m)")]
        public float coChu = 14f;

        public Terrain TerrainChinh { get; private set; }
        public bool CheDoBanDo { get; private set; }

        Texture2D texTuNhien, texBanDo;
        TerrainLayer lopTuNhien, lopBanDo;
        GameObject nhomNhan, matNuoc;
        readonly List<Transform> dsNhan = new List<Transform>();
        List<List<Vector2>> songMin, duongMin;   // đường cong đã làm mềm
        Font fontNhan;
        Camera cam;

        string DuongDanJson => Path.Combine(Application.streamingAssetsPath, "DiaHinhMau.json");

        void Start()
        {
            if (docTuJsonNeuCo && File.Exists(DuongDanJson))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(DuongDanJson), duLieu);
                Debug.Log("[DiaHinhMau] Đã đọc dữ liệu từ " + DuongDanJson);
            }
            Tao();
        }

        [ContextMenu("Xuất dữ liệu ra JSON")]
        void XuatJson()
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.WriteAllText(DuongDanJson, JsonUtility.ToJson(duLieu, true));
            Debug.Log("[DiaHinhMau] Đã ghi " + DuongDanJson);
        }

        void Tao()
        {
            var t0 = Time.realtimeSinceStartup;
            transform.position = Vector3.zero;   // mọi tọa độ trong dữ liệu tính từ gốc (0,0,0)

            songMin = new List<List<Vector2>>();
            foreach (var s in duLieu.songSuoi) songMin.Add(LamMin(s.diem, 8));
            duongMin = new List<List<Vector2>>();
            foreach (var r in duLieu.duong) duongMin.Add(LamMin(r.diem, 8));

            TaoTerrain();
            TaoTexture();
            TaoMatNuoc();
            TaoDiaVatVaNhan();
            DatCheDo(false);

            Debug.Log($"[DiaHinhMau] Sinh xong trong {Time.realtimeSinceStartup - t0:0.0} s");
        }

        // ---------- 1. Heightmap ----------
        void TaoTerrain()
        {
            var d = duLieu;
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

            var go = Terrain.CreateTerrainGameObject(td);            // tự gắn Terrain + TerrainCollider
            go.name = "DiaHinh";
            go.transform.SetParent(transform, false);
            TerrainChinh = go.GetComponent<Terrain>();
            var shTerrain = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (shTerrain != null) TerrainChinh.materialTemplate = new Material(shTerrain);
            TerrainChinh.heightmapPixelError = 2;
            TerrainChinh.basemapDistance = 10000;                         // luôn dùng ảnh phủ độ phân giải đầy đủ
        }

        float TinhDoCao(float x, float z)
        {
            var d = duLieu;

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
                h = SmoothMax(h, hc, 12f);                           // ghép mềm, tự tạo yên ngựa giữa 2 đỉnh
            }

            // Sông suối: khoét thung lũng và lòng sông
            for (int i = 0; i < d.songSuoi.Count; i++)
            {
                float k = KhoangCach(x, z, songMin[i]);
                float nuaRong = d.songSuoi[i].rong * 0.5f;
                float bo = d.mucNuoc + 1f + Mathf.Max(0f, k - nuaRong) * d.doDocBo;
                h = SmoothMin(h, bo, 6f);
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
            var d = duLieu;
            var td = TerrainChinh.terrainData;
            int T = doPhanGiaiTexture;
            float mPx = d.rong / T;                                  // số mét mỗi pixel theo trục X
            float doCaoMax = d.doCaoNen;
            foreach (var c in d.diemCao) doCaoMax = Mathf.Max(doCaoMax, c.doCao);

            var hs = new float[T * T];
            var doc = new float[T * T];
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = x / (T - 1f), v = y / (T - 1f);
                    hs[y * T + x] = td.GetInterpolatedHeight(u, v);
                    doc[y * T + x] = td.GetSteepness(u, v);
                }

            // Đánh dấu đường bình độ: 1 = đường con, 2 = đường cái
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
            for (int y = T - 2; y >= 1; y--)                         // làm đậm đường cái thêm 1 pixel
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
            Color bdLuoi = new Color(0.20f, 0.30f, 0.60f), bdDanCu = new Color(0.30f, 0.30f, 0.30f);
            Color[] phanTang = {
                new Color(0.78f, 0.89f, 0.68f), new Color(0.95f, 0.94f, 0.70f),
                new Color(0.92f, 0.80f, 0.56f), new Color(0.80f, 0.64f, 0.46f) };

            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    int i = y * T + x;
                    float wx = x / (T - 1f) * d.rong, wz = y / (T - 1f) * d.dai;
                    float h = hs[i], dc = doc[i];

                    float dDuong = float.MaxValue; float rongDuong = 0;
                    for (int r = 0; r < duongMin.Count; r++)
                    {
                        float k = KhoangCach(wx, wz, duongMin[r]);
                        if (k < dDuong) { dDuong = k; rongDuong = d.duong[r].rong * 0.5f; }
                    }
                    float dSong = float.MaxValue; float rongSong = 0;
                    for (int r = 0; r < songMin.Count; r++)
                    {
                        float k = KhoangCach(wx, wz, songMin[r]);
                        if (k < dSong) { dSong = k; rongSong = d.songSuoi[r].rong * 0.5f; }
                    }
                    float dDanCu = Vector2.Distance(new Vector2(wx, wz), d.tamKhuDanCu);
                    float bien = 0.92f + 0.16f * Mathf.PerlinNoise(wx * 0.05f, wz * 0.05f);

                    // --- Tự nhiên ---
                    Color c = Color.Lerp(coThap, rung, Buoc(60f, 110f, h));
                    c = Color.Lerp(c, datTrong, Buoc(18f, 28f, dc));
                    c = Color.Lerp(c, da, Buoc(30f, 40f, dc));
                    c *= bien;
                    c = Color.Lerp(c, mauDanCuTN, 0.5f * (1f - Buoc(d.banKinhKhuDanCu * 0.8f, d.banKinhKhuDanCu, dDanCu)));
                    c = Color.Lerp(cat, c, Buoc(d.mucNuoc + 0.3f, d.mucNuoc + 1.6f, h));
                    if (h < d.mucNuoc) c = dayNuoc;
                    c = Color.Lerp(c, mauDuongTN, 1f - Buoc(rongDuong, rongDuong + 1.5f, dDuong));
                    c.a = 1f;
                    cTN[i] = c;

                    // --- Bản đồ ---
                    float t = Mathf.InverseLerp(d.doCaoNen - 5f, doCaoMax, h) * (phanTang.Length - 1);
                    int k0 = Mathf.Clamp(Mathf.FloorToInt(t), 0, phanTang.Length - 2);
                    Color b = Color.Lerp(phanTang[k0], phanTang[k0 + 1], t - k0);
                    if (bd[i] == 1) b = Color.Lerp(b, bdBinhDo, 0.7f);
                    else if (bd[i] >= 2) b = Color.Lerp(b, bdBinhDo, 1f) * 0.85f;
                    if (h < d.mucNuoc) b = bdNuoc;
                    if (dSong < rongSong + mPx) b = bdSong;
                    if (dDanCu < d.banKinhKhuDanCu && dDanCu > d.banKinhKhuDanCu - 2.5f * mPx) b = bdDanCu;
                    if (Mathf.Repeat(wx, d.buocLuoi) < 1.2f * mPx || Mathf.Repeat(wz, d.buocLuoi) < 1.2f * mPx)
                        b = Color.Lerp(b, bdLuoi, 0.6f);
                    if (dDuong < rongDuong + mPx) b = bdDuong;
                    b.a = 1f;
                    cBD[i] = b;
                }

            texTuNhien = TaoAnh(T, cTN, "TuNhien");
            texBanDo = TaoAnh(T, cBD, "BanDo");

            lopTuNhien = new TerrainLayer { diffuseTexture = texTuNhien, tileSize = new Vector2(d.rong, d.dai), tileOffset = Vector2.zero };
            lopBanDo   = new TerrainLayer { diffuseTexture = texBanDo,   tileSize = new Vector2(d.rong, d.dai), tileOffset = Vector2.zero };
            td.terrainLayers = new[] { lopTuNhien };                  // 1 ảnh phủ trọn dải địa hình
            td.alphamapResolution = 32;
            var a = new float[32, 32, 1];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) a[y, x, 0] = 1f;
            td.SetAlphamaps(0, 0, a);
        }

        static Texture2D TaoAnh(int T, Color32[] px, string ten)
        {
            var tex = new Texture2D(T, T, TextureFormat.RGBA32, true) { name = ten, wrapMode = TextureWrapMode.Clamp, anisoLevel = 8 };
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        // ---------- 3. Mặt nước ----------
        void TaoMatNuoc()
        {
            var d = duLieu;
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

        // ---------- 4. Địa vật và nhãn ----------
        void TaoDiaVatVaNhan()
        {
            var d = duLieu;
            fontNhan = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nhomNhan = new GameObject("Lop_Nhan");
            nhomNhan.transform.SetParent(transform, false);
            var nhomDiaVat = new GameObject("Lop_DiaVat");
            nhomDiaVat.transform.SetParent(transform, false);

            // Điểm cao: chấm đỏ + nhãn độ cao đọc từ terrain thật (không lấy số trong dữ liệu)
            var vlDiem = TaoVatLieu(new Color(0.8f, 0.1f, 0.1f));
            foreach (var c in d.diemCao)
            {
                Vector3 p = TrenMatDat(c.viTri);
                var cham = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cham.name = "DiemCao_" + c.ten;
                cham.transform.SetParent(nhomDiaVat.transform, false);
                cham.transform.position = p;
                cham.transform.localScale = Vector3.one * 6f;
                cham.GetComponent<MeshRenderer>().sharedMaterial = vlDiem;
                TaoNhan($"{c.ten}\nĐiểm cao {Mathf.RoundToInt(p.y)}", p + Vector3.up * 8f, new Color(0.55f, 0.1f, 0.05f));
            }

            // Nhãn sông suối, đường: đặt ở điểm giữa đường cong
            for (int i = 0; i < d.songSuoi.Count; i++)
                TaoNhan(d.songSuoi[i].ten, TrenMatDat(songMin[i][songMin[i].Count / 2]) + Vector3.up * 6f, new Color(0.1f, 0.3f, 0.75f));
            for (int i = 0; i < d.duong.Count; i++)
                TaoNhan(d.duong[i].ten, TrenMatDat(duongMin[i][duongMin[i].Count / 3]) + Vector3.up * 6f, new Color(0.75f, 0.2f, 0.05f));

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
                if (KhoangCachMin(p2, duongMin) < 14f || KhoangCachMin(p2, songMin) < 35f) continue;

                float xoay = (float)rng.NextDouble() * 90f;
                Vector3 p = TrenMatDat(p2);
                var nha = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nha.name = "Nha";
                nha.transform.SetParent(nhomDiaVat.transform, false);
                nha.transform.SetPositionAndRotation(p + Vector3.up * 2.5f, Quaternion.Euler(0, xoay, 0));
                nha.transform.localScale = new Vector3(8, 5, 10);
                nha.GetComponent<MeshRenderer>().sharedMaterial = vlTuong;
                var mai = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mai.name = "Nha";
                mai.transform.SetParent(nhomDiaVat.transform, false);
                mai.transform.SetPositionAndRotation(p + Vector3.up * 5.5f, Quaternion.Euler(0, xoay, 0));
                mai.transform.localScale = new Vector3(9, 1.2f, 11);
                mai.GetComponent<MeshRenderer>().sharedMaterial = vlMai;
                dem++;
            }
            TaoNhan("Khu dân cư " + d.tenKhuDanCu, TrenMatDat(d.tamKhuDanCu) + Vector3.up * 20f, new Color(0.15f, 0.15f, 0.15f));

            // Vật chuẩn: cột cao 24 m
            Vector3 pv = TrenMatDat(d.viTriVatChuan);
            var cot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);   // Cylinder gốc cao 2
            cot.name = "VatChuan_" + d.tenVatChuan;
            cot.transform.SetParent(nhomDiaVat.transform, false);
            cot.transform.position = pv + Vector3.up * 12f;
            cot.transform.localScale = new Vector3(5, 12, 5);
            cot.GetComponent<MeshRenderer>().sharedMaterial = TaoVatLieu(new Color(0.75f, 0.75f, 0.78f));
            TaoNhan(d.tenVatChuan + " (vật chuẩn)", pv + Vector3.up * 30f, Color.black);
        }

        void TaoNhan(string noiDung, Vector3 viTri, Color mau)
        {
            var g = new GameObject("Nhan_" + noiDung.Split('\n')[0]);
            g.transform.SetParent(nhomNhan.transform, false);
            g.transform.position = viTri;
            var tm = g.AddComponent<TextMesh>();
            tm.text = noiDung;
            tm.font = fontNhan;
            tm.fontSize = 100;
            tm.characterSize = coChu / 10f;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = mau;
            g.GetComponent<MeshRenderer>().sharedMaterial = fontNhan.material;
            dsNhan.Add(g.transform);
        }

        // ---------- Điều khiển chế độ ----------
        void Update()
        {
            if (NhapLieu.VuaNhan(KeyCode.M)) DatCheDo(!CheDoBanDo);
            if (NhapLieu.VuaNhan(KeyCode.L) && nhomNhan != null) nhomNhan.SetActive(!nhomNhan.activeSelf);
        }

        public void DatCheDo(bool banDo)
        {
            CheDoBanDo = banDo;
            if (lopTuNhien == null) return;
            TerrainChinh.terrainData.terrainLayers = new[] { banDo ? lopBanDo : lopTuNhien };
            if (matNuoc != null) matNuoc.SetActive(!banDo);
        }

        // Nhãn luôn quay về phía camera, xa thì phóng to để vẫn đọc được
        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            foreach (var t in dsNhan)
            {
                t.rotation = cam.transform.rotation;
                float k = Vector3.Distance(cam.transform.position, t.position);
                t.localScale = Vector3.one * Mathf.Max(1f, k / 500f);
            }
        }

        // ======================= HÀM PHỤ =======================

        public Vector3 TrenMatDat(Vector2 p)
        {
            var w = new Vector3(p.x, 0, p.y);
            w.y = TerrainChinh.SampleHeight(w) + TerrainChinh.transform.position.y;
            return w;
        }

        static Material TaoVatLieu(Color mau)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            return new Material(sh) { color = mau };
        }

        static float Buoc(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        static float SmoothMax(float a, float b, float k)
        {
            float t = Mathf.Clamp01(0.5f + 0.5f * (a - b) / k);
            return Mathf.Lerp(b, a, t) + k * t * (1f - t);
        }

        static float SmoothMin(float a, float b, float k) => -SmoothMax(-a, -b, k);

        static float KhoangCach(float x, float z, List<Vector2> pts)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                float dd = (p - (a + ab * t)).sqrMagnitude;
                if (dd < best) best = dd;
            }
            return Mathf.Sqrt(best);
        }

        static float KhoangCachMin(Vector2 p, List<List<Vector2>> ds)
        {
            float best = float.MaxValue;
            foreach (var l in ds) best = Mathf.Min(best, KhoangCach(p.x, p.y, l));
            return best;
        }

        // Làm mềm đường gấp khúc bằng spline Catmull-Rom
        static List<Vector2> LamMin(List<Vector2> p, int chia)
        {
            var kq = new List<Vector2>();
            if (p == null || p.Count < 2) { if (p != null) kq.AddRange(p); return kq; }
            for (int i = 0; i < p.Count - 1; i++)
            {
                Vector2 p0 = p[Mathf.Max(i - 1, 0)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(i + 2, p.Count - 1)];
                for (int k = 0; k < chia; k++)
                {
                    float t = k / (float)chia, t2 = t * t, t3 = t2 * t;
                    kq.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            kq.Add(p[p.Count - 1]);
            return kq;
        }
    }
}
