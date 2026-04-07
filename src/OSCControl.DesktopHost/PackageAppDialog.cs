namespace OSCControl.DesktopHost;

internal sealed class PackageAppDialog : Form
{
    private readonly TextBox _appNameTextBox;
    private readonly TextBox _outputRootTextBox;
    private readonly TextBox _hostSourceTextBox;
    private readonly string? _detectedHostSource;
    private readonly bool _useSimplifiedChinese;

    public PackageAppDialog(string defaultAppName, string defaultOutputRoot, string? detectedHostSource, bool useSimplifiedChinese)
    {
        _detectedHostSource = detectedHostSource;
        _useSimplifiedChinese = useSimplifiedChinese;

        Text = L("Package App Settings", "\u6253\u5305\u5e94\u7528\u8bbe\u7f6e");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Width = 760;
        Height = 330;
        MinimumSize = new Size(720, 330);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var intro = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Text = L(
                "Configure the packaged app output. Host source can be empty for payload-only packages, but distribution builds should point to a published OSCControl.AppHost folder.",
                "\u8bbe\u7f6e\u6253\u5305\u8f93\u51fa\u3002Host \u6765\u6e90\u53ef\u7559\u7a7a\uff1b\u6b63\u5f0f\u5206\u53d1\u8bf7\u6307\u5411 OSCControl.AppHost \u53d1\u5e03\u76ee\u5f55\u3002"),
            Margin = new Padding(0, 0, 0, 12),
        };
        root.Controls.Add(intro, 0, 0);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(fields, 0, 1);

        _appNameTextBox = CreateTextBox(defaultAppName);
        AddLabeledRow(fields, 0, L("App name", "\u5e94\u7528\u540d\u79f0"), _appNameTextBox, null);

        _outputRootTextBox = CreateTextBox(defaultOutputRoot);
        AddLabeledRow(fields, 1, L("Output folder", "\u8f93\u51fa\u76ee\u5f55"), _outputRootTextBox, CreateBrowseButton(BrowseOutputRoot));

        _hostSourceTextBox = CreateTextBox(detectedHostSource ?? string.Empty);
        var hostButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(8, 0, 0, 8),
        };
        hostButtons.Controls.Add(CreateSmallButton(L("Browse", "\u6d4f\u89c8"), BrowseHostSource));
        hostButtons.Controls.Add(CreateSmallButton(L("Auto", "\u81ea\u52a8"), UseDetectedHostSource));
        hostButtons.Controls.Add(CreateSmallButton(L("Clear", "\u6e05\u7a7a"), () => _hostSourceTextBox.Clear()));
        AddLabeledRow(fields, 2, L("Host source", "Host \u6765\u6e90"), _hostSourceTextBox, hostButtons);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Text = detectedHostSource is null
                ? L("No AppHost build was auto-detected. You can still package now and copy host files later.", "\u6ca1\u6709\u81ea\u52a8\u68c0\u6d4b\u5230 AppHost \u76ee\u5f55\u3002\u4f60\u4ecd\u7136\u53ef\u4ee5\u5148\u6253\u5305\uff0c\u7a0d\u540e\u518d\u590d\u5236 host \u6587\u4ef6\u3002")
                : L($"Auto-detected host source: {detectedHostSource}", "\u5df2\u81ea\u52a8\u68c0\u6d4b\u5230 Host \u6765\u6e90\uff1a" + detectedHostSource),
            ForeColor = Color.FromArgb(75, 75, 75),
            Margin = new Padding(120, 4, 0, 0),
        };
        fields.Controls.Add(hint, 1, 3);
        fields.SetColumnSpan(hint, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 12, 0, 0),
        };
        root.Controls.Add(buttons, 0, 2);

        var okButton = CreateSmallButton(L("Package", "\u6253\u5305"), Confirm);
        okButton.DialogResult = DialogResult.None;
        AcceptButton = okButton;
        buttons.Controls.Add(okButton);

        var cancelButton = CreateSmallButton(L("Cancel", "\u53d6\u6d88"), () => DialogResult = DialogResult.Cancel);
        cancelButton.DialogResult = DialogResult.Cancel;
        CancelButton = cancelButton;
        buttons.Controls.Add(cancelButton);
    }

    public string AppName => _appNameTextBox.Text.Trim();

    public string OutputRoot => _outputRootTextBox.Text.Trim();

    public string? HostSource
    {
        get
        {
            var value = _hostSourceTextBox.Text.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    private string L(string english, string zhHans) => _useSimplifiedChinese ? zhHans : english;

    private static TextBox CreateTextBox(string value) => new()
    {
        Dock = DockStyle.Fill,
        Text = value,
        Margin = new Padding(0, 0, 8, 8),
    };

    private static Button CreateSmallButton(string text, Action action)
    {
        var button = new Button
        {
            AutoSize = true,
            Text = text,
            Margin = new Padding(4, 0, 0, 8),
            Padding = new Padding(10, 4, 10, 4),
        };
        button.Click += (_, _) => action();
        return button;
    }

    private Button CreateBrowseButton(Action action) => CreateSmallButton(L("Browse", "\u6d4f\u89c8"), action);

    private static void AddLabeledRow(TableLayoutPanel fields, int row, string labelText, Control editor, Control? action)
    {
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 8, 8),
        };
        fields.Controls.Add(label, 0, row);
        fields.Controls.Add(editor, 1, row);
        if (action is not null)
        {
            fields.Controls.Add(action, 2, row);
        }
    }

    private void BrowseOutputRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = L("Select output folder for packaged app.", "\u9009\u62e9\u6253\u5305\u5e94\u7528\u8f93\u51fa\u76ee\u5f55\u3002"),
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(OutputRoot) ? OutputRoot : string.Empty,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputRootTextBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseHostSource()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = L("Select published OSCControl.AppHost folder.", "\u9009\u62e9\u5df2\u53d1\u5e03\u7684 OSCControl.AppHost \u76ee\u5f55\u3002"),
            ShowNewFolderButton = false,
            SelectedPath = HostSource is { Length: > 0 } hostSource && Directory.Exists(hostSource) ? hostSource : string.Empty,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _hostSourceTextBox.Text = dialog.SelectedPath;
        }
    }

    private void UseDetectedHostSource()
    {
        if (string.IsNullOrWhiteSpace(_detectedHostSource))
        {
            MessageBox.Show(this, L("No AppHost folder was auto-detected.", "\u6ca1\u6709\u81ea\u52a8\u68c0\u6d4b\u5230 AppHost \u76ee\u5f55\u3002"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _hostSourceTextBox.Text = _detectedHostSource;
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(AppName))
        {
            MessageBox.Show(this, L("App name is required.", "\u5e94\u7528\u540d\u79f0\u4e0d\u80fd\u4e3a\u7a7a\u3002"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _appNameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputRoot))
        {
            MessageBox.Show(this, L("Output folder is required.", "\u8f93\u51fa\u76ee\u5f55\u4e0d\u80fd\u4e3a\u7a7a\u3002"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _outputRootTextBox.Focus();
            return;
        }

        if (HostSource is { } hostSource && !Directory.Exists(hostSource))
        {
            MessageBox.Show(this, L("Host source folder does not exist.", "Host \u6765\u6e90\u76ee\u5f55\u4e0d\u5b58\u5728\u3002"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _hostSourceTextBox.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }
}