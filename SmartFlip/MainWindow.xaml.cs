// Copyright Gratian Lup. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//     * Neither the name of Google Inc. nor the names of its
//       contributors may be used to endorse or promote products derived
//       from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.ComponentModel;

using SmartFlip.Manager;
using SmartFlip.WindowThumbnail;
using SmartFlip.Properties;
using System.Configuration;
using ActivityHook;
using System.Windows.Forms;
using System.Reflection;
using System.Resources;
using MessageBox = System.Windows.MessageBox;

namespace SmartFlip {
    public partial class MainWindow : System.Windows.Window {
        [DllImport("dwmapi.dll")]
        static extern void DwmIsCompositionEnabled(ref bool pfEnabled);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
                                        int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOZORDER = 0x0004;
        const UInt32 SWP_NOREDRAW = 0x0008;
        const UInt32 SWP_NOACTIVATE = 0x0010;
        const UInt32 SWP_FRAMECHANGED = 0x0020;  /* The frame changed: send WM_NCCALCSIZE */
        const UInt32 SWP_SHOWWINDOW = 0x0040;
        const UInt32 SWP_HIDEWINDOW = 0x0080;
        const UInt32 SWP_NOCOPYBITS = 0x0100;
        const UInt32 SWP_NOOWNERZORDER = 0x0200;  /* Don't do owner Z ordering */
        const UInt32 SWP_NOSENDCHANGING = 0x0400;  /* Don't send WM_WINDOWPOSCHANGING */


        SmartFlip.WindowThumbnail.Window window;
        public OuterGlowBitmapEffect glow;
        TimeTracker tt;
        bool ft;
        WindowManager wm;
        bool running;
        bool keyPressed;



        System.Windows.Forms.ContextMenu menu;
        System.Windows.Forms.NotifyIcon icon;


        bool closeAllowed;
        bool ignoreWinRestore;

        struct OSVERSIONINFO {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        [DllImport("kernel32")]
        static extern bool GetVersionEx(ref OSVERSIONINFO osvi);

        // activity hooks (keyboard and mouse)
        UserActivityHook uah;

        private void MouseMoveIntercept(object sender, System.Windows.Forms.MouseEventArgs e) {
            if(!running && wm.options.useMouseTrigger) {
                // check if we are in trigger region
                if(e.X >= wm.options.regionLeft &&
                    e.X <= wm.options.regionLeft + wm.options.regionWidth &&
                    e.Y >= wm.options.regionTop &&
                    e.Y <= wm.options.regionTop + wm.options.regionHeight) {
                    showSmartFlip();
                }
            }
        }

        private int winct;
        private bool firtstab;

        private void KeyUpHandler(object sender, System.Windows.Forms.KeyEventArgs e) {
            if(running) {
                if(e.KeyCode == System.Windows.Forms.Keys.Tab) {
                    if(firtstab) {
                        firtstab = false;
                    }
                    else {
                        wm.onTabKeyDown((UserActivityHook.GetKeyState(0x10) & 0x80) == 0x80);
                        e.Handled = true;
                    }
                }
                else if(e.KeyCode == System.Windows.Forms.Keys.Enter) {
                    wm.onEnterKeyUp();
                    e.Handled = true;
                }
            }

            if(running && wm.options.useWinTab && wm.options.selectOnWinReleased) {
                if(e.KeyCode == System.Windows.Forms.Keys.LWin || e.KeyCode == System.Windows.Forms.Keys.RWin) {
                    if(wm.status != 5 && !ignoreWinRestore) {
                        wm.selectWindow(wm.activeWindow);
                    }

                    e.Handled = true;
                    return;
                }
            }
            else if(keyPressed != false && wm.options.useWinTab && wm.options.selectOnWinReleased) {
                e.Handled = true;
                return;
            }
            else {
                e.Handled = false;
            }

            if(e.KeyCode == System.Windows.Forms.Keys.LWin || e.KeyCode == System.Windows.Forms.Keys.RWin) {
                if(winct > 0) {
                    e.Handled = true;
                    winct--;
                }
            }
        }

