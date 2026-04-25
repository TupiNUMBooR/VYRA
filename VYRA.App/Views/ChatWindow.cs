using VYRA.Models;

namespace VYRA.Views;

public partial class ChatWindow : Form
{
    public event Action<string, bool>? SendRequested;
    public event Action? CloseRequested;

    public bool CloseOnDeactivate { get; set; }

    private const int PreviewWidth = 260;

    public ChatWindow()
    {
        InitializeComponent();

        _sendScreenshot.CheckedChanged += (_, _) =>
        {
            UpdatePreviewVisibility();
            FocusInput();
        };

        RegisterFocusReturn(this);
    }

    public void ResetForOpen()
    {
        _sendScreenshot.Checked = true;
        UpdatePreviewVisibility();
        FocusInput();
    }

    public void FocusInput()
    {
        if (IsDisposed) return;

        _input.Focus();
        _input.SelectionStart = _input.TextLength;
    }

    public void ClearInput()
    {
        _input.Clear();
    }

    public void CenterOnVirtualScreen()
    {
        var screen = SystemInformation.VirtualScreen;

        Location = new Point(
            screen.Left + (screen.Width - Width) / 2,
            screen.Top + (screen.Height - Height) / 2);
    }

    // === PREVIEW ===

    public void SetPreviewImage(Image? image)
    {
        _preview.Image = image;
        UpdatePreviewVisibility();
    }

    private void UpdatePreviewVisibility()
    {
        bool visible = _preview.Image != null && _sendScreenshot.Checked;

        _preview.Visible = visible;

        _bottomGrid.ColumnStyles[1].SizeType = SizeType.Absolute;
        _bottomGrid.ColumnStyles[1].Width = visible ? PreviewWidth : 0;

        _bottomGrid.PerformLayout();
    }

    // === MESSAGE API ===

    public void AddTextMessage(string text, bool isUser)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var bubble = CreateTextBubble(text, isUser);
        AddContainer(bubble, isUser);
    }

    public void AddImageMessage(Image image, bool isUser)
    {
        var bubble = CreateImageBubble(image, isUser);
        AddContainer(bubble, isUser);
    }

    public void SetWindowTitle(string? title)
    {
        Text = string.IsNullOrWhiteSpace(title)
            ? "VYRA"
            : $"VYRA — {title}";
    }

    private void AddContainer(Control bubble, bool isUser)
    {
        var container = new Panel
        {
            Width = _history.ClientSize.Width - 25,
            AutoSize = true
        };

        container.Controls.Add(bubble);
        bubble.Top = 0;

        if (isUser)
            bubble.Left = container.Width - bubble.Width - 10;
        else
            bubble.Left = 10;

        _history.Controls.Add(container);

        _history.PerformLayout();
        _history.ScrollControlIntoView(container);

        RegisterFocusReturn(container);
    }

    private Control CreateTextBubble(string text, bool isUser)
    {
        var bubble = new Panel
        {
            AutoSize = true,
            Padding = new Padding(10),
            BackColor = isUser
                ? Color.FromArgb(30, 60, 30)
                : Color.FromArgb(20, 30, 20),
            MaximumSize = new Size(600, 0)
        };

        var label = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Text = text,
            ForeColor = Color.LightGreen,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 11f)
        };

        bubble.Controls.Add(label);
        return bubble;
    }

    private Control CreateImageBubble(Image image, bool isUser)
    {
        var bubble = new Panel
        {
            AutoSize = true,
            Padding = new Padding(6),
            BackColor = isUser
                ? Color.FromArgb(30, 60, 30)
                : Color.FromArgb(20, 30, 20)
        };

        var picture = new PictureBox
        {
            Image = image,
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 400,
            Height = 250
        };

        bubble.Controls.Add(picture);
        return bubble;
    }

    // === INPUT ===

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // ESC
        if (keyData == Keys.Escape)
        {
            CloseRequested?.Invoke();
            return true;
        }

        // ENTER = SEND
        if (keyData == Keys.Enter)
        {
            SendRequested?.Invoke(_input.Text, _sendScreenshot.Checked);
            return true;
        }

        // SHIFT+ENTER или CTRL+ENTER = НОВАЯ СТРОКА
        if (keyData == (Keys.Shift | Keys.Enter))
        {
            _input.AppendText(Environment.NewLine);
            return true;
        }

        // ALT+S = toggle screenshot
        if (keyData == (Keys.Alt | Keys.S))
        {
            _sendScreenshot.Checked = !_sendScreenshot.Checked;
            UpdatePreviewVisibility();
            FocusInput();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);

        if (CloseOnDeactivate && Visible)
            CloseRequested?.Invoke();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            CloseRequested?.Invoke();
            return;
        }

        base.OnFormClosing(e);
    }

    private void RegisterFocusReturn(Control root)
    {
        root.MouseDown += (_, _) => FocusInput();

        foreach (Control child in root.Controls)
            RegisterFocusReturn(child);
    }
}
