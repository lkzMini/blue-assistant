using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Blue.Tray;

public enum TrayMenuAction
{
    ShowHide,
    TogglePin,
    RestartChat,
    OpenSettings,
    CheckOllama,
    Exit
}

internal static class Native
{
    // Shell_NotifyIcon messages
    internal const uint NIM_ADD = 0u;
    internal const uint NIM_DELETE = 2u;

    // NOTIFYICONDATAW flags
    internal const uint NIF_MESSAGE = 1u;
    internal const uint NIF_ICON = 2u;
    internal const uint NIF_TIP = 4u;
    internal const uint NIF_SHOWTIP = 0x400u;

    // LoadImageW types
    internal const uint IMAGE_ICON = 1u;
    internal const uint LR_DEFAULTSIZE = 0x40u;
    internal const uint LR_SHARED = 0x8000u;
    internal const uint LR_LOADFROMFILE = 0x0010u;

    // Window messages
    internal const uint WM_RBUTTONUP = 0x0205u;
    internal const uint WM_COMMAND = 0x0111u;

    // Menu flags
    internal const uint MF_STRING = 0u;
    internal const uint MF_SEPARATOR = 0x800u;
    internal const uint MF_CHECKED = 0x00000008u;
    internal const uint MF_BYPOSITION = 0x00000400u;

    // TrackPopupMenu flags
    internal const uint TPM_RIGHTBUTTON = 2u;
    internal const uint TPM_BOTTOMALIGN = 0x20u;
    internal const uint TPM_RETURNCMD = 0x100u;
    internal const uint TPM_TOPALIGN = 0x0000u;
    internal const uint TPM_LEFTALIGN = 0x0000u;

    // Standard icon IDs
    internal static readonly IntPtr IDI_APPLICATION = new IntPtr(32512);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern int Shell_NotifyIconW(uint cmd, ref NOTIFYICONDATAW data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern IntPtr LoadImageW(
        IntPtr hInst,
        IntPtr name,
        uint type,
        int cx,
        int cy,
        uint fuLoad);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern IntPtr LoadImageW(
        IntPtr hInst,
        string name,
        uint type,
        int cx,
        int cy,
        uint fuLoad);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool InsertMenu(
        IntPtr hMenu,
        uint uPosition,
        uint uFlags,
        IntPtr uIDNewItem,
        string lpNewItem);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern IntPtr TrackPopupMenu(
        IntPtr hMenu,
        uint uFlags,
        int x,
        int y,
        int nReserved,
        IntPtr hWnd,
        IntPtr prcRect);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "PostMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern bool PostMessage(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NOTIFYICONDATAW
    {
        internal uint cbSize;
        internal IntPtr hWnd;
        internal uint uID;
        internal uint uFlags;
        internal uint uCallbackMessage;
        internal IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string szTip;

        internal uint dwState;
        internal uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string szInfo;

        internal uint uTimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        internal string szInfoTitle;

        internal uint dwInfoFlags;
        internal Guid guidItem;
        internal IntPtr hBalloonIcon;
    }
}

public sealed class TrayService : IDisposable
{
    private const uint WmTrayCallback = 0x8000u; // WM_APP + 1
    private const uint TrayIconId = 1u;
    private const uint WmLButtonUp = 0x0202u;
    private const uint WmRButtonUp = 0x0205u;

    // Menu item IDs
    private const int MenuIdShowHide = 100;
    private const int MenuIdTogglePin = 101;
    private const int MenuIdRestartChat = 102;
    private const int MenuIdOpenSettings = 103;
    private const int MenuIdCheckOllama = 105;
    private const int MenuIdExit = 104;

    private IntPtr _hWnd;
    private IntPtr _hIcon;
    private bool _disposed;
    private bool _isCustomIcon;
    private bool _isPinned;

    public event EventHandler? TrayLeftClick;
    public event EventHandler<TrayMenuAction>? TrayMenuClicked;
    public event EventHandler<bool>? TogglePinRequested;
    public event EventHandler? ShowHideRequested;

    public bool IsPinned
    {
        get => _isPinned;
        set => _isPinned = value;
    }

    public bool Initialize(IntPtr hWnd)
    {
        _hWnd = hWnd;

        // First try to load Blue.ico from app directory
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Blue.ico");
        if (File.Exists(iconPath))
        {
            _hIcon = Native.LoadImageW(
                IntPtr.Zero,
                iconPath,
                Native.IMAGE_ICON,
                0, 0,
                Native.LR_DEFAULTSIZE | Native.LR_LOADFROMFILE);

            if (_hIcon != IntPtr.Zero)
            {
                _isCustomIcon = true;
            }
        }

        // Fallback to default application icon
        if (_hIcon == IntPtr.Zero)
        {
            _hIcon = Native.LoadImageW(
                IntPtr.Zero,
                Native.IDI_APPLICATION,
                Native.IMAGE_ICON,
                0, 0,
                Native.LR_DEFAULTSIZE | Native.LR_SHARED);
        }

        if (_hIcon == IntPtr.Zero)
            return false;

        var nid = new Native.NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<Native.NOTIFYICONDATAW>(),
            hWnd = _hWnd,
            uID = TrayIconId,
            uFlags = Native.NIF_MESSAGE | Native.NIF_ICON | Native.NIF_TIP | Native.NIF_SHOWTIP,
            uCallbackMessage = WmTrayCallback,
            hIcon = _hIcon,
            szTip = "Blue Assistant",
        };

