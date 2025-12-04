using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BTD.Services
{
    public class TrayIconService
    {
        private const int WM_TRAYMESSAGE = 0x800;  // 自定义消息 ID
        private readonly Window _window;
        private readonly IntPtr _hWnd;
        private uint _iconId = 1;

        public TrayIconService(Window window)
        {
            _window = window;

            // 1. 获取窗口句柄
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            _hWnd = helper.EnsureHandle();

            // 2. Hook Win32 消息
            System.Windows.Interop.HwndSource.FromHwnd(_hWnd)
                .AddHook(WndProc);

            // 3. 创建托盘图标
            AddTrayIcon();
        }

        #region Win32 API

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA data);

        private const uint NIF_MESSAGE = 0x0001;
        private const uint NIF_ICON = 0x0002;
        private const uint NIF_TIP = 0x0004;

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_DELETE = 0x00000002;

        #endregion

        private void AddTrayIcon()
        {
            var icon = System.Drawing.SystemIcons.Application;

            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = _iconId,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYMESSAGE,
                hIcon = icon.Handle,
                szTip = "应用正在后台运行"
            };

            Shell_NotifyIcon(NIM_ADD, ref data);
        }

        public void RemoveTrayIcon()
        {
            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = _iconId
            };

            Shell_NotifyIcon(NIM_DELETE, ref data);
        }

        // 处理托盘图标事件（Win32 回调）
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TRAYMESSAGE)
            {
                int mouseMsg = (int)lParam;

                switch (mouseMsg)
                {
                    case 0x201: // WM_LBUTTONDOWN
                        ShowWindow();
                        handled = true;
                        break;

                    case 0x205: // WM_RBUTTONUP
                        ShowContextMenu();
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }

        private void ShowWindow()
        {
            _window.Show();
            _window.WindowState = WindowState.Normal;
            _window.Activate();
        }

        private void ShowContextMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Header = "显示主窗口", Command = new RelayCommand(ShowWindow) });
            menu.Items.Add(new MenuItem
            {
                Header = "退出程序",
                Command = new RelayCommand(() =>
                {
                    RemoveTrayIcon();
                    Application.Current.Shutdown();
                })
            });

            menu.IsOpen = true;
        }
    }
}
