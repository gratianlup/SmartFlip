// Copyright (c) 2007 Gratian Lup. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
// * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following
// disclaimer in the documentation and/or other materials provided
// with the distribution.
//
// * The name "SmartFlip" must not be used to endorse or promote
// products derived from this software without prior written permission.
//
// * Products derived from this software may not be called "SmartFlip" nor
// may "SmartFlip" appear in their names without prior written
// permission of the author.
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Input;
using SmartFlip.Properties;
using Microsoft.Win32;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;

namespace SmartFlip {
    public struct EnumEnumerator<T> : IEnumerable<T> where T : struct {
        public EnumEnumerator(T value) {
            _value = value;
        }

        private T _value;

        public T Value {
            get {
                return _value;
            }
            set {
                _value = value;
            }
        }

        public static implicit operator T(EnumEnumerator<T> e) {
            return e._value;
        }
        public static implicit operator EnumEnumerator<T>(T e) {
            return new EnumEnumerator<T>(e);
        }

        public static EnumEnumerator<T> FromEnumerable(IEnumerable<T> e) {
            int value = 0;
            foreach(T t in e) {
                value |= Convert.ToInt32(t);
            }
            return (T)Enum.ToObject(typeof(T), value);
        }

        public IEnumerable<T> AllPossibleValues {
            get {
                if(!typeof(T).IsSubclassOf(typeof(Enum)))
                    throw new Exception("Must be an Enum");

                int[] allValues = (int[])Enum.GetValues(typeof(T));

                int intValues = Convert.ToInt32(_value);
                foreach(int i in allValues) {
                    yield return (T)Enum.ToObject(typeof(T), i);
                }
            }
        }

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            if(!typeof(T).IsSubclassOf(typeof(Enum)))
                throw new Exception("Must be an Enum");

            int[] allValues = (int[])Enum.GetValues(typeof(T));

            int intValues = Convert.ToInt32(_value);
            foreach(int i in allValues) {
                if((i & intValues) != 0)
                    yield return (T)Enum.ToObject(typeof(T), i);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return (this as IEnumerable<T>).GetEnumerator() as IEnumerator;
        }

        #endregion

        public override string ToString() {
            return _value.ToString();
        }
    }

    public partial class Window3 {
        bool moveWindow;
        double initialX, initialY;
        double initialLeft, initialTop;

        double screenWidth;
        double screenHeight;
        double widthProportion;
        double heightProportion;

        bool moveMouseRegion;

        public int selectedPanel;

        public Window3() {
            Debug.WriteLine("At moment   " + DateTime.Now.ToLongTimeString() + "  " + DateTime.Now.Millisecond.ToString());
            this.InitializeComponent();
            Debug.WriteLine("At moment   " + DateTime.Now.ToLongTimeString() + "  " + DateTime.Now.Millisecond.ToString());
            // Insert code required on object creation below this point.
        }

        private void OnGlowSizeChanged(object sender, RoutedEventArgs e) {
            if(GlowSizeTextbox == null) return;

            GlowSizeTextbox.Text = ((int)(GlowSizeSlider.Value)).ToString();
        }

        private void syncTextBoxWithSlider(TextBox t, Slider s, int min, int max) {
            if(t == null || s == null) return;

            if(t.Text.Length > 0) {
                int value = Convert.ToInt32(t.Text);

                // check if value is between correct margins
                if(value < min || value > max) {
                    if(value > max) {
                        value = max;
                    }
                    else {
                        value = min;
                    }

                    value = min;
                    t.Text = min.ToString();
                }

                // set the slider to the new value
                s.Value = value;
            }
        }

        private void GlowSizeChangedTb(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(GlowSizeTextbox, GlowSizeSlider, 0, 48);
        }

        private void GlowSizeKeyUp(object sender, System.Windows.Input.KeyEventArgs e) {

        }

        private void OnScreenAmountTextChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(ScreenAmountTextbox, ScreenAmountSlider, 1, 100);
        }

        private void OnScreenAmountChanged(object sender, RoutedEventArgs e) {
            if(ScreenAmountTextbox == null) return;

            ScreenAmountTextbox.Text = ((int)(ScreenAmountSlider.Value)).ToString();
        }

