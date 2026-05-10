using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace VolumeMixerPro
{
    public partial class MainWindow : Window
    {
        private readonly VolumeService _volumeService;
        private readonly HotkeyService _hotkeyService;
        private readonly DispatcherTimer _updateTimer;
        private readonly OverlayWindow _overlayWindow;
        private readonly Dictionary<int, Border> _sessionRows = new Dictionary<int, Border>();
        private DateTime _lastInteraction;
        private bool _isUpdatingFromSource = false;
        private bool _isDragging = false;

        public MainWindow()
        {
            InitializeComponent();
            _volumeService = new VolumeService();
            _hotkeyService = new HotkeyService();
            _overlayWindow = new OverlayWindow();
            
            _hotkeyService.OnToggleMenu += ToggleMenu;
            _hotkeyService.OnVolumeScroll += HandleVolumeScroll;
            _hotkeyService.OnMuteActive += HandleMuteActive;
            _hotkeyService.OnSoloMode += HandleSoloMode;
            _hotkeyService.OnUnmuteAll += HandleUnmuteAll;
            _hotkeyService.OnPanicReset += HandlePanicReset;

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _updateTimer.Tick += (s, e) => {
                UpdateSessions();
                CheckIdle();
            };

            this.Loaded += (s, e) => {
                WindowHelper.ApplyModernStyle(this);
                PositionWindow();
                // Ensure it starts collapsed and transparent
                this.Visibility = Visibility.Collapsed;
                this.Opacity = 0;
                
                // Wake up the overlay in the void so it's ready
                _overlayWindow.Show();
                _overlayWindow.Visibility = Visibility.Hidden; 
            };

            _lastInteraction = DateTime.Now;
        }

        private void PositionWindow()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 20;
            this.Top = desktopWorkingArea.Top + 40;
        }

        public void ToggleMenu()
        {
            Dispatcher.Invoke(() =>
            {
                if (this.Visibility == Visibility.Visible)
                {
                    HideWithAnimation();
                }
                else
                {
                    ShowWithAnimation();
                }
            });
        }

        private void ShowWithAnimation()
        {
            _lastInteraction = DateTime.Now;
            UpdateSessions();
            this.Visibility = Visibility.Visible;
            this.Opacity = 0;
            
            var anim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(OpacityProperty, anim);
            _updateTimer.Start();
        }

        private void HideWithAnimation()
        {
            var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            anim.Completed += (s, e) =>
            {
                this.Visibility = Visibility.Collapsed;
                _updateTimer.Stop();
            };
            this.BeginAnimation(OpacityProperty, anim);
        }

        private void UpdateSessions()
        {
            var sessions = _volumeService.GetActiveSessions();
            var currentPids = sessions.Take(8).Select(s => s.ProcessId).ToList();

            // 1. Remove old sessions
            var toRemove = _sessionRows.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
            foreach (var pid in toRemove)
            {
                SessionsContainer.Children.Remove(_sessionRows[pid]);
                _sessionRows.Remove(pid);
            }

            // 2. Update or Add sessions
            _isUpdatingFromSource = true;
            foreach (var session in sessions.Take(8))
            {
                if (_sessionRows.ContainsKey(session.ProcessId))
                {
                    // Update existing
                    var border = _sessionRows[session.ProcessId];
                    var grid = border.Child as Grid;
                    var stack = grid.Children[0] as StackPanel;
                    var header = stack.Children[0] as DockPanel;
                    var volTxt = header.Children.OfType<TextBlock>().Last();
                    var slider = stack.Children[1] as Slider;

                    if (!_isDragging)
                    {
                        slider.Value = session.Volume;
                        volTxt.Text = $"{(int)session.Volume}%";
                    }
                }
                else
                {
                    // Add new
                    var row = CreateSessionRow(session);
                    _sessionRows[session.ProcessId] = (Border)row;
                    SessionsContainer.Children.Add(row);
                }
            }
            _isUpdatingFromSource = false;

            this.Height = 110 + (_sessionRows.Count * 85);
        }

        private FrameworkElement CreateSessionRow(AudioSessionModel session)
        {
            var border = new Border
            {
                Background = System.Windows.Media.Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var mainStack = new StackPanel();
            Grid.SetColumn(mainStack, 0);

            var header = new DockPanel();
            if (session.AppIcon != null)
            {
                var iconImg = new Image 
                { 
                    Source = session.AppIcon, 
                    Width = 20, Height = 20, 
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                header.Children.Add(iconImg);
            }

            var nameTxt = new TextBlock 
            { 
                Text = session.DisplayName, 
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            var volTxt = new TextBlock 
            { 
                Text = $"{(int)session.Volume}%", 
                Foreground = System.Windows.Media.Brushes.LightGray,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(nameTxt);
            header.Children.Add(volTxt);

            var slider = new Slider
            {
                Style = (Style)FindResource("PremiumSliderStyle"),
                Minimum = 0,
                Maximum = 100,
                Value = session.Volume,
                Margin = new Thickness(0, 10, 0, 10),
                VerticalAlignment = VerticalAlignment.Center
            };

            var muteBtn = new Button
            {
                Content = session.IsMuted ? "🔇" : "🔊",
                Style = (Style)Resources["ActionButtonStyle"],
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(muteBtn, 1);
            muteBtn.Click += (s, e) => {
                session.IsMuted = !session.IsMuted;
                _volumeService.SetMute(session.ProcessId, session.IsMuted);
                muteBtn.Content = session.IsMuted ? "🔇" : "🔊";
            };

            int lastVal = (int)session.Volume;
            slider.PreviewMouseLeftButtonDown += (s, e) => _isDragging = true;
            slider.PreviewMouseLeftButtonUp += (s, e) => _isDragging = false;
            slider.ValueChanged += async (s, e) => {
                if (_isUpdatingFromSource) return;
                int currentVal = (int)Math.Round(e.NewValue);
                if (currentVal == lastVal) return;
                lastVal = currentVal;
                volTxt.Text = $"{currentVal}%";
                await _volumeService.SetVolume(session.ProcessId, currentVal);
                _lastInteraction = DateTime.Now;
            };

            mainStack.Children.Add(header);
            mainStack.Children.Add(slider);
            grid.Children.Add(mainStack);
            grid.Children.Add(muteBtn);
            border.Child = grid;

            return border;
        }

        private async void HandleVolumeScroll(int direction)
        {
            int activePid = _volumeService.GetActiveProcessId();
            string activeName = "";
            try { activeName = Process.GetProcessById(activePid).ProcessName.ToLower(); } catch { }

            var sessions = _volumeService.GetActiveSessions();
            // Match by process name (like AHK does) to handle browsers/multi-process apps
            var activeSession = sessions.FirstOrDefault(s => {
                try {
                    return Process.GetProcessById(s.ProcessId).ProcessName.ToLower() == activeName;
                } catch { return false; }
            });

            if (activeSession != null)
            {
                float newVol = Math.Max(0, Math.Min(100, activeSession.Volume + (direction * 5)));
                await _volumeService.SetVolume(activeSession.ProcessId, newVol);
                Dispatcher.Invoke(() => {
                    _overlayWindow.ShowVolume(activeSession.DisplayName, newVol);
                });
            }
        }

        private void HandleMuteActive()
        {
            int activePid = _volumeService.GetActiveProcessId();
            var sessions = _volumeService.GetActiveSessions();
            var active = sessions.FirstOrDefault(s => s.ProcessId == activePid);
            if (active != null)
            {
                _volumeService.SetMute(activePid, !active.IsMuted);
                _overlayWindow.ShowVolume(active.DisplayName + (active.IsMuted ? " (Unmuted)" : " (Muted)"), active.Volume);
            }
        }

        private void HandleSoloMode()
        {
            int activePid = _volumeService.GetActiveProcessId();
            var sessions = _volumeService.GetActiveSessions();
            foreach (var s in sessions)
            {
                _volumeService.SetMute(s.ProcessId, s.ProcessId != activePid);
            }
            _overlayWindow.ShowVolume("Solo Mode Active", 100);
        }

        private void HandleUnmuteAll()
        {
            var sessions = _volumeService.GetActiveSessions();
            foreach (var s in sessions) _volumeService.SetMute(s.ProcessId, false);
            _overlayWindow.ShowVolume("All Unmuted", 100);
        }

        private void HandlePanicReset()
        {
            var sessions = _volumeService.GetActiveSessions();
            foreach (var s in sessions)
            {
                _volumeService.SetVolume(s.ProcessId, 50);
                _volumeService.SetMute(s.ProcessId, false);
            }
            _overlayWindow.ShowVolume("PANIC RESET", 50);
        }

        private void CheckIdle()
        {
            if (_isDragging) 
            {
                _lastInteraction = DateTime.Now;
                return;
            }

            var mousePos = GetCursorPosition();
            var windowRect = new Rect(this.Left, this.Top, this.Width, this.Height);
            if (windowRect.Contains(new Point(mousePos.X, mousePos.Y)))
            {
                _lastInteraction = DateTime.Now;
                return;
            }

            if ((DateTime.Now - _lastInteraction).TotalSeconds > 4)
            {
                Dispatcher.Invoke(HideWithAnimation);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        private Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);
            return new Point(lpPoint.X, lpPoint.Y);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideWithAnimation();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyService.Dispose();
            base.OnClosed(e);
        }
    }
}