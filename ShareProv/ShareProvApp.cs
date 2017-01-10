using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareProv
{
    public class ShareProvApp : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public ShareProvApp()
        {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = System.Drawing.SystemIcons.Information,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("ShareProv", ShowApp), 
                new MenuItem("Exit", Exit)
            }),
                Visible = true,
                BalloonTipTitle = "The App is running",
                BalloonTipText = "ShareProv is great. the detail is here"
            };
            trayIcon.ShowBalloonTip(5000);
            trayIcon.DoubleClick += TrayIcon_DoubleClick;
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowApp(sender, e);
        }

        frmMain frm = new frmMain();
        void ShowApp(object sender, EventArgs e)
        {
            // If we are already showing the window, merely focus it.
            if (frm.Visible)
            {
                frm.Activate();
            }
            else
            {
                frm.ShowDialog();
            }
        }
        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;

            Application.Exit();
        }
    }
}
