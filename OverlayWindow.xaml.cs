using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VolumeMixerPro
{
    public partial class OverlayWindow : Window
    {
        private DispatcherTimer _hideTimer;
        private double _targetX;
        private double _targetY;

        public OverlayWindow()
        {
            InitializeComponent();
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _hideTimer.Tick += (s, e) => HideWithAnimation();

            this.Loaded += (s, e) => {
                WindowHelper.ApplyModernStyle(this);
                // Position in the void
                this.Left = -5000;
                this.Top = -5000;
            };
        }

        private void CalculatePosition()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            _targetX = desktopWorkingArea.Right - this.Width - 20;
            _targetY = 40;
        }

        public void ShowVolume(string appName, float volume)
        {
            Dispatcher.Invoke(() => {
                AppNameText.Text = appName.ToUpper();
                VolumeText.Text = $"{(int)volume}%";
                
                int filled = (int)Math.Round(volume / 5);
                string bar = new string('█', filled) + new string('▒', 20 - filled);
                AsciiBar.Text = bar;

                CalculatePosition();
                this.Left = _targetX;
                this.Top = _targetY;
                
                // Force visibility and opacity
                this.Visibility = Visibility.Visible;
                this.Opacity = 1;
                MainBorder.Opacity = 1;

                _hideTimer.Stop();
                _hideTimer.Start();
            });
        }

        private void HideWithAnimation()
        {
            var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
            anim.Completed += (s, e) => {
                this.Left = -5000;
                this.Opacity = 0;
            };
            MainBorder.BeginAnimation(OpacityProperty, anim);
            _hideTimer.Stop();
        }
    }
}
