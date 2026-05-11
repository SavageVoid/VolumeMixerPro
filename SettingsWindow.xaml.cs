using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
namespace VolumeMixerPro
{
    public partial class SettingsWindow : Window
    {
        public Action OnRequestClose;
        public bool IsClosingForReal = false;

        public SettingsWindow(int tabIndex = 0)
        {
            InitializeComponent();
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.Loaded += (s, e) => {
                LoadSettingsToUI();
                SelectTab(tabIndex);
                this.Background = System.Windows.Media.Brushes.Transparent;
            };
        }
        private void SelectTab(int index)
        {
            if (index == 2)
            {
                HelpButton_Click(null, null);
                return;
            }
            RadioButton rb = null;
            switch(index)
            {
                case 0: rb = TabGeneral; break;
                case 1: rb = TabAdvanced; break;
                case 3: rb = TabAbout; break;
            }
            if (rb != null) rb.IsChecked = true;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            OnRequestClose?.Invoke();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!IsClosingForReal)
            {
                e.Cancel = true;
                OnRequestClose?.Invoke();
            }
            base.OnClosing(e);
        }

        public void ShowWithAnimation()
        {
            this.Visibility = Visibility.Visible;
            this.Opacity = 0;
            var anim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(OpacityProperty, anim);
        }

        public void HideWithAnimation(Action onCompleted)
        {
            var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            anim.Completed += (s, e) => onCompleted?.Invoke();
            this.BeginAnimation(OpacityProperty, anim);
        }
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            if (PanelHelp == null) return;
            if (PanelHelp.Visibility == Visibility.Visible)
            {
                CloseHelp_Click(null, null);
                return;
            }
            PanelGeneral.Visibility = Visibility.Collapsed;
            PanelAdvanced.Visibility = Visibility.Collapsed;
            PanelAbout.Visibility = Visibility.Collapsed;
            PanelHelp.Visibility = Visibility.Visible;
            if (TabGeneral != null) TabGeneral.IsChecked = false;
            if (TabAdvanced != null) TabAdvanced.IsChecked = false;
            if (TabAbout != null) TabAbout.IsChecked = false;
        }
        private void CloseHelp_Click(object sender, RoutedEventArgs e)
        {
            PanelHelp.Visibility = Visibility.Collapsed;
            if (TabGeneral != null) TabGeneral.IsChecked = true;
        }
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (PanelGeneral == null) return;
            PanelGeneral.Visibility = Visibility.Collapsed;
            PanelAdvanced.Visibility = Visibility.Collapsed;
            PanelHelp.Visibility = Visibility.Collapsed;
            PanelAbout.Visibility = Visibility.Collapsed;
            var rb = sender as RadioButton;
            if (rb?.Tag == null) return;
            switch (rb.Tag.ToString())
            {
                case "0": PanelGeneral.Visibility = Visibility.Visible; break;
                case "1": PanelAdvanced.Visibility = Visibility.Visible; break;
                case "3": PanelAbout.Visibility = Visibility.Visible; break;
            }
        }
        private void LoadSettingsToUI()
        {
            var s = SettingsManager.Current;
            HkToggleMenu.Text = s.ToggleMenu.ToString();
            HkMuteActive.Text = s.MuteActive.ToString();
            HkSoloMode.Text = s.SoloMode.ToString();
            HkUnmuteAll.Text = s.UnmuteAll.ToString();
            HkPanicReset.Text = s.PanicReset.ToString();
            CbMuteMode.IsChecked = s.EnableMuteMode;
            CbSoloMode.IsChecked = s.EnableSoloMode;
            CbPanicMode.IsChecked = s.EnablePanicMode;
            CbTrackpadGesture.IsChecked = s.EnableTrackpadGesture;
        }
        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string tag = btn.Tag.ToString();
                TextBox targetBox = FindName("Hk" + tag) as TextBox;
                if (targetBox != null)
                {
                    targetBox.Text = "Press any key...";
                    targetBox.Focus();
                }
            }
        }
        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Text != "Press any key...") return;
            e.Handled = true;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
                return;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            string res = "";
            if (ctrl) res += "Ctrl + ";
            if (alt) res += "Alt + ";
            if (shift) res += "Shift + ";
            string kStr = key.ToString();
            if (kStr == "D0") kStr = "0";
            if (kStr == "D1") kStr = "1";
            if (kStr == "D2") kStr = "2";
            if (kStr == "D3") kStr = "3";
            if (kStr == "D4") kStr = "4";
            if (kStr == "D5") kStr = "5";
            if (kStr == "D6") kStr = "6";
            if (kStr == "D7") kStr = "7";
            if (kStr == "D8") kStr = "8";
            if (kStr == "D9") kStr = "9";
            res += kStr;
            tb.Text = res;
            Keyboard.ClearFocus();
        }
        private HotkeyDef ParseHotkeyString(string str)
        {
            var def = new HotkeyDef();
            if (str == "Press any key..." || string.IsNullOrWhiteSpace(str)) return def;
            string[] parts = str.Split(new[] { " + " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part == "Ctrl") def.Ctrl = true;
                else if (part == "Alt") def.Alt = true;
                else if (part == "Shift") def.Shift = true;
                else
                {
                    string k = part;
                    if (k == "0") k = "D0";
                    if (k == "1") k = "D1";
                    if (k == "2") k = "D2";
                    if (k == "3") k = "D3";
                    if (k == "4") k = "D4";
                    if (k == "5") k = "D5";
                    if (k == "6") k = "D6";
                    if (k == "7") k = "D7";
                    if (k == "8") k = "D8";
                    if (k == "9") k = "D9";
                    def.KeyStr = k;
                }
            }
            return def;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current;
            s.ToggleMenu = ParseHotkeyString(HkToggleMenu.Text);
            s.MuteActive = ParseHotkeyString(HkMuteActive.Text);
            s.SoloMode = ParseHotkeyString(HkSoloMode.Text);
            s.UnmuteAll = ParseHotkeyString(HkUnmuteAll.Text);
            s.PanicReset = ParseHotkeyString(HkPanicReset.Text);
            s.EnableMuteMode = CbMuteMode.IsChecked ?? true;
            s.EnableSoloMode = CbSoloMode.IsChecked ?? true;
            s.EnablePanicMode = CbPanicMode.IsChecked ?? true;
            s.EnableTrackpadGesture = CbTrackpadGesture.IsChecked ?? false;
            SettingsManager.Save();
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all settings to defaults?", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                SettingsManager.Reset();
                LoadSettingsToUI();
                MessageBox.Show("Settings have been reset.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
