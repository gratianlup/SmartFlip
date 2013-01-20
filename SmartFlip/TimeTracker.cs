namespace SmartFlip {
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;


    public class TimeTracker {
        int startTime;
        public int duration;
        public int deltaTime;

        #region Event

        public bool eventOnTimeEllapsed;
        public event EventHandler onTimeEllapsed;

        #endregion

        // --------------------------------------------------------------------------
        public void startTimer() {
            startTime = Environment.TickCount;
        }

        // --------------------------------------------------------------------------
        public int update() {
            deltaTime = Environment.TickCount - startTime;

            if(deltaTime >= duration && eventOnTimeEllapsed == true) {
                // fire the event !
                onTimeEllapsed(this, null);
            }

            return deltaTime;
        }

        // --------------------------------------------------------------------------
        public double deltaTimeToPercent() {
            return Math.Min(1, (double)deltaTime / (double)duration);
        }
    }
}
