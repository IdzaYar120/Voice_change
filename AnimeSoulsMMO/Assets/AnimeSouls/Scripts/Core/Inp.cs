using UnityEngine;

namespace AnimeSouls
{
    /// <summary>
    /// Thin wrapper over the legacy Input Manager. If the project is switched to "Input System only",
    /// calls return neutral values and <see cref="Missing"/> is true so the HUD can explain how to fix it.
    /// </summary>
    public static class Inp
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        public static bool Missing => false;
        public static bool Key(KeyCode k) => Input.GetKey(k);
        public static bool Down(KeyCode k) => Input.GetKeyDown(k);
        public static bool Mouse(int b) => Input.GetMouseButton(b);
        public static bool MouseDown(int b) => Input.GetMouseButtonDown(b);
        public static float MouseX => Input.GetAxisRaw("Mouse X");
        public static float MouseY => Input.GetAxisRaw("Mouse Y");
        public static float Wheel => Input.mouseScrollDelta.y;
        public static Vector3 MousePosition => Input.mousePosition;
#else
        public static bool Missing => true;
        public static bool Key(KeyCode k) => false;
        public static bool Down(KeyCode k) => false;
        public static bool Mouse(int b) => false;
        public static bool MouseDown(int b) => false;
        public static float MouseX => 0f;
        public static float MouseY => 0f;
        public static float Wheel => 0f;
        public static Vector3 MousePosition => new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
#endif
    }
}
