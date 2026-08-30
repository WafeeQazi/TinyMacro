using System.Diagnostics;

namespace TinyMacro;

public partial class MainForm : Form
{
    private MenuStrip _menuStrip = null!;
    private Button _recordButton = null!;
    private Button _playButton = null!;
    private Button _stopPlaybackButton = null!;
    private Panel _repeatPanel = null!;
    private NumericUpDown _repeatCountInput = null!;
    private CheckBox _repeatForeverCheckbox = null!;
    private Label _statusLabel = null!;
    private Label _macroNameLabel = null!;

    private readonly MouseHook _mouseHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly List<MacroEvent> _recordedEvents = new();
    private readonly Stopwatch _stopwatch = new();
    private long _lastEventElapsedMs;
    private bool _isRecording;
    private bool _isPlaying;
    private bool _isPaused;
    private CancellationTokenSource? _playbackCts;
    private string _macroName = "Untitled";
    private string? _currentFilePath;

    public MainForm()
    {
        InitializeComponent();
        _mouseHook.LeftClick += OnLeftClick;
        _mouseHook.MiddleClick += OnMiddleClick;
        _mouseHook.RightClick += OnRightClick;
        _mouseHook.Scroll += OnScroll;
        _keyboardHook.KeyDown += OnKeyDown;
        Deactivate += (sender, e) => TopMost = true;
    }

    private void InitializeComponent()
    {
        Text = "TinyMacro";
        ClientSize = new Size(240, 280);
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
        _statusLabel.Text = "Status: Idle";
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
        if (_isRecording || _isPlaying)
            return;

        using var dialog = new OpenFileDialog { Filter = "TinyMacro files (*.tmacro)|*.tmacro" };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        var data = MacroFile.Load(dialog.FileName);
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
        var data = new MacroData { Name = _macroName, Events = _recordedEvents };
        MacroFile.Save(path, data);
        _statusLabel.Text = $"Status: Saved ({_recordedEvents.Count} events)";
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
        _isRecording = true;
        _recordedEvents.Clear();
        _lastEventElapsedMs = 0;
        _stopwatch.Restart();
        _recordButton.Text = "■ Stop";
        UpdateRecordingStatus();
        _mouseHook.Start();
        _keyboardHook.Start();
    }

    private void StopRecording()
    {
        _isRecording = false;
        _recordButton.Text = "● Record";
        _mouseHook.Stop();
        _keyboardHook.Stop();
        _stopwatch.Stop();
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
        _playButton.Text = "⏸ Pause";
        _stopPlaybackButton.Enabled = true;
        _recordButton.Enabled = false;
        _repeatCountInput.Enabled = false;
        _repeatForeverCheckbox.Enabled = false;
        _statusLabel.Text = "Status: Playing...";
        _playbackCts = new CancellationTokenSource();

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
            _isPlaying = false;
            _isPaused = false;
            _playButton.Text = "▶ Play";
            _stopPlaybackButton.Enabled = false;
            _recordButton.Enabled = true;
            _repeatCountInput.Enabled = !_repeatForeverCheckbox.Checked;
            _repeatForeverCheckbox.Enabled = true;
        }
    }

    private async Task PlaybackAsync(CancellationToken token)
    {
        var repeatForever = _repeatForeverCheckbox.Checked;
        var repeatCount = (int)_repeatCountInput.Value;
        var iteration = 0;

        while (repeatForever || iteration < repeatCount)
        {
            foreach (var macroEvent in _recordedEvents)
            {
                await WaitWhilePausedAsync(token);
                await Task.Delay((int)macroEvent.DelayMs, token);
                token.ThrowIfCancellationRequested();
                await WaitWhilePausedAsync(token);

                switch (macroEvent.Type)
                {
                    case MacroEventType.MouseClick:
                        MacroPlayer.MoveAndClick(macroEvent.X, macroEvent.Y);
                        break;
                    case MacroEventType.MiddleClick:
                        MacroPlayer.MoveAndMiddleClick(macroEvent.X, macroEvent.Y);
                        break;
                    case MacroEventType.RightClick:
                        MacroPlayer.MoveAndRightClick(macroEvent.X, macroEvent.Y);
                        break;
                    case MacroEventType.Scroll:
                        MacroPlayer.MoveAndScroll(macroEvent.X, macroEvent.Y, macroEvent.WheelDelta);
                        break;
                    default:
                        MacroPlayer.SendKey(macroEvent.Key);
                        break;
                }
            }

            iteration++;
        }
    }

    private async Task WaitWhilePausedAsync(CancellationToken token)
    {
        while (_isPaused)
        {
            await Task.Delay(100, token);
        }
    }

    private void OnLeftClick(Point location)
    {
        RecordEvent(MacroEvent.Click(location.X, location.Y, ElapsedSinceLastEvent()));
    }

    private void OnMiddleClick(Point location)
    {
        RecordEvent(MacroEvent.MiddleClick(location.X, location.Y, ElapsedSinceLastEvent()));
    }

    private void OnRightClick(Point location)
    {
        RecordEvent(MacroEvent.RightClick(location.X, location.Y, ElapsedSinceLastEvent()));
    }

    private void OnScroll(Point location, int wheelDelta)
    {
        RecordEvent(MacroEvent.Scroll(location.X, location.Y, wheelDelta, ElapsedSinceLastEvent()));
    }

    private void OnKeyDown(Keys key)
    {
        RecordEvent(MacroEvent.KeyPress(key, ElapsedSinceLastEvent()));
    }

    private long ElapsedSinceLastEvent()
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        var delay = elapsed - _lastEventElapsedMs;
        _lastEventElapsedMs = elapsed;
        return delay;
    }

    private void RecordEvent(MacroEvent macroEvent)
    {
        _recordedEvents.Add(macroEvent);
        UpdateRecordingStatus();
    }

    private void UpdateRecordingStatus()
    {
        _statusLabel.Text = $"Recording... {_recordedEvents.Count} events";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _mouseHook.Stop();
        _keyboardHook.Stop();
        _playbackCts?.Cancel();
        base.OnFormClosed(e);
    }
}
