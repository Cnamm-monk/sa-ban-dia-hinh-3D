// CameraQuanSat.cs — gắn vào Main Camera.
// Chuột phải kéo: xoay | Cuộn: zoom | WASD hoặc chuột giữa kéo: di chuyển
// Chuột trái: chọn điểm, xem tọa độ / độ cao / độ dốc (Physics.Raycast)
// 1 = Tổng quan (nhìn thẳng từ trên, Bắc lên trên) | 2 = Nhìn xiên | 3 = Cận cảnh điểm đang chọn
using UnityEngine;

namespace SaBan3D.ViDu
{
    [RequireComponent(typeof(Camera))]
    public class CameraQuanSat : MonoBehaviour
    {
        public DiaHinhMau diaHinh;
        public float tocDoXoay = 0.25f;
        public float tocDoZoom = 0.12f;
        public float tocDoDiChuyen = 500f;

        Camera cam;
        Vector3 tam;
        float khoangCach = 1700f, yaw = -20f, pitch = 40f;
        Vector3? diemChon;
        GameObject dauChon;
        string thongTin = "Nhấp chuột trái vào địa hình để xem thông tin điểm.";
        GUIStyle kieuChu;

        void Start()
        {
            cam = GetComponent<Camera>();
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 10000f;
            if (diaHinh == null) diaHinh = FindFirstObjectByType<DiaHinhMau>();
            if (diaHinh != null) tam = new Vector3(diaHinh.duLieu.rong / 2, 30f, diaHinh.duLieu.dai / 2);
        }

        void LateUpdate()
        {
            // Xoay
            if (NhapLieu.ChuotPhaiGiu)
            {
                Vector2 dl = NhapLieu.DoDichChuot;
                yaw += dl.x * tocDoXoay;
                pitch = Mathf.Clamp(pitch - dl.y * tocDoXoay, 5f, 89f);
            }
            // Zoom
            float cuon = NhapLieu.Cuon;
            if (cuon != 0f) khoangCach = Mathf.Clamp(khoangCach * (1f - Mathf.Sign(cuon) * tocDoZoom), 30f, 6000f);

            // Di chuyển theo hướng đang nhìn (tốc độ tỉ lệ với độ cao camera)
            Quaternion qYaw = Quaternion.Euler(0, yaw, 0);
            Vector3 truoc = qYaw * Vector3.forward, phai = qYaw * Vector3.right;
            Vector2 wasd = NhapLieu.WASD;
            tam += (truoc * wasd.y + phai * wasd.x) * tocDoDiChuyen * (khoangCach / 1500f) * Time.deltaTime;
            if (NhapLieu.ChuotGiuaGiu)
            {
                Vector2 dl = NhapLieu.DoDichChuot;
                tam -= (phai * dl.x + truoc * dl.y) * khoangCach * 0.0015f;
            }

            // Các góc nhìn đặt sẵn
            if (NhapLieu.VuaNhan(KeyCode.Alpha1)) TongQuan();
            if (NhapLieu.VuaNhan(KeyCode.Alpha2)) { pitch = 40f; yaw = -20f; khoangCach = 1700f; }
            if (NhapLieu.VuaNhan(KeyCode.Alpha3) && diemChon.HasValue) { tam = diemChon.Value; khoangCach = 160f; pitch = 25f; }

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = tam - transform.forward * khoangCach;

            if (NhapLieu.ChuotTraiNhan) ChonDiem();
        }

        void TongQuan()
        {
            if (diaHinh == null) return;
            var d = diaHinh.duLieu;
            tam = new Vector3(d.rong / 2, 0f, d.dai / 2);
            yaw = 0f; pitch = 89.9f;                                  // Bắc (+Z) hướng lên trên màn hình
            float nuaDoc = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float nuaNgang = Mathf.Atan(Mathf.Tan(nuaDoc) * cam.aspect);
            khoangCach = 1.08f * Mathf.Max(d.dai / 2 / Mathf.Tan(nuaDoc), d.rong / 2 / Mathf.Tan(nuaNgang));
        }

        void ChonDiem()
        {
            Ray tia = cam.ScreenPointToRay(NhapLieu.ViTriChuot);
            if (!Physics.Raycast(tia, out RaycastHit hit, 20000f) || diaHinh == null || diaHinh.TerrainChinh == null) return;

            var d = diaHinh.duLieu;
            var t = diaHinh.TerrainChinh;
            Vector3 p = hit.point;
            float u = (p.x - t.transform.position.x) / t.terrainData.size.x;
            float v = (p.z - t.transform.position.z) / t.terrainData.size.z;
            float doCaoMatDat = t.SampleHeight(p) + t.transform.position.y;
            float doDoc = t.terrainData.GetSteepness(u, v);
            float x = d.gocToaDoX + p.z;   // quy ước bản đồ quân sự: X theo hướng Bắc
            float y = d.gocToaDoY + p.x;   //                         Y theo hướng Đông

            string doiTuong = hit.collider is TerrainCollider ? "Mặt đất" : hit.collider.name;
            string dangDoc = doDoc < 5 ? "bằng phẳng" : doDoc < 15 ? "thoải" : doDoc < 30 ? "dốc" : "rất dốc";
            thongTin = $"Đối tượng: {doiTuong}\n" +
                       $"Tọa độ giả lập: X = {x:0} m, Y = {y:0} m\n" +
                       $"Độ cao: {doCaoMatDat:0.0} m\n" +
                       $"Độ dốc: {doDoc:0.0}° ({dangDoc})" +
                       (doCaoMatDat < d.mucNuoc ? "\n(Dưới mặt nước – lòng sông/suối)" : "");

            diemChon = new Vector3(p.x, doCaoMatDat, p.z);
            if (dauChon == null)
            {
                dauChon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dauChon.name = "DauDiemChon";
                Destroy(dauChon.GetComponent<Collider>());
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh != null) dauChon.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = Color.yellow };
                dauChon.transform.localScale = new Vector3(3f, 15f, 3f);
            }
            dauChon.transform.position = diemChon.Value + Vector3.up * 15f;
        }

        void OnGUI()
        {
            if (kieuChu == null) kieuChu = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15, wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
            string cheDo = diaHinh != null && diaHinh.CheDoBanDo ? "Bản đồ" : "Tự nhiên";
            GUI.Box(new Rect(10, 10, 430, 128),
                $"Chế độ: {cheDo}   (M: đổi chế độ, L: bật/tắt nhãn)\n" +
                "Chuột phải kéo: xoay | Cuộn: zoom | WASD / chuột giữa: di chuyển\n" +
                "1: Tổng quan  2: Nhìn xiên  3: Cận cảnh điểm chọn\n" +
                (diaHinh != null && diaHinh.CheDoBanDo ? $"Khoảng cao đều {diaHinh.duLieu.khoangCaoDeu:0} m, lưới {diaHinh.duLieu.buocLuoi:0} m" : ""),
                kieuChu);
            GUI.Box(new Rect(10, 146, 430, 110), thongTin, kieuChu);
        }
    }
}