        private void KeyDownHandler(object sender, System.Windows.Forms.KeyEventArgs e) {
            keyPressed = e.KeyCode == System.Windows.Forms.Keys.LWin || e.KeyCode == System.Windows.Forms.Keys.RWin;

            if(running) {
                if(e.KeyCode >= System.Windows.Forms.Keys.A && e.KeyCode <= System.Windows.Forms.Keys.Z) {
                    e.Handled = true;
                    return;
                }

                // block window key
                if(e.KeyCode == System.Windows.Forms.Keys.LWin || e.KeyCode == System.Windows.Forms.Keys.RWin) {
                    e.Handled = true;
                    return;
                }
                else if(e.KeyCode == System.Windows.Forms.Keys.Tab && wm.options.useWinTab) {
                    bool shift = (UserActivityHook.GetKeyState(0x10) & 0x80) == 0x80;

                    //wm.onTabKeyDown(shift);

                    e.Handled = true;
                    return;
                }
                else if(e.KeyCode == Keys.Escape) {
                    wm.selectWindow(0);
                }
            }
            else {
                if(wm.options.useWinTab) {
                    if(e.KeyCode == System.Windows.Forms.Keys.Tab) {
                        if((UserActivityHook.GetKeyState(0x5B) & 0x80) == 0x80) {
                            // check if Shift is pressed
                            if((UserActivityHook.GetKeyState(0x10) & 0x80) == 0x80) {
                                // get active window and show only related windows
                                IntPtr aw = WindowManager.GetForegroundWindow();
                                WindowManager.GetWindowThreadProcessId(aw, out wm.filterKey);
                            }
                            else {
                                wm.filterKey = 0;
                            }

                            // shift
                            ignoreWinRestore = (UserActivityHook.GetKeyState(0x11) & 0x80) == 0x80;
                            winct = 1;
                            showSmartFlip();
                            firtstab = true;

                            e.Handled = true;
                            return;
                        }
                    }
                }
                else if(e.KeyCode == wm.options.triggerKey) {
                    if(wm.options.useShift) {
                        if((UserActivityHook.GetKeyState(0x10) & 0x80) != 0x80) {
                            return;
                        }
                    }

                    if(wm.options.useCtrl) {
                        if((UserActivityHook.GetKeyState(0x11) & 0x80) != 0x80) {
                            return;
                        }
                    }

                    if(wm.options.useAlt) {
                        if((UserActivityHook.GetKeyState(0x12) & 0x80) != 0x80) {
                            return;
                        }
                    }

                    showSmartFlip();

                    e.Handled = true;
                    return;
                }

                if(e.KeyCode == wm.options.triggerKey2) {
                    if(wm.options.useShift) {
                        if((UserActivityHook.GetKeyState(0x10) & 0x80) != 0x80) {
                            return;
                        }
                    }

                    if(wm.options.useCtrl2) {
                        if((UserActivityHook.GetKeyState(0x11) & 0x80) != 0x80) {
                            return;
                        }
                    }

                    if(wm.options.useAlt2) {
                        if((UserActivityHook.GetKeyState(0x12) & 0x80) != 0x80) {
                            return;
                        }
                    }

                    // get active window and show only related windows
                    IntPtr aw = WindowManager.GetForegroundWindow();
                    WindowManager.GetWindowThreadProcessId(aw, out wm.filterKey);

                    showSmartFlip();

                    e.Handled = true;
                    return;
                }
            }

            e.Handled = false;
        }

        private void convertSettingsToSmartFlipOptions(Settings st, ref SmartFlipOptions sfo) {
            // we use ref because sfo is a "struct"
            sfo.showWindowTitle = st.ShowWindowTitle;
            sfo.windowTitleFontSize = st.TitleFontSize;
            sfo.titleGlowSize = st.TitleGlowSize;
            sfo.titleGlowColor = st.TitleGlowColor;
            sfo.defaultWindowOpacity = st.DefaultWindowOpacity;
            sfo.flipDuration = st.AnimateWindowFlip ? st.WindowFlipDuration : 0;
            sfo.bringOnFrontDuration = st.AnimateBringWindowOnTop ? st.BringWindowOnTopDuration : 0;
            sfo.windowSelectDuration = st.AnimateWindowSelect ? st.WindowSelectDuration : 0;
            sfo.animateAllWindowsOnSelection = st.AnimateAllOnSelection;
            sfo.animateWindowEntrance = st.AnimateWindowEntrance;
            sfo.windowEntranceDuration = st.WindowEntranceDuration;
            sfo.screenAmountUsed = st.ScreenAmountUsed;
            sfo.useWinTab = st.UseWinTab;
            sfo.selectOnWinReleased = st.SelectOnWinReleased;
            sfo.triggerKey = st.Key;
            sfo.useShift = st.UseShift;
            sfo.useAlt = st.UseAlt;
            sfo.useCtrl = st.UseCtrl;
            sfo.showButtonPanel = st.ShowButtonPanel;
            sfo.showButtonPanelReflection = st.ShowButtonPanelReflection;
            sfo.backgroundOpacity = st.BackgroundOpacity;
            sfo.useMouseTrigger = st.MouseTrigger;
            sfo.regionLeft = st.RegionLeft;
            sfo.regionTop = st.RegionTop;
            sfo.regionWidth = st.RegionWidth;
            sfo.regionHeight = st.RegionHeight;
            sfo.triggerKey2 = st.Key2;
            sfo.useShift2 = st.UseShift2;
            sfo.useAlt2 = st.UseAlt2;
            sfo.useCtrl2 = st.UseCtrl2;
            sfo.windowFilterDuration = st.AnimateFilter ? st.FilterAnimationDuration : 0;
        }

