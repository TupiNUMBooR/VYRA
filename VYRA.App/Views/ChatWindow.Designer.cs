namespace VYRA.Views;

partial class ChatWindow
{
    private FlowLayoutPanel _history;
    private TextBox _input;
    private CheckBox _sendScreenshot;
    private PictureBox _preview;
    private TableLayoutPanel _bottomGrid;

    private void InitializeComponent()
    {
        Text = "VYRA";
        Width = 1000;
        Height = 700;

        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        KeyPreview = true;

        BackColor = Color.FromArgb(18, 22, 18);

        // === HISTORY ===
        _history = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.FromArgb(10, 14, 10),
            Padding = new Padding(12)
        };

        // === INPUT ===
        _input = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            AcceptsReturn = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(4, 8, 4),
            ForeColor = Color.FromArgb(190, 255, 190),
            Font = new Font("Consolas", 11f)
        };

        // === CHECKBOX ===
        _sendScreenshot = new CheckBox
        {
            Text = "Send screenshot (Alt+S)",
            Checked = true,
            AutoSize = true,
            ForeColor = Color.FromArgb(190, 255, 190)
        };

        var hint = new Label
        {
            Text = "Enter send    Shift+Enter new line    Esc close",
            AutoSize = true,
            ForeColor = Color.FromArgb(130, 190, 130)
        };

        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        controls.Controls.Add(hint);
        controls.Controls.Add(_sendScreenshot);

        // === PREVIEW ===
        _preview = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Visible = false
        };

        // === GRID ===
        _bottomGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 220,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(12)
        };

        // КОЛОНКИ (ВАЖНО)
        _bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // input
        _bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));   // preview (скрыт по умолчанию)

        // СТРОКИ
        _bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _bottomGrid.Controls.Add(_input, 0, 0);
        _bottomGrid.Controls.Add(_preview, 1, 0);
        _bottomGrid.Controls.Add(controls, 0, 1);

        _bottomGrid.SetColumnSpan(controls, 2);

        Controls.Add(_history);
        Controls.Add(_bottomGrid);
    }
}
