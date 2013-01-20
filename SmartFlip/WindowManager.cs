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

using System.Windows;

namespace SmartFlip.Manager {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Windows.Media.Effects;
    using System.Collections;
    using SmartFlip.WindowThumbnail;
    using System.Diagnostics;

    public struct SmartFlipOptions {
        public bool showWindowTitle;
        public int titleGlowSize;
        public Color titleGlowColor;
        public double windowTitleFontSize;
        public int defaultWindowOpacity;
        public int flipDuration;
        public int bringOnFrontDuration;
        public int windowSelectDuration;
        public bool animateAllWindowsOnSelection;
        public bool animateWindowEntrance;
        public int windowEntranceDuration;
        public bool showButtonPanel;
        public bool showButtonPanelReflection;
        public int screenAmountUsed;
        public bool useWinTab;
        public bool selectOnWinReleased;
        public System.Windows.Forms.Keys triggerKey;
        public bool useAlt;
        public bool useShift;
        public bool useCtrl;
        public int backgroundOpacity;
        public bool useMouseTrigger;
        public int regionLeft;
        public int regionTop;
        public int regionWidth;
        public int regionHeight;

        public bool filterWindows;
        public bool animateWindowFilter;
        public int windowFilterAnimationType;
        public int windowFilterDuration;

        public System.Windows.Forms.Keys triggerKey2;
        public bool useAlt2;
        public bool useShift2;
        public bool useCtrl2;
    }

    public class WindowManager {
        #region Win32 constants

        static readonly int GWL_STYLE = -16;
        static readonly int GWL_EXSTYLE = -20;

        static readonly ulong WS_VISIBLE = 0x10000000L;
        static readonly ulong WS_BORDER = 0x00800000L;
        static readonly ulong WS_EX_APPWIND = 0x00040000L;

        #endregion

        #region Win32 functions

