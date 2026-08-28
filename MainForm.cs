using System.Diagnostics;

namespace TinyMacro;

public partial class MainForm : Form
{
    private Button _recordButton = null!;
    private Button _playButton = null!;
    private Label _statusLabel = null!;
    private Label _macroNameLabel = null!;

    private readonly MouseHook _mouseHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly List<MacroEvent> _recordedEvents = new();
    private readonly Stopwatch _stopwatch = new();
    private long _lastEventElapsedMs;
    private bool _isRecording;
    private bool _isPlaying;
    private CancellationTokenSource? _playbackCts;

    public MainForm()
    {
        InitializeComponent();
        _mouseHook.LeftClick += OnLeftClick;
        _keyboardHook.KeyDown += OnKeyDown;
    }

    private void InitializeComponent()
    {
        Text = "TinyMacro";
        ClientSize = new Size(240, 220);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        _macroNameLabel = new Label
        {
            Text = "Macro: Untitled",
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

        _statusLabel = new Label
        {
            Text = "Status: Idle",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Bottom,
            Height = 40
        };

        Controls.Add(_playButton);
        Controls.Add(_recordButton);
        Controls.Add(_macroNameLabel);
        Controls.Add(_statusLabel);
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

    private async void OnPlayButtonClicked(object? sender, EventArgs e)
    {
        if (_isPlaying)
        {
            _playbackCts?.Cancel();
            return;
        }

        if (_recordedEvents.Count == 0)
        {
            MessageBox.Show("No macro recorded yet.", "TinyMacro");
            return;
        }

        _isPlaying = true;
        _playButton.Text = "■ Stop";
        _recordButton.Enabled = false;
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
            _playButton.Text = "▶ Play";
            _recordButton.Enabled = true;
        }
    }

    private async Task PlaybackAsync(CancellationToken token)
    {
        foreach (var macroEvent in _recordedEvents)
        {
            await Task.Delay((int)macroEvent.DelayMs, token);
            token.ThrowIfCancellationRequested();

            if (macroEvent.Type == MacroEventType.MouseClick)
            {
                MacroPlayer.MoveAndClick(macroEvent.X, macroEvent.Y);
            }
            else
            {
                MacroPlayer.SendKey(macroEvent.Key);
            }
        }
    }

    private void OnLeftClick(Point location)
    {
        RecordEvent(MacroEvent.Click(location.X, location.Y, ElapsedSinceLastEvent()));
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
