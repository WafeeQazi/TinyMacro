using System.Diagnostics;

namespace TinyMacro;

public partial class MainForm : Form
{
    private const int MinMoveIntervalMs = 40;
    private const int MinMoveDistancePx = 4;

    private MenuStrip _menuStrip = null!;
    private Button _recordButton = null!;
    private Button _playButton = null!;
    private Button _stopPlaybackButton = null!;
    private Panel _repeatPanel = null!;
    private NumericUpDown _repeatCountInput = null!;
    private CheckBox _repeatForeverCheckbox = null!;
    private Panel _speedPanel = null!;
    private NumericUpDown _speedInput = null!;
    private Label _statusLabel = null!;
    private Label _macroNameLabel = null!;

    private readonly MouseHook _mouseHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly List<MacroEvent> _recordedEvents = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly Dictionary<Keys, long> _keyDownElapsed = new();
    private long _lastEventElapsedMs;
    private Point? _lastMovePosition;
    private long _lastMoveRecordedElapsed;
    private bool _isRecording;
    private bool _isPlaying;
    private bool _isPaused;
    private bool _leftButtonPhysicallyDown;
    private bool _middleButtonPhysicallyDown;
    private bool _rightButtonPhysicallyDown;
    private CancellationTokenSource? _playbackCts;
    private string _macroName = "Untitled";
    private string? _currentFilePath;

    public MainForm()
    {
        InitializeComponent();
        _mouseHook.LeftDown += OnLeftDown;
        _mouseHook.LeftUp += OnLeftUp;
        _mouseHook.MiddleDown += OnMiddleDown;
        _mouseHook.MiddleUp += OnMiddleUp;
        _mouseHook.RightDown += OnRightDown;
        _mouseHook.RightUp += OnRightUp;
        _mouseHook.Scroll += OnScroll;
        _mouseHook.Move += OnMouseMove;
        _keyboardHook.KeyDown += OnKeyDown;
        _keyboardHook.KeyUp += OnKeyUp;
        Deactivate += (sender, e) => TopMost = true;
    }

    private void InitializeComponent()
    {
        Text = "TinyMacro";
        ClientSize = new Size(240, 320);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        _macroNameLabel = new Label
        {
            Text = $"Macro: {_macroName}",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };

        _recordButton = new Button
        {
            Text = "● Record",
            Dock = DockStyle.Top,
            Height = 40
        };
        _recordButton.Click += OnRecordButtonClicked;

        _playButton = new Button
        {
            Text = "▶ Play",
            Dock = DockStyle.Top,
            Height = 40
        };
        _playButton.Click += OnPlayButtonClicked;

        _repeatPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36
        };

        var repeatLabel = new Label
        {
            Text = "Repeat:",
            Location = new Point(8, 10),
            AutoSize = true
        };

        _repeatCountInput = new NumericUpDown
        {
            Location = new Point(70, 7),
            Width = 55,
            Minimum = 1,
            Maximum = 9999,
            Value = 1
        };

        _repeatForeverCheckbox = new CheckBox
        {
            Text = "Forever",
            Location = new Point(135, 10),
            AutoSize = true
        };
        _repeatForeverCheckbox.CheckedChanged += OnRepeatForeverChanged;

        _repeatPanel.Controls.Add(repeatLabel);
        _repeatPanel.Controls.Add(_repeatCountInput);
        _repeatPanel.Controls.Add(_repeatForeverCheckbox);

