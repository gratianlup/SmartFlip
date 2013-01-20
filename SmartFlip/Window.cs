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

namespace SmartFlip.WindowThumbnail {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Windows.Media.Effects;

    #region DWM Thumbnail API

    [StructLayout(LayoutKind.Sequential)]
    internal struct PSIZE {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DWM_THUMBNAIL_PROPERTIES {
        public int dwFlags;
        public Rect rcDestination;
        public Rect rcSource;
        public byte opacity;
        public bool fVisible;
        public bool fSourceClientAreaOnly;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect {
        internal Rect(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion

    // **************************************************************************
    // **************************************************************************
    class WindowBase {
        #region Constants

        public const byte STATE_RUNNING = 1;
        public const byte STATE_STOPPED = 2;
        public const byte STATE_FINISHED = 3;

        public const byte ANIMATE_POSITION = 1;
        public const byte ANIMATE_SIZE = 2;
        public const byte ANIMATE_WINDOW_OPACITY = 4;
        public const byte ANIMATE_TITLE_OPACITY = 8;

        #endregion
    }

    // **************************************************************************
    // **************************************************************************
    class WindowAnimation : WindowBase {
        #region Variables

        public int animationState;
        public TimeTracker time;
        public int startTime;
        public int duration;
        public byte animatedElements;

        /*
         * position and size animation
         */
        public double startX;
        public double startY;
        public double deltaX;
        public double deltaY;
        public double startWidth;
        public double startHeight;
        public double deltaWidth;
        public double deltaHeight;

        /*
         * window opacity and glow opacity animation
         */
        public int startWindowOpacity;
        public int deltaWindowOpacity;
        public int startTitleOpacity;
        public int deltaTitleOpacity;
        #endregion

        #region Funtions
        public WindowAnimation() {
            animationState = STATE_STOPPED;
            time = new TimeTracker();
        }

        // --------------------------------------------------------------------------
        public void setEndPosition(double endX, double endY) {
            deltaX = endX - startX;
            deltaY = endY - startY;

            animatedElements |= ANIMATE_POSITION;
        }

        // --------------------------------------------------------------------------
        public void setEndSize(double endWidth, double endHeight) {
            deltaWidth = endWidth - startWidth;
            deltaHeight = endHeight - startHeight;

            animatedElements |= ANIMATE_SIZE;
        }

        // --------------------------------------------------------------------------
        public void setEndWindowOpacity(int endOpacity) {
            deltaWindowOpacity = endOpacity - startWindowOpacity;

            animatedElements |= ANIMATE_WINDOW_OPACITY;
        }

        // --------------------------------------------------------------------------
        public void setEndTitleOpacity(int endGlowOpacity) {
            deltaTitleOpacity = endGlowOpacity - startTitleOpacity;

            animatedElements |= ANIMATE_TITLE_OPACITY;
        }

        // --------------------------------------------------------------------------
        public void startAnimation() {
            animationState = STATE_RUNNING;

            time.duration = duration;
            time.eventOnTimeEllapsed = true;
            time.onTimeEllapsed += onFinished;
            time.startTimer();
        }

        // --------------------------------------------------------------------------
        public void stopAnimation() {
            animationState = STATE_STOPPED;
        }

        // --------------------------------------------------------------------------
        // This event fires when the animation time has ellapsed
        // --------------------------------------------------------------------------
        private void onFinished(object sender, EventArgs e) {
            animationState = STATE_FINISHED;
        }

        public void reset() {
            animatedElements = 0;
        }

        #endregion
    }


    // **************************************************************************
    // **************************************************************************
    class Window : WindowBase {
        #region DWM Thumbnail API functions

        [DllImport("dwmapi.dll")]
        static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        [DllImport("dwmapi.dll")]
        static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        #endregion

        #region Other Win32 functions

        [DllImport("user32.Dll")]
        static extern void GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        #endregion

        #region Constants
        static readonly int DWM_TNP_VISIBLE = 0x8;
        static readonly int DWM_TNP_OPACITY = 0x4;
        static readonly int DWM_TNP_RECTDESTINATION = 0x1;
        #endregion

        #region Variables

        public int number;
        public IntPtr sourceHwnd;
        public IntPtr destinationHwnd;
        public uint procId;
        public IntPtr thumb;
        public bool displayWindowTitle;

        public int sourceX;
        public int sourceY;
        public int sourceWidth;
        public int sourceHeight;
        public double sourceWidthDenominator;
        public double sourceHeightDenominator;

        public double windowX;
        public double windowY;
        public double windowWidth;
        public double windowHeight;

        public int windowOpacity;
        public bool windowVisible;
        public bool windowTitleVisible;
        public int windowTitleOpacity;

        public bool onTop;

        public WindowAnimation animation;
        DWM_THUMBNAIL_PROPERTIES windowProperties;
        int flags;

        #region WPF related

        public Color primaryGlowColor;
        public Color secondaryGlowColor;
        public SolidColorBrush fontColor;
        public TextBlock windowTitle;
        public double titleX;
        public double titleY;

        #endregion

        #region Drawing related

        public double angle;
        public double yRadius;
        public double smallWindowWidth;
        public double smallWindowHeight;
        public double largeWindowWidth;
        public double largeWindowHeight;
        public double zoomCenterY;
        public double focus;
        public double deltaWidth1;
        public double deltaHeight1;
        public double deltaWidth2;
        public double deltaHeight2;
        public double zoomZone2StartWidth;
        public double zoomZone2StartHeight;
        public double deltaWidth;
        public double deltaHeight;

        #endregion

        #region Window state

        public bool minimized;
        public bool restoreAsMaximized;

        #endregion

        #region Event

        public bool eventOnAnimationFinished;
        public event EventHandler onAnimationFinished;

        #endregion

        #endregion

        #region Optimization

        double lastTitleOpacity;
        double lastTitleX;
        double lastTitleY;
        int lastWindowX;
        int lastWindowY;

        #endregion

        #region Functions

        // --------------------------------------------------------------------------
        public Window() {
            animation = new WindowAnimation();
            windowProperties = new DWM_THUMBNAIL_PROPERTIES();
            flags = 0;
        }

        // --------------------------------------------------------------------------
        // Register a window in the DWM. Should be called after sourceHwnd and
        // destinationHwnd where set
        // --------------------------------------------------------------------------
        public void registerWindow() {
            DwmRegisterThumbnail(destinationHwnd, sourceHwnd, out thumb);
        }

        // --------------------------------------------------------------------------
        // Unregister the window in the DWM.
        // --------------------------------------------------------------------------
        public void unregisterWindow() {
            DwmUnregisterThumbnail(thumb);
        }

        // --------------------------------------------------------------------------
        // Redraw the window
        // --------------------------------------------------------------------------
        public void updateWindow() {
            windowProperties.rcDestination.Left = (int)(windowX - (windowWidth * 0.5));
            windowProperties.rcDestination.Top = (int)(windowY - (windowHeight * 0.5));

            if(Math.Abs(windowProperties.rcDestination.Left - lastWindowX) >= 1 ||
                Math.Abs(windowProperties.rcDestination.Top - lastWindowY) >= 1) {
                lastWindowX = windowProperties.rcDestination.Left;
                lastWindowY = windowProperties.rcDestination.Top;

                windowProperties.dwFlags = flags;
                windowProperties.fVisible = windowVisible && windowOpacity > 0;
                windowProperties.opacity = (byte)windowOpacity;

                windowProperties.rcDestination.Right = (int)(windowX + (windowWidth * 0.5));
                windowProperties.rcDestination.Bottom = (int)(windowY + (windowHeight * 0.5));

                DwmUpdateThumbnailProperties(thumb, ref windowProperties);
            }

            // reset flags
            flags = 0;

            // display / hide the window title
            if(windowTitle == null) {
                return;
            }

            if(windowTitleVisible == false || windowTitleOpacity <= 0) {
                if(windowTitleVisible) {
                    windowTitle.Visibility = Visibility.Collapsed;
                    windowVisible = false;
                }
            }
            else {
                titleY = windowY + windowHeight / 2 + 5;
                titleX = windowX - windowWidth / 2 - 102.5;

                if(windowTitle.Width != windowWidth + 200) {
                    windowTitle.Width = windowWidth + 200;
                }

                if(!windowVisible) {
                    windowVisible = true;
                }

                // set title color
                if(Math.Abs(windowTitleOpacity - lastTitleOpacity) > 1) {
                    fontColor.Color = Color.FromArgb((byte)windowTitleOpacity, 255, 255, 255);
                    lastTitleOpacity = windowTitleOpacity;
                }

                if(Math.Abs(titleX - lastTitleX) >= 1 || Math.Abs(titleY - lastTitleY) >= 1) {
                    Canvas.SetLeft(windowTitle, titleX);
                    Canvas.SetTop(windowTitle, titleY);

                    lastTitleX = titleX;
                    lastTitleY = titleY;
                }
            }
        }

        // --------------------------------------------------------------------------
        // Bring the window on top of the other ones.
        // --------------------------------------------------------------------------
        public void bringWindowOnTop() {
            DwmUnregisterThumbnail(thumb);
            DwmRegisterThumbnail(destinationHwnd, sourceHwnd, out thumb);
            DwmUpdateThumbnailProperties(thumb, ref windowProperties);
            onTop = true;
        }

        // --------------------------------------------------------------------------
        // Get the original size of the window
        // --------------------------------------------------------------------------
        public void getWindowSourceSize() {
            PSIZE size;
            Rect xy = new Rect();

            DwmQueryThumbnailSourceSize(thumb, out size);

            sourceWidth = size.x;
            sourceHeight = size.y;
            sourceWidthDenominator = 1.0 / size.x;
            sourceHeightDenominator = 1.0 / size.y;

            GetWindowRect(sourceHwnd, out xy);
            sourceX = xy.Left;
            sourceY = xy.Top;
        }

        // --------------------------------------------------------------------------
        public void setWindowOpacity(int newOpacity) {
            windowOpacity = newOpacity;
            flags |= DWM_TNP_OPACITY;
        }

        // --------------------------------------------------------------------------
        public void setWindowVisibility(bool visible) {
            windowVisible = visible;
            flags |= DWM_TNP_VISIBLE;
        }

        // --------------------------------------------------------------------------
        public void setWindowSize(ref double newWidth, ref double newHeight) {
            double percentW, percentH;

            /*
             * avoid "division by zero" error
             */
            if(sourceHeight == 0 || sourceWidth == 0) {
                return;
            }

            percentW = newWidth * sourceWidthDenominator;
            percentH = newHeight * sourceHeightDenominator;

            if(percentH < percentW) {
                windowWidth = sourceWidth * percentH;
                windowHeight = sourceHeight * percentH;
            }
            else {
                windowWidth = sourceWidth * percentW;
                windowHeight = sourceHeight * percentW;
            }

            // don't make the window bigger than the original
            if(windowWidth > sourceWidth || windowHeight > sourceHeight) {
                windowWidth = sourceWidth;
                windowHeight = sourceHeight;
            }

            flags |= DWM_TNP_RECTDESTINATION;
        }

        // --------------------------------------------------------------------------
        public void getWindowTitle() {
            StringBuilder sb = new StringBuilder(260);

            /*
             * call the GetWindowText API function
             */
            GetWindowText(sourceHwnd, sb, sb.Capacity);
            windowTitle.Text = sb.ToString();
        }

        // --------------------------------------------------------------------------
        public void initWindowTitleInfo(int newGlowSize, Color newSecondaryColor, double newFontSize) {
            displayWindowTitle = true;
            windowTitle = new TextBlock();

            // set colors
            fontColor = new SolidColorBrush();
            fontColor.Color = Color.FromArgb((byte)windowTitleOpacity, 255, 255, 255);

            primaryGlowColor = Colors.White;
            fontColor = new SolidColorBrush();
            fontColor.Color = Color.FromArgb((byte)windowTitleOpacity, 255, 255, 255);
            secondaryGlowColor = newSecondaryColor;

            // get window title and set font color
            getWindowTitle();
            windowTitle.Foreground = fontColor;
            windowTitle.FontSize = newFontSize;
            windowTitle.Visibility = windowTitleVisible ? Visibility.Visible : Visibility.Hidden;
            windowTitle.TextAlignment = TextAlignment.Center;
        }

        // --------------------------------------------------------------------------
        // Animates the window
        // --------------------------------------------------------------------------
        public void applyAnimationStep() {
            double completition;
            animation.time.update();

            if(animation.animationState == STATE_RUNNING) {
                // get animation completition
                completition = animation.time.deltaTimeToPercent();
                flags = 0;

                // update position
                if((animation.animatedElements & ANIMATE_POSITION) != 0) {
                    windowX = animation.startX + (animation.deltaX * completition);
                    windowY = animation.startY + (animation.deltaY * completition);

                    flags |= DWM_TNP_RECTDESTINATION;
                }

                // update window size
                if((animation.animatedElements & ANIMATE_SIZE) != 0) {
                    windowWidth = animation.startWidth + (animation.deltaWidth * completition);
                    windowHeight = animation.startHeight + (animation.deltaHeight * completition);

                    setWindowSize(ref windowWidth, ref windowHeight);
                }

                // update window opacity
                if((animation.animatedElements & ANIMATE_WINDOW_OPACITY) != 0) {
                    windowOpacity = (int)((double)animation.startWindowOpacity + ((double)animation.deltaWindowOpacity * completition));

                    flags |= DWM_TNP_OPACITY;
                }

                // update window title opacity
                if((animation.animatedElements & ANIMATE_TITLE_OPACITY) != 0) {
                    windowTitleOpacity = (int)((double)animation.startTitleOpacity + ((double)animation.deltaTitleOpacity * completition));
                }
            }
            else if(animation.animationState == STATE_FINISHED) {
                // update position
                if((animation.animatedElements & ANIMATE_POSITION) != 0) {
                    windowX = animation.startX + animation.deltaX;
                    windowY = animation.startY + animation.deltaY;

                    flags |= DWM_TNP_RECTDESTINATION;
                }

                // update window size
                if((animation.animatedElements & ANIMATE_SIZE) != 0) {
                    windowWidth = animation.startWidth + animation.deltaWidth;
                    windowHeight = animation.startHeight + animation.deltaHeight;

                    setWindowSize(ref windowWidth, ref windowHeight);
                }

                // update window opacity
                if((animation.animatedElements & ANIMATE_WINDOW_OPACITY) != 0) {
                    windowOpacity = animation.startWindowOpacity + animation.deltaWindowOpacity;

                    flags |= DWM_TNP_OPACITY;
                }

                // update window title opacity
                if((animation.animatedElements & ANIMATE_TITLE_OPACITY) != 0) {
                    windowTitleOpacity = animation.startTitleOpacity + animation.deltaTitleOpacity;
                }

                // stop animation !
                animation.animationState = STATE_STOPPED;

                // call event function
                if(eventOnAnimationFinished) {
                    onAnimationFinished(this, null);
                }
            }
        }

        public void test() {
            if(eventOnAnimationFinished) {
                onAnimationFinished(this, null);
            }
        }

        #endregion
    }
}