        private void MouseWheelHandler(object sender, MouseWheelEventArgs e) {
            wm.onTabKeyDown(e.Delta > 0);
        }


        private void onWindow1Closing(Object sender, CancelEventArgs e) {
            if(!closeAllowed) {
                e.Cancel = true;
            }
        }

        private void OnWindowClose(object sender, EventArgs e) {
            if(wm.options.showWindowTitle) {
                wm.overlayWindow.Hide();

                wm.overlayWindow.LayoutRoot.Children.Clear();
                wm.overlayWindow.titles = null;
                wm.unregisterWindowTitles();
            }

            wm.destroyWindows();
            wm.alreadyFiltered = false;
            wm.filterKey = 0;

            //SmartFlip.Properties.Settings.Default.Save();

            //this.WindowState = WindowState.Normal;

            keyPressed = false;
            running = false;

            this.Hide();
        }

        private void exitSmartFlip() {
            // hide tray icon
            icon.Visible = false;

            closeAllowed = true;
            wm.overlayWindow.closeAllowed = true;
            uah.Stop();

            wm.overlayWindow.Close();
            this.Close();

        }

        private void MouseButtonUpHandler(object sender, MouseButtonEventArgs e) {
            Point aux = e.GetPosition(this);

            if(e.ChangedButton == MouseButton.Left) {
                wm.onLeftButtonUp((int)aux.X, (int)aux.Y);
            }
            else if(e.ChangedButton == MouseButton.Right) {
                KeyStates s = Keyboard.GetKeyStates(Key.LeftShift);

                if(s != KeyStates.None) {
                    wm.onRightButtonUp((int)aux.X, (int)aux.Y, true);
                }
                else {
                    wm.onRightButtonUp((int)aux.X, (int)aux.Y, false);
                }
            }

            if(wm.options.showWindowTitle) {
                this.Topmost = true;
                wm.overlayWindow.Topmost = true;
            }
        }

        private void MouseMoveHandler(object sender, System.Windows.Input.MouseEventArgs e) {
            Point aux = e.GetPosition(this);
            wm.onMouseMove((int)aux.X, (int)aux.Y);
        }

        protected void Onb2Click(object sender, RoutedEventArgs e) {
            if(ft) {
                window.animation.duration = 450;
                window.animation.startX = 200;
                window.animation.startY = 100;
                window.animation.startWindowOpacity = 0;
                window.animation.setEndWindowOpacity(255);
                window.animation.setEndPosition(450, 400);
                window.animation.startHeight = 250;
                window.animation.startWidth = 250;
                window.animation.setEndSize(510, 510);
                window.animation.startAnimation();

                ft = false;
            }
            else {
                window.animation.duration = 450;
                window.animation.startX = window.windowX;
                window.animation.startY = window.windowY;
                window.animation.startWindowOpacity = window.windowOpacity;
                window.animation.setEndWindowOpacity(255);
                window.animation.setEndPosition(450, 400);
                window.animation.startHeight = window.windowHeight;
                window.animation.startWidth = window.windowWidth;
                window.animation.setEndSize(510, 510);
                window.animation.startAnimation();
            }
        }

        protected void Onb2Click2(object sender, RoutedEventArgs e) {
            wm.startAngle += 2;
            wm.placeWindows(true);
        }

        protected void update(object sender, EventArgs e) {

        }

        public MainWindow() {
            InitializeComponent();


        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            wm.onTabKeyDown(false);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) {
            wm.onTabKeyDown(true);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e) {
            wm.selectWindow(wm.activeWindow);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            wm.selectWindow(0);
        }

        private void vchanged(object sender) {

        }

