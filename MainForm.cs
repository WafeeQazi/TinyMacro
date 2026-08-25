namespace TinyMacro;

public partial class MainForm : Form
{
    private Button _recordButton = null!;
    private Button _stopButton = null!;
    private Button _playButton = null!;
    private Label _statusLabel = null!;
    private Label _macroNameLabel = null!;

    private readonly MouseHook _mouseHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private int _clickCount;
    private int _keyCount;

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
        _recordButton.Click += OnRecordClicked;

        _playButton = new Button
        {
            Text = "▶ Play",
            Dock = DockStyle.Top,
            Height = 40
        };
        _playButton.Click += OnPlayClicked;

        _stopButton = new Button
        {
            Text = "■ Stop",
            Dock = DockStyle.Top,
            Height = 40
        };
        _stopButton.Click += OnStopClicked;

        _statusLabel = new Label
        {
            Text = "Status: Idle",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Bottom,
            Height = 40
        };

        Controls.Add(_stopButton);
        Controls.Add(_playButton);
        Controls.Add(_recordButton);
        Controls.Add(_macroNameLabel);
        Controls.Add(_statusLabel);
    }

    private void OnRecordClicked(object? sender, EventArgs e)
    {
        _clickCount = 0;
        _keyCount = 0;
        UpdateRecordingStatus();
        _mouseHook.Start();
        _keyboardHook.Start();
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        _statusLabel.Text = "Status: Playing...";
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _mouseHook.Stop();
        _keyboardHook.Stop();
        _statusLabel.Text = $"Status: Idle ({_clickCount} clicks, {_keyCount} keys)";
    }

    private void OnLeftClick(Point location)
    {
        _clickCount++;
        UpdateRecordingStatus();
    }

    private void OnKeyDown(Keys key)
    {
        _keyCount++;
        UpdateRecordingStatus();
    }

    private void UpdateRecordingStatus()
    {
        _statusLabel.Text = $"Status: Recording... {_clickCount} clicks, {_keyCount} keys";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _mouseHook.Stop();
        _keyboardHook.Stop();
        base.OnFormClosed(e);
    }
}
