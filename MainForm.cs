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
        _playButton.Click += OnPlayClicked;

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
        ShowRecordedEvents();
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        _statusLabel.Text = "Status: Playing...";
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

    private void ShowRecordedEvents()
    {
        if (_recordedEvents.Count == 0)
            return;

        var summary = string.Join(Environment.NewLine, _recordedEvents);
        MessageBox.Show(summary, "Recorded Events (debug)");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _mouseHook.Stop();
        _keyboardHook.Stop();
        base.OnFormClosed(e);
    }
}