        [DllImport("user32.dll")]
        static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);
        delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        static extern bool SetWindowPlacement(IntPtr hWnd,
           [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetShellWindow();


        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        private struct POINTAPI {
            public int x;
            public int y;
        }

        private struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private struct WINDOWPLACEMENT {
            public int length;
            public int flags;
            public int showCmd;
            public POINTAPI ptMinPosition;
            public POINTAPI ptMaxPosition;
            public RECT rcNormalPosition;
        }

        #endregion

        #region Display constants

        const int CENTER_POSITION = 35;
        const double MIN_RADIUS = 68;
        const double MAX_RADIUS = 80;
        const double THUMBS_FOR_1280 = 14;
        const double SECOND_CENTER_POSITON = 40;
        const double MARGIN = 20;

        #endregion

        #region Constants

        const int MODE_SIMPLE = 1;
        const int MODE_COMPLEX = 2;

        const int STATUS_FLIP = 1;
        const int STATUS_IDLE = 2;
        const int STATUS_WINDOW_ANIMATION_IN = 3;
        const int STATUS_WINDOW_ANIMATION_OUT = 6;
        const int STATUS_WINDOW_ENTRANCE = 4;
        const int STATUS_WINDOW_SELECT = 5;
        const int STATUS_WINDOW_FILTER = 7;

        #endregion

        #region Common variables

        static List<Window> filteredWindows;
        bool filterWindows;
        public uint filterKey;
        int fwCount;
        public bool alreadyFiltered;

        static List<Window> windows;
        public List<int> windowsZOrder;
        public List<int> animatedWindows;
        public int windowNumber;
        public int mode;
        public double startAngle;
        public IntPtr destinationHandle;
        public double destinationDpi;
        public SmartFlipOptions options;
        public TimeTracker time;
        public int status;
        private int lastWindowOnTop;
        public int activeWindow;
        public int nextActiveWindow;
        public event EventHandler windowClose;
        public Window2 overlayWindow;
        Random rand;
        public Panel grid;
        public bool running;
        int zoomedWindow;
        public MainWindow mainWindow;
        //public Window zeroWindow;

        #region flipAnimation

        private double deltaAngle;
        private double initialAngle;

        #endregion

        #endregion

        #region Simple mode variables

        private double angleStep;
        private double activeAngleStart;
        private double activeAngleEnd;

        #endregion

        #region Complex mode variables

        double normalAngleStep;
        double normalStartProportion;
        int startWindow;
        public double complexZoomAngle;
        double smallComplexModeOptimization;

        #endregion

        #region Window placement optimisation variables

        public double zoomRegionStartAngle;
        public double zoomRegionEndAngle;
        public double radDenominator;
        public double reverseZoomAngle;
        public double zoomProportion;
        public double reverseZoomAngleDen;
        public double zoomProportion2;
        public double deltaWindowOpacity;
        public double windowOpacityProportion;

        #endregion

        #region Drawing related

        public double screenWidth;
        public double screenHeight;
        double centerX;
        double centerY;
        double horizontalRadius;
        double zoomCenterY;
        double zoomPointX;
        double zoomPointY;
        double zoomAngle;
        double zoomAngleDen;
        double smallWindowXZone;
        double smallWindowYZone;
        double largeWindowXZone;
        double halfRadAngleDen;

        #endregion

        #region Sin/Cos optimization

        double[] sin;
        double[] cos;

        // --------------------------------------------------------------------------
        public void computeSin() {
            sin = new double[3600];
            double x = 0;

            for(int i = 0; i < 3600; i++) {
                sin[i] = Math.Sin((x * Math.PI) / 180.0);

                x += 0.1;
            }
        }

        // --------------------------------------------------------------------------
        public void computeCos() {
            cos = new double[3600];
            double x = 0;

            for(int i = 0; i < 3600; i++) {
                cos[i] = Math.Cos((x * Math.PI) / 180.0);

                x += 0.1;
            }
        }

        // --------------------------------------------------------------------------
        public double getSin(double angle) {
            return sin[(int)(angle * 10)];
        }

        // --------------------------------------------------------------------------
        public double getCos(double angle) {
            return cos[(int)(angle * 10)];
        }

        #endregion

        public WindowManager() {
            windows = new List<Window>();
            windowsZOrder = new List<int>();
            animatedWindows = new List<int>();
            rand = new Random();

            lastWindowOnTop = -1;
            time = new TimeTracker();
            activeWindow = 0;
        }

        // --------------------------------------------------------------------------
        // Function called by Windows (callback), giving information
        // about each window in the system. We just need to exclude the bad ones.
        // --------------------------------------------------------------------------
        private bool enumerateWindows(IntPtr hwnd, int lParam) {
            Window window = new Window();
            ulong style, style2;
            bool minimized = false;

            // exclude bad windows
            if(hwnd != destinationHandle) {
                style = GetWindowLongA(hwnd, GWL_STYLE);
                minimized = (style & 0x20000000L) != 0;

                // window not visible, skip it
                if((int)(style & WS_VISIBLE) == 0) {
                    return true;
                }

                style2 = GetWindowLongA(hwnd, GWL_EXSTYLE);

                if((int)(style & WS_BORDER) != 0 || (int)(style2 & WS_EX_APPWIND) != 0) {
                    window.destinationHwnd = destinationHandle;
                    window.sourceHwnd = hwnd;

                    // get process id and check if we need to filter the window
                    GetWindowThreadProcessId(hwnd, out window.procId);

                    if(filterKey != 0 && window.procId != filterKey) {
                        // skip window
                        return true;
                    }

                    // register window in DWM
                    window.registerWindow();
                    window.getWindowSourceSize();

                    // if the window is minimized, get it's default position
                    if(minimized) {
                        WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                        placement.length = Marshal.SizeOf(placement);
                        GetWindowPlacement(window.sourceHwnd, ref placement);

                        window.minimized = true;

                        if(placement.flags == 2) {
                            // window is maximized
                            window.sourceX = window.sourceY = 0;
                            window.restoreAsMaximized = true;
                        }
                        else {
                            // just restore
                            window.sourceX = placement.rcNormalPosition.left;
                            window.sourceY = placement.rcNormalPosition.top;
                            window.restoreAsMaximized = false;
                        }
                    }

                    windows.Add(window);
                }
            }
            else {
                return true; // continue enumeration
            }

            return true; //continue enumeration
        }

        // --------------------------------------------------------------------------
        // get a list of all windows in the system
        // --------------------------------------------------------------------------
        public void getWindows() {
            EnumWindows(enumerateWindows, 0);
            windowNumber = windows.Count;
        }

        // --------------------------------------------------------------------------
        // initialize SmartFlip according to the resolution of the display and the
        // number of window thumbnails
        // --------------------------------------------------------------------------
        public void initDisplay() {
            double radiusPercentage;
            double step;

            // compute sin and cos
            computeSin();
            computeCos();

            // get screen resolution
            screenWidth = (int)((double)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Right * 
                               ((double)options.screenAmountUsed / 100));
            screenHeight = (int)((double)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Bottom *
                                ((double)options.screenAmountUsed / 100));

            // center coordinates
            step = (MAX_RADIUS - MIN_RADIUS) / (THUMBS_FOR_1280 / (screenWidth / 1280));

            if(windowNumber > 3) {
                radiusPercentage = MIN_RADIUS + step * (windowNumber - 3);
            }
            else {
                radiusPercentage = MIN_RADIUS;
            }

            if(radiusPercentage < MIN_RADIUS) {
                radiusPercentage = MIN_RADIUS;
            }
            else if(radiusPercentage > MAX_RADIUS) {
                radiusPercentage = MAX_RADIUS;
            }

            centerX = screenWidth / 2.0;
            centerY = (CENTER_POSITION * screenHeight) / 100.0;

            horizontalRadius = ((screenWidth / 2.0) * radiusPercentage) / 100;
            double sws = ((screenWidth / 2.0) - horizontalRadius) * 2 - 2 * MARGIN;
            
            foreach(Window window in windows) {
                window.smallWindowWidth = window.smallWindowHeight = sws;
                window.setWindowSize(ref window.smallWindowWidth, ref window.smallWindowHeight);
                window.yRadius = centerY - window.windowHeight / 2 - MARGIN;
            }

            // second center height
            zoomCenterY = centerY + (screenHeight - centerY) / 2;

            // maximum size for large window
            double lts = screenHeight - centerY;

            if(screenWidth / screenHeight >= 1.5) {
                // optimize for widescreen (16:10,16:9) (like mine :D)
                zoomPointX = centerX - (3.0 / 4.0) * horizontalRadius;
                zoomPointY = centerY + 0.85 * (zoomCenterY - centerY);
            }
            else {
                // normal 4:3 screen
                zoomPointX = centerX - (5.0 / 6.0) * horizontalRadius;
                zoomPointY = centerY + 0.8 * (zoomCenterY - centerY);
            }

            zoomAngle = Math.Atan((zoomPointY - centerY) / (centerX - zoomPointX)) * (180.0 / Math.PI);
            smallWindowXZone = zoomPointX - (centerX - horizontalRadius);
            smallWindowYZone = zoomPointY - centerY;

            foreach(Window window in windows) {
                double ltsEx;

                if(screenWidth / screenHeight >= 1.5) {
                    // optimize for widescreen
                    ltsEx = lts * 1.15;
                    window.setWindowSize(ref ltsEx, ref lts);

                    window.largeWindowWidth = lts * 1.15;
                    window.largeWindowHeight = lts;
                }
                else {
                    ltsEx = lts * 1.05;

                    window.setWindowSize(ref ltsEx, ref lts);

                    window.largeWindowWidth = lts * 1.05;
                    window.largeWindowHeight = lts;
                }

                if(options.showWindowTitle) {
                    // allocate space for window title
                    window.focus = ((screenHeight - zoomPointY) - window.windowHeight / 2 - 
                               MARGIN - options.windowTitleFontSize / (destinationDpi / 72.0) - options.titleGlowSize) / 
                               ((centerX - zoomPointX) * (centerX - zoomPointX));
                    window.zoomCenterY = zoomPointY + (screenHeight - zoomPointY) - window.windowHeight / 2 - 
                                    MARGIN - options.windowTitleFontSize / (destinationDpi / 72.0) - options.titleGlowSize;
                }
                else {
                    window.focus = ((screenHeight - zoomPointY) - window.windowHeight / 2 - MARGIN) / 
                              ((centerX - zoomPointX) * (centerX - zoomPointX));
                    window.zoomCenterY = zoomPointY + (screenHeight - zoomPointY) - window.windowHeight / 2 - MARGIN;
                }

                window.deltaWidth1 = (window.largeWindowWidth - window.smallWindowWidth) * 0.2;
                window.deltaHeight1 = (window.largeWindowHeight - window.smallWindowHeight) * 0.2;

                window.zoomZone2StartWidth = window.smallWindowWidth + window.deltaWidth1;
                window.zoomZone2StartHeight = window.smallWindowHeight + window.deltaHeight1;

                window.deltaWidth2 = window.largeWindowWidth - window.zoomZone2StartWidth;
                window.deltaHeight2 = window.largeWindowHeight - window.zoomZone2StartHeight;

                window.deltaWidth = window.largeWindowWidth - window.smallWindowWidth;
                window.deltaHeight = window.largeWindowHeight - window.smallWindowHeight;
            }

            // other initializations
            zoomRegionStartAngle = 180 + zoomAngle;
            zoomRegionEndAngle = 360 - zoomAngle;
            radDenominator = Math.PI / 180.0;
            reverseZoomAngle = 90 - zoomAngle;
            zoomProportion = 90 / reverseZoomAngle;
            largeWindowXZone = centerX - zoomPointX;
            reverseZoomAngleDen = 1.0 / reverseZoomAngle;
            zoomAngleDen = 1.0 / zoomAngle;
            zoomProportion2 = largeWindowXZone * reverseZoomAngleDen;
            halfRadAngleDen = 1.0 / 90.0;

            deltaWindowOpacity = 255 - options.defaultWindowOpacity;
            windowOpacityProportion = 90.0 / deltaWindowOpacity;

            // choose display mode
            if(360.0 / windowNumber < reverseZoomAngle - 3) {
                mode = MODE_COMPLEX;
            }
            else {
                mode = MODE_SIMPLE;
            }

            if(mode == MODE_SIMPLE) {
                angleStep = 360.0 / windowNumber;

                activeAngleStart = 270 - (angleStep / 2);
                activeAngleEnd = 270 + (angleStep / 2);
            }
            else {
                normalAngleStep = (180 + 2 * zoomAngle) / (windowNumber - 2);
                normalStartProportion = normalAngleStep / reverseZoomAngle;

                activeAngleStart = 270 - reverseZoomAngle / 1.3;
                activeAngleEnd = 270 + reverseZoomAngle / 1.3;
                complexZoomAngle = reverseZoomAngle / ((double)windowNumber * (reverseZoomAngle / 360));
                smallComplexModeOptimization = windowNumber * reverseZoomAngle / 360.0; // :D
            }

            // compute window ZOrder
            int middle = 0;

            if(windowNumber > 2) {
                middle = (windowNumber - 2) / 2;
                middle += 2;

                for(int i = middle; i > 1; i--) {
                    windows[i].bringWindowOnTop();
                }
                for(int i = middle + 1; i < windowNumber; i++) {
                    windows[i].bringWindowOnTop();
                }
            }

            windows[0].bringWindowOnTop();

            if(windowNumber > 2) {
                for(int i = 1; i < middle; i++) {
                    windowsZOrder.Add(i);
                }

                for(int i = windowNumber - 1; i >= middle; i--) {
                    windowsZOrder.Add(i);
                }
            }

            for(int i = 0; i < windowNumber; i++) {
                windows[i].number = i;
            }

            windows[activeWindow].bringWindowOnTop();

            // check if all windows all included (it's actually a hack :P)
            for(int i = 0; i < windowNumber; i++) {
                if(!windowsZOrder.Contains(i)) {
                    windowsZOrder.Add(i);
                }
            }

            running = true;
        }

        // --------------------------------------------------------------------------
        // computes the position of a window according its angle
        // --------------------------------------------------------------------------
        private void placeWindow(int n, double angle) {
            Window window = windows[n];

            double auxAngle;
            double realAngle;
            double xx;

            if(angle >= 0 && angle < 180) {
                window.windowX = centerX + (getCos(angle) * horizontalRadius);
                window.windowY = centerY - (getSin(angle) * window.yRadius);

                window.windowWidth = window.smallWindowWidth;
                window.windowHeight = window.smallWindowHeight;

                // set window opacity
                window.setWindowOpacity((int)(255 - deltaWindowOpacity * ((90 - Math.Abs(angle - 90)) * 0.011)));

                window.windowTitleOpacity = 0;
                window.windowTitleVisible = false;
            }
            else if(angle >= 180 && angle < zoomRegionStartAngle) {
                auxAngle = angle - 180;

                window.windowX = (centerX - horizontalRadius) + (auxAngle * zoomAngleDen) * smallWindowXZone;
                window.windowY = centerY + (auxAngle * zoomAngleDen) * smallWindowYZone;

                window.windowWidth = window.smallWindowWidth + (window.deltaWidth1 * (angle - 180) * zoomAngleDen);
                window.windowHeight = window.smallWindowHeight + (window.deltaHeight1 * (angle - 180) * zoomAngleDen);

                window.setWindowOpacity(255);
                window.windowTitleOpacity = 0;
                window.windowTitleVisible = false;
            }
            else if(angle >= zoomRegionStartAngle && angle < 270) {
                window.windowX = zoomPointX + zoomProportion2 * (angle - zoomRegionStartAngle);

                xx = largeWindowXZone - (window.windowX - zoomPointX);
                window.windowY = window.zoomCenterY - (window.focus * xx * xx);

                realAngle = (angle - zoomRegionStartAngle) * zoomProportion;

                window.windowWidth = window.zoomZone2StartWidth + (window.deltaWidth2 * realAngle * halfRadAngleDen);
                window.windowHeight = window.zoomZone2StartHeight + (window.deltaHeight2 * realAngle * halfRadAngleDen);

                window.setWindowOpacity(255);
                window.windowTitleOpacity = (int)(255 * getSin(realAngle));
                window.windowTitleVisible = true;
            }
            else if(angle >= 270 && angle < zoomRegionEndAngle) {
                window.windowX = centerX + zoomProportion2 * (angle - 270);
                xx = window.windowX - centerX;
                window.windowY = window.zoomCenterY - (window.focus * xx * xx);

                realAngle = (angle - 270) * zoomProportion;

                window.windowWidth = window.largeWindowWidth - (window.deltaWidth2 * realAngle * halfRadAngleDen);
                window.windowHeight = window.largeWindowHeight - (window.deltaHeight2 * realAngle * halfRadAngleDen);

                window.setWindowOpacity(255);
                window.windowTitleOpacity = (int)(255 * getCos(realAngle));
                window.windowTitleVisible = true;
            }
            else {
                auxAngle = angle - zoomRegionEndAngle;
                window.windowX = (centerX + horizontalRadius) - ((zoomAngle - auxAngle) * zoomAngleDen) * smallWindowXZone;
                window.windowY = zoomPointY - (auxAngle * zoomAngleDen) * smallWindowYZone;

                window.windowWidth = window.zoomZone2StartWidth - (window.deltaWidth1 * (angle - zoomRegionEndAngle) * zoomAngleDen);
                window.windowHeight = window.zoomZone2StartWidth - (window.deltaHeight1 * (angle - zoomRegionEndAngle) * zoomAngleDen);

                window.setWindowOpacity(255);

                window.windowTitleOpacity = 0;
                window.windowTitleVisible = false;
            }

            window.setWindowSize(ref window.windowWidth, ref window.windowHeight);
        }

        // --------------------------------------------------------------------------
        // check if a window should be brought in front of the other ones
        // --------------------------------------------------------------------------
        private void checkBringWindowOnTop(Window window) {
            if(window.angle >= activeAngleStart && window.angle <= activeAngleEnd) {
                if(window.onTop == false) {
                    window.bringWindowOnTop();

                    windowsZOrder.Remove(window.number);
                    windowsZOrder.Insert(0, window.number);

                    activeWindow = window.number;
                }
            }
            else {
                window.onTop = false;
            }
        }

        // --------------------------------------------------------------------------
        // place all windows according the drawing mode (simple or complex)
        // --------------------------------------------------------------------------
        public void placeWindows(bool update) {
            Window window;

            if(mode == MODE_SIMPLE) {
                double angle = startAngle % 360.0;

                for(int i = 0; i < windowNumber; i++) {
                    window = windows[i];

                    placeWindow(i, angle % 360.0);
                    window.angle = angle;

                    if(update) {
                        window.updateWindow();
                    }

                    // check if we need to bring the window on top
                    checkBringWindowOnTop(window);

                    // increment angle
                    angle = (angle + angleStep) % 360.0;
                }
            }
            else {
                // compute selected window
                double zoomStart = startAngle * smallComplexModeOptimization;
                startWindow = windowNumber - (int)(zoomStart / reverseZoomAngle) - 1;
                if(startAngle == 200) startWindow++;

                double sw = windowNumber - (zoomStart / reverseZoomAngle) - 1;

                if(startWindow < 0) {
                    startWindow = windowNumber + startWindow % windowNumber;
                }

                // second window
                int pos = (startWindow + 1) % windowNumber;
                if(pos < 0) pos = windowNumber + pos;

                // place first two windows
                windows[startWindow].angle = zoomRegionStartAngle + (zoomStart % reverseZoomAngle);
                windows[pos].angle = windows[startWindow].angle + reverseZoomAngle;

                placeWindow(startWindow, windows[startWindow].angle);
                placeWindow(pos, windows[pos].angle);

                if(update) {
                    // update display
                    windows[startWindow].updateWindow();
                    windows[pos].updateWindow();
                }

                // check if we need to bring the windows on top
                checkBringWindowOnTop(windows[startWindow]);
                checkBringWindowOnTop(windows[pos]);

                // compute start angle for the rest of the windows
                double angle = 360 - zoomAngle + ((zoomStart % reverseZoomAngle) * normalStartProportion);

                for(int i = 0; i < windowNumber - 2; i++) {
                    pos = (pos + 1) % windowNumber;

                    windows[pos].angle = angle % 360.0;
                    placeWindow(pos, windows[pos].angle);

                    if(update) {
                        // update display
                        windows[pos].updateWindow();
                    }

                    // check if we need to bring the window on top
                    checkBringWindowOnTop(windows[pos]);

                    // compute angle for next window
                    angle += normalAngleStep;
                }
            }
        }

        // --------------------------------------------------------------------------
        public void displayWindows() {
            for(int i = 0; i < windowNumber; i++) {
                windows[i].updateWindow();
            }
        }

        // --------------------------------------------------------------------------
        public void zoomWindow(int n) {
            Window window = windows[n];

            if(mode == MODE_SIMPLE) {
                if(window.angle >= 90 && window.angle <= 270) {
                    deltaAngle = 270 - window.angle;
                }
                else {
                    if(window.angle >= 0 && window.angle < 90) {
                        deltaAngle = -(90 + window.angle);
                    }
                    else {
                        deltaAngle = -(window.angle - 270);
                    }
                }

                initialAngle = startAngle;
                time.duration = (int)Math.Max(100, (Math.Abs((int)deltaAngle) * options.flipDuration) / angleStep);
                time.duration = Math.Min(time.duration, options.flipDuration);
            }
            else {
                // COMPLEX mode
                double x;
                double y = (180 + 2 * zoomAngle) / (windowNumber - 2);

                x = 270 - reverseZoomAngle - window.angle;

                if(window.angle >= zoomRegionStartAngle && window.angle < 270) {
                    deltaAngle = (270 - window.angle) * (complexZoomAngle / reverseZoomAngle);
                }
                else if(window.angle < 90) {
                    deltaAngle = -((windowNumber * complexZoomAngle) - (complexZoomAngle + (x / y) * complexZoomAngle));
                }
                else if(window.angle >= 270 && window.angle <= zoomRegionEndAngle) {
                    deltaAngle = -(window.angle - 270) * (complexZoomAngle / reverseZoomAngle);
                }
                else if(window.angle > zoomRegionEndAngle) {
                    x = window.angle - 270 - reverseZoomAngle;

                    deltaAngle = -complexZoomAngle - (x / y) * complexZoomAngle;
                }
                else {
                    // between 90 and zoomRegionStartAngle
                    deltaAngle = complexZoomAngle + (x / y) * complexZoomAngle;
                }

                initialAngle = startAngle;
                time.duration = options.flipDuration;
                //time.duration = Math.Min(time.duration, options.flipDuration);
            }

            if(deltaAngle == 0) {
                // don't start animation if deltaAngle = 0
                return;
            }

            // start animation

            if(options.showWindowTitle && window.windowTitle != null) {
                window.windowTitle.Visibility = Visibility.Visible;
            }

            if(time.duration < 100) {
                onFlipFinished(this, null);
            }
            else {
                changeStatus(STATUS_FLIP);
                time.eventOnTimeEllapsed = true;
                time.onTimeEllapsed += onFlipFinished;

                time.startTimer();
            }
        }

        // --------------------------------------------------------------------------
        private void onFlipFinished(object sender, EventArgs e) {
            changeStatus(STATUS_IDLE);

            startAngle = initialAngle + deltaAngle;
            if(startAngle < 0) {
                startAngle = 360.0 + startAngle;
            }

            placeWindows(true);
            nextActiveWindow = activeWindow;

            if(options.showWindowTitle == true) {
                for(int i = 0; i < windows.Count; i++) {
                    if(windows[i].number == activeWindow) continue;

                    if(windows[i].windowTitle != null) {
                        windows[i].windowTitle.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        // --------------------------------------------------------------------------
        public void updateDisplay(object sender, EventArgs e) {
            if(status == STATUS_FLIP) {
                time.update();
                startAngle = initialAngle + (deltaAngle * time.deltaTimeToPercent());
                startAngle %= 360;

                if(startAngle < 0) {
                    startAngle = 360.0 + startAngle;
                }

                placeWindows(true);
            }
            else if(status != STATUS_IDLE) {
                Window window;

                // update filtered windows
                if(status == STATUS_WINDOW_FILTER) {
                    for(int i = 0; i < filteredWindows.Count; i++) {
                        window = filteredWindows[i];
                        window.applyAnimationStep();
                        window.updateWindow();
                    }
                }

                for(int i = 0; i < animatedWindows.Count; i++) {
                    window = windows[animatedWindows[i]];
                    window.applyAnimationStep();
                    window.updateWindow();

                    if(status == STATUS_WINDOW_SELECT && window.number == zoomedWindow) {
                        if(mainWindow != null) {
                            SolidColorBrush b = mainWindow.Background as SolidColorBrush;
                            b.Opacity = 1.0 - window.animation.time.deltaTimeToPercent();
                        }
                    }

                    if(status == STATUS_WINDOW_SELECT && window.number == zoomedWindow &&
                        window.animation.time.deltaTime >= window.animation.duration) {
                        onSelectFinished(window, null);
                    }
                }
            }
        }

        // --------------------------------------------------------------------------
        // --------------------------------------------------------------------------
        private void startOnTopAnimation(ref Window window) {
            window.animation.reset();
            window.animation.startWindowOpacity = window.windowOpacity;
            window.animation.setEndWindowOpacity(255);

            // hide window title of active window
            if(options.showWindowTitle && window.windowTitleOpacity > 0) {
                window.animation.startTitleOpacity = window.windowTitleOpacity;
                window.animation.setEndTitleOpacity(0);
            }

            window.animation.duration = options.bringOnFrontDuration;
            window.eventOnAnimationFinished = true;
            window.onAnimationFinished += onWindowAnimationFinished;
            animatedWindows.Add(window.number);
            window.animation.startAnimation();
            changeStatus(STATUS_WINDOW_ANIMATION_IN);

            // show window title
            if(options.showWindowTitle && status != STATUS_WINDOW_FILTER) {
                overlayWindow.setWTitleSize(window.number, window.windowWidth, window.windowHeight);
                overlayWindow.setWTitlePosition(window.number, window.windowX - window.windowWidth / 2, window.windowY - window.windowHeight / 2);
                overlayWindow.titles[window.number].title.Opacity = 0;
                overlayWindow.titles[window.number].title.Content = window.windowTitle.Text;

                // let WPF do this animation for us
                overlayWindow.startFadeInAnimation(window.number, options.bringOnFrontDuration);
            }
        }

        // --------------------------------------------------------------------------
        private void startToBackAnimation(ref Window window) {
            animatedWindows.Remove(window.number);

            window.animation.reset();
            window.animation.startWindowOpacity = window.windowOpacity;
            placeWindow(window.number, window.angle);
            window.animation.setEndWindowOpacity(window.windowOpacity);

            // show window title of active window
            if(options.showWindowTitle) {
                window.animation.startTitleOpacity = 0;
                window.animation.setEndTitleOpacity(window.windowTitleOpacity);
            }

            window.animation.duration = options.bringOnFrontDuration;
            window.eventOnAnimationFinished = true;
            window.onAnimationFinished += onWindowAnimationFinished;
            animatedWindows.Add(window.number);
            window.animation.startAnimation();
            changeStatus(STATUS_WINDOW_ANIMATION_OUT);

            if(options.showWindowTitle) {
                overlayWindow.setWTitleSize(window.number, window.windowWidth, window.windowHeight);
                overlayWindow.setWTitlePosition(window.number, window.windowX - window.windowWidth / 2, window.windowY - window.windowHeight / 2);
                overlayWindow.titles[window.number].title.Opacity = 0;
                overlayWindow.titles[window.number].title.Content = window.windowTitle.Text;

                // let WPF do this animation for us
                overlayWindow.startFadeOutAnimation(window.number, options.bringOnFrontDuration);
            }
        }

        // --------------------------------------------------------------------------
        public void onMouseMove(int x, int y) {
            Window window;
            int aux;

            // abort if flip animation is running
            if(status == STATUS_FLIP || status == STATUS_WINDOW_ENTRANCE ||
                status == STATUS_WINDOW_SELECT || status == STATUS_WINDOW_FILTER) {
                return;
            }

            // find window under mouse cursor
            for(int i = 0; i < windowNumber; i++) {
                aux = windowsZOrder[i];
                window = windows[aux];

                if(x >= (window.windowX - window.windowWidth / 2) && x <= (window.windowX + window.windowWidth / 2) &&
                   y >= (window.windowY - window.windowHeight / 2) && y <= (window.windowY + window.windowHeight / 2)) {
                    if(aux == lastWindowOnTop) return;

                    if(lastWindowOnTop != -1) {
                        Window t = windows[lastWindowOnTop];
                        startToBackAnimation(ref t);
                    }

                    // found it!
                    if(window.onTop == false || window.number == activeWindow) {
                        for(int j = 0; j < windowNumber; j++) {
                            if(j != aux) {
                                windows[j].onTop = false;
                            }
                        }

                        lastWindowOnTop = aux;
                        window.bringWindowOnTop();
                        window.setWindowSize(ref window.windowWidth, ref window.windowHeight);
                        window.updateWindow();
                        windowsZOrder.Remove(aux);
                        windowsZOrder.Insert(0, aux);

                        // fade to 100% opacity
                        startOnTopAnimation(ref window);
                    }

                    return;
                }
            }

            if(lastWindowOnTop != -1 && lastWindowOnTop < windowNumber) {
                Window t = windows[lastWindowOnTop];

                startToBackAnimation(ref t);
                t.onTop = false;

                lastWindowOnTop = -1;
            }
        }

        // --------------------------------------------------------------------------
        private void changeStatus(int newStatus) {
            // stop the actions from the previous status
            if((status == STATUS_WINDOW_ANIMATION_IN || status == STATUS_WINDOW_ANIMATION_OUT) && 
               (newStatus != STATUS_WINDOW_ANIMATION_IN && newStatus != STATUS_WINDOW_ANIMATION_OUT)) {
                for(int i = 0; i < animatedWindows.Count; i++) {
                    Window window = windows[animatedWindows[i]];

                    window.eventOnAnimationFinished = false;
                    window.onAnimationFinished -= onWindowAnimationFinished;
                }

                animatedWindows.Clear();


            }
            else if(status == STATUS_FLIP) {
                time.eventOnTimeEllapsed = false;
                time.onTimeEllapsed -= onFlipFinished;
            }

            if(options.showWindowTitle && newStatus == STATUS_FLIP) {
                for(int i = 0; i < windowNumber; i++) {
                    if(overlayWindow.titles[i].title.Opacity != 0) {
                        overlayWindow.startFadeOutAnimation(i, options.flipDuration / 3);
                    }
                }
            }
            else if(options.showWindowTitle && newStatus == STATUS_WINDOW_SELECT) {
                for(int i = 0; i < windowNumber; i++) {
                    if(overlayWindow.titles[i].title.Opacity != 0) {
                        overlayWindow.startFadeOutAnimation(i, options.windowSelectDuration / 3);
                    }
                }
            }

            status = newStatus;
        }

        // --------------------------------------------------------------------------
        // --------------------------------------------------------------------------
        private void onWindowAnimationFinished(object sender, EventArgs e) {
            Window window = sender as Window;
            animatedWindows.Remove(window.number);

            if(status == STATUS_WINDOW_ANIMATION_OUT) {
                if(options.showWindowTitle) {
                    overlayWindow.startFadeOutAnimation(window.number, 0);
                    overlayWindow.titles[window.number].title.Opacity = 0;
                }
            }
            else if(status == STATUS_WINDOW_ANIMATION_IN) {
                if(options.showWindowTitle) {
                    //overlayWindow.startFadeInAnimation(window.number, 0);
                    //overlayWindow.titles[window.number].title.Opacity = 1;
                }
            }

            if(animatedWindows.Count == 0) {
                changeStatus(STATUS_IDLE);
            }
        }

        // --------------------------------------------------------------------------
        public void onLeftButtonUp(int x, int y) {
            Window window;
            int aux = -1;

            if(status == STATUS_FLIP || status == STATUS_WINDOW_ENTRANCE) {
                return;
            }

            // find the window first
            for(int i = 0; i < windowNumber; i++) {
                if(!running) break;

                window = windows[windowsZOrder[i]];

                if(x >= (window.windowX - window.windowWidth / 2) && x <= (window.windowX + window.windowWidth / 2) &&
                   y >= (window.windowY - window.windowHeight / 2) && y <= (window.windowY + window.windowHeight / 2)) {
                    aux = windowsZOrder[i];
                    break;
                }
            }

            if(aux != -1) {
                selectWindow(aux);
            }
        }

        public void onRightButtonUp(int x, int y, bool filter) {
            int aux = -1;
            Window window;

            if(status == STATUS_FLIP || status == STATUS_WINDOW_ENTRANCE) {
                return;
            }

            // find the window first
            for(int i = 0; i < windowNumber; i++) {
                window = windows[windowsZOrder[i]];

                if(x >= (window.windowX - window.windowWidth / 2) && x <= (window.windowX + window.windowWidth / 2) &&
                    y >= (window.windowY - window.windowHeight / 2) && y <= (window.windowY + window.windowHeight / 2)) {
                    aux = windowsZOrder[i];
                    break;
                }
            }

            if(aux != -1) {
                if(filter) {
                    startFilterWindows(aux);
                }
                else {
                    zoomWindow(aux);
                }
            }
        }

        // --------------------------------------------------------------------------
        public void onTabKeyDown(bool shift) {
            int aux;

            if(windowNumber == 0) {
                return;
            }

            // rotate clockwise
            if(shift) {
                aux = nextActiveWindow - 1;
                if(aux < 0) {
                    aux = windowNumber + aux;
                }

                nextActiveWindow = aux;
                zoomWindow(aux);
            }
            else {
                aux = (nextActiveWindow + 1) % windowNumber;
                nextActiveWindow = aux;
                zoomWindow(aux);
            }
        }

        // --------------------------------------------------------------------------
        public void selectWindow(int n) {
            Window window;

            if(status == STATUS_WINDOW_SELECT || status == STATUS_WINDOW_FILTER || !running) {
                return;
            }

            if(options.windowSelectDuration < 100) {
                onSelectFinished(windows[n], null);
                return;
            }

            for(int i = 0; i < windowNumber; i++) {
                if(i == n) continue;

                window = windows[i];
                window.animation.reset();

                if(options.animateAllWindowsOnSelection) {
                    window.animation.startX = window.windowX;
                    window.animation.startY = window.windowY;
                    window.animation.setEndPosition(window.sourceX + window.sourceWidth / 2, window.sourceY + window.sourceHeight / 2);

                    window.animation.startWidth = window.windowWidth;
                    window.animation.startHeight = window.windowHeight;
                    window.animation.setEndSize(window.sourceWidth, window.sourceHeight);
                }

                window.animation.startWindowOpacity = window.windowOpacity;
                window.animation.setEndWindowOpacity(0);

                if(options.showWindowTitle && window.windowTitleOpacity > 0) {
                    window.animation.startTitleOpacity = window.windowTitleOpacity;
                    window.animation.setEndTitleOpacity(0);
                }

                window.eventOnAnimationFinished = false;
                window.animation.duration = Math.Max(100, options.windowSelectDuration / 3);
                animatedWindows.Add(i);
            }

            window = windows[n];
            window.animation.reset();
            window.animation.startX = window.windowX;
            window.animation.startY = window.windowY;
            window.animation.setEndPosition(window.sourceX + window.sourceWidth / 2, window.sourceY + window.sourceHeight / 2);

            window.animation.startWidth = window.windowWidth;
            window.animation.startHeight = window.windowHeight;
            window.animation.setEndSize(window.sourceWidth, window.sourceHeight);

            if(options.showWindowTitle && window.windowTitleOpacity > 0) {
                window.animation.startTitleOpacity = window.windowTitleOpacity;
                window.animation.setEndTitleOpacity(0);
            }

            window.eventOnAnimationFinished = false;
            window.animation.duration = options.windowSelectDuration;

            // bring window on top only if needed (prevents flickering)
            if(n != windowsZOrder[0]) {
                window.bringWindowOnTop();
            }

            zoomedWindow = n;
            changeStatus(STATUS_WINDOW_SELECT);
            animatedWindows.Add(n);

            foreach(Window aux in windows) {
                aux.animation.startAnimation();
            }
        }

        // --------------------------------------------------------------------------
        public void onEnterKeyUp() {
            if(status == STATUS_FLIP) {
                return;
            }

            selectWindow(activeWindow);
        }

        // --------------------------------------------------------------------------
        private void onSelectFinished(object sender, EventArgs e) {
            Window window = sender as Window;
            IntPtr hwnd = window.sourceHwnd;
            bool minimized = window.minimized;
            bool restoreAsMaximized = window.restoreAsMaximized;

            filterKey = 0;
            running = false;

            // bring window on top
            SetForegroundWindow(hwnd);
            BringWindowToTop(hwnd);

            if(minimized) {
                // window is minimized, restore it
                WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                placement.length = Marshal.SizeOf(placement);
                GetWindowPlacement(hwnd, ref placement);

                if(restoreAsMaximized) {
                    // window should be maximized
                    placement.showCmd = 3;
                }
                else {
                    // just restore
                    placement.showCmd = 10;
                }

                SetWindowPlacement(hwnd, ref placement);
            }

            windowClose(this, null);
            SetFocus(hwnd);
        }

        // --------------------------------------------------------------------------
        public void animateWindowsEntrance() {
            Window window;

            if(options.animateWindowEntrance) {
                for(int i = 0; i < windowNumber; i++) {
                    window = windows[i];

                    window.animation.reset();
                    window.animation.startWidth = window.windowWidth / 3;
                    window.animation.startHeight = window.windowHeight / 3;
                    window.animation.setEndSize(window.windowWidth, window.windowHeight);

                    window.animation.startWindowOpacity = 0;
                    window.animation.setEndWindowOpacity(window.windowOpacity);

                    if(options.showWindowTitle && window.windowTitleOpacity > 0) {
                        window.animation.startTitleOpacity = 0;
                        window.animation.setEndTitleOpacity(window.windowTitleOpacity);
                    }

                    window.eventOnAnimationFinished = true;
                    window.onAnimationFinished += onWindowAnimationFinished;

                    window.animation.duration = options.windowEntranceDuration;

                    animatedWindows.Add(i);
                    window.animation.startAnimation();
                }

                if(mainWindow != null) {
                    DoubleAnimation da = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(options.windowEntranceDuration));
                    da.FillBehavior = FillBehavior.Stop;
                    ((SolidColorBrush)mainWindow.Background).BeginAnimation(SolidColorBrush.OpacityProperty, da);
                }

                changeStatus(STATUS_WINDOW_ENTRANCE);
            }
        }

        // --------------------------------------------------------------------------
        public void registerWindowTitles() {
            foreach(Window window in windows) {
                window.windowTitleVisible = true;
                window.windowTitleOpacity = 0;

                window.initWindowTitleInfo(options.titleGlowSize, options.titleGlowColor, options.windowTitleFontSize);
                window.getWindowTitle();

                grid.Children.Add(window.windowTitle);
            }
        }

        // --------------------------------------------------------------------------
        public void unregisterWindowTitles() {
            foreach(Window window in windows) {
                grid.Children.Remove(window.windowTitle);
            }
        }

        public void destroyWindows() {
            // unregister first the windows
            foreach(Window window in windows) {
                window.unregisterWindow();
            }

            windows.Clear();
            windowsZOrder.Clear();
            animatedWindows.Clear();

            status = STATUS_IDLE;
            lastWindowOnTop = -1;
        }

        // --------------------------------------------------------------------------
        // --------------------------------------------------------------------------
        public void onMoveKeysDown(bool reverse) {
            if(reverse) {
                startAngle--;
            }
            else {
                startAngle++;
            }

            if(startAngle < 0) {
                startAngle = 360 + startAngle;
            }
            
            startAngle %= 360;
            nextActiveWindow = activeWindow;
            placeWindows(true);
        }

        public void filterSetPosition(int n) {
            Window window = filteredWindows[n];

            window.animation.reset();
            window.animation.startX = window.windowX;
            window.animation.startY = window.windowY;

            // calculate final position
            if(window.angle > 315 && (window.angle - 315) < 45) {
                window.animation.setEndPosition(screenWidth, screenHeight / 2 - rand.Next(-(int)screenHeight / 2, (int)screenHeight / 2));
            }
            else if(window.angle >= 45 && window.angle < 135) {
                window.animation.setEndPosition(screenWidth / 2 + rand.Next(-(int)screenWidth / 2, (int)screenWidth / 2), 0);
            }
            else if(window.angle >= 135 && window.angle <= 225) {
                window.animation.setEndPosition(0, screenHeight / 2 - rand.Next(-(int)screenHeight / 2, (int)screenHeight / 2));
            }
            else {
                window.animation.setEndPosition(screenWidth / 2 + rand.Next(-(int)screenWidth / 2, (int)screenWidth / 2), screenHeight);
            }

            window.animation.startWidth = window.windowWidth;
            window.animation.startHeight = window.windowHeight;
            window.animation.setEndSize(0, 0);
            window.animation.startWindowOpacity = window.windowOpacity;
            window.animation.setEndWindowOpacity(0);

            if(options.showWindowTitle && window.windowTitleOpacity > 0) {
                window.animation.startTitleOpacity = window.windowTitleOpacity;
                window.animation.setEndTitleOpacity(0);
            }
        }

        private void onFilterWindowFinished(object sender, EventArgs e) {
            fwCount--;

            if(fwCount == 0) {
                // destroy the old windows
                foreach(Window window in filteredWindows) {
                    window.unregisterWindow();
                }

                filteredWindows.Clear();
                changeStatus(STATUS_IDLE);
                filterWindows = false;
            }
        }

        public void startFilterWindows(int n) {
            Window window;
            IntPtr originalHwnd;
            int ct;

            if(status == STATUS_WINDOW_FILTER) {
                return;
            }
            else if(alreadyFiltered) {
                // maybe the user pressed accidentally the A key
                // so zoom window instead
                zoomWindow(n);
                return;
            }

            // count how many windows are related with this one
            // if there are less than 2, abort filtering and
            // instead zoom the window
            ct = 0;

            for(int i = 0; i < windowNumber; i++) {
                if(windows[i].procId == windows[n].procId) {
                    ct++;
                }
            }

            if(ct < 2) {
                zoomWindow(n);
                return;
            }

            // prepare to filter windows
            filterWindows = true;
            fwCount = windowNumber;
            filterKey = windows[n].procId;
            originalHwnd = windows[n].sourceHwnd;

            // save current windows so we can do the explode animation
            filteredWindows = new List<Window>();

            for(int i = 0; i < windowNumber; i++) {
                filteredWindows.Add(windows[i]);
            }

            // destroy the old windows, they are no longer needed
            windows.Clear();
            windowsZOrder.Clear();
            animatedWindows.Clear();
            lastWindowOnTop = -1;

            // start the explode animation
            for(int i = 0; i < windowNumber; i++) {
                window = filteredWindows[i];

                // compute where the window should move
                filterSetPosition(i);
                window.eventOnAnimationFinished = true;
                window.onAnimationFinished += onFilterWindowFinished;
                window.animation.duration = options.windowFilterDuration;
                window.animation.startAnimation();
            }

            // delete titles from overlay window
            if(options.showWindowTitle) {
                try {
                    for(int i = 0; i < windowNumber; i++) {
                        overlayWindow.UnregisterName(overlayWindow.titles[i].title.Name);
                    }
                }
                catch(Exception ex) {
                    // nothing can be really done
                }

                overlayWindow.LayoutRoot.Children.RemoveRange(0, overlayWindow.LayoutRoot.Children.Count);
                overlayWindow.titles = null;
                unregisterWindowTitles();
            }

            // get the new windows (filtered)
            getWindows();

            // count at witch position the selected window is
            for(int i = 0; i < windowNumber; i++) {
                if(windows[i].sourceHwnd == originalHwnd) {
                    ct = i;
                    break;
                }
            }

            // initialize display
            activeWindow = nextActiveWindow = ct;
            initDisplay();

            // set start angle in order to bring the selected window
            // in the zoom region
            if(mode == MODE_SIMPLE) {
                startAngle = 270 - (ct * (360.0 / windowNumber));

                if(startAngle < 0) {
                    startAngle = 360 + startAngle % 360;
                }
            }
            else {
                startAngle = 360 - ct * complexZoomAngle;

                if(startAngle < 0) {
                    startAngle = 360 + startAngle % 360;
                }
            }

            // if we show the window titles, register them now
            if(options.showWindowTitle) {
                registerWindowTitles();
                overlayWindow.setTitleOptions(options.titleGlowColor, options.titleGlowSize, options.windowTitleFontSize);
                overlayWindow.createWTitles(windowNumber);
            }

            // compute the positions of the windows
            placeWindows(false);

            // start the fade animation
            for(int i = 0; i < windowNumber; i++) {
                window = windows[i];

                window.animation.reset();
                window.animation.startWindowOpacity = 0;
                window.animation.setEndWindowOpacity(window.windowOpacity);

                // animate the window title
                if(options.showWindowTitle && window.windowTitleOpacity > 0) {
                    window.animation.startTitleOpacity = 0;
                    window.animation.setEndTitleOpacity(255);
                }

                window.eventOnAnimationFinished = true;
                window.onAnimationFinished += onWindowAnimationFinished;
                window.animation.duration = options.windowFilterDuration;

                // needed
                window.setWindowVisibility(true);

                if(window.animation.duration > 100) {
                    window.setWindowOpacity(1);
                }
                else {
                    window.setWindowOpacity(255);
                }

                window.updateWindow();
                animatedWindows.Add(i);
                window.animation.startAnimation();
            }

            // change status
            changeStatus(STATUS_WINDOW_FILTER);
            alreadyFiltered = true;
        }
    }
}
