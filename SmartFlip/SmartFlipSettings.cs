using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Windows.Media;

namespace SmartFlip {
    class SmartFlipSettings : ApplicationSettingsBase {
        [UserScopedSetting()]
        public bool ShowWindowTitle {
            get { return (bool)this["ShowWindowTitle"]; }
            set { this["ShowWindowTitle"] = value; }
        }

        [UserScopedSetting()]
        public int TitleGlowSize {
            get { return (int)this["TitleGlowSize"]; }
            set { this["TitleGlowSize"] = value; }
        }

        [UserScopedSetting()]
        public Color TitleGlowColor {
            get { return (Color)this["TitleGlowColor"]; }
            set { this["TitleGlowColor"] = value; }
        }

        [UserScopedSetting()]
        public int TitleFontSize {
            get { return (int)this["TitleFontSize"]; }
            set { this["TitleFontSize"] = value; }
        }

        [UserScopedSetting()]
        public int DefaultWindowOpacity {
            get { return (int)this["DefaultWindowOpacity"]; }
            set { this["DefaultWindowOpacity"] = value; }
        }

        [UserScopedSetting()]
        public bool DnimateWindowFlip {
            get { return (bool)this["AnimateWindowFlip"]; }
            set { this["AnimateWindowFlip"] = value; }
        }

        [UserScopedSetting()]
        public int WindowFlipDuration {
            get { return (int)this["WindowFlipDuration"]; }
            set { this["WindowFlipDuration"] = value; }
        }

        [UserScopedSetting()]
        public bool AnimateBringWindowOnTop {
            get { return (bool)this["AnimateBringWindowOnTop"]; }
            set { this["AnimateBringWindowOnTop"] = value; }
        }

        [UserScopedSetting()]
        public int BringWindowOnTopDuration {
            get { return (int)this["BringWindowOnTopDuration"]; }
            set { this["BringWindowOnTopDuration"] = value; }
        }

        [UserScopedSetting()]
        public bool AnimateWindowSelect {
            get { return (bool)this["AnimateWindowSelect"]; }
            set { this["AnimateWindowSelect"] = value; }
        }

        [UserScopedSetting()]
        public int WindowSelectDuration {
            get { return (int)this["WindowSelectDuration"]; }
            set { this["WindowSelectDuration"] = value; }
        }

        [UserScopedSetting()]
        public bool AnimateAllOnSelection {
            get { return (bool)this["AnimateAllOnSelection"]; }
            set { this["AnimateAllOnSelection"] = value; }
        }

        [UserScopedSetting()]
        public bool AnimateWindowEntrance {
            get { return (bool)this["AnimateWindowEntrance"]; }
            set { this["AnimateWindowEntrance"] = value; }
        }

        [UserScopedSetting()]
        public int WindowEntranceDuration {
            get { return (int)this["WindowEntranceDuration"]; }
            set { this["WindowEntranceDuration"] = value; }
        }

        [UserScopedSetting()]
        public bool ShowButtonPanel {
            get { return (bool)this["ShowButtonPanel"]; }
            set { this["ShowButtonPanel"] = value; }
        }
    }
}
