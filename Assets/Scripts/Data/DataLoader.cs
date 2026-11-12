// DataLoader.cs — cửa duy nhất nhận dữ liệu của ứng dụng.
// Bước 1: đọc StreamingAssets/Data/diahinh.json; thiếu tệp hoặc tệp lỗi thì dùng dữ liệu dự phòng trong mã.
// Bước 5 sẽ mở rộng: kiểm tra phiên bản và tải bản mới từ GitHub khi khởi động.
using System;
using System.IO;
using UnityEngine;

namespace SaBan3D
{
    public class DataLoader : MonoBehaviour
    {
        [Tooltip("Đường dẫn tệp địa hình, tính từ thư mục StreamingAssets")]
        public string tepDiaHinh = "Data/diahinh.json";

        public DuLieuDiaHinh DuLieu { get; private set; }
        public string NguonDuLieu { get; private set; } = "chưa nạp";   // hiện trên màn hình để biết đang dùng dữ liệu nào

        string DuongDan => Path.Combine(Application.streamingAssetsPath, tepDiaHinh);

        // Nạp (hoặc nạp lại) dữ liệu địa hình. Luôn trả về dữ liệu dùng được.
        public DuLieuDiaHinh Nap()
        {
            string p = DuongDan;
            try
            {
                if (File.Exists(p))
                {
                    var d = JsonUtility.FromJson<DuLieuDiaHinh>(File.ReadAllText(p));
                    string loi = d == null ? "tệp rỗng" : d.KiemTra();
                    if (loi == null)
                    {
                        DuLieu = d;
                        NguonDuLieu = $"{tepDiaHinh} (phiên bản {d.phienBan})";
                        Debug.Log("[DataLoader] Đã đọc " + p);
                        return DuLieu;
                    }
                    Debug.LogWarning($"[DataLoader] {tepDiaHinh} không hợp lệ: {loi}. Dùng dữ liệu dự phòng.");
                    NguonDuLieu = $"dự phòng (tệp lỗi: {loi})";
                }
                else
                {
                    Debug.LogWarning("[DataLoader] Không thấy " + p + ". Dùng dữ liệu dự phòng.");
                    NguonDuLieu = "dự phòng (không thấy tệp JSON)";
                }
            }
            catch (Exception e)
            {
                // Thường là JSON sai cú pháp: thiếu dấu phẩy, ngoặc
                Debug.LogError($"[DataLoader] Lỗi đọc {tepDiaHinh}: {e.Message}. Dùng dữ liệu dự phòng.");
                NguonDuLieu = "dự phòng (JSON sai cú pháp)";
            }
            DuLieu = DuLieuDiaHinh.MacDinh();
            return DuLieu;
        }

        // Chuột phải vào component trong Inspector để gọi: ghi dữ liệu dự phòng ra tệp JSON
        [ContextMenu("Xuất dữ liệu dự phòng ra JSON")]
        void XuatMacDinh()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DuongDan));
            File.WriteAllText(DuongDan, JsonUtility.ToJson(DuLieuDiaHinh.MacDinh(), true));
            Debug.Log("[DataLoader] Đã ghi " + DuongDan);
        }
    }
}
