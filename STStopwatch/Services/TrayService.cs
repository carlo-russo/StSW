using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using STStopwatch.Core.Services;

namespace STStopwatch.Services
{
    public class TrayService : ITrayService
    {
        private NotifyIcon _notifyIcon;
        private MenuItem _minimalMenuItem;
        private MenuItem _topmostMenuItem;
        private Action _onRestore;
        private Action _onToggleMinimal;
        private Action _onToggleTopmost;
        private Action _onExit;

        public void Initialize(Action onRestore, Action onToggleMinimal, Action onToggleTopmost, Action onExit)
        {
            _onRestore = onRestore;
            _onToggleMinimal = onToggleMinimal;
            _onToggleTopmost = onToggleTopmost;
            _onExit = onExit;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "TimerQt";

            // Load icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            var contextMenu = new ContextMenu();

            var titleItem = new MenuItem("TimerQt");
            titleItem.Enabled = false;
            contextMenu.MenuItems.Add(titleItem);
            contextMenu.MenuItems.Add("-");

            var restoreItem = new MenuItem("Show TimerQt", delegate { if (_onRestore != null) _onRestore(); });
            restoreItem.DefaultItem = true;
            contextMenu.MenuItems.Add(restoreItem);

            _minimalMenuItem = new MenuItem("Minimal Mode", delegate { if (_onToggleMinimal != null) _onToggleMinimal(); });
            contextMenu.MenuItems.Add(_minimalMenuItem);

            _topmostMenuItem = new MenuItem("Always on Top", delegate { if (_onToggleTopmost != null) _onToggleTopmost(); });
            contextMenu.MenuItems.Add(_topmostMenuItem);

            contextMenu.MenuItems.Add("-");

            var exitItem = new MenuItem("Exit", delegate { if (_onExit != null) _onExit(); });
            contextMenu.MenuItems.Add(exitItem);

            _notifyIcon.ContextMenu = contextMenu;
            _notifyIcon.DoubleClick += delegate { if (_onRestore != null) _onRestore(); };
            _notifyIcon.Visible = true;
        }

        public void UpdateState(bool isMinimal, bool isAlwaysOnTop)
        {
            if (_minimalMenuItem != null)
            {
                _minimalMenuItem.Checked = isMinimal;
            }
            if (_topmostMenuItem != null)
            {
                _topmostMenuItem.Checked = isAlwaysOnTop;
            }
        }

        public void ShowNotification(string title, string message)
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
        }

        public void SetVisible(bool isVisible)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = isVisible;
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
