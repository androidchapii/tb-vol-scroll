﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TbVolScroll_Reloaded
{
    public partial class frmMain : Form
    {

        #region DLLImports
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(int hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

        private static void ShowInactiveTopmost(Form frm)
        {
            frm.Invoke((MethodInvoker)delegate
            {
                ShowWindow(frm.Handle, 4);
                SetWindowPos(frm.Handle.ToInt32(), -1, frm.Left, frm.Top, frm.Width, frm.Height, 16u);
            });
        }
        public struct RECT
        {
            public int Left;

            public int Top;

            public int Right;

            public int Bottom;
        }

        public static bool IsTaskbarHidden()
        {
            return CheckTaskbarVisibility(null);
        }

        public static bool CheckTaskbarVisibility(Screen screen)
        {
            if (screen == null)
            {
                screen = Screen.PrimaryScreen;
            }
            RECT rect = new RECT();
            GetWindowRect(new HandleRef(null, GetForegroundWindow()), ref rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top).Contains(screen.Bounds);
        }

        public RECT TaskbarRect;
        public InputHandler inputHandler;
        public bool IsDisplayingVolume = false;

        #endregion

        public frmMain()
        {
            InitializeComponent();
        }

        public void DoVolumeChanges(int delta)
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    int CurrentVolume = (int)Math.Round(VolumeHandler.GetMasterVolume());
                    if (CursorInTaskbar() && !IsTaskbarHidden())
                    {
                        if (delta < 0)
                        {
                            if (inputHandler.IsAltDown)
                            {
                                VolumeHandler.SetMasterVolume(CurrentVolume - 1);
                            }
                            else if (CurrentVolume <= 10)
                            {
                                VolumeHandler.SetMasterVolume(CurrentVolume - 1);
                            }
                            else
                            {
                                VolumeHandler.SetMasterVolume(CurrentVolume - 5);
                            }
                        }
                        else
                        {
                            if (inputHandler.IsAltDown)
                            {
                                VolumeHandler.SetMasterVolume(CurrentVolume + 1);
                            }
                            else if (CurrentVolume < 10)
                            {
                                VolumeHandler.SetMasterVolume(CurrentVolume + 1);
                            }
                            else
                            {
                                VolumeHandler.SetMasterVolume(CurrentVolume + 5);
                            }
                        }

                        CurrentVolume = (int)Math.Round(VolumeHandler.GetMasterVolume());
                        lblVolumeText.Text = CurrentVolume + "%";


                        Point CursorPosition = Cursor.Position;
                        Width = CurrentVolume + 30;
                        Height = 17;
                        Left = CursorPosition.X - Width / 2;
                        Top = CursorPosition.Y - 20;
                        lblVolumeText.BackColor = CalculateColor(CurrentVolume);
                        if (!IsDisplayingVolume)
                        {
                            AutoHideVolume();
                        }
                    }
                    else
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            Hide();
                            WindowState = FormWindowState.Minimized;
                        });
                        IsDisplayingVolume = false;
                    }
                });
            }
            catch { }
        }

        private static Color CalculateColor(double percentage)
        {
            double num = ((percentage > 50.0) ? (1.0 - 2.0 * (percentage - 50.0) / 100.0) : 1.0) * 255.0;
            double num2 = ((percentage > 50.0) ? 1.0 : (2.0 * percentage / 100.0)) * 255.0;
            double num3 = 0.0;
            return Color.FromArgb((int)num, (int)num2, (int)num3);
        }

        async Task PutTaskDelay()
        {
            await Task.Delay(100);
        }



        private async void AutoHideVolume()
        {
            Application.DoEvents();
            IsDisplayingVolume = true;
            ShowInactiveTopmost(this);

            Invoke((MethodInvoker)delegate
            {
                Opacity = 1;
            });

            while (inputHandler.TimeOutHelper != 0)
            {
                await PutTaskDelay();
                inputHandler.TimeOutHelper--;
            }

            Invoke((MethodInvoker)delegate
            {
                Hide();
                WindowState = FormWindowState.Minimized;
            });
            IsDisplayingVolume = false;
        }

        public bool CursorInTaskbar()
        {
            Point position = Cursor.Position;
            Opacity = 1;
            if (position.Y >= TaskbarRect.Top && position.Y <= TaskbarRect.Bottom)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void tsmExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void tsmRestart_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void tsmResetVolume_Click(object sender, EventArgs e)
        {
            VolumeHandler.SetMasterVolume(0);
        }


        private void SetupProgramVars(object sender, EventArgs e)
        {
            if (Height != 15)
            {
                Height = 15;
            }

            ShowInactiveTopmost(this);
            lblVolumeText.Text = "Detecting taskbar  . . .";
            IntPtr hwnd = FindWindow("Shell_traywnd", "");
            GetWindowRect(hwnd, out TaskbarRect);
            lblVolumeText.Text = "Initializing input hooks  . . .";
            inputHandler = new InputHandler(this);
            lblVolumeText.Text = "Initializing audio hooks  . . .";
            Hide();

        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            System.Reflection.MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            mi.Invoke(trayIcon, null);
        }
    }
}