        private void SmartFlip_Loaded(object sender, RoutedEventArgs e) {
            if(!checkVista()) {
                System.Windows.MessageBox.Show("You are not running Window Vista.\nWindows Vista is needed for SmartFlip to run", "SmartFlip");
                this.Close();
            }

            // create and display NotifyIcon
            menu = new System.Windows.Forms.ContextMenu();

            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Options", Settings_Click));
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("About", About_Click));
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("-"));
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Exit", Exit_Click));

            icon = new System.Windows.Forms.NotifyIcon();

            icon.ContextMenu = menu;
            icon.Text = "SmartFlip (beta1)";
            icon.Visible = true;

            // load icon from resource
            icon.Icon = (System.Drawing.Icon)SmartFlip.Properties.Resources.SmartFlip_Icon;

            // handle double-click on the icon
            icon.DoubleClick += Icon_DblClick;

            // perform other initializations
            initSmartFlip();
        }

        private void Settings_Click(object Sender, EventArgs e) {
            Window3 settingsWindow = new Window3();

            Debug.WriteLine("At moment   " + DateTime.Now.ToLongTimeString() + "  " + DateTime.Now.Millisecond.ToString());

            settingsWindow.selectedPanel = 0;
            settingsWindow.ShowDialog();

            Debug.WriteLine("At moment   " + DateTime.Now.ToLongTimeString() + "  " + DateTime.Now.Millisecond.ToString());

            convertSettingsToSmartFlipOptions(SmartFlip.Properties.Settings.Default, ref wm.options);
        }

        private void About_Click(object Sender, EventArgs e) {
            Window3 settingsWindow = new Window3();

            settingsWindow.selectedPanel = 3;
            settingsWindow.ShowDialog();

            convertSettingsToSmartFlipOptions(SmartFlip.Properties.Settings.Default, ref wm.options);
        }

        private void Exit_Click(object Sender, EventArgs e) {
            exitSmartFlip();
        }

        private void Icon_DblClick(object Sender, EventArgs e) {
            Window3 settingsWindow = new Window3();

            settingsWindow.selectedPanel = 0;
            settingsWindow.ShowDialog();

            Properties.Settings.Default.Reload();
            convertSettingsToSmartFlipOptions(SmartFlip.Properties.Settings.Default, ref wm.options);
        }

        private void initSmartFlip() {
            wm = new WindowManager();
            wm.mainWindow = this;

            // create user activity hooks (keyboard and mouse)
            uah = new UserActivityHook();
            uah.KeyDown += (System.Windows.Forms.KeyEventHandler)KeyDownHandler;
            uah.KeyUp += (System.Windows.Forms.KeyEventHandler)KeyUpHandler;
            uah.OnMouseActivity += MouseMoveIntercept;
            uah.Start();

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            wm.destinationHandle = hwnd;
            wm.destinationDpi = 96.0;
            wm.grid = grid1;

            wm.overlayWindow = new Window2();

            // create windows and hide them
            this.Left = this.Top = -32000;
            wm.overlayWindow.Left = wm.overlayWindow.Top = -32000;
            this.Hide();
            buttonPanel.Visibility = Visibility.Hidden;

            wm.windowClose += OnWindowClose;

            // add keyboard and mouse handlers for both windows
            this.Closing += new CancelEventHandler(onWindow1Closing);

            Mouse.AddMouseMoveHandler(this, MouseMoveHandler);
            Mouse.AddMouseUpHandler(this, MouseButtonUpHandler);
            Mouse.AddMouseWheelHandler(this, MouseWheelHandler);

            Mouse.AddMouseMoveHandler(wm.overlayWindow, MouseMoveHandler);
            Mouse.AddMouseUpHandler(wm.overlayWindow, MouseButtonUpHandler);
            Mouse.AddMouseWheelHandler(wm.overlayWindow, MouseWheelHandler);
            CompositionTarget.Rendering += wm.updateDisplay;

            convertSettingsToSmartFlipOptions(SmartFlip.Properties.Settings.Default, ref wm.options);
        }