        _speedPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36
        };

        var speedLabel = new Label
        {
            Text = "Speed:",
            Location = new Point(8, 10),
            AutoSize = true
        };

        _speedInput = new NumericUpDown
        {
            Location = new Point(70, 7),
            Width = 55,
            Minimum = 0.1m,
            Maximum = 5.0m,
            Increment = 0.1m,
            DecimalPlaces = 1,
            Value = 1.0m
        };

        var speedSuffixLabel = new Label
        {
            Text = "x",
            Location = new Point(130, 10),
            AutoSize = true
        };

        _speedPanel.Controls.Add(speedLabel);
        _speedPanel.Controls.Add(_speedInput);
        _speedPanel.Controls.Add(speedSuffixLabel);

        _stopPlaybackButton = new Button
        {
            Text = "■ Stop Playback",
            Dock = DockStyle.Top,
            Height = 36,
            Enabled = false
        };
        _stopPlaybackButton.Click += OnStopPlaybackClicked;

        _statusLabel = new Label
        {
            Text = "Status: Idle",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Bottom,
            Height = 40
        };

        _menuStrip = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("New", null, OnNewClicked));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Open...", null, OnOpenClicked));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save", null, OnSaveClicked));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save As...", null, OnSaveAsClicked));
        _menuStrip.Items.Add(fileMenu);
        MainMenuStrip = _menuStrip;

        Controls.Add(_stopPlaybackButton);
        Controls.Add(_speedPanel);
        Controls.Add(_repeatPanel);
        Controls.Add(_playButton);
        Controls.Add(_recordButton);
        Controls.Add(_macroNameLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_menuStrip);
    }

    private void OnRepeatForeverChanged(object? sender, EventArgs e)
    {
        _repeatCountInput.Enabled = !_repeatForeverCheckbox.Checked;
    }

    private void OnNewClicked(object? sender, EventArgs e)
    {
        if (_isRecording || _isPlaying)
            return;

        _recordedEvents.Clear();
        _macroName = "Untitled";
        _currentFilePath = null;
        _macroNameLabel.Text = $"Macro: {_macroName}";
        _statusLabel.Text = "Status: Idle (0 events)";
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
        if (_isRecording || _isPlaying)
            return;

        using var dialog = new OpenFileDialog { Filter = "TinyMacro files (*.tmacro)|*.tmacro" };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        MacroData data;
        try
        {
            data = MacroFile.Load(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Couldn't load this macro file. It may be corrupted or in an unsupported format.\n\n{ex.Message}",
                "TinyMacro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _recordedEvents.Clear();
        _recordedEvents.AddRange(data.Events);
        _macroName = data.Name;
        _currentFilePath = dialog.FileName;
        _macroNameLabel.Text = $"Macro: {_macroName}";
        _statusLabel.Text = $"Status: Idle ({_recordedEvents.Count} events loaded)";
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_currentFilePath == null)
        {
            OnSaveAsClicked(sender, e);
            return;
        }

        SaveToFile(_currentFilePath);
    }

    private void OnSaveAsClicked(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog { Filter = "TinyMacro files (*.tmacro)|*.tmacro", FileName = _macroName };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        _macroName = Path.GetFileNameWithoutExtension(dialog.FileName);
        _currentFilePath = dialog.FileName;
        _macroNameLabel.Text = $"Macro: {_macroName}";
        SaveToFile(_currentFilePath);
    }

    private void SaveToFile(string path)
    {
        try
        {
            var data = new MacroData { Name = _macroName, Events = _recordedEvents };
            MacroFile.Save(path, data);
            _statusLabel.Text = $"Status: Saved ({_recordedEvents.Count} events)";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Couldn't save the macro file.\n\n{ex.Message}",
                "TinyMacro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnRecordButtonClicked(object? sender, EventArgs e)
    {
        if (_isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        _recordedEvents.Clear();
        _lastEventElapsedMs = 0;
        _lastMovePosition = null;
        _lastMoveRecordedElapsed = 0;
        _keyDownElapsed.Clear();
        _stopwatch.Restart();

        var mouseHooked = _mouseHook.Start();
        var keyboardHooked = _keyboardHook.Start();

        if (!mouseHooked || !keyboardHooked)
        {
            _mouseHook.Stop();
            _keyboardHook.Stop();
            _stopwatch.Stop();
            MessageBox.Show(
                "TinyMacro couldn't install the system input hooks needed to record. This can happen if another program is blocking global hooks. Try restarting the app, or running it as administrator.",
                "TinyMacro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _isRecording = true;
        _recordButton.Text = "■ Stop";
        UpdateRecordingStatus();
    }

    private void StopRecording()
    {
        _isRecording = false;
        _recordButton.Text = "● Record";
        _mouseHook.Stop();
        _keyboardHook.Stop();
        _stopwatch.Stop();
        _keyDownElapsed.Clear();
        _statusLabel.Text = $"Status: Idle ({_recordedEvents.Count} events)";
    }

    private void OnStopPlaybackClicked(object? sender, EventArgs e)
    {
        _playbackCts?.Cancel();
    }

    private async void OnPlayButtonClicked(object? sender, EventArgs e)
    {
        if (_isPlaying)
        {
            _isPaused = !_isPaused;
            _playButton.Text = _isPaused ? "▶ Resume" : "⏸ Pause";
            _statusLabel.Text = _isPaused ? "Status: Paused" : "Status: Playing...";
            return;
        }

        if (_recordedEvents.Count == 0)
        {
            MessageBox.Show("No macro recorded yet.", "TinyMacro");
            return;
        }

        _isPlaying = true;
        _isPaused = false;
        _leftButtonPhysicallyDown = false;
        _middleButtonPhysicallyDown = false;
        _rightButtonPhysicallyDown = false;
        _playButton.Text = "⏸ Pause";
        _stopPlaybackButton.Enabled = true;
        _recordButton.Enabled = false;
        _repeatCountInput.Enabled = false;
        _repeatForeverCheckbox.Enabled = false;
        _speedInput.Enabled = false;
        _statusLabel.Text = "Status: Playing...";
        _playbackCts = new CancellationTokenSource();
        MacroPlayer.BeginHighResolutionTiming();

        try
        {
            await PlaybackAsync(_playbackCts.Token);
            _statusLabel.Text = "Status: Idle (playback finished)";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Status: Idle (playback stopped)";
        }
        finally
        {
            MacroPlayer.EndHighResolutionTiming();
            ReleaseAnyHeldButtons();

            _isPlaying = false;
            _isPaused = false;
            _playButton.Text = "▶ Play";
            _stopPlaybackButton.Enabled = false;
            _recordButton.Enabled = true;
            _repeatCountInput.Enabled = !_repeatForeverCheckbox.Checked;
            _repeatForeverCheckbox.Enabled = true;
            _speedInput.Enabled = true;
        }
    }

    private async Task PlaybackAsync(CancellationToken token)
    {
        var repeatForever = _repeatForeverCheckbox.Checked;
        var repeatCount = Math.Max(1, (int)_repeatCountInput.Value);
        var speed = Math.Max(0.1, (double)_speedInput.Value);
        var iteration = 0;

        while (repeatForever || iteration < repeatCount)
        {
            foreach (var macroEvent in _recordedEvents)
            {
                await WaitWhilePausedAsync(token);
                await Task.Delay(ScaledMs(macroEvent.DelayMs, speed), token);
                token.ThrowIfCancellationRequested();
                await WaitWhilePausedAsync(token);

                switch (macroEvent.Type)
                {
                    case MacroEventType.LeftDown:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        MacroPlayer.LeftDown();
                        _leftButtonPhysicallyDown = true;
                        break;
                    case MacroEventType.LeftUp:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        MacroPlayer.LeftUp();
                        _leftButtonPhysicallyDown = false;
                        break;
                    case MacroEventType.MiddleDown:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        MacroPlayer.MiddleDown();
                        _middleButtonPhysicallyDown = true;
                        break;
                    case MacroEventType.MiddleUp:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        MacroPlayer.MiddleUp();
                        _middleButtonPhysicallyDown = false;
                        break;
                    case MacroEventType.RightDown:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        MacroPlayer.RightDown();
                        _rightButtonPhysicallyDown = true;
                        break;
                    case MacroEventType.RightUp:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        MacroPlayer.RightUp();
                        _rightButtonPhysicallyDown = false;
                        break;
                    case MacroEventType.Scroll:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        MacroPlayer.Scroll(macroEvent.WheelDelta);
                        break;
                    case MacroEventType.MouseMove:
                        MacroPlayer.MoveTo(macroEvent.X, macroEvent.Y);
                        break;
                    default:
                        await HoldKeyAsync(macroEvent.Key, ScaledMs(macroEvent.HoldMs, speed), token);
                        break;
                }
            }

            iteration++;
        }
    }

    private static int ScaledMs(long ms, double speed)
    {
        if (speed <= 0)
            speed = 1.0;

        return (int)Math.Max(0, ms / speed);
    }

    private async Task WaitWhilePausedAsync(CancellationToken token)
    {
        while (_isPaused)
        {
            await Task.Delay(100, token);
        }
    }

    private async Task HoldKeyAsync(Keys key, long holdMs, CancellationToken token)
    {
        MacroPlayer.KeyDown(key);
        try
        {
            if (holdMs <= 0)
                return;

            var repeatDelay = MacroPlayer.GetKeyRepeatDelayMs();
            var repeatInterval = MacroPlayer.GetKeyRepeatIntervalMs();

            if (holdMs <= repeatDelay)
            {
                await Task.Delay((int)holdMs, token);
                return;
            }

            await Task.Delay(repeatDelay, token);
            var elapsed = repeatDelay;

            while (elapsed < holdMs)
            {
                MacroPlayer.KeyDown(key);
                var step = Math.Min(repeatInterval, (int)holdMs - elapsed);
                await Task.Delay(step, token);
                elapsed += step;
            }
        }
        finally
        {
            MacroPlayer.KeyUp(key);
        }
    }

    private void ReleaseAnyHeldButtons()
    {
        if (_leftButtonPhysicallyDown)
        {
            MacroPlayer.LeftUp();
            _leftButtonPhysicallyDown = false;
        }

        if (_middleButtonPhysicallyDown)
        {
            MacroPlayer.MiddleUp();
            _middleButtonPhysicallyDown = false;
        }

        if (_rightButtonPhysicallyDown)
        {
            MacroPlayer.RightUp();
            _rightButtonPhysicallyDown = false;
        }
    }

    private void OnLeftDown(Point location)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.LeftDown(location.X, location.Y, delayMs), elapsed);
    }

    private void OnLeftUp(Point location)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.LeftUp(location.X, location.Y, delayMs), elapsed);
    }

    private void OnMiddleDown(Point location)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.MiddleDown(location.X, location.Y, delayMs), elapsed);
    }

    private void OnMiddleUp(Point location)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.MiddleUp(location.X, location.Y, delayMs), elapsed);
    }

    private void OnRightDown(Point location)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.RightDown(location.X, location.Y, delayMs), elapsed);
    }

    private void OnRightUp(Point location)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.RightUp(location.X, location.Y, delayMs), elapsed);
    }

    private void OnScroll(Point location, int wheelDelta)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.Scroll(location.X, location.Y, wheelDelta, delayMs), elapsed);
    }

    private void OnMouseMove(Point location)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;

        if (_lastMovePosition is Point lastPosition)
        {
            var dx = location.X - lastPosition.X;
            var dy = location.Y - lastPosition.Y;
            var distanceSq = (dx * dx) + (dy * dy);

            if (elapsed - _lastMoveRecordedElapsed < MinMoveIntervalMs && distanceSq < MinMoveDistancePx * MinMoveDistancePx)
                return;
        }

        _lastMovePosition = location;
        _lastMoveRecordedElapsed = elapsed;
        var delayMs = elapsed - _lastEventElapsedMs;
        RecordEvent(MacroEvent.Move(location.X, location.Y, delayMs), elapsed);
    }

    private void OnKeyDown(Keys key)
    {
        if (_keyDownElapsed.ContainsKey(key))
            return;

        _keyDownElapsed[key] = _stopwatch.ElapsedMilliseconds;
    }

    private void OnKeyUp(Keys key)
    {
        if (!_keyDownElapsed.TryGetValue(key, out var downElapsed))
            return;

        _keyDownElapsed.Remove(key);
        var upElapsed = _stopwatch.ElapsedMilliseconds;
        var delayMs = downElapsed - _lastEventElapsedMs;
        var holdMs = upElapsed - downElapsed;
        RecordEvent(MacroEvent.KeyPress(key, delayMs, holdMs), upElapsed);
    }

    private void RecordEvent(MacroEvent macroEvent, long completionElapsedMs)
    {
        _recordedEvents.Add(macroEvent);
        _lastEventElapsedMs = completionElapsedMs;
        UpdateRecordingStatus();
    }

    private void UpdateRecordingStatus()
    {
        _statusLabel.Text = $"Recording... {_recordedEvents.Count} events";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isRecording || _isPlaying)
        {
            var activity = _isRecording ? "recording" : "playback";
            var result = MessageBox.Show(
                $"TinyMacro is currently doing {activity}. Stop and exit?",
                "TinyMacro",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _mouseHook.Stop();
        _keyboardHook.Stop();
        _playbackCts?.Cancel();
        ReleaseAnyHeldButtons();

        if (_isPlaying)
        {
            MacroPlayer.EndHighResolutionTiming();
        }

        base.OnFormClosed(e);
    }
}
