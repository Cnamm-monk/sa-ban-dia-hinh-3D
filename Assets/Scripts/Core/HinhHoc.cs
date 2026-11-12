// HinhHoc.cs — các hàm toán dùng chung: ghép mềm, khoảng cách tới đường gấp khúc, làm mềm đường cong.
using System.Collections.Generic;
using UnityEngine;

namespace SaBan3D
{
    public static class HinhHoc
    {
        // Chuyển mượt từ 0 đến 1 khi x đi từ a đến b
        public static float Buoc(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        // Lấy giá trị lớn hơn nhưng bo tròn chỗ giao nhau (k = bề rộng vùng bo, mét)
        public static float SmoothMax(float a, float b, float k)
        {
            float t = Mathf.Clamp01(0.5f + 0.5f * (a - b) / k);
            return Mathf.Lerp(b, a, t) + k * t * (1f - t);
        }

        public static float SmoothMin(float a, float b, float k) => -SmoothMax(-a, -b, k);

        // Khoảng cách từ điểm (x, z) tới đường gấp khúc pts
        public static float KhoangCach(float x, float z, List<Vector2> pts)
        {
            if (pts == null || pts.Count == 0) return float.MaxValue;
            var p = new Vector2(x, z);
            if (pts.Count == 1) return Vector2.Distance(p, pts[0]);
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

        // Bản nhanh cho vòng lặp lớn: dùng mảng và phép tính số thực thô (không tạo Vector2 trung gian)
        public static float KhoangCach(float x, float z, Vector2[] pts)
        {
            int n = pts.Length;
            if (n == 0) return float.MaxValue;
            float best = float.MaxValue;
            if (n == 1) { float ex = x - pts[0].x, ez = z - pts[0].y; return Mathf.Sqrt(ex * ex + ez * ez); }
            for (int i = 0; i < n - 1; i++)
            {
                float dd = BinhPhuongKhoangCachDoan(x, z, pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y);
                if (dd < best) best = dd;
            }
            return Mathf.Sqrt(best);
        }

        // Bình phương khoảng cách từ (x, z) tới đoạn thẳng A-B
        public static float BinhPhuongKhoangCachDoan(float x, float z, float ax, float az, float bx, float bz)
        {
            float abx = bx - ax, abz = bz - az;
            float len2 = abx * abx + abz * abz;
            float t = len2 > 1e-6f ? ((x - ax) * abx + (z - az) * abz) / len2 : 0f;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            float ex = x - (ax + abx * t), ez = z - (az + abz * t);
            return ex * ex + ez * ez;
        }

        // Khoảng cách nhỏ nhất tới một nhóm đường
        public static float KhoangCachMin(Vector2 p, List<List<Vector2>> ds)
        {
            float best = float.MaxValue;
            foreach (var l in ds) best = Mathf.Min(best, KhoangCach(p.x, p.y, l));
            return best;
        }

        // Giao điểm của hai đoạn thẳng A-B và C-D; trả về false nếu không cắt nhau
        public static bool GiaoDoan(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 giao)
        {
            giao = Vector2.zero;
            float rx = b.x - a.x, ry = b.y - a.y, sx = d.x - c.x, sy = d.y - c.y;
            float mau = rx * sy - ry * sx;
            if (Mathf.Abs(mau) < 1e-6f) return false;                 // song song
            float t = ((c.x - a.x) * sy - (c.y - a.y) * sx) / mau;
            float u = ((c.x - a.x) * ry - (c.y - a.y) * rx) / mau;
            if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
            giao = new Vector2(a.x + rx * t, a.y + ry * t);
            return true;
        }

        // Làm mềm đường gấp khúc bằng spline Catmull-Rom; chia = số đoạn nhỏ giữa 2 điểm khống chế
        public static List<Vector2> LamMin(List<Vector2> p, int chia)
        {
            var kq = new List<Vector2>();
            if (p == null) return kq;
            if (p.Count < 2) { kq.AddRange(p); return kq; }
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
