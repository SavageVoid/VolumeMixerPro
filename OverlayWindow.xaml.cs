using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace VolumeMixerPro
{
    public partial class OverlayWindow : Window
    {
        private readonly DispatcherTimer _hideTimer;
        private bool _hasBeenShown = false;
        private double _maxFillWidth;
        private readonly DoubleAnimation _showAnim;
        private readonly DoubleAnimation _hideAnim;
        public OverlayWindow()
        {
            InitializeComponent();
            _showAnim = new DoubleAnimation(0d, 1d, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            _hideAnim = new DoubleAnimation(1d, 0d, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            _hideAnim.Completed += (s, e) => this.Visibility = Visibility.Hidden;
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
            _hideTimer.Tick += (s, e) => { _hideTimer.Stop(); HideWithAnimation(); };
            this.SourceInitialized += (s, e) =>
            {
                WindowHelper.ApplyRoundedCorners(this);
                PositionWindow();
            };
            this.SizeChanged += (s, e) => _maxFillWidth = ActualWidth - 28; 
        }
        public void ShowVolume(string appName, float volume,
                               ImageSource icon = null)
        {
            Dispatcher.Invoke(() =>
            {
                MainBorder.BeginAnimation(OpacityProperty, null);
                AppNameText.Text = appName;
                if (icon != null)
                {
                    AppIconImage.Source     = icon;
                    AppIconImage.Visibility = Visibility.Visible;
                }
                else
                {
                    AppIconImage.Visibility = Visibility.Collapsed;
                }
                int pct = (int)Math.Round(volume);
                VolumeText.Text = $"{pct}%";
                double targetWidth = (_maxFillWidth > 0 ? _maxFillWidth : 232) * volume / 100.0;
                VolumeFill.Width = targetWidth;
                VolumeFill.Background = volume > 90
                    ? new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B))
                    : volume > 70
                        ? new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x00))
                        : new SolidColorBrush(Color.FromRgb(0x00, 0xAD, 0xEF));
                if (!_hasBeenShown)
                {
                    PositionWindow();
                    this.Show();
                    _hasBeenShown = true;
                }
                this.Visibility  = Visibility.Visible;
                MainBorder.Opacity = 0;
                MainBorder.BeginAnimation(OpacityProperty, _showAnim);
                _hideTimer.Stop();
                _hideTimer.Start();
            });
        }
        private void HideWithAnimation()
        {
            MainBorder.BeginAnimation(OpacityProperty, null);
            MainBorder.Opacity = 1;
            MainBorder.BeginAnimation(OpacityProperty, _hideAnim);
        }
        private void PositionWindow()
        {
            var area = SystemParameters.WorkArea;
            this.Left = area.Right  - this.Width  - 20;
            this.Top  = area.Top    + 40;
        }
    }
}
