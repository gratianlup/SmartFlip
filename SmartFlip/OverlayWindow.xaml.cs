// Copyright (c) Gratian Lup. All rights reserved.
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
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SmartFlip {
    public struct WTitle {
        public int number;
        public Label title;
        public double x, y;
        public double width, height;
        public double titleWidth, titleHeight;
    }

    public partial class Window2 {
        public WTitle[] titles;
        public bool closeAllowed;

        public Window2() {
            this.InitializeComponent();

            // Insert code required on object creation below this point.
            this.Closing += new CancelEventHandler(onWindow2Closing);
            closeAllowed = false;
        }

        private void onWindow2Closing(Object sender, CancelEventArgs e) {
            if(!closeAllowed) {
                e.Cancel = true;
            }
        }

        public void setTitleOptions(Color glowColor, double glowSize, double fontSize) {
            // first try to remove previous resources
            this.Resources.Remove("glowColor");
            this.Resources.Remove("glowSize");
            this.Resources.Remove("glowSize2");
            this.Resources.Remove("fontSize");

            // now add new resources
            this.Resources.Add("glowColor", glowColor);
            this.Resources.Add("glowSize", glowSize);
            this.Resources.Add("glowSize2", glowSize - 8 > 0 ? glowSize - 8 : 1);
            this.Resources.Add("fontSize", fontSize);
        }

        public void createWTitles(int number) {
            titles = new WTitle[number];

            for(int i = 0; i < number; i++) {
                titles[i].title = new Label();
                titles[i].title.Style = (Style)(this.Resources["wTitle"]);

                LayoutRoot.Children.Add(titles[i].title);
            }
        }

        public void setWTitleSize(int n, double width, double height) {
            WTitle t = titles[n];

            double labelWidth, labelHeight;

            labelWidth = 0.7 * width;
            labelHeight = 0.3 * height;

            if(labelWidth < 175) {
                labelWidth = 175;
            }

            if(labelHeight < 75) {
                labelHeight = 75;
            }

            t.width = width;
            t.height = height;
            t.titleWidth = labelWidth;
            t.titleHeight = labelHeight;

            t.title.Width = labelWidth;
            t.title.Height = labelHeight;
            titles[n] = t;
        }

        public void setWTitlePosition(int n, double newX, double newY) {
            WTitle t = titles[n];
            Canvas.SetLeft(t.title, newX + (t.width - t.titleWidth) / 2);
            Canvas.SetTop(t.title, newY + (t.height - t.titleHeight) / 2);
        }

        public void startFadeInAnimation(int n, int duration) {
            // don't use animation
            duration = 0;
            titles[n].title.Opacity = 1;
        }

        public void startFadeOutAnimation(int n, int duration) {
            duration = 0;
            // don't use animation
            titles[n].title.Opacity = 0;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e) {
            ActivateGlass();
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

            // set Margins
            MARGINS margins = new MARGINS();

            // extend glass frame into client area
            // note that the default desktop Dpi is 96dpi. The  margins are
            // adjusted for the system Dpi
            margins.cxLeftWidth = Convert.ToInt32(-1 * (DesktopDpiX / 96));
            margins.cxRightWidth = Convert.ToInt32(-1 * (DesktopDpiX / 96));
            margins.cyTopHeight = Convert.ToInt32(-1 * (DesktopDpiX / 96));
            margins.cyBottomHeight = Convert.ToInt32(this.ActualHeight * (DesktopDpiX / 96));

            int hr = DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
        }

    }
}
