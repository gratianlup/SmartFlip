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
