using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class SplashScreenResizer : MonoBehaviour
{
    public static IntPtr SavedHwnd;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongA")]
    public static extern int SetWindowLong(int hwnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    const int GWL_STYLE = -16;
    const int WS_POPUP = 0x800000;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void OnBeforeSplashScreen()
    {
#if !UNITY_EDITOR
        Process currentProcess = Process.GetCurrentProcess();
        SavedHwnd = currentProcess.MainWindowHandle;
        
        // Убрать рамку
        SetWindowLong((int)SavedHwnd, GWL_STYLE, WS_POPUP);

        // Позиционировать окно по центру
        CenterWindow(SavedHwnd);
#endif
    }

    static void CenterWindow(IntPtr hwnd)
    {
        int screenWidth = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;

        int windowWidth = 600; 
        int windowHeight = 400; 

        int x = (screenWidth - windowWidth) / 2;
        int y = (screenHeight - windowHeight) / 2;

        const uint SWP_NOZORDER = 0x4;
        SetWindowPos(hwnd, IntPtr.Zero, x, y, windowWidth, windowHeight, SWP_NOZORDER);
    }
}