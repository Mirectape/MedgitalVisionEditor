using System;
using UnityEngine;
using System.Runtime.InteropServices;

public class ScreenFit : MonoBehaviour
{
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int GWL_STYLE = -16;
    const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    const uint WS_VISIBLE = 0x10000000;
    const int SW_SHOWMAXIMIZED = 3;

    void Awake()
    {
#if !UNITY_EDITOR
        IntPtr hwnd = SplashScreenResizer.SavedHwnd;

        // Установить стили окна для отображения элементов управления и делаем окно видимым
        SetWindowLong(hwnd, GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);

        // Развернуть окно в полноэкранный режим, но не перекрывая панель задач
        ShowWindow(hwnd, SW_SHOWMAXIMIZED);
#endif
    }
}