        int result = Native.Shell_NotifyIconW(Native.NIM_ADD, ref nid);
        return result != 0;
    }

    public void HandleWindowMessage(uint messageId, nuint wParam, nint lParam)
    {
        if (_disposed)
            return;

        // Handle WM_COMMAND for menu item selection (backup mechanism)
        if (messageId == Native.WM_COMMAND)
        {
            int menuId = (int)(nuint)wParam;
            HandleMenuSelection(menuId);
            return;
        }

        // Handle tray icon callback messages
        if (messageId == WmTrayCallback && (uint)(nuint)wParam == TrayIconId)
        {
            uint msg = (uint)(nuint)lParam;
            if (msg == WmLButtonUp)
            {
                TrayLeftClick?.Invoke(this, EventArgs.Empty);
            }
            else if (msg == WmRButtonUp)
            {
                ShowContextMenu();
            }
        }
    }

    private void ShowContextMenu()
    {
        // Get cursor position
        if (!CubeKit.UI.Helpers.NativeHelper.GetCursorPos(out var cursorPos))
            return;

        // Create popup menu
        IntPtr hMenu = Native.CreatePopupMenu();
        if (hMenu == IntPtr.Zero)
            return;

        try
        {
            // Insert menu items
            // Show/Hide
            Native.InsertMenu(hMenu, 0, Native.MF_BYPOSITION, new IntPtr(MenuIdShowHide), "Show/Hide Blue");

            // Toggle Pin (with checkmark if pinned)
            string pinText = "Always on Top";
            uint pinFlags = Native.MF_BYPOSITION;
            if (_isPinned)
                pinFlags |= Native.MF_CHECKED;
            Native.InsertMenu(hMenu, 1, pinFlags, new IntPtr(MenuIdTogglePin), pinText);

            // Separator
            Native.InsertMenu(hMenu, 2, Native.MF_BYPOSITION | Native.MF_SEPARATOR, IntPtr.Zero, string.Empty);

            // Restart Chat
            Native.InsertMenu(hMenu, 3, Native.MF_BYPOSITION, new IntPtr(MenuIdRestartChat), "Restart Chat");

            // Check Ollama
            Native.InsertMenu(hMenu, 4, Native.MF_BYPOSITION, new IntPtr(MenuIdCheckOllama), "Check Ollama Status");

            // Settings
            Native.InsertMenu(hMenu, 5, Native.MF_BYPOSITION, new IntPtr(MenuIdOpenSettings), "Settings...");

            // Separator
            Native.InsertMenu(hMenu, 6, Native.MF_BYPOSITION | Native.MF_SEPARATOR, IntPtr.Zero, string.Empty);

            // Exit
            Native.InsertMenu(hMenu, 7, Native.MF_BYPOSITION, new IntPtr(MenuIdExit), "Exit");

            // Set foreground window (required for TrackPopupMenu to work properly)
            Native.SetForegroundWindow(_hWnd);

            // Show the popup menu
            uint tpmFlags = Native.TPM_RETURNCMD | Native.TPM_RIGHTBUTTON | Native.TPM_TOPALIGN | Native.TPM_LEFTALIGN;
            IntPtr result = Native.TrackPopupMenu(
                hMenu,
                tpmFlags,
                cursorPos.X,
                cursorPos.Y,
                0,
                _hWnd,
                IntPtr.Zero);

            int menuId = result.ToInt32();
            if (menuId > 0)
            {
                HandleMenuSelection(menuId);
            }

            // Post a WM_NULL to process the menu properly
            Native.PostMessage(_hWnd, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            Native.DestroyMenu(hMenu);
        }
    }

    private void HandleMenuSelection(int menuId)
    {
        TrayMenuAction? action = menuId switch
        {
            MenuIdShowHide => TrayMenuAction.ShowHide,
            MenuIdTogglePin => TrayMenuAction.TogglePin,
            MenuIdRestartChat => TrayMenuAction.RestartChat,
            MenuIdOpenSettings => TrayMenuAction.OpenSettings,
            MenuIdCheckOllama => TrayMenuAction.CheckOllama,
            MenuIdExit => TrayMenuAction.Exit,
            _ => null
        };

        if (action.HasValue)
        {
            TrayMenuClicked?.Invoke(this, action.Value);

            // Also raise specific events for convenience
            if (action == TrayMenuAction.ShowHide)
                ShowHideRequested?.Invoke(this, EventArgs.Empty);
            else if (action == TrayMenuAction.TogglePin)
                TogglePinRequested?.Invoke(this, !_isPinned);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var nid = new Native.NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<Native.NOTIFYICONDATAW>(),
            hWnd = _hWnd,
            uID = TrayIconId,
        };
        Native.Shell_NotifyIconW(Native.NIM_DELETE, ref nid);

        // Destroy custom icon if we loaded it from file
        if (_isCustomIcon && _hIcon != IntPtr.Zero)
        {
            Native.DestroyIcon(_hIcon);
        }
        // System-owned icons (IDI_APPLICATION with LR_SHARED) don't need DestroyIcon

        _disposed = true;
    }
}