        private void OnWindowLoad(object sender, RoutedEventArgs e) {
            loadSettings();

            CategoryList.SelectedIndex = selectedPanel;

            // add mouse event handlers
            Mouse.AddMouseDownHandler(this, MouseDownHandler);
            Mouse.AddMouseMoveHandler(this, MouseMoveHandler);
            Mouse.AddMouseUpHandler(this, MouseUpHandler);
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e) {
            if(moveWindow) {
                Point p = e.GetPosition(this);

                this.Left = initialLeft + (this.Left + p.X - initialX);
                this.Top = initialTop + (this.Top + p.Y - initialY);
                //	Mouse.
            }
        }

        private void MouseDownHandler(object sender, MouseEventArgs e) {
            Point p = e.GetPosition(this);

            if(p.Y <= OptionsHost.Margin.Top) {
                moveWindow = true;
                initialLeft = this.Left;
                initialTop = this.Top;
                initialX = initialLeft + p.X;
                initialY = initialTop + p.Y;
            }
        }

        private void MouseUpHandler(object sender, MouseEventArgs e) {
            if(moveWindow) {
                moveWindow = false;
            }
        }

        private void WindowTitleClick(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                FontSizeTextbox.Foreground = (Brush)this.Resources["FontColor"];
                GlowSizeTextbox1.Foreground = (Brush)this.Resources["FontColor"];
                GlowColorTextbox.Foreground = (Brush)this.Resources["FontColor"];
                GlowSizeSlider.IsEnabled = true;
                FontSizeCombobox.IsEnabled = true;
                FontSizeCombobox.Foreground = (Brush)this.Resources["FontColor"];
                GlowSizeTextbox.IsEnabled = true;
                GlowSizeTextbox.Foreground = (Brush)this.Resources["FontColor"];
                GlowColorChanger.IsEnabled = true;
            }
            else {
                FontSizeTextbox.Foreground = (Brush)this.Resources["ControlBackground"];
                GlowSizeTextbox1.Foreground = (Brush)this.Resources["ControlBackground"];
                GlowColorTextbox.Foreground = (Brush)this.Resources["ControlBackground"];
                GlowSizeSlider.IsEnabled = false;
                FontSizeCombobox.IsEnabled = false;
                FontSizeCombobox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                GlowSizeTextbox.IsEnabled = false;
                GlowSizeTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                GlowColorChanger.IsEnabled = false;
            }
        }

        private void ButtonPanelClick(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                ReflectionCheckbox.IsEnabled = true;
                ReflectionCheckbox.Foreground = (Brush)this.Resources["FontColor"];
            }
            else {
                ReflectionCheckbox.IsEnabled = false;
                ReflectionCheckbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }

        private void hideAllPanels() {
            GeneralPanel.Visibility = Visibility.Hidden;
            EventsPanel.Visibility = Visibility.Hidden;
            TriggersPanel.Visibility = Visibility.Hidden;
            AboutPanel.Visibility = Visibility.Hidden;
        }

        private void CategoryChanged(object sender, SelectionChangedEventArgs e) {
            hideAllPanels();

            if((String)(((ListBoxItem)e.AddedItems[0]).Content) == "General") {
                GeneralPanel.Visibility = Visibility.Visible;
            }
            else if((String)(((ListBoxItem)e.AddedItems[0]).Content) == "Events") {
                EventsPanel.Visibility = Visibility.Visible;
            }
            else if((String)(((ListBoxItem)e.AddedItems[0]).Content) == "Triggers") {
                TriggersPanel.Visibility = Visibility.Visible;
            }
            else {
                // about
                AboutPanel.Visibility = Visibility.Visible;
            }
        }

        private void CancelClicked(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void WindowOpacityChanged_Slider(object sender, RoutedEventArgs e) {
            if(WindowOpacityTextbox == null) return;

            WindowOpacityTextbox.Text = ((int)(WindowOpacitySlider.Value)).ToString();
        }

        private void WindowOpacityChanged_Textbox(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(WindowOpacityTextbox, WindowOpacitySlider, 0, 255);
        }

        private void WindowPlacementClick(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                Duration1.Foreground = (Brush)this.Resources["FontColor"];
                WindowPlacementSlider.IsEnabled = true;
                WindowPlacementTextbox.IsEnabled = true;
                WindowPlacementTextbox.Foreground = (Brush)this.Resources["FontColor"];
            }
            else {
                Duration1.Foreground = (Brush)this.Resources["DisabledFontColor"];
                WindowPlacementSlider.IsEnabled = false;
                WindowPlacementTextbox.IsEnabled = false;
                WindowPlacementTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }

        private void WindowFlipClick(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                Duration2.Foreground = (Brush)this.Resources["FontColor"];
                WindowFlipSlider.IsEnabled = true;
                WindowFlipTextbox.IsEnabled = true;
                WindowFlipTextbox.Foreground = (Brush)this.Resources["FontColor"];
            }
            else {
                Duration2.Foreground = (Brush)this.Resources["DisabledFontColor"];
                WindowFlipSlider.IsEnabled = false;
                WindowFlipTextbox.IsEnabled = false;
                WindowFlipTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }

        private void WindowOnTopClick(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                Duration3.Foreground = (Brush)this.Resources["FontColor"];
                WindowOnTopSlider.IsEnabled = true;
                WindowOnTopTextbox.IsEnabled = true;
                WindowOnTopTextbox.Foreground = (Brush)this.Resources["FontColor"];
            }
            else {
                Duration3.Foreground = (Brush)this.Resources["DisabledFontColor"];
                WindowOnTopSlider.IsEnabled = false;
                WindowOnTopTextbox.IsEnabled = false;
                WindowOnTopTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }

        private void WindowSelectionClick(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                AllWindowsCheckbox.IsEnabled = true;
                AllWindowsCheckbox.Foreground = (Brush)this.Resources["FontColor"];
                Duration4.Foreground = (Brush)this.Resources["FontColor"];
                WindowSelectionSlider.IsEnabled = true;
                WindowSelectionTextbox.IsEnabled = true;
                WindowSelectionTextbox.Foreground = (Brush)this.Resources["FontColor"];
            }
            else {
                AllWindowsCheckbox.IsEnabled = false;
                AllWindowsCheckbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                Duration4.Foreground = (Brush)this.Resources["DisabledFontColor"];
                WindowSelectionSlider.IsEnabled = false;
                WindowSelectionTextbox.IsEnabled = false;
                WindowSelectionTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }

        private void AllWindowsClick(object sender, RoutedEventArgs e) {

        }

        private void WindowPlacementTextboxChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(WindowPlacementTextbox, WindowPlacementSlider, 0, 5000);
        }

        private void WindowFlipTextboxChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(WindowFlipTextbox, WindowFlipSlider, 0, 5000);
        }

        private void WindowOnTopTextboxChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(WindowOnTopTextbox, WindowOnTopSlider, 0, 5000);
        }

        private void WindowSelectionTextboxChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(WindowSelectionTextbox, WindowSelectionSlider, 0, 5000);
        }

        private void WindowPlacementSliderChanged(object sender, RoutedEventArgs e) {
            if(WindowPlacementTextbox == null) return;

            WindowPlacementTextbox.Text = ((int)(WindowPlacementSlider.Value)).ToString();
        }

        private void WindowFlipSliderChanged(object sender, RoutedEventArgs e) {
            if(WindowFlipTextbox == null) return;

            WindowFlipTextbox.Text = ((int)(WindowFlipSlider.Value)).ToString();
        }

        private void WindowOnTopSliderChanged(object sender, RoutedEventArgs e) {
            if(WindowOnTopTextbox == null) return;

            WindowOnTopTextbox.Text = ((int)(WindowOnTopSlider.Value)).ToString();
        }

        private void WindowSelectionSliderChanged(object sender, RoutedEventArgs e) {
            if(WindowSelectionTextbox == null) return;

            WindowSelectionTextbox.Text = ((int)(WindowSelectionSlider.Value)).ToString();
        }

        private void loadSettings() {
            // load settings from file
            SmartFlip.Properties.Settings.Default.Reload();

            updateControlsFromSettings(SmartFlip.Properties.Settings.Default);

            // fill KeyCombobox
            KeyCombobox.Items.Clear();
            KeyCombobox.ItemsSource = TriggerKey.AllPossibleValues;
            KeyCombobox2.Items.Clear();
            KeyCombobox2.ItemsSource = TriggerKey.AllPossibleValues;
        }

        private void updateControlsFromSettings(Settings st) {
            #region General panel

            if(st.BackgroundOpacity > 0 && st.BackgroundOpacity <= 255) {
                BackgroundOpacitySlider.Value = st.BackgroundOpacity;
            }

            WindowTitleCheckbox.IsChecked = st.ShowWindowTitle;
            WindowTitleClick(WindowTitleCheckbox, null);

            // ---
            if(st.TitleFontSize >= 8 && st.TitleFontSize <= 48) {
                FontSizeCombobox.Text = st.TitleFontSize.ToString();
            }

            // ---
            if(st.TitleGlowSize >= 1 && st.TitleGlowSize <= 48) {
                GlowSizeSlider.Value = st.TitleGlowSize;
            }

            // ---
            GlowColorChanger.Background = new SolidColorBrush(st.TitleGlowColor);

            // ---
            ReflectionCheckbox.IsChecked = st.ShowButtonPanelReflection;
            ButtonPanelCheckbox.IsChecked = st.ShowButtonPanel;
            ButtonPanelClick(ButtonPanelCheckbox, null);

            // ---
            if(st.DefaultWindowOpacity >= 0 && st.DefaultWindowOpacity <= 255) {
                WindowOpacitySlider.Value = st.DefaultWindowOpacity;
            }

            // ---
            if(st.ScreenAmountUsed >= 1 && st.ScreenAmountUsed <= 100) {
                ScreenAmountSlider.Value = st.ScreenAmountUsed;
            }

            // ---
            RunOnStartupCheckbox.IsChecked = st.RunOnStartup;

            #endregion

            #region Events panel

            // ---
            WindowPlacementCheckbox.IsChecked = st.AnimateWindowEntrance;
            if(st.WindowEntranceDuration >= 0 && st.WindowEntranceDuration <= 5000) {
                WindowPlacementSlider.Value = st.WindowEntranceDuration;
            }
            WindowPlacementClick(WindowPlacementCheckbox, null);

            // ---
            WindowFlipCheckbox.IsChecked = st.AnimateWindowFlip;
            if(st.WindowFlipDuration >= 0 && st.WindowFlipDuration <= 5000) {
                WindowFlipSlider.Value = st.WindowFlipDuration;
            }
            WindowFlipClick(WindowFlipCheckbox, null);

            // ---
            WindowOnTopCheckbox.IsChecked = st.AnimateBringWindowOnTop;
            if(st.BringWindowOnTopDuration >= 0 && st.BringWindowOnTopDuration <= 5000) {
                WindowOnTopSlider.Value = st.BringWindowOnTopDuration;
            }
            WindowOnTopClick(WindowOnTopCheckbox, null);

            // ---
            WindowSelectionCheckbox.IsChecked = st.AnimateWindowSelect;
            if(st.WindowSelectDuration >= 0 && st.WindowSelectDuration <= 5000) {
                WindowSelectionSlider.Value = st.WindowSelectDuration;
            }
            AllWindowsCheckbox.IsChecked = st.AnimateAllOnSelection;
            WindowSelectionClick(WindowSelectionCheckbox, null);

            // ---
            FilterCheckbox.IsChecked = st.AnimateFilter;
            if(st.FilterAnimationDuration >= 0 && st.FilterAnimationDuration <= 5000) {
                FilterSlider.Value = st.FilterAnimationDuration;
            }
            FilterCheckboxClick(FilterCheckbox, null);

            #endregion

            #region Triggers Panel

            #region Keyboard trigger

            WinTabOptionbox.IsChecked = st.UseWinTab;
            OtherKeyOptionbox.IsChecked = !st.UseWinTab;

            SelectZoomedCheckbox.IsChecked = st.SelectOnWinReleased;

            ShiftCheckbox.IsChecked = st.UseShift;
            AltCheckbox.IsChecked = st.UseAlt;
            CtrlCheckbox.IsChecked = st.UseCtrl;

            WinTabClicked(WinTabOptionbox, null);
            OtherKeyClicked(OtherKeyOptionbox, null);

            KeyCombobox.SelectedItem = st.Key;

            WinTabClicked(WinTabOptionbox, null);

            ShiftCheckbox2.IsChecked = st.UseShift2;
            AltCheckbox2.IsChecked = st.UseAlt2;
            CtrlCheckbox2.IsChecked = st.UseCtrl2;
            KeyCombobox2.SelectedItem = st.Key2;

            #endregion

            #region Mouse trigger

            // initialize mouse trigger information
            if(screenWidth == 0 || screenHeight == 0) {
                // get primary display resolution
                screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Right;
                screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Bottom;

                heightProportion = 180 / screenHeight;
                VirtualMonitor.Width = screenWidth * heightProportion;
                widthProportion = VirtualMonitor.Width / screenWidth;

                LeftSlider.Maximum = screenWidth;
                TopSlider.Maximum = screenHeight;
                WidthSlider.Maximum = screenWidth;
                HeightSlider.Maximum = screenWidth;
            }

            MouseTriggerCheckbox.IsChecked = st.MouseTrigger;
            MouseTriggerClicked(MouseTriggerCheckbox, null);

            LeftSlider.Value = st.RegionLeft;
            TopSlider.Value = st.RegionTop;
            WidthSlider.Value = st.RegionWidth;
            HeightSlider.Value = st.RegionHeight;

            #endregion

            #endregion
        }

        private void saveSettings() {
            Settings st = SmartFlip.Properties.Settings.Default;

            #region General panel

            st.BackgroundOpacity = (int)BackgroundOpacitySlider.Value;

            st.ShowWindowTitle = (bool)WindowTitleCheckbox.IsChecked;

            if(FontSizeCombobox.Text.Length > 0) {
                st.TitleFontSize = Convert.ToDouble(FontSizeCombobox.Text);
            }

            st.TitleGlowSize = (int)GlowSizeSlider.Value;

            st.TitleGlowColor = ((SolidColorBrush)GlowColorChanger.Background).Color;

            st.ShowButtonPanel = (bool)ButtonPanelCheckbox.IsChecked;

            st.ShowButtonPanelReflection = (bool)ReflectionCheckbox.IsChecked;

            st.ScreenAmountUsed = (int)ScreenAmountSlider.Value;

            st.DefaultWindowOpacity = (int)WindowOpacitySlider.Value;

            // check if key is already created
            RegistryKey reg = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if(reg != null) {
                try {
                    if(reg.GetValue("SmartFlip") == null && RunOnStartupCheckbox.IsChecked == true) {
                        // create registry key in HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
                        reg.SetValue("SmartFlip", System.Windows.Forms.Application.ExecutablePath);
                    }
                    else if(RunOnStartupCheckbox.IsChecked == false) {
                        // delete key
                        reg.DeleteValue("SmartFlip");
                    }
                }
                catch(Exception e) {
                }
            }

            // save value in file
            st.RunOnStartup = (bool)RunOnStartupCheckbox.IsChecked;

            #endregion

            #region Events panel

            st.AnimateWindowEntrance = (bool)WindowPlacementCheckbox.IsChecked;
            st.WindowEntranceDuration = (int)WindowPlacementSlider.Value;

            st.AnimateWindowFlip = (bool)WindowFlipCheckbox.IsChecked;
            st.WindowFlipDuration = (int)WindowFlipSlider.Value;

            st.AnimateBringWindowOnTop = (bool)WindowOnTopCheckbox.IsChecked;
            st.BringWindowOnTopDuration = (int)WindowOnTopSlider.Value;

            st.AnimateWindowSelect = (bool)WindowSelectionCheckbox.IsChecked;
            st.WindowSelectDuration = (int)WindowSelectionSlider.Value;
            st.AnimateAllOnSelection = (bool)AllWindowsCheckbox.IsChecked;

            st.AnimateFilter = (bool)FilterCheckbox.IsChecked;
            st.FilterAnimationDuration = (int)FilterSlider.Value;

            #endregion

            #region Triggers panel

            #region Keyboard trigger

            st.UseWinTab = (bool)WinTabOptionbox.IsChecked;
            st.SelectOnWinReleased = (bool)SelectZoomedCheckbox.IsChecked;
            st.UseShift = (bool)ShiftCheckbox.IsChecked;
            st.UseAlt = (bool)AltCheckbox.IsChecked;
            st.UseCtrl = (bool)CtrlCheckbox.IsChecked;
            st.Key = (System.Windows.Forms.Keys)KeyCombobox.SelectedItem;

            st.Key2 = (System.Windows.Forms.Keys)KeyCombobox2.SelectedItem;
            st.UseShift2 = (bool)ShiftCheckbox2.IsChecked;
            st.UseAlt2 = (bool)AltCheckbox2.IsChecked;
            st.UseCtrl2 = (bool)CtrlCheckbox2.IsChecked;

            #endregion

            #region Mouse trigger

            st.MouseTrigger = (bool)MouseTriggerCheckbox.IsChecked;
            st.RegionLeft = (int)LeftSlider.Value;
            st.RegionTop = (int)TopSlider.Value;
            st.RegionWidth = (int)WidthSlider.Value;
            st.RegionHeight = (int)HeightSlider.Value;

            #endregion

            #endregion

            // now save
            st.Save();
        }

        private void SaveClick(object sender, RoutedEventArgs e) {
            saveSettings();

            this.Close();
        }

        private void ColorChangerClicked(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();

            Color c = ((SolidColorBrush)GlowColorChanger.Background).Color;
            cd.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

            if(cd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                GlowColorChanger.Background = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));
            }
        }

        private void ResetClicked(object sender, RoutedEventArgs e) {
            Settings st = new Settings();

            st.Reset();
            updateControlsFromSettings(st);
        }

        private void WinTabClicked(object sender, RoutedEventArgs e) {
            if(((RadioButton)(sender)).IsChecked == true) {
                SelectZoomedCheckbox.IsEnabled = true;
                SelectZoomedCheckbox.Foreground = (Brush)this.Resources["FontColor"];
                OtherKeyClicked(OtherKeyOptionbox, null);
            }
            else {
                SelectZoomedCheckbox.IsEnabled = false;
                SelectZoomedCheckbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }

        private void OtherKeyClicked(object sender, RoutedEventArgs e) {
            if(((RadioButton)(sender)).IsChecked == true) {
                KeyTextblock.Foreground = (Brush)this.Resources["FontColor"];
                ShiftCheckbox.Foreground = (Brush)this.Resources["FontColor"];
                AltCheckbox.Foreground = (Brush)this.Resources["FontColor"];
                CtrlCheckbox.Foreground = (Brush)this.Resources["FontColor"];
                KeyCombobox.Foreground = (Brush)this.Resources["FontColor"];
                ShiftCheckbox.IsEnabled = true;
                AltCheckbox.IsEnabled = true;
                CtrlCheckbox.IsEnabled = true;
                KeyCombobox.IsEnabled = true;

                WinTabClicked(WinTabOptionbox, null);
            }
            else {
                KeyTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
                ShiftCheckbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                AltCheckbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                CtrlCheckbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                KeyCombobox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                ShiftCheckbox.IsEnabled = false;
                AltCheckbox.IsEnabled = false;
                CtrlCheckbox.IsEnabled = false;
                KeyCombobox.IsEnabled = false;
            }
        }

        private void MouseTriggerTabUp(object sender, MouseButtonEventArgs e) {
        }

        private void LeftSliderChanged(object sender, RoutedEventArgs e) {
            if(LeftTextbox == null) return;
            LeftTextbox.Text = ((int)(LeftSlider.Value)).ToString();

            if(widthProportion <= 0) return;

            if(LeftSlider.Value * widthProportion + MouseRegion.Width > screenWidth * widthProportion) {
                LeftSlider.Value = screenWidth - WidthSlider.Value;
                Canvas.SetLeft(MouseRegion, screenWidth * widthProportion - MouseRegion.Width - 3);
            }
            else {
                Canvas.SetLeft(MouseRegion, LeftSlider.Value * widthProportion - 1);
            }
        }

        private void TopSliderChanged(object sender, RoutedEventArgs e) {
            if(TopTextbox == null) return;
            TopTextbox.Text = ((int)(TopSlider.Value)).ToString();

            if(heightProportion <= 0) return;

            if(TopSlider.Value * heightProportion + MouseRegion.Height > 180) {
                TopSlider.Value = screenHeight - HeightSlider.Value;
                Canvas.SetTop(MouseRegion, screenHeight * heightProportion - MouseRegion.Height - 3);
            }
            else {
                Canvas.SetTop(MouseRegion, TopSlider.Value * heightProportion);
            }
        }

        private void WidthSliderChanged(object sender, RoutedEventArgs e) {
            if(WidthTextbox == null) return;
            WidthTextbox.Text = ((int)(WidthSlider.Value)).ToString();

            if(widthProportion <= 0) return;

            MouseRegion.Width = WidthSlider.Value * widthProportion;
        }

        private void HeightSliderChanged(object sender, RoutedEventArgs e) {
            if(HeightTextbox == null) return;
            HeightTextbox.Text = ((int)(HeightSlider.Value)).ToString();

            if(heightProportion <= 0) return;

            MouseRegion.Height = HeightSlider.Value * heightProportion;
        }

        private void LeftBottomClicked(object sender, MouseButtonEventArgs e) {
            WidthSlider.Value = 10;
            HeightSlider.Value = 10;
            LeftSlider.Value = 0;
            TopSlider.Value = screenHeight;

            WidthSliderChanged(WidthSlider, null);
            HeightSliderChanged(HeightSlider, null);
            LeftSliderChanged(LeftSlider, null);
            TopSliderChanged(TopSlider, null);
        }

        private void RightBottomClicked(object sender, MouseButtonEventArgs e) {
            WidthSlider.Value = 10;
            HeightSlider.Value = 10;
            LeftSlider.Value = screenWidth;
            TopSlider.Value = screenHeight;

            WidthSliderChanged(WidthSlider, null);
            HeightSliderChanged(HeightSlider, null);
            LeftSliderChanged(LeftSlider, null);
            TopSliderChanged(TopSlider, null);
        }

        private void RightUpClicked(object sender, MouseButtonEventArgs e) {
            WidthSlider.Value = 10;
            HeightSlider.Value = 10;
            LeftSlider.Value = screenWidth;
            TopSlider.Value = 0;

            WidthSliderChanged(WidthSlider, null);
            HeightSliderChanged(HeightSlider, null);
            LeftSliderChanged(LeftSlider, null);
            TopSliderChanged(TopSlider, null);
        }

        private void LeftUpClicked(object sender, MouseButtonEventArgs e) {
            WidthSlider.Value = 10;
            HeightSlider.Value = 10;
            LeftSlider.Value = 0;
            TopSlider.Value = 0;

            WidthSliderChanged(WidthSlider, null);
            HeightSliderChanged(HeightSlider, null);
            LeftSliderChanged(LeftSlider, null);
            TopSliderChanged(TopSlider, null);
        }

        private void MouseRegionKeyDown(object sender, KeyEventArgs e) {

        }

        private void MouseRegionKeyUp(object sender, KeyEventArgs e) {

        }

        private void MouseRegionMouseMove(object sender, MouseEventArgs e) {
            Point p = e.GetPosition(this);

            if(moveMouseRegion) {
                LeftSlider.Value = initialLeft + (p.X - initialX) / widthProportion;
                TopSlider.Value = initialTop + (p.Y - initialY) / heightProportion;
            }
        }

        private void MouseRegionMouseUp(object sender, MouseButtonEventArgs e) {
            moveMouseRegion = false;
        }

        private void MouseRegionDown(object sender, MouseButtonEventArgs e) {
            Point p = e.GetPosition(this);

            moveMouseRegion = true;

            initialX = p.X;
            initialY = p.Y;
            initialLeft = LeftSlider.Value;
            initialTop = TopSlider.Value;
        }

        private void MouseTriggerClicked(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                ScreenZoneTextblock.Foreground = (Brush)this.Resources["FontColor"];
                LeftTextblock.Foreground = (Brush)this.Resources["FontColor"];
                TopTextblock.Foreground = (Brush)this.Resources["FontColor"];
                WidthTextblock.Foreground = (Brush)this.Resources["FontColor"];
                HeightTextblock.Foreground = (Brush)this.Resources["FontColor"];
                LeftTextbox.Foreground = (Brush)this.Resources["FontColor"];
                TopTextbox.Foreground = (Brush)this.Resources["FontColor"];
                WidthTextbox.Foreground = (Brush)this.Resources["FontColor"];
                HeightTextbox.Foreground = (Brush)this.Resources["FontColor"];

                LeftTextbox.IsEnabled = true;
                TopTextbox.IsEnabled = true;
                WidthTextbox.IsEnabled = true;
                HeightTextbox.IsEnabled = true;

                LeftSlider.IsEnabled = true;
                TopSlider.IsEnabled = true;
                WidthSlider.IsEnabled = true;
                HeightSlider.IsEnabled = true;

                VirtualMonitor.Opacity = 1;
            }
            else {
                ScreenZoneTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
                LeftTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
                TopTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
                WidthTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
                HeightTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
                LeftTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                TopTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                WidthTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                HeightTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];

                LeftTextbox.IsEnabled = false;
                TopTextbox.IsEnabled = false;
                WidthTextbox.IsEnabled = false;
                HeightTextbox.IsEnabled = false;

                LeftSlider.IsEnabled = false;
                TopSlider.IsEnabled = false;
                WidthSlider.IsEnabled = false;
                HeightSlider.IsEnabled = false;

                VirtualMonitor.Opacity = 0.3;
                //KeyTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }

        private void BackgroundOpacityValueChanged(object sender, RoutedEventArgs e) {
            if(BackgroundOpacityTextbox == null) return;

            BackgroundOpacityTextbox.Text = ((int)(BackgroundOpacitySlider.Value)).ToString();
        }

        private void BackgroundOpacityChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(BackgroundOpacityTextbox, BackgroundOpacitySlider, 1, 255);
        }

        private EnumEnumerator<System.Windows.Forms.Keys> m_Key = new EnumEnumerator<System.Windows.Forms.Keys>();

        public EnumEnumerator<System.Windows.Forms.Keys> TriggerKey {
            get { return m_Key; }
            set {
                m_Key = value;
            }
        }

        private void LeftTextChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(LeftTextbox, LeftSlider, 0, 32000);
        }

        private void TopTextChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(TopTextbox, TopSlider, 0, 32000);
        }

        private void WidthTextChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(WidthTextbox, WidthSlider, 0, 32000);
        }

        private void HeightTextChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(HeightTextbox, HeightSlider, 0, 32000);
        }

        private void FilterSliderChanged(object sender, RoutedEventArgs e) {
            if(FilterTextbox == null) return;

            FilterTextbox.Text = ((int)(FilterSlider.Value)).ToString();
        }

        private void SliderTextboxChanged(object sender, TextChangedEventArgs e) {
            syncTextBoxWithSlider(FilterTextbox, FilterSlider, 0, 5000);
        }

        private void FilterCheckboxClick(object sender, RoutedEventArgs e) {
            if(((CheckBox)(sender)).IsChecked == true) {
                FilterTextblock.Foreground = (Brush)this.Resources["FontColor"];
                FilterTextbox.Foreground = (Brush)this.Resources["FontColor"];
                FilterSlider.IsEnabled = true;
                FilterTextbox.IsEnabled = true;
                FilterTextbox.Foreground = (Brush)this.Resources["FontColor"];
            }
            else {
                FilterTextblock.Foreground = (Brush)this.Resources["DisabledFontColor"];
                FilterTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
                FilterSlider.IsEnabled = false;
                FilterTextbox.IsEnabled = false;
                FilterTextbox.Foreground = (Brush)this.Resources["DisabledFontColor"];
            }
        }
    }
}