        private void showSmartFlip() {
            // check if Aero is enabled
            bool enabled = false;

            DwmIsCompositionEnabled(ref enabled);
            if(!enabled) {
                System.Windows.MessageBox.Show("You don't have Windows Aero activated. SmartFlip cannot continue.", "SmartFlip");
                return;
            }

            running = true;
            this.Background = new SolidColorBrush(Color.FromArgb((byte)wm.options.backgroundOpacity, 0, 0, 0));

            // read settings
            SmartFlip.Properties.Settings.Default.Reload();
            convertSettingsToSmartFlipOptions(SmartFlip.Properties.Settings.Default, ref wm.options);
            SmartFlip.Properties.Settings.Default.Save();

            wm.getWindows();

            if(wm.windowNumber < 2) {
                // don't display, perform cleanup
                wm.destroyWindows();
                wm.alreadyFiltered = false;
                wm.filterKey = 0;
                keyPressed = false;
                running = false;
                return;
            }

            this.Left = this.Top = 0;
            this.Width = wm.screenWidth;
            this.Height = wm.screenHeight;
            this.Topmost = true;
            ActivateGlass();
            this.Activate();
            this.Show();

            wm.activeWindow = 1;
            wm.nextActiveWindow = 1;
            wm.initDisplay();
            this.Topmost = true;

            if(wm.mode == 1) {
                wm.startAngle = 270 - 360.0 / wm.windowNumber;
            }
            else {
                wm.startAngle = 360 - wm.complexZoomAngle;
            }

            // show overlay window (if needed)
            if(wm.options.showWindowTitle) {
                wm.registerWindowTitles();

                wm.overlayWindow.Left = wm.overlayWindow.Top = 0;
                wm.overlayWindow.Topmost = true;
                wm.overlayWindow.Visibility = Visibility.Visible;

                wm.overlayWindow.setTitleOptions(wm.options.titleGlowColor, wm.options.titleGlowSize, wm.options.windowTitleFontSize);
                wm.overlayWindow.createWTitles(wm.windowNumber);

                wm.overlayWindow.Topmost = true;
                wm.overlayWindow.WindowState = WindowState.Maximized;

                wm.overlayWindow.Width = wm.screenWidth;
                wm.overlayWindow.Height = wm.screenHeight;

                wm.overlayWindow.Owner = this;
                wm.overlayWindow.Hide();
                wm.overlayWindow.Show();
                wm.overlayWindow.Show();
                wm.overlayWindow.Topmost = true;
                wm.overlayWindow.Activate();
                this.Topmost = true;
            }

            // show button panel
            if(wm.options.showButtonPanel) {
                Canvas.SetLeft(buttonPanel, wm.screenWidth - 20 - buttonPanel.Width);
                Canvas.SetLeft(reflectedButtonPanel, wm.screenWidth - 20 - buttonPanel.Width);
                Canvas.SetTop(buttonPanel, 10);
                Canvas.SetTop(reflectedButtonPanel, buttonPanel.Height + 10);

                if(wm.options.showButtonPanelReflection) {
                    reflectedButtonPanel.Visibility = Visibility.Visible;
                }
                else {
                    reflectedButtonPanel.Visibility = Visibility.Hidden;
                }

                buttonPanel.Visibility = Visibility.Visible;
            }
            else {
                buttonPanel.Visibility = Visibility.Hidden;
            }

            if(wm.options.animateWindowEntrance) {
                wm.placeWindows(false);
                wm.animateWindowsEntrance();
            }
            else {
                wm.placeWindows(true);
            }
        }

        bool checkVista() {
            OSVERSIONINFO osvi = new OSVERSIONINFO();
            osvi.dwOSVersionInfoSize = Marshal.SizeOf(osvi);

            GetVersionEx(ref osvi);

            return osvi.dwMajorVersion >= 6;
        }

        #region DWM API

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS {
            public int cxLeftWidth;      // width of left border that retains its size
            public int cxRightWidth;     // width of right border that retains its size
            public int cyTopHeight;      // height of top border that retains its size
            public int cyBottomHeight;   // height of bottom border that retains its size
        };


        [DllImport("DwmApi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd,
            ref MARGINS pMarInset);

        #endregion

        private void ActivateGlass() {
            // check if we are running on Vista
            if(Environment.OSVersion.Version.Major < 6) {
                return;
            }

            // obtain the window handle for WPF application
            IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
            HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
            mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

            // get system Dpi
            System.Drawing.Graphics desktop = System.Drawing.Graphics.FromHwnd(mainWindowPtr);
            float DesktopDpiX = desktop.DpiX;
            float DesktopDpiY = desktop.DpiY;

            // extend glass frame into client area
            // note that the default desktop Dpi is 96dpi. The  margins are
            // adjusted for the system Dpi
            MARGINS margins = new MARGINS();
            margins.cxLeftWidth = Convert.ToInt32(-1 * (DesktopDpiX / 96));
            margins.cxRightWidth = Convert.ToInt32(-1 * (DesktopDpiX / 96));
            margins.cyTopHeight = Convert.ToInt32(-1 * (DesktopDpiX / 96));
            margins.cyBottomHeight = Convert.ToInt32(this.ActualHeight * (DesktopDpiX / 96));

            int hr = DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
        }
    }
}
