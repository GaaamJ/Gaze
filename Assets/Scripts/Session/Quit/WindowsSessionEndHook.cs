#if UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using Gaze.Save;
using Debug = UnityEngine.Debug;

namespace Gaze.Session
{
    // 윈도우 종료·로그오프에서는 Unity 종료 훅이 불리지 않고 창 메시지만 옴(#71 실측). 게임 창의 WndProc를 가로채 받음.
    // WM_QUERYENDSESSION은 종료가 취소될 수 있어 저장하지 않고, 종료가 확정된 WM_ENDSESSION(wParam != 0)에서만 저장함.
    // QuitHooks가 Install·Uninstall을 부름.
    internal static class WindowsSessionEndHook
    {
        const int GWLP_WNDPROC = -4;
        const uint WM_ENDSESSION = 0x0016;
        const string UnityWindowClass = "UnityWndClass";

        delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);
        // SetWindowLongPtrW는 64비트 user32에만 있음. 빌드는 Windows 64비트 기준.
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")] static extern IntPtr CallWindowProc(IntPtr previous, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // 네이티브 쪽이 들고 있는 함수 포인터라 GC가 대리자를 거두면 다음 메시지에서 튕김. 창이 살아 있는 동안 붙잡아 둠.
        static readonly WndProcDelegate wndProc = WndProc;
        static readonly EnumWindowsProc findWindow = FindUnityWindow;

        static IntPtr hwnd = IntPtr.Zero;
        static IntPtr originalWndProc = IntPtr.Zero;
        static bool hooked;
        static uint processId;
        static IntPtr foundWindow;

        // 이 프로세스의 UnityWndClass 창을 찾아 WndProc를 바꿔 끼움. 창을 못 찾거나 후킹에 실패하면 경고만 남기고 끝남.
        internal static void Install()
        {
            if (hooked)
                return;

            try
            {
                processId = (uint)Process.GetCurrentProcess().Id;
                foundWindow = IntPtr.Zero;
                EnumWindows(findWindow, IntPtr.Zero);
                if (foundWindow == IntPtr.Zero)
                {
                    Debug.LogWarning("[WindowsSessionEndHook] 게임 창을 찾지 못해 윈도우 종료·로그오프를 감지하지 않는다.");
                    return;
                }

                hwnd = foundWindow;
                originalWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(wndProc));
                hooked = originalWndProc != IntPtr.Zero;
                if (!hooked)
                    Debug.LogWarning($"[WindowsSessionEndHook] 창 메시지 후킹 실패(오류 {Marshal.GetLastWin32Error()}). 윈도우 종료·로그오프를 감지하지 않는다.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // WndProc를 원래대로 되돌림.
        // 되돌린 뒤에도 이미 들어온 메시지가 WndProc로 올 수 있어 originalWndProc는 비우지 않음.
        internal static void Uninstall()
        {
            if (!hooked)
                return;
            hooked = false;
            SetWindowLongPtr(hwnd, GWLP_WNDPROC, originalWndProc);
        }

        // EnumWindows 콜백. 이 프로세스 소유의 UnityWndClass 창을 foundWindow에 담고 열거를 멈춤.
        [MonoPInvokeCallback(typeof(EnumWindowsProc))]
        static bool FindUnityWindow(IntPtr candidate, IntPtr lParam)
        {
            GetWindowThreadProcessId(candidate, out uint owner);
            if (owner != processId)
                return true;

            var className = new StringBuilder(64);
            GetClassName(candidate, className, className.Capacity);
            if (className.ToString() != UnityWindowClass)
                return true;

            foundWindow = candidate;
            return false;
        }

        // 게임 창의 메시지 처리기. 종료가 확정된 WM_ENDSESSION이면 창 닫기 사망을 기록하고, 모든 메시지를 원래 WndProc로 넘김.
        [MonoPInvokeCallback(typeof(WndProcDelegate))]
        static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_ENDSESSION && wParam != IntPtr.Zero)
            {
                try
                {
                    ExitRecorder.RecordWindowClose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return CallWindowProc(originalWndProc, hWnd, msg, wParam, lParam);
        }
    }
}
#endif
