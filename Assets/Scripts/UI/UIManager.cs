// UIManager.cs — giao diện UGUI, dựng hoàn toàn bằng mã (không cần kéo thả trong Editor).
// Bước 2: bảng bên phải gồm 5 ô bật/tắt lớp thông tin, ô chế độ bản đồ, 2 nút góc nhìn.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaBan3D
{
    public class UIManager : MonoBehaviour
    {
        static readonly Color mauNen = new Color(0.08f, 0.10f, 0.12f, 0.85f);
        static readonly Color mauChu = new Color(0.94f, 0.94f, 0.90f);
        static readonly Color mauTieuDe = new Color(0.62f, 0.72f, 0.60f);
        static readonly Color mauDau = new Color(0.25f, 0.55f, 0.22f);
        static readonly Color mauNut = new Color(0.24f, 0.29f, 0.33f);

        Font font;
        GameObject canvasGo;
        Toggle oBanDo;
        TerrainManager diaHinh;
        LayerManager lop;
        CameraController mayQuay;

        // Dựng giao diện. Gọi một lần sau khi các module khác đã sẵn sàng.
        public void Tao(TerrainManager terrain, LayerManager layer, CameraController camera)
        {
            if (canvasGo != null) return;
            diaHinh = terrain; lop = layer; mayQuay = camera;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            DamBaoEventSystem();

            canvasGo = new GameObject("GiaoDien", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var tiLe = canvasGo.GetComponent<CanvasScaler>();
            tiLe.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;   // giao diện co giãn theo màn hình
            tiLe.referenceResolution = new Vector2(1280, 720);              // số nhỏ hơn thì giao diện to hơn
            tiLe.matchWidthOrHeight = 1f;                                     // bám theo chiều cao màn hình

            // Bảng ở góc trên bên phải, các dòng tự xếp từ trên xuống
            var bang = new GameObject("BangLop", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            bang.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)bang.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-14, -14);
            rt.sizeDelta = new Vector2(300, 0);
            bang.GetComponent<Image>().color = mauNen;
            var xep = bang.GetComponent<VerticalLayoutGroup>();
            xep.padding = new RectOffset(16, 16, 12, 16);
            xep.spacing = 6;
            xep.childControlWidth = true; xep.childControlHeight = true;
            xep.childForceExpandWidth = true; xep.childForceExpandHeight = false;
            bang.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TaoTieuDe(bang.transform, "LỚP THÔNG TIN");
            for (int i = 0; i < LayerManager.TenLop.Length; i++)
            {
                var loai = (LopThongTin)i;                                    // biến riêng cho từng vòng lặp để hàm bên dưới nhớ đúng lớp
                TaoOChon(bang.transform, LayerManager.TenLop[i], lop.DangBat(loai), hien => lop.Bat(loai, hien));
            }

            TaoTieuDe(bang.transform, "HIỂN THỊ");
            oBanDo = TaoOChon(bang.transform, "Chế độ bản đồ (M)", diaHinh.CheDoBanDo, banDo => diaHinh.DatCheDo(banDo));
            TaoNut(bang.transform, "Tổng quan (1)", mayQuay.TongQuan);
            TaoNut(bang.transform, "Nhìn xiên (2)", mayQuay.NhinXien);
        }

        // Khi đổi chế độ bằng phím M thì ô chọn trên bảng cũng phải đổi theo
        void Update()
        {
            if (oBanDo != null && oBanDo.isOn != diaHinh.CheDoBanDo) oBanDo.SetIsOnWithoutNotify(diaHinh.CheDoBanDo);
        }

        // UGUI cần một EventSystem để nhận chuột; tạo nếu scene chưa có
        static void DamBaoEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // ======================= CÁC THÀNH PHẦN GIAO DIỆN =======================

        Text TaoChu(Transform cha, string noiDung, int co, FontStyle kieu, TextAnchor neo, Color mau)
        {
            var g = new GameObject("Chu", typeof(RectTransform));
            g.transform.SetParent(cha, false);
            var t = g.AddComponent<Text>();
            t.font = font; t.text = noiDung; t.fontSize = co; t.fontStyle = kieu;
            t.alignment = neo; t.color = mau;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        void TaoTieuDe(Transform cha, string noiDung)
        {
            var t = TaoChu(cha, noiDung, 15, FontStyle.Bold, TextAnchor.LowerLeft, mauTieuDe);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        }

        // Ô chọn: ô vuông bên trái, nhãn bên phải; bấm vào ô hoặc nhãn đều được
        Toggle TaoOChon(Transform cha, string nhan, bool giaTri, UnityAction<bool> khiDoi)
        {
            var g = new GameObject("OChon_" + nhan, typeof(RectTransform));
            g.transform.SetParent(cha, false);
            g.AddComponent<LayoutElement>().preferredHeight = 32;

            var nen = new GameObject("Nen", typeof(RectTransform), typeof(Image));
            nen.transform.SetParent(g.transform, false);
            var rn = (RectTransform)nen.transform;
            rn.anchorMin = rn.anchorMax = rn.pivot = new Vector2(0, 0.5f);
            rn.anchoredPosition = Vector2.zero;
            rn.sizeDelta = new Vector2(24, 24);
            nen.GetComponent<Image>().color = Color.white;

            var dau = new GameObject("Dau", typeof(RectTransform), typeof(Image));
            dau.transform.SetParent(nen.transform, false);
            var rd = (RectTransform)dau.transform;
            rd.anchorMin = Vector2.zero; rd.anchorMax = Vector2.one;
            rd.offsetMin = new Vector2(5, 5); rd.offsetMax = new Vector2(-5, -5);
            dau.GetComponent<Image>().color = mauDau;

            var chu = TaoChu(g.transform, nhan, 19, FontStyle.Normal, TextAnchor.MiddleLeft, mauChu);
            chu.raycastTarget = true;                                         // để bấm vào nhãn cũng đổi được ô chọn
            var rc = (RectTransform)chu.transform;
            rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one;
            rc.offsetMin = new Vector2(36, 0); rc.offsetMax = Vector2.zero;

            var o = g.AddComponent<Toggle>();
            o.targetGraphic = nen.GetComponent<Image>();
            o.graphic = dau.GetComponent<Image>();
            o.isOn = giaTri;                                                  // đặt giá trị trước khi gắn hàm, tránh gọi thừa
            o.onValueChanged.AddListener(khiDoi);
            return o;
        }

        void TaoNut(Transform cha, string nhan, UnityAction khiBam)
        {
            var g = new GameObject("Nut_" + nhan, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(cha, false);
            g.AddComponent<LayoutElement>().preferredHeight = 38;
            g.GetComponent<Image>().color = mauNut;

            var chu = TaoChu(g.transform, nhan, 18, FontStyle.Normal, TextAnchor.MiddleCenter, mauChu);
            var rc = (RectTransform)chu.transform;
            rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one;
            rc.offsetMin = rc.offsetMax = Vector2.zero;

            var nut = g.AddComponent<Button>();
            nut.targetGraphic = g.GetComponent<Image>();
            nut.onClick.AddListener(khiBam);
        }
    }
}
