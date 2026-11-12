// CameraController.cs — điều khiển góc nhìn quanh sa bàn.
// Chuột phải kéo: xoay | Cuộn: zoom | WASD hoặc chuột giữa kéo: di chuyển
// 1 = Tổng quan (nhìn thẳng từ trên, Bắc lên trên) | 2 = Nhìn xiên
using UnityEngine;

namespace SaBan3D
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public float tocDoXoay = 0.25f;       // độ trên mỗi pixel kéo chuột
        public float tocDoZoom = 0.12f;       // tỉ lệ đổi khoảng cách mỗi nấc cuộn
        public float tocDoDiChuyen = 500f;    // m/giây khi camera ở xa 1500 m

        Camera cam;
        Vector3 tam;                          // điểm camera đang nhìn vào
        float khoangCach = 1700f, yaw = -20f, pitch = 40f;
        float rong = 2000f, dai = 1200f;      // kích thước khu vực, dùng cho góc Tổng quan

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 10000f;
        }

        // Gọi sau khi dựng địa hình: đưa camera về giữa khu vực, góc nhìn xiên
        public void DatKhuVuc(float rongKhuVuc, float daiKhuVuc)
        {
            rong = rongKhuVuc; dai = daiKhuVuc;
            NhinXien();
        }

        // Bay tới nhìn cận cảnh một điểm (bước 3 và bước 4 sẽ dùng)
        public void NhinToi(Vector3 diem, float cach = 160f)
        {
            tam = diem; khoangCach = cach; pitch = 25f;
        }

        public void NhinXien()
        {
            tam = new Vector3(rong / 2, 30f, dai / 2);
            pitch = 40f; yaw = -20f;
            khoangCach = Mathf.Max(rong, dai) * 0.85f;
        }

        public void TongQuan()
        {
            tam = new Vector3(rong / 2, 0f, dai / 2);
            yaw = 0f; pitch = 89.9f;                                  // Bắc (+Z) hướng lên trên màn hình
            float nuaDoc = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float nuaNgang = Mathf.Atan(Mathf.Tan(nuaDoc) * cam.aspect);
            khoangCach = 1.08f * Mathf.Max(dai / 2 / Mathf.Tan(nuaDoc), rong / 2 / Mathf.Tan(nuaNgang));
        }

        void LateUpdate()
        {
            // Xoay
            if (NhapLieu.ChuotPhaiGiu)
            {
                Vector2 dl = NhapLieu.DoDichChuot;
                yaw += dl.x * tocDoXoay;
                pitch = Mathf.Clamp(pitch - dl.y * tocDoXoay, 5f, 89.9f);
            }
            // Zoom
            float cuon = NhapLieu.Cuon;
            if (cuon != 0f) khoangCach = Mathf.Clamp(khoangCach * (1f - Mathf.Sign(cuon) * tocDoZoom), 30f, 6000f);

            // Di chuyển theo hướng đang nhìn (càng xa càng đi nhanh)
            Quaternion qYaw = Quaternion.Euler(0, yaw, 0);
            Vector3 truoc = qYaw * Vector3.forward, phai = qYaw * Vector3.right;
            Vector2 wasd = NhapLieu.WASD;
            tam += (truoc * wasd.y + phai * wasd.x) * tocDoDiChuyen * (khoangCach / 1500f) * Time.deltaTime;
            if (NhapLieu.ChuotGiuaGiu)
            {
                Vector2 dl = NhapLieu.DoDichChuot;
                tam -= (phai * dl.x + truoc * dl.y) * khoangCach * 0.0015f;
            }

            if (NhapLieu.VuaNhan(KeyCode.Alpha1)) TongQuan();
            if (NhapLieu.VuaNhan(KeyCode.Alpha2)) NhinXien();

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = tam - transform.forward * khoangCach;
        }
    }
}
