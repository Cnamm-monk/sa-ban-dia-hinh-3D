// SaBanApp.cs — điểm khởi động của ứng dụng. Gắn vào một GameObject rỗng trong scene Main.
// Nối các module theo thứ tự:
//   DataLoader đọc dữ liệu -> TerrainManager dựng địa hình -> LayerManager dựng các lớp thông tin
//   -> CameraController đặt góc nhìn -> UIManager dựng giao diện.
//
// Phím tắt:  M = đổi chế độ Tự nhiên / Bản đồ      R = đọc lại tệp JSON và dựng lại (không cần tắt ứng dụng)
using UnityEngine;

namespace SaBan3D
{
    [RequireComponent(typeof(DataLoader), typeof(TerrainManager), typeof(LayerManager))]
    public class SaBanApp : MonoBehaviour
    {
        DataLoader duLieu;
        TerrainManager diaHinh;
        LayerManager lop;
        UIManager giaoDien;
        CameraController mayQuay;
        GUIStyle kieuChu;

        void Start()
        {
            // Lấy module; nếu object chưa gắn thì tự gắn thêm
            duLieu = LayHoacThem<DataLoader>();
            diaHinh = LayHoacThem<TerrainManager>();
            lop = LayHoacThem<LayerManager>();
            giaoDien = LayHoacThem<UIManager>();

            // Tự gắn CameraController vào Main Camera nếu chưa có
            var cam = Camera.main;
            if (cam == null)
            {
                var g = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = g.AddComponent<Camera>();
            }
            mayQuay = cam.GetComponent<CameraController>();
            if (mayQuay == null) mayQuay = cam.gameObject.AddComponent<CameraController>();

            NapVaDung(true);
            giaoDien.Tao(diaHinh, lop, mayQuay);
        }

        T LayHoacThem<T>() where T : Component
        {
            var c = GetComponent<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }

        // datLaiCamera = true: đưa camera về giữa khu vực; false: giữ nguyên góc đang nhìn
        void NapVaDung(bool datLaiCamera)
        {
            var d = duLieu.Nap();
            diaHinh.Tao(d);
            lop.Tao(diaHinh);
            if (datLaiCamera) mayQuay.DatKhuVuc(d.rong, d.dai);
        }

        void Update()
        {
            if (NhapLieu.VuaNhan(KeyCode.M)) diaHinh.DatCheDo(!diaHinh.CheDoBanDo);
            if (NhapLieu.VuaNhan(KeyCode.R)) NapVaDung(false);
        }

        // Bảng thông tin tạm thời ở góc trên trái; bước 3 sẽ thay bằng giao diện UGUI
        void OnGUI()
        {
            if (kieuChu == null)
                kieuChu = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15, wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
            string ten = duLieu.DuLieu != null ? duLieu.DuLieu.tenKhuVuc : "";
            GUI.Box(new Rect(10, 10, 470, 92),
                $"{ten}\n" +
                $"Dữ liệu: {duLieu.NguonDuLieu}\n" +
                "Chuột phải: xoay | Cuộn: zoom | WASD: di chuyển\n" +
                "R: nạp lại JSON",
                kieuChu);
        }
    }
}
