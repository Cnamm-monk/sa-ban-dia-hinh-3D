// NhapLieu.cs — lớp đọc chuột/bàn phím dùng chung cho cả ứng dụng.
// Project đang dùng Input System mới; nếu đổi về Input Manager cũ thì tự chuyển sang API cũ.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaBan3D
{
    public static class NhapLieu
    {
#if ENABLE_INPUT_SYSTEM
        public static bool ChuotTraiNhan => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool ChuotPhaiGiu  => Mouse.current != null && Mouse.current.rightButton.isPressed;
        public static bool ChuotGiuaGiu  => Mouse.current != null && Mouse.current.middleButton.isPressed;
        public static Vector2 DoDichChuot => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        public static float Cuon          => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        public static Vector2 ViTriChuot  => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        // Thêm phím mới: bổ sung một dòng case ở đây
        public static bool VuaNhan(KeyCode phim)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (phim)
            {
                case KeyCode.Alpha1: return kb.digit1Key.wasPressedThisFrame;
                case KeyCode.Alpha2: return kb.digit2Key.wasPressedThisFrame;
                case KeyCode.Alpha3: return kb.digit3Key.wasPressedThisFrame;
                case KeyCode.M:      return kb.mKey.wasPressedThisFrame;
                case KeyCode.L:      return kb.lKey.wasPressedThisFrame;
                case KeyCode.R:      return kb.rKey.wasPressedThisFrame;
                case KeyCode.Escape: return kb.escapeKey.wasPressedThisFrame;
                default:             return false;
            }
        }

        public static Vector2 WASD
        {
            get
            {
                var kb = Keyboard.current;
                if (kb == null) return Vector2.zero;
                float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
                float y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
                return new Vector2(x, y);
            }
        }
#else
        public static bool ChuotTraiNhan => Input.GetMouseButtonDown(0);
        public static bool ChuotPhaiGiu  => Input.GetMouseButton(1);
        public static bool ChuotGiuaGiu  => Input.GetMouseButton(2);
        // Input cũ trả về đơn vị nhỏ hơn pixel, nhân 20 để cảm giác giống Input System mới
        public static Vector2 DoDichChuot => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 20f;
        public static float Cuon          => Input.mouseScrollDelta.y;
        public static Vector2 ViTriChuot  => Input.mousePosition;
        public static bool VuaNhan(KeyCode phim) => Input.GetKeyDown(phim);
        public static Vector2 WASD => new Vector2(
            (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
            (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
#endif
    }
}
