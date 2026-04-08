using System.ComponentModel;
using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Runtime;
using OSCControl.Packaging;

namespace OSCControl.DesktopHost;

internal sealed class MainForm : Form
{
    private readonly RuntimeAppController _controller = new();
    private readonly BlockDocument _blocks = BlockDocument.CreateDefault();
    private readonly BindingSource _endpointBindingSource = new();
    private readonly BindingSource _variableBindingSource = new();
    private readonly BindingSource _ruleBindingSource = new();
    private readonly BindingSource _stepBindingSource = new();
    private readonly TextBox _pathTextBox;
    private readonly TabControl _editorTabs;
    private readonly TabPage _scriptTab;
    private readonly TabPage _blocksTab;
    private readonly TextBox _editorTextBox;
    private TextBox _blocksPreviewTextBox = null!;
    private DglabSocketSession? _dglabSocketSession;
    private IDisposable? _dglabSocketSubscription;
    private CancellationTokenSource? _dglabSocketCts;
    private TextBox _dglabSocketHostTextBox = null!;
    private NumericUpDown _dglabSocketPortInput = null!;
    private TextBox _dglabSocketPathTextBox = null!;
    private TextBox _dglabSocketQrUrlTextBox = null!;
    private TextBox _dglabSocketCommandTextBox = null!;
    private CheckBox _dglabSocketUnsafeRawCheckBox = null!;
    private Label _dglabSocketStatusLabel = null!;
    private Button _dglabSocketConnectButton = null!;
    private Button _dglabSocketDisconnectButton = null!;
    private Button _dglabSocketSendButton = null!;
#if OSCCONTROL_BLOCKLY_WEBVIEW2
    private string _blocklyGeneratedSource = "on startup [\r\n    log info \"ready\"\r\n]\r\n";
    private string _blocklyWorkspaceJson = string.Empty;
    private BlocklyWebViewHost? _blocklyWebViewHost;
#endif
    private readonly ListView _diagnosticsView;
    private readonly RichTextBox _runtimeOutputTextBox;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly Button _reloadButton;
    private readonly Button _saveButton;
    private readonly Button _checkButton;
    private readonly Button _packageButton;
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private SplitContainer _blocksOuterSplit = null!;
    private SplitContainer _blocksConfigSplit = null!;
    private SplitContainer _blocksInnerSplit = null!;
    private SplitContainer _blocksRuleSplit = null!;
    private DataGridView _endpointGrid = null!;
    private DataGridView _variableGrid = null!;
    private ListBox _rulesListBox = null!;
    private ComboBox _triggerComboBox = null!;
    private TextBox _ruleEndpointTextBox = null!;
    private TextBox _ruleAddressTextBox = null!;
    private TextBox _ruleWhenTextBox = null!;
    private ListBox _stepsListBox = null!;
    private Label _stepHintLabel = null!;
    private ComboBox _stepKindComboBox = null!;
    private TextBox _stepTargetTextBox = null!;
    private TextBox _stepValueTextBox = null!;
    private ComboBox _stepPayloadComboBox = null!;
    private TextBox _stepExtraTextBox = null!;
    private Button _stepEnterBodyButton = null!;
    private Button _stepEnterElseButton = null!;
    private Button _stepBackButton = null!;
    private readonly Control _blocksEditorRoot;
    private readonly bool _useSimplifiedChinese = DesktopLocalization.UseSimplifiedChinese();
    private readonly Stack<StepContainerContext> _stepContainerStack = new();
    private bool _suppressBlockEvents;
    private bool _blockSplittersInitialized;

    public MainForm()
    {
        Text = L("OSCControl Desktop Host", "OSCControl Desktop Host");
        Width = 1360;
        Height = 880;
        MinimumSize = new Size(1080, 720);
        StartPosition = FormStartPosition.CenterScreen;

        var chromeFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        var monoFont = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        Font = chromeFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 8,
            Margin = new Padding(0, 0, 0, 12),
        };
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
        {
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }
        root.Controls.Add(topPanel, 0, 0);

        var pathLabel = new Label
        {
            Text = L("Script", "Script"),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 8, 8, 0),
        };
        topPanel.Controls.Add(pathLabel, 0, 0);

        _pathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
        };
        topPanel.Controls.Add(_pathTextBox, 1, 0);

        var browseButton = CreateButton(L("Open", "Open"), async (_, _) => await BrowseAsync());
        topPanel.Controls.Add(browseButton, 2, 0);

        _reloadButton = CreateButton(L("Reload", "Reload"), async (_, _) => await ReloadAsync());
        topPanel.Controls.Add(_reloadButton, 3, 0);

        _saveButton = CreateButton(L("Save", "Save"), async (_, _) => await SaveAsync());
        topPanel.Controls.Add(_saveButton, 4, 0);

        _checkButton = CreateButton(L("Check", "Check"), async (_, _) => await CheckAsync());
        topPanel.Controls.Add(_checkButton, 5, 0);

        _packageButton = CreateButton(L("Package App...", "Package App..."), async (_, _) => await PackageAppAsync());
        topPanel.Controls.Add(_packageButton, 6, 0);

        var runPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(8, 0, 0, 0),
        };
        topPanel.Controls.Add(runPanel, 7, 0);

        _startButton = CreateButton(L("Start Host", "Start Host"), async (_, _) => await StartAsync());
        _startButton.MinimumSize = new Size(96, 0);
        runPanel.Controls.Add(_startButton);

        _stopButton = CreateButton(L("Stop Host", "Stop Host"), async (_, _) => await StopAsync());
        _stopButton.MinimumSize = new Size(96, 0);
        _stopButton.BackColor = Color.FromArgb(220, 38, 38);
        _stopButton.ForeColor = Color.White;
        _stopButton.FlatStyle = FlatStyle.Flat;
        _stopButton.UseVisualStyleBackColor = false;
        _stopButton.Enabled = false;
        runPanel.Controls.Add(_stopButton);

        _editorTabs = new TabControl
        {
            Dock = DockStyle.Fill,
        };
        root.Controls.Add(_editorTabs, 0, 1);

        _scriptTab = new TabPage(L("Script", "Script"));
        _editorTabs.TabPages.Add(_scriptTab);

        _editorTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            AcceptsTab = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = monoFont,
            BorderStyle = BorderStyle.FixedSingle,
            Text = "on startup [\r\n    log info \"ready\"\r\n]\r\n",
        };
        _scriptTab.Controls.Add(_editorTextBox);

        _blocksTab = new TabPage(L("Blocks", "Blocks"));
        _editorTabs.TabPages.Add(_blocksTab);
#if OSCCONTROL_BLOCKLY_WEBVIEW2
        _blocksEditorRoot = CreateBlocklyWebViewEditor();
#else
        _blocksEditorRoot = CreateBlocksEditor(monoFont);
#endif
        _blocksTab.Controls.Add(_blocksEditorRoot);

        var dglabConnectionTab = new TabPage("DGLabConnection");
        dglabConnectionTab.Controls.Add(CreateDglabConnectionPanel());
        _editorTabs.TabPages.Add(dglabConnectionTab);

        var diagnosticsTab = new TabPage(L("Diagnostics", "Diagnostics"));
        _editorTabs.TabPages.Add(diagnosticsTab);

        _diagnosticsView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
        };
        _diagnosticsView.Columns.Add(L("Severity", "Severity"), 90);
        _diagnosticsView.Columns.Add(L("Line", "Line"), 60);
        _diagnosticsView.Columns.Add(L("Column", "Column"), 70);
        _diagnosticsView.Columns.Add(L("Message", "Message"), 900);
        diagnosticsTab.Controls.Add(_diagnosticsView);

        var runtimeTab = new TabPage(L("Runtime", "Runtime"));
        _editorTabs.TabPages.Add(runtimeTab);

        _runtimeOutputTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = monoFont,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
        runtimeTab.Controls.Add(_runtimeOutputTextBox);

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel(L("Idle", "Idle"));
        statusStrip.Items.Add(_statusLabel);
        Controls.Add(statusStrip);

        _controller.RuntimeOutput += message => PostToUi(() => AppendRuntimeOutput(message));
        _controller.StatusChanged += status => PostToUi(() => UpdateStatus(status));

#if OSCCONTROL_BLOCKLY_WEBVIEW2
        UpdateStatus(L("Blockly WebView2 editor is enabled. Use Send To Host in the Blocks tab to apply generated script.", "Blockly WebView2 editor is enabled. Use Send To Host in the Blocks tab to apply generated script."));
#else
        WireBlockEvents();
        UpdateBlocksPreview();
#endif
        RenderDiagnostics(_controller.Compile(_editorTextBox.Text));

        FormClosed += OnFormClosedAsync;
        Shown += (_, _) => ResetBottomSplit();
        Resize += (_, _) => ResetBottomSplit();
    }

    private BlockStep? SelectedStep => _stepsListBox.SelectedItem as BlockStep;
    private BindingList<BlockStep>? CurrentStepContainer => _stepContainerStack.Count == 0 ? null : _stepContainerStack.Peek().Steps;
    private bool IsAtRootStepContainer => _stepContainerStack.Count <= 1;

    private string L(string zhHans, string english) => _useSimplifiedChinese ? DesktopLocalization.Translate(english, zhHans) : english;

    private List<OptionItem<BlockEndpointTransport>> GetEndpointTransportOptions() => new()
    {
        new(BlockEndpointTransport.OscUdp, L("OSC UDP", "OSC UDP")),
        new(BlockEndpointTransport.WsClient, L("WebSocket Client", "WebSocket Client")),
        new(BlockEndpointTransport.WsServer, L("WebSocket Server", "WebSocket Server")),
        new(BlockEndpointTransport.Vrchat, "VRChat"),
    };

    private List<OptionItem<BlockEndpointMode>> GetEndpointModeOptions() => new()
    {
        new(BlockEndpointMode.Input, L("Input", "Input")),
        new(BlockEndpointMode.Output, L("Output", "Output")),
    };

    private List<OptionItem<BlockTriggerKind>> GetTriggerOptions() => new()
    {
        new(BlockTriggerKind.Startup, L("Startup", "Startup")),
        new(BlockTriggerKind.Receive, L("Receive", "Receive")),
        new(BlockTriggerKind.VrchatAvatarChange, L("VRChat Avatar Change", "VRChat Avatar Change")),
        new(BlockTriggerKind.VrchatParameter, L("VRChat Param", "VRChat Param")),
    };

    private List<OptionItem<BlockStepKind>> GetStepKindOptions() => new()
    {
        new(BlockStepKind.Log, L("Log", "Log")),
        new(BlockStepKind.Store, L("Store", "Store")),
        new(BlockStepKind.Send, L("Send", "Send")),
        new(BlockStepKind.Stop, L("Stop", "Stop")),
        new(BlockStepKind.If, L("If", "If")),
        new(BlockStepKind.While, L("While", "While")),
        new(BlockStepKind.Break, L("Break", "Break")),
        new(BlockStepKind.Continue, L("Continue", "Continue")),
        new(BlockStepKind.VrchatParam, L("VRChat Param", "VRChat Param")),
        new(BlockStepKind.VrchatInput, L("VRChat Input", "VRChat Input")),
        new(BlockStepKind.VrchatChat, L("VRChat Chatbox", "VRChat Chatbox")),
        new(BlockStepKind.VrchatTyping, L("VRChat Typing", "VRChat Typing")),
    };

    private List<OptionItem<BlockPayloadMode>> GetPayloadModeOptions() => new()
    {
        new(BlockPayloadMode.None, L("None", "None")),
        new(BlockPayloadMode.Args, "Args"),
        new(BlockPayloadMode.Body, "Body"),
    };

    private string FormatPayloadMode(BlockPayloadMode mode) => mode switch
    {
        BlockPayloadMode.None => L("None", "None"),
        BlockPayloadMode.Args => "Args",
        BlockPayloadMode.Body => "Body",
        _ => mode.ToString(),
    };

    private Control CreateDglabConnectionPanel()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var note = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(1280, 0),
            Text = "DG-LAB Socket and BLE connections run beside script runtime. Device sessions here are not generated into .osccontrol scripts.",
            ForeColor = Color.FromArgb(55, 55, 55),
            Margin = new Padding(0, 0, 0, 12),
        };
        root.Controls.Add(note, 0, 0);

        root.Controls.Add(CreateDglabSocketManualGroup(), 0, 1);
        root.Controls.Add(CreateDglabBleManualGroup(), 0, 2);

        return root;
    }

    private Control CreateDglabSocketManualGroup()
    {
        var group = new GroupBox
        {
            Text = "DG-LAB Socket",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 12),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 8,
        };
        for (var i = 0; i < 8; i++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        layout.Controls.Add(CreateFieldLabel(L("Host", "Host")), 0, 0);
        _dglabSocketHostTextBox = new TextBox { Text = "127.0.0.1", Width = 130, Margin = new Padding(0, 0, 12, 8) };
        layout.Controls.Add(_dglabSocketHostTextBox, 1, 0);

        layout.Controls.Add(CreateFieldLabel(L("Port", "Port")), 2, 0);
        _dglabSocketPortInput = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 5678, Width = 80, Margin = new Padding(0, 0, 12, 8) };
        layout.Controls.Add(_dglabSocketPortInput, 3, 0);

        layout.Controls.Add(CreateFieldLabel(L("Path", "Path")), 4, 0);
        _dglabSocketPathTextBox = new TextBox { Text = "/", Width = 100, Margin = new Padding(0, 0, 12, 8) };
        layout.Controls.Add(_dglabSocketPathTextBox, 5, 0);

        _dglabSocketConnectButton = CreateButton("Connect", async (_, _) => await ConnectDglabSocketAsync());
        layout.Controls.Add(_dglabSocketConnectButton, 6, 0);

        _dglabSocketDisconnectButton = CreateButton("Disconnect", async (_, _) => await DisconnectDglabSocketAsync());
        _dglabSocketDisconnectButton.Enabled = false;
        layout.Controls.Add(_dglabSocketDisconnectButton, 7, 0);

        layout.Controls.Add(CreateFieldLabel("Status"), 0, 1);
        _dglabSocketStatusLabel = new Label
        {
            Text = "Disconnected",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 8, 12, 8),
        };
        layout.Controls.Add(_dglabSocketStatusLabel, 1, 1);
        layout.SetColumnSpan(_dglabSocketStatusLabel, 7);

        layout.Controls.Add(CreateFieldLabel("QR URL"), 0, 2);
        _dglabSocketQrUrlTextBox = new TextBox
        {
            ReadOnly = true,
            Width = 720,
            Margin = new Padding(0, 0, 12, 8),
        };
        layout.Controls.Add(_dglabSocketQrUrlTextBox, 1, 2);
        layout.SetColumnSpan(_dglabSocketQrUrlTextBox, 7);

        layout.Controls.Add(CreateFieldLabel("Command"), 0, 3);
        _dglabSocketCommandTextBox = new TextBox
        {
            Text = "strength-1+2+50",
            Width = 520,
            Margin = new Padding(0, 0, 12, 8),
        };
        layout.Controls.Add(_dglabSocketCommandTextBox, 1, 3);
        layout.SetColumnSpan(_dglabSocketCommandTextBox, 5);

        _dglabSocketSendButton = CreateButton("Send", async (_, _) => await SendDglabSocketCommandAsync());
        _dglabSocketSendButton.Enabled = false;
        layout.Controls.Add(_dglabSocketSendButton, 6, 3);

        layout.Controls.Add(CreateFieldLabel("Advanced"), 0, 4);
        _dglabSocketUnsafeRawCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Allow unsafe raw command (advanced)",
            Margin = new Padding(0, 2, 12, 0),
        };
        layout.Controls.Add(_dglabSocketUnsafeRawCheckBox, 1, 4);
        layout.SetColumnSpan(_dglabSocketUnsafeRawCheckBox, 3);

        var advancedHint = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            ForeColor = Color.FromArgb(120, 70, 0),
            Text = "Only enable this for reviewed DG-LAB commands outside the validated strength / clear / pulse set.",
            Margin = new Padding(0, 4, 12, 0),
        };
        layout.Controls.Add(advancedHint, 4, 4);
        layout.SetColumnSpan(advancedHint, 4);

        group.Controls.Add(layout);
        return group;
    }
    private Control CreateDglabBleManualGroup()
    {
        var group = new GroupBox
        {
            Text = "DG-LAB BLE",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 12),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 5,
        };
        for (var i = 0; i < 5; i++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        var scan = CreateButton("Scan", (_, _) => UpdateStatus("DG-LAB BLE scan is reserved for the next implementation step."));
        scan.Enabled = false;
        layout.Controls.Add(scan, 0, 0);

        var devices = new ComboBox { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false, Margin = new Padding(8, 0, 12, 8) };
        devices.Items.Add("No device scanned");
        devices.SelectedIndex = 0;
        layout.Controls.Add(devices, 1, 0);

        var connect = CreateButton("Connect", (_, _) => { });
        connect.Enabled = false;
        layout.Controls.Add(connect, 2, 0);

        var disconnect = CreateButton("Disconnect", (_, _) => { });
        disconnect.Enabled = false;
        layout.Controls.Add(disconnect, 3, 0);

        var status = new Label
        {
            Text = "Not connected",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(12, 8, 0, 0),
        };
        layout.Controls.Add(status, 4, 0);

        group.Controls.Add(layout);
        return group;
    }
#if OSCCONTROL_BLOCKLY_WEBVIEW2
    private Control CreateBlocklyWebViewEditor()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var noteLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(1280, 0),
            Text = L("Blockly WebView2 editor is experimental. Generate script in Blockly, click Send To Host, then use Check/Save/Start Host/Stop Host from the desktop host.", "Blockly WebView2 editor is experimental. Generate script in Blockly, click Send To Host, then use Check/Save/Start Host/Stop Host from the desktop host."),
            ForeColor = Color.FromArgb(55, 55, 55),
            Margin = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(noteLabel, 0, 0);

        var host = new BlocklyWebViewHost(BlocklyEditorAssets.GetIndexPath());
        _blocklyWebViewHost = host;
        host.GeneratedScriptReceived += (_, script) =>
        {
            _blocklyGeneratedSource = string.IsNullOrWhiteSpace(script.Source) ? _blocklyGeneratedSource : script.Source;
            _blocklyWorkspaceJson = script.WorkspaceJson;
            _editorTextBox.Text = _blocklyGeneratedSource;
            RenderDiagnostics(_controller.Compile(_blocklyGeneratedSource));
            var reason = string.IsNullOrWhiteSpace(script.Reason) ? "sync" : script.Reason.Trim();
            UpdateStatus($"Blockly {reason}: {_blocklyGeneratedSource.Length} chars generated. Check/Save/Start Host now uses this script while Blocks is selected.");
        };
        root.Controls.Add(host, 0, 1);

        return root;
    }
#endif
    private Control CreateBlocksEditor(Font monoFont)
    {
        _endpointBindingSource.DataSource = _blocks.Endpoints;
        _variableBindingSource.DataSource = _blocks.Variables;
        _ruleBindingSource.DataSource = _blocks.Rules;
        _stepBindingSource.DataSource = new BindingList<BlockStep>();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var noteLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(1280, 0),
            Text = L("Blocks is a one-way visual generator for now. Build endpoints, triggers, and steps here, then apply the generated script to the Script tab when you want to save or fine-tune it.", "Blocks is a one-way visual generator for now. Build endpoints, triggers, and steps here, then apply the generated script to the Script tab when you want to save or fine-tune it."),
            ForeColor = Color.FromArgb(55, 55, 55),
            Margin = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(noteLabel, 0, 0);

        var toolbar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(toolbar, 0, 1);

        toolbar.Controls.Add(CreateButton(L("Add Endpoint", "Add Endpoint"), (_, _) => AddEndpoint()));
        toolbar.Controls.Add(CreateButton(L("Add VRChat Endpoint", "Add VRChat Endpoint"), (_, _) => AddVrchatEndpoint()));
        toolbar.Controls.Add(CreateButton(L("Remove Endpoint", "Remove Endpoint"), (_, _) => RemoveSelectedEndpoint()));
        toolbar.Controls.Add(CreateButton(L("Add Variable", "Add Variable"), (_, _) => AddVariable()));
        toolbar.Controls.Add(CreateButton(L("Remove Variable", "Remove Variable"), (_, _) => RemoveSelectedVariable()));
        toolbar.Controls.Add(CreateButton(L("Import Script To Blocks", "Import Script To Blocks"), (_, _) => ImportScriptToBlocks()));
        toolbar.Controls.Add(CreateButton(L("Preview Script", "Preview Script"), (_, _) => UpdateBlocksPreview()));
        toolbar.Controls.Add(CreateButton(L("Apply To Script", "Apply To Script"), (_, _) => ApplyBlocksToScript()));

        _blocksOuterSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.FixedSingle,
            SplitterWidth = 8,
        };
        root.Controls.Add(_blocksOuterSplit, 0, 2);

        _blocksConfigSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BorderStyle = BorderStyle.None,
            SplitterWidth = 8,
        };
        _blocksOuterSplit.Panel1.Controls.Add(_blocksConfigSplit);

        var endpointsGroup = new GroupBox
        {
            Text = L("Endpoints", "Endpoints"),
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
        };
        _blocksConfigSplit.Panel1.Controls.Add(endpointsGroup);

        _endpointGrid = CreateGrid();
        _endpointGrid.DataSource = _endpointBindingSource;
        _endpointGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockEndpoint.Name), HeaderText = L("Name", "Name"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 18 });
        _endpointGrid.Columns.Add(new DataGridViewComboBoxColumn { DataPropertyName = nameof(BlockEndpoint.Transport), HeaderText = L("Transport", "Transport"), DataSource = GetEndpointTransportOptions(), DisplayMember = nameof(OptionItem<BlockEndpointTransport>.Label), ValueMember = nameof(OptionItem<BlockEndpointTransport>.Value), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 16 });
        _endpointGrid.Columns.Add(new DataGridViewComboBoxColumn { DataPropertyName = nameof(BlockEndpoint.Mode), HeaderText = L("Mode", "Mode"), DataSource = GetEndpointModeOptions(), DisplayMember = nameof(OptionItem<BlockEndpointMode>.Label), ValueMember = nameof(OptionItem<BlockEndpointMode>.Value), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12 });
        _endpointGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockEndpoint.Host), HeaderText = L("Host", "Host"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 18 });
        _endpointGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockEndpoint.Port), HeaderText = L("Port", "Port"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 10 });
        _endpointGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockEndpoint.InputPort), HeaderText = L("Input Port", "Input Port"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 10 });
        _endpointGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockEndpoint.Path), HeaderText = L("Path", "Path"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12 });
        _endpointGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockEndpoint.Codec), HeaderText = L("Codec", "Codec"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12 });
        endpointsGroup.Controls.Add(_endpointGrid);

        var variablesGroup = new GroupBox
        {
            Text = L("Variables", "Variables"),
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
        };
        _blocksConfigSplit.Panel2.Controls.Add(variablesGroup);

        _variableGrid = CreateGrid();
        _variableGrid.DataSource = _variableBindingSource;
        _variableGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockVariable.Name), HeaderText = L("Name", "Name"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 45 });
        _variableGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlockVariable.InitialValue), HeaderText = L("Initial", "Initial"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 55 });
        variablesGroup.Controls.Add(_variableGrid);
        _blocksInnerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.FixedSingle,
            SplitterWidth = 8,
        };
        _blocksOuterSplit.Panel2.Controls.Add(_blocksInnerSplit);

        _blocksRuleSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            SplitterWidth = 8,
        };
        _blocksInnerSplit.Panel1.Controls.Add(_blocksRuleSplit);

        var rulesGroup = new GroupBox { Text = L("Rules", "Rules"), Dock = DockStyle.Fill, Padding = new Padding(8) };
        _blocksRuleSplit.Panel1.Controls.Add(rulesGroup);

        var rulesLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        rulesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rulesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rulesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rulesGroup.Controls.Add(rulesLayout);

        _rulesListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            DataSource = _ruleBindingSource,
            IntegralHeight = false,
            HorizontalScrollbar = true,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 54,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
        };
        rulesLayout.Controls.Add(_rulesListBox, 0, 0);

        var ruleButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        ruleButtons.Controls.Add(CreateButton(L("Add Startup", "Add Startup"), (_, _) => AddRule(BlockTriggerKind.Startup)));
        ruleButtons.Controls.Add(CreateButton(L("Add Receive", "Add Receive"), (_, _) => AddRule(BlockTriggerKind.Receive)));
        ruleButtons.Controls.Add(CreateButton(L("Add Avatar Change", "Add Avatar Change"), (_, _) => AddRule(BlockTriggerKind.VrchatAvatarChange)));
        ruleButtons.Controls.Add(CreateButton(L("Add Param Trigger", "Add Param Trigger"), (_, _) => AddRule(BlockTriggerKind.VrchatParameter)));
        ruleButtons.Controls.Add(CreateButton(L("Move Up", "Move Up"), (_, _) => MoveSelectedRule(-1)));
        ruleButtons.Controls.Add(CreateButton(L("Move Down", "Move Down"), (_, _) => MoveSelectedRule(1)));
        ruleButtons.Controls.Add(CreateButton(L("Duplicate", "Duplicate"), (_, _) => DuplicateSelectedRule()));
        ruleButtons.Controls.Add(CreateButton(L("Delete", "Delete"), (_, _) => RemoveSelectedRule()));
        rulesLayout.Controls.Add(ruleButtons, 0, 1);

        var rulesHint = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Text = L("Rules are the block equivalent of event stacks. Drag the splitter if you want more room for this panel.", "Rules are the block equivalent of event stacks. Drag the splitter if you want more room for this panel."),
            ForeColor = Color.FromArgb(70, 70, 70),
            Margin = new Padding(0, 8, 0, 0),
        };
        rulesLayout.Controls.Add(rulesHint, 0, 2);

        var editorLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _blocksRuleSplit.Panel2.Controls.Add(editorLayout);
        var ruleGroup = new GroupBox { Text = L("Selected Rule", "Selected Rule"), Dock = DockStyle.Fill, Padding = new Padding(8) };
        editorLayout.Controls.Add(ruleGroup, 0, 0);

        var ruleLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
        ruleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ruleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ruleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ruleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ruleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ruleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ruleGroup.Controls.Add(ruleLayout);

        ruleLayout.Controls.Add(CreateFieldLabel(L("Trigger", "Trigger")), 0, 0);
        _triggerComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = GetTriggerOptions(), DisplayMember = nameof(OptionItem<BlockTriggerKind>.Label), ValueMember = nameof(OptionItem<BlockTriggerKind>.Value), Margin = new Padding(0, 0, 12, 8) };
        ruleLayout.Controls.Add(_triggerComboBox, 1, 0);

        ruleLayout.Controls.Add(CreateFieldLabel(L("Endpoint", "Endpoint")), 2, 0);
        _ruleEndpointTextBox = CreateFieldTextBox();
        ruleLayout.Controls.Add(_ruleEndpointTextBox, 3, 0);

        ruleLayout.Controls.Add(CreateFieldLabel(L("Address", "Address")), 0, 1);
        _ruleAddressTextBox = CreateFieldTextBox();
        ruleLayout.Controls.Add(_ruleAddressTextBox, 1, 1);

        ruleLayout.Controls.Add(CreateFieldLabel(L("When", "When")), 2, 1);
        _ruleWhenTextBox = CreateFieldTextBox();
        ruleLayout.Controls.Add(_ruleWhenTextBox, 3, 1);

        var stepsGroup = new GroupBox { Text = L("Steps", "Steps"), Dock = DockStyle.Fill, Padding = new Padding(8), Margin = new Padding(0, 8, 0, 0) };
        editorLayout.Controls.Add(stepsGroup, 0, 1);

        var stepsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        stepsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stepsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        stepsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stepsGroup.Controls.Add(stepsLayout);

        _stepHintLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(920, 0),
            ForeColor = Color.FromArgb(70, 70, 70),
            Margin = new Padding(0, 0, 0, 8),
        };
        stepsLayout.Controls.Add(_stepHintLabel, 0, 0);

        var stepsSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            SplitterWidth = 8,
        };
        stepsLayout.Controls.Add(stepsSplit, 0, 1);

        var stepsStackGroup = new GroupBox { Text = L("Stack", "Stack"), Dock = DockStyle.Fill, Padding = new Padding(8) };
        stepsSplit.Panel1.Controls.Add(stepsStackGroup);

        _stepsListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            DataSource = _stepBindingSource,
            IntegralHeight = false,
            HorizontalScrollbar = true,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 52,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
        };
        stepsStackGroup.Controls.Add(_stepsListBox);

        var stepEditorGroup = new GroupBox { Text = L("Selected Step", "Selected Step"), Dock = DockStyle.Fill, Padding = new Padding(8) };
        stepsSplit.Panel2.Controls.Add(stepEditorGroup);

        var stepEditorLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3 };
        stepEditorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        stepEditorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        stepEditorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        stepEditorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        stepEditorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stepEditorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stepEditorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stepEditorGroup.Controls.Add(stepEditorLayout);

        stepEditorLayout.Controls.Add(CreateFieldLabel(L("Step", "Step")), 0, 0);
        _stepKindComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = GetStepKindOptions(), DisplayMember = nameof(OptionItem<BlockStepKind>.Label), ValueMember = nameof(OptionItem<BlockStepKind>.Value), Margin = new Padding(0, 0, 12, 8) };
        stepEditorLayout.Controls.Add(_stepKindComboBox, 1, 0);

        stepEditorLayout.Controls.Add(CreateFieldLabel(L("Target", "Target")), 2, 0);
        _stepTargetTextBox = CreateFieldTextBox();
        stepEditorLayout.Controls.Add(_stepTargetTextBox, 3, 0);

        stepEditorLayout.Controls.Add(CreateFieldLabel(L("Value", "Value")), 0, 1);
        _stepValueTextBox = CreateFieldTextBox();
        stepEditorLayout.Controls.Add(_stepValueTextBox, 1, 1);

        stepEditorLayout.Controls.Add(CreateFieldLabel(L("Payload", "Payload")), 2, 1);
        _stepPayloadComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = GetPayloadModeOptions(), DisplayMember = nameof(OptionItem<BlockPayloadMode>.Label), ValueMember = nameof(OptionItem<BlockPayloadMode>.Value), Margin = new Padding(0, 0, 0, 8) };
        stepEditorLayout.Controls.Add(_stepPayloadComboBox, 3, 1);

        stepEditorLayout.Controls.Add(CreateFieldLabel(L("Extra", "Extra")), 0, 2);
        _stepExtraTextBox = CreateFieldTextBox();
        stepEditorLayout.Controls.Add(_stepExtraTextBox, 1, 2);
        stepEditorLayout.SetColumnSpan(_stepExtraTextBox, 3);

        var stepButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        stepButtons.Controls.Add(CreateButton(L("Add Log", "Add Log"), (_, _) => AddStep(BlockStepKind.Log)));
        stepButtons.Controls.Add(CreateButton(L("Add Store", "Add Store"), (_, _) => AddStep(BlockStepKind.Store)));
        stepButtons.Controls.Add(CreateButton(L("Add Send", "Add Send"), (_, _) => AddStep(BlockStepKind.Send)));
        stepButtons.Controls.Add(CreateButton(L("Add If", "Add If"), (_, _) => AddStep(BlockStepKind.If)));
        stepButtons.Controls.Add(CreateButton(L("Add While", "Add While"), (_, _) => AddStep(BlockStepKind.While)));
        stepButtons.Controls.Add(CreateButton(L("Add Break", "Add Break"), (_, _) => AddStep(BlockStepKind.Break)));
        stepButtons.Controls.Add(CreateButton(L("Add Continue", "Add Continue"), (_, _) => AddStep(BlockStepKind.Continue)));
        stepButtons.Controls.Add(CreateButton(L("Add Stop", "Add Stop"), (_, _) => AddStep(BlockStepKind.Stop)));
        stepButtons.Controls.Add(CreateButton(L("Add VRChat Param", "Add VRChat Param"), (_, _) => AddStep(BlockStepKind.VrchatParam)));
        stepButtons.Controls.Add(CreateButton(L("Add VRChat Input", "Add VRChat Input"), (_, _) => AddStep(BlockStepKind.VrchatInput)));
        stepButtons.Controls.Add(CreateButton(L("Add VRChat Chat", "Add VRChat Chat"), (_, _) => AddStep(BlockStepKind.VrchatChat)));
        stepButtons.Controls.Add(CreateButton(L("Add VRChat Typing", "Add VRChat Typing"), (_, _) => AddStep(BlockStepKind.VrchatTyping)));
        _stepEnterBodyButton = CreateButton(L("Enter Body", "Enter Body"), (_, _) => EnterSelectedStepBody());
        stepButtons.Controls.Add(_stepEnterBodyButton);
        _stepEnterElseButton = CreateButton(L("Enter Else", "Enter Else"), (_, _) => EnterSelectedStepElseBody());
        stepButtons.Controls.Add(_stepEnterElseButton);
        _stepBackButton = CreateButton(L("Back", "Back"), (_, _) => ExitStepBody());
        stepButtons.Controls.Add(_stepBackButton);
        stepButtons.Controls.Add(CreateButton(L("Move Up", "Move Up"), (_, _) => MoveSelectedStep(-1)));
        stepButtons.Controls.Add(CreateButton(L("Move Down", "Move Down"), (_, _) => MoveSelectedStep(1)));
        stepButtons.Controls.Add(CreateButton(L("Duplicate", "Duplicate"), (_, _) => DuplicateSelectedStep()));
        stepButtons.Controls.Add(CreateButton(L("Delete", "Delete"), (_, _) => RemoveSelectedStep()));
        stepsLayout.Controls.Add(stepButtons, 0, 2);

        var previewGroup = new GroupBox { Text = L("Generated Script Preview", "Generated Script Preview"), Dock = DockStyle.Fill, Padding = new Padding(8) };
        _blocksInnerSplit.Panel2.Controls.Add(previewGroup);

        _blocksPreviewTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = monoFont,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
        previewGroup.Controls.Add(_blocksPreviewTextBox);

        if (_blocks.Rules.Count > 0)
        {
            _rulesListBox.SelectedItem = _blocks.Rules[0];
        }

        BindSelectedRule();
        return root;
    }

    private void WireBlockEvents()
    {
        _rulesListBox.SelectedIndexChanged += (_, _) => BindSelectedRule();
        _rulesListBox.DrawItem += DrawRuleItem;
        _triggerComboBox.SelectedIndexChanged += (_, _) => UpdateSelectedRuleFromEditor();
        _ruleEndpointTextBox.TextChanged += (_, _) => UpdateSelectedRuleFromEditor();
        _ruleAddressTextBox.TextChanged += (_, _) => UpdateSelectedRuleFromEditor();
        _ruleWhenTextBox.TextChanged += (_, _) => UpdateSelectedRuleFromEditor();

        _endpointGrid.CellValueChanged += (_, _) => UpdateBlocksPreview();
        _endpointGrid.RowsRemoved += (_, _) => UpdateBlocksPreview();
        _endpointGrid.CurrentCellDirtyStateChanged += (_, _) => CommitGridEdit(_endpointGrid);
        _endpointGrid.DataError += (_, _) => { };

        _variableGrid.CellValueChanged += (_, _) => UpdateBlocksPreview();
        _variableGrid.RowsRemoved += (_, _) => UpdateBlocksPreview();
        _variableGrid.CurrentCellDirtyStateChanged += (_, _) => CommitGridEdit(_variableGrid);
        _variableGrid.DataError += (_, _) => { };

        _stepsListBox.SelectedIndexChanged += (_, _) => BindSelectedStep();
        _stepsListBox.DrawItem += DrawStepItem;
        _stepKindComboBox.SelectedIndexChanged += (_, _) => UpdateSelectedStepFromEditor();
        _stepTargetTextBox.TextChanged += (_, _) => UpdateSelectedStepFromEditor();
        _stepValueTextBox.TextChanged += (_, _) => UpdateSelectedStepFromEditor();
        _stepPayloadComboBox.SelectedIndexChanged += (_, _) => UpdateSelectedStepFromEditor();
        _stepExtraTextBox.TextChanged += (_, _) => UpdateSelectedStepFromEditor();
    }

    private async Task ConnectDglabSocketAsync()
    {
        await DisconnectDglabSocketAsync();

        var endpoint = new RuntimeResolvedEndpoint(
            "dglab-manual",
            "dglab.socket",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["mode"] = "duplex",
                ["host"] = _dglabSocketHostTextBox.Text.Trim(),
                ["port"] = Convert.ToDouble(_dglabSocketPortInput.Value),
                ["path"] = string.IsNullOrWhiteSpace(_dglabSocketPathTextBox.Text) ? "/" : _dglabSocketPathTextBox.Text.Trim(),
                ["secure"] = false,
                ["codec"] = "json"
            });

        _dglabSocketCts = new CancellationTokenSource();
        _dglabSocketSession = new DglabSocketSession(endpoint);
        _dglabSocketSubscription = _dglabSocketSession.Subscribe(OnDglabSocketMessageAsync);

        _dglabSocketConnectButton.Enabled = false;
        _dglabSocketDisconnectButton.Enabled = true;
        _dglabSocketSendButton.Enabled = false;
        _dglabSocketUnsafeRawCheckBox.Checked = false;
        _dglabSocketQrUrlTextBox.Text = string.Empty;
        SetDglabSocketStatus("Connecting. Waiting for DG-LAB clientId...");

        try
        {
            await _dglabSocketSession.StartAsync(_dglabSocketCts.Token);
        }
        catch (Exception ex)
        {
            await DisconnectDglabSocketAsync();
            SetDglabSocketStatus("Connection failed: " + ex.Message);
            UpdateStatus("DG-LAB Socket connection failed: " + ex.Message);
        }
    }

    private async Task DisconnectDglabSocketAsync()
    {
        _dglabSocketCts?.Cancel();
        _dglabSocketSubscription?.Dispose();
        _dglabSocketSubscription = null;

        if (_dglabSocketSession is not null)
        {
            await _dglabSocketSession.DisposeAsync();
            _dglabSocketSession = null;
        }

        _dglabSocketCts?.Dispose();
        _dglabSocketCts = null;

        if (_dglabSocketConnectButton is not null)
        {
            _dglabSocketConnectButton.Enabled = true;
            _dglabSocketDisconnectButton.Enabled = false;
            _dglabSocketSendButton.Enabled = false;
            _dglabSocketUnsafeRawCheckBox.Checked = false;
            SetDglabSocketStatus("Disconnected");
        }
    }

    private async Task SendDglabSocketCommandAsync()
    {
        if (_dglabSocketSession is null)
        {
            SetDglabSocketStatus("Connect DG-LAB Socket first.");
            return;
        }

        var command = _dglabSocketCommandTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            SetDglabSocketStatus("Command is empty.");
            return;
        }

        var allowUnsafeRaw = _dglabSocketUnsafeRawCheckBox.Checked;

        try
        {
            await _dglabSocketSession.SendCommandAsync(command, CancellationToken.None, allowUnsafeRaw);
            var mode = allowUnsafeRaw ? "advanced raw" : "validated";
            SetDglabSocketStatus($"Sent ({mode}): {command}");
        }
        catch (Exception ex)
        {
            SetDglabSocketStatus("Send failed: " + ex.Message);
            UpdateStatus("DG-LAB Socket send failed: " + ex.Message);
        }
    }

    private Task OnDglabSocketMessageAsync(RuntimeEventMessage message, CancellationToken cancellationToken)
    {
        PostToUi(() => HandleDglabSocketMessage(message));
        return Task.CompletedTask;
    }

    private void HandleDglabSocketMessage(RuntimeEventMessage message)
    {
        var type = GetDglabBodyValue(message, "type");
        var clientId = GetDglabBodyValue(message, "clientId");
        var targetId = GetDglabBodyValue(message, "targetId");
        var text = GetDglabBodyValue(message, "message");
        var qrUrl = GetDglabBodyValue(message, "qrUrl") ?? (message.Extras.TryGetValue("qrUrl", out var rawQr) ? rawQr?.ToString() : null);

        if (!string.IsNullOrWhiteSpace(qrUrl))
        {
            _dglabSocketQrUrlTextBox.Text = qrUrl;
        }

        if (string.Equals(type, "bind", StringComparison.OrdinalIgnoreCase) && string.Equals(text, "targetId", StringComparison.OrdinalIgnoreCase))
        {
            SetDglabSocketStatus("Connected. Scan the QR URL in DG-LAB App. clientId=" + clientId);
            return;
        }

        if (string.Equals(type, "bind", StringComparison.OrdinalIgnoreCase) && string.Equals(text, "200", StringComparison.OrdinalIgnoreCase))
        {
            _dglabSocketSendButton.Enabled = true;
            SetDglabSocketStatus("Bound to DG-LAB App. targetId=" + targetId);
            return;
        }

        SetDglabSocketStatus($"Received {message.Address}: {text}");
    }

    private void SetDglabSocketStatus(string status)
    {
        if (_dglabSocketStatusLabel is not null)
        {
            _dglabSocketStatusLabel.Text = status;
        }

        UpdateStatus(status);
    }

    private static string? GetDglabBodyValue(RuntimeEventMessage message, string key)
    {
        return message.Body is IReadOnlyDictionary<string, object?> body && body.TryGetValue(key, out var value)
            ? value?.ToString()
            : null;
    }
    private async void OnFormClosedAsync(object? sender, FormClosedEventArgs e)
    {
        Enabled = false;
        await DisconnectDglabSocketAsync();
        await _controller.DisposeAsync();
    }

    private async Task BrowseAsync()
    {
        using var dialog = new OpenFileDialog { Filter = "OSCControl Files (*.osccontrol)|*.osccontrol|All Files (*.*)|*.*", CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _pathTextBox.Text = dialog.FileName;
        _editorTextBox.Text = await File.ReadAllTextAsync(dialog.FileName);
#if OSCCONTROL_BLOCKLY_WEBVIEW2
        _blocklyGeneratedSource = _editorTextBox.Text;
        var restoredBlocks = await LoadBlocklyWorkspaceForScriptAsync(dialog.FileName);
        _editorTabs.SelectedTab = restoredBlocks ? _blocksTab : _scriptTab;
        UpdateStatus(restoredBlocks
            ? L("Loaded script and restored Blockly workspace.", "Loaded script and restored Blockly workspace.")
            : L("Loaded script into Script tab. No Blockly workspace sidecar was found.", "Loaded script into Script tab. No Blockly workspace sidecar was found."));
#else
        _editorTabs.SelectedTab = _scriptTab;
        UpdateStatus(L("Loaded script into Script tab. Blocks draft was left unchanged.", "Loaded script into Script tab. Blocks draft was left unchanged."));
#endif
        await CheckAsync();
    }

    private async Task ReloadAsync()
    {
        var path = _pathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            UpdateStatus(L("Select a script file first", "Select a script file first"));
            return;
        }

        _editorTextBox.Text = await File.ReadAllTextAsync(path);
#if OSCCONTROL_BLOCKLY_WEBVIEW2
        _blocklyGeneratedSource = _editorTextBox.Text;
        var restoredBlocks = await LoadBlocklyWorkspaceForScriptAsync(path);
        _editorTabs.SelectedTab = restoredBlocks ? _blocksTab : _scriptTab;
        UpdateStatus(restoredBlocks
            ? L("Reloaded script and restored Blockly workspace.", "Reloaded script and restored Blockly workspace.")
            : L("Reloaded script into Script tab. No Blockly workspace sidecar was found.", "Reloaded script into Script tab. No Blockly workspace sidecar was found."));
#else
        _editorTabs.SelectedTab = _scriptTab;
        UpdateStatus(L("Reloaded script into Script tab. Blocks draft was left unchanged.", "Reloaded script into Script tab. Blocks draft was left unchanged."));
#endif
        await CheckAsync();
    }

    private async Task SaveAsync()
    {
        var path = _pathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            using var dialog = new SaveFileDialog { Filter = "OSCControl Files (*.osccontrol)|*.osccontrol|All Files (*.*)|*.*", FileName = "script.osccontrol" };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            path = dialog.FileName;
            _pathTextBox.Text = path;
        }

        var source = GetCurrentSource();
        await File.WriteAllTextAsync(path, source);
        if (_editorTabs.SelectedTab == _blocksTab)
        {
            _editorTextBox.Text = source;
#if OSCCONTROL_BLOCKLY_WEBVIEW2
            await SaveBlocklyWorkspaceForScriptAsync(path);
#endif
        }

        UpdateStatus(L("Saved", "Saved"));
    }

    private Task CheckAsync()
    {
        var result = _controller.Compile(GetCurrentSource());
        RenderDiagnostics(result);
        UpdateStatus(result.HasErrors ? L("Diagnostics found", "Diagnostics found") : L("Ready", "Ready"));
        return Task.CompletedTask;
    }

    private async Task PackageAppAsync()
    {
        await SetBusyAsync(true);
        try
        {
            var source = GetCurrentSource();
            var compileResult = _controller.Compile(source);
            RenderDiagnostics(compileResult);
            if (compileResult.HasErrors)
            {
                UpdateStatus(L("Fix diagnostics before packaging.", "Fix diagnostics before packaging."));
                return;
            }

            var scriptPath = _pathTextBox.Text.Trim();
            var appName = string.IsNullOrWhiteSpace(scriptPath)
                ? "OSCControlApp"
                : Path.GetFileNameWithoutExtension(scriptPath);
            var defaultOutputRoot = GetDefaultPackageOutputRoot(scriptPath);
            var detectedHostSource = FindAppHostSourceDirectory();

            using var dialog = new PackageAppDialog(appName, defaultOutputRoot, detectedHostSource, _useSimplifiedChinese);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var package = await new PackagedAppBuilder().BuildAsync(new PackageBuildRequest
            {
                Source = source,
                ScriptPath = string.IsNullOrWhiteSpace(scriptPath) ? null : scriptPath,
                OutputRoot = dialog.OutputRoot,
                AppName = dialog.AppName,
                HostSource = dialog.HostSource
            });

            var hostMessage = package.HostCopied
                ? L("Host files copied.", "Host files copied.")
                : L("Host files were not found; copy OSCControl.AppHost into the host folder before distribution.", "Host files were not found; copy OSCControl.AppHost into the host folder before distribution.");
            AppendRuntimeOutput($"{L("Packaged app", "Packaged app")}: {package.AppRoot}");
            AppendRuntimeOutput(hostMessage);
            UpdateStatus($"{L("Packaged app", "Packaged app")}: {package.AppRoot}");
            MessageBox.Show(this, $"{L("Packaged app", "Packaged app")}:\r\n{package.AppRoot}\r\n\r\n{hostMessage}", L("Package App", "Package App"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (PackageBuildException ex)
        {
            foreach (var diagnostic in ex.Diagnostics)
            {
                AppendRuntimeOutput($"{diagnostic.Severity} {diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}: {diagnostic.Message}");
            }

            UpdateStatus(L("Package failed", "Package failed"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException or InvalidOperationException)
        {
            AppendRuntimeOutput($"{L("Package failed", "Package failed")}: {ex.Message}");
            UpdateStatus(L("Package failed", "Package failed"));
        }
        finally
        {
            SetEditingEnabled(true);
        }
    }

    private async Task StartAsync()
    {
        await SetBusyAsync(true);
        try
        {
            _runtimeOutputTextBox.Clear();
            var result = await _controller.StartAsync(GetCurrentSource(), CancellationToken.None);
            RenderDiagnostics(result);
            if (result.HasErrors)
            {
                _stopButton.Enabled = false;
                _startButton.Enabled = true;
                _statusLabel.Text = L("Compile failed", "Compile failed");
                return;
            }

            _startButton.Enabled = false;
            _stopButton.Enabled = true;
        }
        catch (Exception ex)
        {
            AppendRuntimeOutput($"{L("Runtime start failed", "Runtime start failed")}: {ex.Message}");
            UpdateStatus(L("Start failed", "Start failed"));
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
        }
        finally
        {
            SetEditingEnabled(true);
        }
    }

    private async Task StopAsync()
    {
        await SetBusyAsync(true);
        try
        {
            await _controller.StopAsync();
            AppendRuntimeOutput(L("Runtime stopped.", "Runtime stopped."));
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
        }
        finally
        {
            SetEditingEnabled(true);
        }
    }

    private void RenderDiagnostics(CompilationResult result)
    {
        _diagnosticsView.BeginUpdate();
        try
        {
            _diagnosticsView.Items.Clear();
            foreach (var diagnostic in result.Diagnostics)
            {
                var item = new ListViewItem(diagnostic.Severity.ToString());
                item.SubItems.Add(diagnostic.Span.Start.Line.ToString());
                item.SubItems.Add(diagnostic.Span.Start.Column.ToString());
                item.SubItems.Add(diagnostic.Message);
                item.ForeColor = diagnostic.Severity == DiagnosticSeverity.Error ? Color.Firebrick : Color.DarkGoldenrod;
                _diagnosticsView.Items.Add(item);
            }

            if (result.Diagnostics.Count == 0)
            {
                var item = new ListViewItem(L("Info", "Info"));
                item.SubItems.Add("-");
                item.SubItems.Add("-");
                item.SubItems.Add(L("No diagnostics.", "No diagnostics."));
                item.ForeColor = Color.SeaGreen;
                _diagnosticsView.Items.Add(item);
            }
        }
        finally
        {
            _diagnosticsView.EndUpdate();
        }
    }

    private void AppendRuntimeOutput(string message)
    {
        _runtimeOutputTextBox.AppendText(message + Environment.NewLine);
        _runtimeOutputTextBox.SelectionStart = _runtimeOutputTextBox.TextLength;
        _runtimeOutputTextBox.ScrollToCaret();
    }

    private void UpdateStatus(string status)
    {
        _statusLabel.Text = status;
    }

    private Button CreateButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12, 4, 12, 4) };
        button.Click += handler;
        return button;
    }

    private static Label CreateFieldLabel(string text) => new() { Text = text, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 7, 8, 0) };

    private static TextBox CreateFieldTextBox() => new() { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }

    private void AddEndpoint()
    {
        _blocks.Endpoints.Add(new BlockEndpoint
        {
            Name = $"endpoint{_blocks.Endpoints.Count + 1}",
            Transport = BlockEndpointTransport.OscUdp,
            Mode = BlockEndpointMode.Input,
            Host = "127.0.0.1",
            Port = 9000 + _blocks.Endpoints.Count,
            Codec = "osc"
        });
        UpdateBlocksPreview();
    }

    private void AddVrchatEndpoint()
    {
        if (_blocks.Endpoints.Any(endpoint => endpoint.Transport == BlockEndpointTransport.Vrchat))
        {
            UpdateStatus(L("VRChat endpoint already exists.", "VRChat endpoint already exists."));
            return;
        }

        _blocks.Endpoints.Add(new BlockEndpoint
        {
            Name = "vrchat",
            Transport = BlockEndpointTransport.Vrchat,
            Mode = BlockEndpointMode.Output,
            Host = "127.0.0.1",
            Port = 9001,
            InputPort = 9000,
            Codec = "osc"
        });
        _endpointBindingSource.ResetBindings(false);
        UpdateBlocksPreview();
    }

    private void RemoveSelectedEndpoint()
    {
        if (_endpointGrid.CurrentRow?.DataBoundItem is not BlockEndpoint endpoint)
        {
            return;
        }

        _blocks.Endpoints.Remove(endpoint);
        UpdateBlocksPreview();
    }

    private void AddVariable()
    {
        var index = _blocks.Variables.Count + 1;
        var variable = new BlockVariable
        {
            Name = index == 1 ? "count" : $"variable{index}",
            InitialValue = "0"
        };

        _blocks.Variables.Add(variable);
        _variableBindingSource.ResetBindings(false);
        UpdateBlocksPreview();
    }

    private void RemoveSelectedVariable()
    {
        if (_variableGrid.CurrentRow?.DataBoundItem is not BlockVariable variable)
        {
            return;
        }

        _blocks.Variables.Remove(variable);
        _variableBindingSource.ResetBindings(false);
        UpdateBlocksPreview();
    }

    private void EnsureVrchatEndpoint()
    {
        if (_blocks.Endpoints.Any(endpoint => endpoint.Transport == BlockEndpointTransport.Vrchat))
        {
            return;
        }

        _blocks.Endpoints.Add(new BlockEndpoint
        {
            Name = "vrchat",
            Transport = BlockEndpointTransport.Vrchat,
            Mode = BlockEndpointMode.Output,
            Host = "127.0.0.1",
            Port = 9001,
            InputPort = 9000,
            Codec = "osc"
        });
        _endpointBindingSource.ResetBindings(false);
    }

    private void AddRule(BlockTriggerKind trigger)
    {
        if (trigger is BlockTriggerKind.VrchatAvatarChange or BlockTriggerKind.VrchatParameter)
        {
            EnsureVrchatEndpoint();
        }

        var rule = new BlockRule
        {
            Trigger = trigger,
            EndpointName = trigger switch
            {
                BlockTriggerKind.Receive => _blocks.Endpoints.FirstOrDefault(endpoint => endpoint.Transport != BlockEndpointTransport.Vrchat && endpoint.Mode == BlockEndpointMode.Input)?.Name
                    ?? _blocks.Endpoints.FirstOrDefault(endpoint => endpoint.Mode == BlockEndpointMode.Input)?.Name
                    ?? string.Empty,
                BlockTriggerKind.VrchatParameter => "GestureLeft",
                _ => string.Empty,
            },
            Address = trigger == BlockTriggerKind.Receive ? "/example" : string.Empty,
        };
        var step = CreateDefaultStep(BlockStepKind.Log);
        step.Value = trigger switch
        {
            BlockTriggerKind.Startup => "ready",
            BlockTriggerKind.VrchatAvatarChange => "avatar changed",
            BlockTriggerKind.VrchatParameter => "arg(0)",
            _ => "got message"
        };
        rule.Steps.Add(step);

        _blocks.Rules.Add(rule);
        RefreshRuleDisplay();
        _rulesListBox.SelectedItem = rule;
        UpdateBlocksPreview();
    }

    private void RemoveSelectedRule()
    {
        if (_rulesListBox.SelectedItem is not BlockRule rule)
        {
            return;
        }

        var index = _rulesListBox.SelectedIndex;
        _blocks.Rules.Remove(rule);
        RefreshRuleDisplay();
        if (_blocks.Rules.Count > 0)
        {
            _rulesListBox.SelectedIndex = Math.Min(index, _blocks.Rules.Count - 1);
        }
        else
        {
            BindSelectedRule();
        }

        UpdateBlocksPreview();
    }

    private void MoveSelectedRule(int offset)
    {
        if (_rulesListBox.SelectedItem is not BlockRule rule)
        {
            return;
        }

        var currentIndex = _blocks.Rules.IndexOf(rule);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = currentIndex + offset;
        if (targetIndex < 0 || targetIndex >= _blocks.Rules.Count)
        {
            return;
        }

        _blocks.Rules.RemoveAt(currentIndex);
        _blocks.Rules.Insert(targetIndex, rule);
        RefreshRuleDisplay();
        _rulesListBox.SelectedItem = rule;
        UpdateBlocksPreview();
    }

    private void DuplicateSelectedRule()
    {
        if (_rulesListBox.SelectedItem is not BlockRule rule)
        {
            return;
        }

        var clone = CloneRule(rule);
        var insertIndex = _blocks.Rules.IndexOf(rule) + 1;
        _blocks.Rules.Insert(insertIndex, clone);
        RefreshRuleDisplay();
        _rulesListBox.SelectedItem = clone;
        UpdateBlocksPreview();
    }

    private void AddStep(BlockStepKind kind)
    {
        if (_rulesListBox.SelectedItem is not BlockRule)
        {
            return;
        }

        if (kind is BlockStepKind.VrchatParam or BlockStepKind.VrchatInput or BlockStepKind.VrchatChat or BlockStepKind.VrchatTyping)
        {
            EnsureVrchatEndpoint();
        }

        var container = CurrentStepContainer;
        if (container is null)
        {
            return;
        }

        var step = CreateDefaultStep(kind);
        container.Add(step);
        RefreshStepDisplay();
        SelectStep(step);
        RefreshRuleDisplay();
        UpdateBlocksPreview();
    }

    private void RemoveSelectedStep()
    {
        if (CurrentStepContainer is not { } container || SelectedStep is not BlockStep step)
        {
            return;
        }

        var index = container.IndexOf(step);
        container.Remove(step);
        RefreshStepDisplay();
        if (container.Count > 0)
        {
            SelectStep(container[Math.Min(index, container.Count - 1)]);
        }
        else
        {
            BindSelectedStep();
        }

        RefreshRuleDisplay();
        UpdateBlocksPreview();
    }

    private void MoveSelectedStep(int offset)
    {
        if (CurrentStepContainer is not { } container || SelectedStep is not BlockStep step)
        {
            return;
        }

        var currentIndex = container.IndexOf(step);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = currentIndex + offset;
        if (targetIndex < 0 || targetIndex >= container.Count)
        {
            return;
        }

        container.RemoveAt(currentIndex);
        container.Insert(targetIndex, step);
        RefreshStepDisplay();
        SelectStep(step);
        RefreshRuleDisplay();
        UpdateBlocksPreview();
    }

    private void DuplicateSelectedStep()
    {
        if (CurrentStepContainer is not { } container || SelectedStep is not BlockStep step)
        {
            return;
        }

        var clone = CloneStep(step);
        var insertIndex = container.IndexOf(step) + 1;
        container.Insert(insertIndex, clone);
        RefreshStepDisplay();
        SelectStep(clone);
        RefreshRuleDisplay();
        UpdateBlocksPreview();
    }

    private static BlockRule CloneRule(BlockRule source)
    {
        var clone = new BlockRule
        {
            Trigger = source.Trigger,
            EndpointName = source.EndpointName,
            Address = source.Address,
            WhenExpression = source.WhenExpression,
        };

        foreach (var step in source.Steps)
        {
            clone.Steps.Add(CloneStep(step));
        }

        return clone;
    }

    private static BlockStep CloneStep(BlockStep source)
    {
        var clone = new BlockStep
        {
            Kind = source.Kind,
            Target = source.Target,
            Value = source.Value,
            PayloadMode = source.PayloadMode,
            Extra = source.Extra,
        };

        foreach (var child in source.Children)
        {
            clone.Children.Add(CloneStep(child));
        }

        foreach (var child in source.ElseChildren)
        {
            clone.ElseChildren.Add(CloneStep(child));
        }

        return clone;
    }

    private static BlockStep CreateDefaultStep(BlockStepKind kind) => kind switch
    {
        BlockStepKind.Log => new BlockStep { Kind = BlockStepKind.Log, Target = "info", Value = "next step" },
        BlockStepKind.Store => new BlockStep { Kind = BlockStepKind.Store, Target = "state_value", Value = "arg(0)" },
        BlockStepKind.Send => new BlockStep { Kind = BlockStepKind.Send, Target = string.Empty, Value = "/example", PayloadMode = BlockPayloadMode.Args, Extra = "1, 2, 3" },
        BlockStepKind.If => CreateDefaultIfStep(),
        BlockStepKind.While => CreateDefaultWhileStep(),
        BlockStepKind.Break => new BlockStep { Kind = BlockStepKind.Break },
        BlockStepKind.Continue => new BlockStep { Kind = BlockStepKind.Continue },
        BlockStepKind.Stop => new BlockStep { Kind = BlockStepKind.Stop },
        BlockStepKind.VrchatParam => new BlockStep { Kind = BlockStepKind.VrchatParam, Target = "GestureLeft", Value = "3" },
        BlockStepKind.VrchatInput => new BlockStep { Kind = BlockStepKind.VrchatInput, Target = "Jump", Value = "1" },
        BlockStepKind.VrchatChat => new BlockStep { Kind = BlockStepKind.VrchatChat, Value = "Hello from OSCControl", Extra = "send=true notify=false" },
        BlockStepKind.VrchatTyping => new BlockStep { Kind = BlockStepKind.VrchatTyping, Value = "true" },
        _ => new BlockStep { Kind = kind }
    };

    private static BlockStep CreateDefaultIfStep()
    {
        var step = new BlockStep
        {
            Kind = BlockStepKind.If,
            Value = "arg(0) > 0",
        };
        step.Children.Add(new BlockStep { Kind = BlockStepKind.Log, Target = "info", Value = "condition matched" });
        step.ElseChildren.Add(new BlockStep { Kind = BlockStepKind.Log, Target = "info", Value = "condition not matched" });
        return step;
    }
    private static BlockStep CreateDefaultWhileStep()
    {
        var step = new BlockStep
        {
            Kind = BlockStepKind.While,
            Value = "state(\"count\") < 3",
        };
        step.Children.Add(new BlockStep { Kind = BlockStepKind.Break });
        return step;
    }

    private void BindSelectedRule()
    {
        _suppressBlockEvents = true;
        try
        {
            if (_rulesListBox.SelectedItem is not BlockRule rule)
            {
                _triggerComboBox.Enabled = false;
                _ruleEndpointTextBox.Enabled = false;
                _ruleAddressTextBox.Enabled = false;
                _ruleWhenTextBox.Enabled = false;
                _triggerComboBox.SelectedValue = BlockTriggerKind.Startup;
                _ruleEndpointTextBox.Text = string.Empty;
                _ruleAddressTextBox.Text = string.Empty;
                _ruleWhenTextBox.Text = string.Empty;
                _stepContainerStack.Clear();
                _stepBindingSource.DataSource = new BindingList<BlockStep>();
                _stepsListBox.DataSource = _stepBindingSource;
                _stepsListBox.ClearSelected();
                UpdateStepNavigationButtons();
                return;
            }

            _triggerComboBox.Enabled = true;
            _ruleEndpointTextBox.Enabled = true;
            _ruleAddressTextBox.Enabled = true;
            _ruleWhenTextBox.Enabled = true;
            _triggerComboBox.SelectedValue = rule.Trigger;
            _ruleEndpointTextBox.Text = rule.EndpointName;
            _ruleAddressTextBox.Text = rule.Address;
            _ruleWhenTextBox.Text = rule.WhenExpression;
            ResetToRuleStepContainer(rule);
        }
        finally
        {
            _suppressBlockEvents = false;
        }

        if (_rulesListBox.SelectedItem is BlockRule selectedRule && selectedRule.Steps.Count > 0)
        {
            SelectStep(selectedRule.Steps[0]);
        }
        else
        {
            BindSelectedStep();
        }
    }

    private void BindSelectedStep()
    {
        _suppressBlockEvents = true;
        try
        {
            if (SelectedStep is not BlockStep step)
            {
                _stepKindComboBox.Enabled = false;
                _stepTargetTextBox.Enabled = false;
                _stepValueTextBox.Enabled = false;
                _stepPayloadComboBox.Enabled = false;
                _stepExtraTextBox.Enabled = false;
                _stepKindComboBox.SelectedValue = BlockStepKind.Log;
                _stepTargetTextBox.Text = string.Empty;
                _stepValueTextBox.Text = string.Empty;
                _stepPayloadComboBox.SelectedValue = BlockPayloadMode.None;
                _stepExtraTextBox.Text = string.Empty;
                UpdateStepNavigationButtons();
                UpdateStepHint();
                return;
            }

            _stepKindComboBox.SelectedValue = step.Kind;
            _stepTargetTextBox.Text = step.Target;
            _stepValueTextBox.Text = step.Value;
            _stepPayloadComboBox.SelectedValue = step.PayloadMode;
            _stepExtraTextBox.Text = step.Extra;
            ApplyStepEditorState(step.Kind);
        }
        finally
        {
            _suppressBlockEvents = false;
        }

        UpdateStepNavigationButtons();
        UpdateStepHint();
    }

    private void ApplyStepEditorState(BlockStepKind kind)
    {
        var hasStep = SelectedStep is not null;
        _stepKindComboBox.Enabled = hasStep;
        _stepTargetTextBox.Enabled = hasStep && kind is BlockStepKind.Log or BlockStepKind.Store or BlockStepKind.Send or BlockStepKind.VrchatParam or BlockStepKind.VrchatInput;
        _stepValueTextBox.Enabled = hasStep && kind is BlockStepKind.Log or BlockStepKind.Store or BlockStepKind.Send or BlockStepKind.If or BlockStepKind.While or BlockStepKind.VrchatParam or BlockStepKind.VrchatInput or BlockStepKind.VrchatChat or BlockStepKind.VrchatTyping;
        _stepPayloadComboBox.Enabled = hasStep && kind == BlockStepKind.Send;
        _stepExtraTextBox.Enabled = hasStep && kind is BlockStepKind.Send or BlockStepKind.VrchatChat;
    }

    private void UpdateSelectedRuleFromEditor()
    {
        if (_suppressBlockEvents || _rulesListBox.SelectedItem is not BlockRule rule)
        {
            return;
        }

        rule.Trigger = _triggerComboBox.SelectedValue is BlockTriggerKind trigger ? trigger : BlockTriggerKind.Startup;
        rule.EndpointName = _ruleEndpointTextBox.Text;
        rule.Address = _ruleAddressTextBox.Text;
        rule.WhenExpression = _ruleWhenTextBox.Text;
        RefreshRuleDisplay();
        UpdateBlocksPreview();
    }

    private void UpdateSelectedStepFromEditor()
    {
        if (_suppressBlockEvents || SelectedStep is not BlockStep step)
        {
            return;
        }

        var previousKind = step.Kind;
        step.Kind = _stepKindComboBox.SelectedValue is BlockStepKind kind ? kind : step.Kind;
        step.Target = _stepTargetTextBox.Text;
        step.Value = _stepValueTextBox.Text;
        step.PayloadMode = _stepPayloadComboBox.SelectedValue is BlockPayloadMode payloadMode ? payloadMode : BlockPayloadMode.None;
        step.Extra = _stepExtraTextBox.Text;

        if (step.Kind != BlockStepKind.Send)
        {
            step.PayloadMode = BlockPayloadMode.None;
        }

        if (step.Kind is BlockStepKind.Stop or BlockStepKind.Break or BlockStepKind.Continue)
        {
            step.Target = string.Empty;
            step.Value = string.Empty;
            step.Extra = string.Empty;
        }

        if (step.Kind is BlockStepKind.If or BlockStepKind.While)
        {
            step.Target = string.Empty;
            step.PayloadMode = BlockPayloadMode.None;
            step.Extra = string.Empty;
            if (previousKind != step.Kind)
            {
                step.Children.Clear();
                step.ElseChildren.Clear();
            }

            if (step.Children.Count == 0)
            {
                if (step.Kind == BlockStepKind.While)
                {
                    step.Children.Add(new BlockStep { Kind = BlockStepKind.Break });
                }
                else
                {
                    step.Children.Add(new BlockStep { Kind = BlockStepKind.Log, Target = "info", Value = "condition matched" });
                }
            }
        }
        else if (previousKind is BlockStepKind.If or BlockStepKind.While)
        {
            step.Children.Clear();
            step.ElseChildren.Clear();
        }

        if (step.Kind != BlockStepKind.VrchatChat && step.Kind != BlockStepKind.Send)
        {
            step.Extra = string.Empty;
        }

        ApplyStepEditorState(step.Kind);
        RefreshStepDisplay();
        RefreshRuleDisplay();
        UpdateStepNavigationButtons();
        UpdateStepHint();
        UpdateBlocksPreview();
    }

    private void SelectStep(BlockStep step)
    {
        _stepsListBox.SelectedItem = step;
        BindSelectedStep();
    }

    private void RefreshRuleDisplay()
    {
        _ruleBindingSource.ResetBindings(false);
        _rulesListBox.Refresh();
    }

    private void RefreshStepDisplay()
    {
        if (CurrentStepContainer is not null)
        {
            _stepBindingSource.DataSource = CurrentStepContainer;
            _stepsListBox.DataSource = _stepBindingSource;
        }

        _stepBindingSource.ResetBindings(false);
        _stepsListBox.Refresh();
        UpdateStepNavigationButtons();
    }

    private void UpdateBlocksPreview()
    {
        _blocksPreviewTextBox.Text = OSCControlScriptGenerator.Generate(_blocks);
    }

    private void ApplyBlocksToScript()
    {
        UpdateBlocksPreview();
        _editorTextBox.Text = _blocksPreviewTextBox.Text;
        _editorTabs.SelectedTab = _scriptTab;
        UpdateStatus(L("Applied generated script to the Script tab.", "Applied generated script to the Script tab."));
    }

    private void InitializeBlockSplitters()
    {
        if (_blockSplittersInitialized)
        {
            return;
        }

        if (!TryInitializeHorizontalSplit(_blocksOuterSplit, 170, 220, 210))
        {
            return;
        }

        if (!TryInitializeVerticalSplit(_blocksConfigSplit, 520, 260, 860))
        {
            return;
        }

        if (!TryInitializeHorizontalSplit(_blocksInnerSplit, 220, 120, 430))
        {
            return;
        }

        if (!TryInitializeVerticalSplit(_blocksRuleSplit, 320, 420, 440))
        {
            return;
        }

        _blockSplittersInitialized = true;
    }

    private static bool TryInitializeHorizontalSplit(SplitContainer split, int panel1Min, int panel2Min, int preferredDistance)
    {
        if (split.Height <= 0)
        {
            return false;
        }

        var available = split.Height - split.SplitterWidth;
        if (available <= 0)
        {
            return false;
        }

        if (available < panel1Min + panel2Min)
        {
            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;
            split.SplitterDistance = Math.Max(0, available / 2);
            return true;
        }

        split.Panel1MinSize = panel1Min;
        split.Panel2MinSize = panel2Min;
        var maxDistance = split.Height - panel2Min - split.SplitterWidth;
        split.SplitterDistance = Math.Max(panel1Min, Math.Min(preferredDistance, maxDistance));
        return true;
    }

    private static bool TryInitializeVerticalSplit(SplitContainer split, int panel1Min, int panel2Min, int preferredDistance)
    {
        if (split.Width <= 0)
        {
            return false;
        }

        var available = split.Width - split.SplitterWidth;
        if (available <= 0)
        {
            return false;
        }

        if (available < panel1Min + panel2Min)
        {
            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;
            split.SplitterDistance = Math.Max(0, available / 2);
            return true;
        }

        split.Panel1MinSize = panel1Min;
        split.Panel2MinSize = panel2Min;
        var maxDistance = split.Width - panel2Min - split.SplitterWidth;
        split.SplitterDistance = Math.Max(panel1Min, Math.Min(preferredDistance, maxDistance));
        return true;
    }

    private void ImportScriptToBlocks()
    {
        var result = _controller.Compile(_editorTextBox.Text);
        RenderDiagnostics(result);
        if (result.HasErrors)
        {
            UpdateStatus(L("Fix script diagnostics before importing into Blocks.", "Fix script diagnostics before importing into Blocks."));
            return;
        }

        var imported = BlockDocumentImporter.Import(result);
        LoadBlocksDocument(imported.Document);
        _editorTabs.SelectedTab = _blocksTab;
        UpdateBlocksPreview();

        if (imported.Warnings.Count == 0)
        {
            UpdateStatus(L("Imported script into Blocks.", "Imported script into Blocks."));
            return;
        }

        AppendRuntimeOutput(L("Block import notes:", "Block import notes:"));
        foreach (var warning in imported.Warnings)
        {
            AppendRuntimeOutput($"- {warning}");
        }

        UpdateStatus(_useSimplifiedChinese ? $"已导入，并附带 {imported.Warnings.Count} 条说明。" : $"Imported with {imported.Warnings.Count} note(s).");
    }

    private void LoadBlocksDocument(BlockDocument document)
    {
        _suppressBlockEvents = true;
        try
        {
            _blocks.Endpoints.Clear();
            foreach (var endpoint in document.Endpoints)
            {
                _blocks.Endpoints.Add(CloneEndpoint(endpoint));
            }

            _blocks.Variables.Clear();
            foreach (var variable in document.Variables)
            {
                _blocks.Variables.Add(CloneVariable(variable));
            }

            _blocks.Rules.Clear();
            foreach (var rule in document.Rules)
            {
                _blocks.Rules.Add(CloneRule(rule));
            }

            _endpointBindingSource.ResetBindings(false);
            RefreshRuleDisplay();
            _variableBindingSource.ResetBindings(false);
            _rulesListBox.ClearSelected();
            if (_blocks.Rules.Count > 0)
            {
                _rulesListBox.SelectedItem = _blocks.Rules[0];
            }
        }
        finally
        {
            _suppressBlockEvents = false;
        }

        BindSelectedRule();
    }
    private static BlockEndpoint CloneEndpoint(BlockEndpoint source) => new()
    {
        Name = source.Name,
        Transport = source.Transport,
        Mode = source.Mode,
        Host = source.Host,
        Port = source.Port,
        InputPort = source.InputPort,
        Path = source.Path,
        Codec = source.Codec,
    };

    private static BlockVariable CloneVariable(BlockVariable source) => new()
    {
        Name = source.Name,
        InitialValue = source.InitialValue,
    };
    private void DrawRuleItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0 || e.Index >= _rulesListBox.Items.Count)
        {
            return;
        }

        var rule = (BlockRule)_rulesListBox.Items[e.Index]!;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var baseColor = rule.Trigger switch
        {
            BlockTriggerKind.Startup => Color.FromArgb(255, 241, 204),
            BlockTriggerKind.Receive => Color.FromArgb(217, 234, 253),
            BlockTriggerKind.VrchatAvatarChange => Color.FromArgb(244, 222, 255),
            BlockTriggerKind.VrchatParameter => Color.FromArgb(225, 239, 223),
            _ => Color.White,
        };
        var fillColor = selected ? Color.FromArgb(56, 112, 214) : baseColor;
        var titleColor = selected ? Color.White : Color.FromArgb(30, 30, 30);
        var bodyColor = selected ? Color.FromArgb(235, 242, 255) : Color.FromArgb(70, 70, 70);
        using var background = new SolidBrush(fillColor);
        using var borderPen = new Pen(selected ? Color.FromArgb(24, 76, 176) : Color.FromArgb(180, 180, 180));

        var bounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 4, Math.Max(0, e.Bounds.Width - 8), Math.Max(0, e.Bounds.Height - 8));
        e.Graphics.FillRectangle(background, bounds);
        e.Graphics.DrawRectangle(borderPen, bounds);

        var title = rule.Trigger switch
        {
            BlockTriggerKind.Startup => L("When app starts", "When app starts"),
            BlockTriggerKind.Receive => _useSimplifiedChinese ? $"当从 {FormatDisplayValue(rule.EndpointName, "端点")} 接收到消息时" : $"When receive from {FormatDisplayValue(rule.EndpointName, "endpoint")}",
            BlockTriggerKind.VrchatAvatarChange => L("When VRChat avatar changes", "When VRChat avatar changes"),
            BlockTriggerKind.VrchatParameter => _useSimplifiedChinese ? $"当 VRChat 参数 {FormatDisplayValue(rule.EndpointName, "参数")} 变化时" : $"When VRChat param {FormatDisplayValue(rule.EndpointName, "param")} changes",
            _ => L("Rule", "Rule"),
        };

        var detail = rule.Trigger switch
        {
            BlockTriggerKind.Receive when string.IsNullOrWhiteSpace(rule.Address) => (string.IsNullOrWhiteSpace(rule.WhenExpression) ? L("No filter", "No filter") : rule.WhenExpression),
            BlockTriggerKind.Receive => (_useSimplifiedChinese ? $"地址 {rule.Address}" : $"Address {rule.Address}") + (string.IsNullOrWhiteSpace(rule.WhenExpression) ? string.Empty : (_useSimplifiedChinese ? $" 且 {rule.WhenExpression}" : $" and {rule.WhenExpression}")),
            BlockTriggerKind.VrchatAvatarChange => string.IsNullOrWhiteSpace(rule.WhenExpression) ? L("Listening to /avatar/change", "Listening to /avatar/change") : rule.WhenExpression,
            BlockTriggerKind.VrchatParameter => string.IsNullOrWhiteSpace(rule.WhenExpression) ? L("Listening to /avatar/parameters/...", "Listening to /avatar/parameters/...") : rule.WhenExpression,
            _ => string.IsNullOrWhiteSpace(rule.WhenExpression) ? L("No filter", "No filter") : rule.WhenExpression,
        };
        var steps = rule.Steps.Count == 0 ? L("No steps yet", "No steps yet") : (_useSimplifiedChinese ? $"{rule.Steps.Count} 步" : $"{rule.Steps.Count} step(s)");

        TextRenderer.DrawText(e.Graphics, title, Font, new Rectangle(bounds.X + 10, bounds.Y + 8, bounds.Width - 20, 18), titleColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, detail, Font, new Rectangle(bounds.X + 10, bounds.Y + 28, bounds.Width - 20, 16), bodyColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, steps, Font, new Rectangle(bounds.X + 10, bounds.Y + 44, bounds.Width - 20, 14), bodyColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }

    private void DrawStepItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0 || e.Index >= _stepsListBox.Items.Count)
        {
            return;
        }

        var step = (BlockStep)_stepsListBox.Items[e.Index]!;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var baseColor = step.Kind switch
        {
            BlockStepKind.Log => Color.FromArgb(229, 243, 255),
            BlockStepKind.Store => Color.FromArgb(232, 246, 230),
            BlockStepKind.Send => Color.FromArgb(255, 236, 214),
            BlockStepKind.If => Color.FromArgb(255, 244, 214),
            BlockStepKind.While => Color.FromArgb(248, 233, 255),
            BlockStepKind.Break => Color.FromArgb(255, 229, 229),
            BlockStepKind.Continue => Color.FromArgb(232, 248, 255),
            BlockStepKind.Stop => Color.FromArgb(250, 224, 224),
            BlockStepKind.VrchatParam => Color.FromArgb(243, 226, 255),
            BlockStepKind.VrchatInput => Color.FromArgb(225, 238, 255),
            BlockStepKind.VrchatChat => Color.FromArgb(255, 230, 242),
            BlockStepKind.VrchatTyping => Color.FromArgb(231, 255, 240),
            _ => Color.White,
        };
        var fillColor = selected ? Color.FromArgb(56, 112, 214) : baseColor;
        var titleColor = selected ? Color.White : Color.FromArgb(30, 30, 30);
        var bodyColor = selected ? Color.FromArgb(235, 242, 255) : Color.FromArgb(75, 75, 75);
        using var background = new SolidBrush(fillColor);
        using var borderPen = new Pen(selected ? Color.FromArgb(24, 76, 176) : Color.FromArgb(185, 185, 185));

        var bounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 4, Math.Max(0, e.Bounds.Width - 8), Math.Max(0, e.Bounds.Height - 8));
        e.Graphics.FillRectangle(background, bounds);
        e.Graphics.DrawRectangle(borderPen, bounds);

        var title = step.Kind switch
        {
            BlockStepKind.Log => _useSimplifiedChinese ? $"日志 {FormatDisplayValue(step.Target, "info")}" : $"Log {FormatDisplayValue(step.Target, "info")}",
            BlockStepKind.Store => _useSimplifiedChinese ? $"存储 {FormatDisplayValue(step.Target, "状态")}" : $"Store {FormatDisplayValue(step.Target, "state")}",
            BlockStepKind.Send => _useSimplifiedChinese ? $"发送 {FormatDisplayValue(step.Target, "端点")}" : $"Send {FormatDisplayValue(step.Target, "endpoint")}",
            BlockStepKind.If => L("If", "If"),
            BlockStepKind.While => L("While", "While"),
            BlockStepKind.Break => L("Break", "Break"),
            BlockStepKind.Continue => L("Continue", "Continue"),
            BlockStepKind.Stop => L("Stop chain", "Stop chain"),
            BlockStepKind.VrchatParam => _useSimplifiedChinese ? $"VRChat 参数 {FormatDisplayValue(step.Target, "参数")}" : $"VRChat param {FormatDisplayValue(step.Target, "param")}",
            BlockStepKind.VrchatInput => _useSimplifiedChinese ? $"VRChat 输入 {FormatDisplayValue(step.Target, "输入")}" : $"VRChat input {FormatDisplayValue(step.Target, "input")}",
            BlockStepKind.VrchatChat => L("VRChat Chatbox", "VRChat Chatbox"),
            BlockStepKind.VrchatTyping => L("VRChat Typing", "VRChat Typing"),
            _ => step.Kind.ToString(),
        };
        var detail = step.Kind switch
        {
            BlockStepKind.Log => FormatDisplayValue(step.Value, L("message", "message")),
            BlockStepKind.Store => FormatDisplayValue(step.Value, L("expression", "expression")),
            BlockStepKind.Send => $"{FormatDisplayValue(step.Value, "/address")} | {FormatPayloadMode(step.PayloadMode)} | {FormatDisplayValue(step.Extra, L("payload", "payload"))}",
            BlockStepKind.If => _useSimplifiedChinese ? $"条件 {FormatDisplayValue(step.Value, "true")} | then {step.Children.Count} / else {step.ElseChildren.Count}" : $"Condition {FormatDisplayValue(step.Value, "true")} | then {step.Children.Count} / else {step.ElseChildren.Count}",
            BlockStepKind.While => _useSimplifiedChinese ? $"条件 {FormatDisplayValue(step.Value, "true")} | {step.Children.Count} 个子步骤" : $"Condition {FormatDisplayValue(step.Value, "true")} | {step.Children.Count} child step(s)",
            BlockStepKind.Break => L("Exit the current loop immediately", "Exit the current loop immediately"),
            BlockStepKind.Continue => L("Skip to the next loop iteration", "Skip to the next loop iteration"),
            BlockStepKind.Stop => L("End current rule chain immediately", "End current rule chain immediately"),
            BlockStepKind.VrchatParam => FormatDisplayValue(step.Value, "0"),
            BlockStepKind.VrchatInput => FormatDisplayValue(step.Value, "1"),
            BlockStepKind.VrchatChat => string.IsNullOrWhiteSpace(step.Extra) ? FormatDisplayValue(step.Value, L("chat text", "chat text")) : $"{FormatDisplayValue(step.Value, L("chat text", "chat text"))} | {step.Extra}",
            BlockStepKind.VrchatTyping => FormatDisplayValue(step.Value, "true"),
            _ => string.Empty,
        };

        TextRenderer.DrawText(e.Graphics, title, Font, new Rectangle(bounds.X + 10, bounds.Y + 8, bounds.Width - 20, 18), titleColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, detail, Font, new Rectangle(bounds.X + 10, bounds.Y + 28, bounds.Width - 20, 16), bodyColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }

    private static string FormatDisplayValue(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string GetDefaultPackageOutputRoot(string scriptPath)
    {
        if (!string.IsNullOrWhiteSpace(scriptPath))
        {
            var scriptDirectory = Path.GetDirectoryName(Path.GetFullPath(scriptPath));
            if (!string.IsNullOrWhiteSpace(scriptDirectory))
            {
                return Path.Combine(scriptDirectory, "packages");
            }
        }

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return string.IsNullOrWhiteSpace(documents)
            ? Path.Combine(Directory.GetCurrentDirectory(), "packages")
            : Path.Combine(documents, "OSCControl Packages");
    }
    private static string? FindAppHostSourceDirectory()
    {
        foreach (var candidate in EnumerateAppHostCandidates())
        {
            if (File.Exists(Path.Combine(candidate, "OSCControl.AppHost.exe")) || File.Exists(Path.Combine(candidate, "OSCControl.AppHost.dll")))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateAppHostCandidates()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddWorkspaceRoot(AppContext.BaseDirectory, roots);
        AddWorkspaceRoot(Directory.GetCurrentDirectory(), roots);

        foreach (var root in roots)
        {
            yield return Path.Combine(root, "artifacts", "apphost");
            yield return Path.Combine(root, "src", "OSCControl.AppHost", "bin", "Debug", "net8.0");
            yield return Path.Combine(root, "src", "OSCControl.AppHost", "bin", "Release", "net8.0");
        }
    }

    private static void AddWorkspaceRoot(string startDirectory, ISet<string> roots)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var marker = Path.Combine(directory.FullName, "src", "OSCControl.AppHost", "OSCControl.AppHost.csproj");
            if (File.Exists(marker))
            {
                roots.Add(directory.FullName);
                return;
            }

            directory = directory.Parent;
        }
    }

#if OSCCONTROL_BLOCKLY_WEBVIEW2
    private static string GetBlocklyWorkspacePath(string scriptPath) => scriptPath + ".blocks.json";

    private async Task<bool> LoadBlocklyWorkspaceForScriptAsync(string scriptPath)
    {
        _blocklyWorkspaceJson = string.Empty;
        var workspacePath = GetBlocklyWorkspacePath(scriptPath);
        if (!File.Exists(workspacePath))
        {
            return false;
        }

        _blocklyWorkspaceJson = await File.ReadAllTextAsync(workspacePath);
        if (_blocklyWebViewHost is not null)
        {
            await _blocklyWebViewHost.LoadWorkspaceJsonAsync(_blocklyWorkspaceJson);
        }

        return !string.IsNullOrWhiteSpace(_blocklyWorkspaceJson);
    }

    private async Task SaveBlocklyWorkspaceForScriptAsync(string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(_blocklyWorkspaceJson))
        {
            return;
        }

        await File.WriteAllTextAsync(GetBlocklyWorkspacePath(scriptPath), _blocklyWorkspaceJson);
    }

    private string GetCurrentSource() => _editorTabs.SelectedTab == _blocksTab ? _blocklyGeneratedSource : _editorTextBox.Text;
#else
    private string GetCurrentSource() => _editorTabs.SelectedTab == _blocksTab ? _blocksPreviewTextBox.Text : _editorTextBox.Text;
#endif
    private void UpdateStepHint()
    {
        var step = SelectedStep;
        if (step is null)
        {
            _stepHintLabel.Text = L("Select a step to edit it. Send steps use Target = endpoint, Value = address, Payload = args/body, Extra = payload content. While steps use Value = condition, then Enter Body to edit child steps.", "Select a step to edit it. Send steps use Target = endpoint, Value = address, Payload = args/body, Extra = payload content. While steps use Value = condition, then Enter Body to edit child steps.");
            return;
        }

        _stepHintLabel.Text = step.Kind switch
        {
            BlockStepKind.Log => L("Log: Target = log level (info/warn/error/debug), Value = text or expression.", "Log: Target = log level (info/warn/error/debug), Value = text or expression."),
            BlockStepKind.Store => L("Store: Target = state name, Value = expression to keep between events.", "Store: Target = state name, Value = expression to keep between events."),
            BlockStepKind.Send => L("Send: Target = endpoint name, Value = address, Payload = Args or Body, Extra = payload content. For args you can type 1, 2, 3 and the generator will wrap it into [[...]].", "Send: Target = endpoint name, Value = address, Payload = Args or Body, Extra = payload content. For args you can type 1, 2, 3 and the generator will wrap it into [[...]]."),
            BlockStepKind.If => L("If: Value = condition expression. Enter Body edits the then branch; Enter Else edits the else branch.", "If: Value = condition expression. Enter Body edits the then branch; Enter Else edits the else branch."),
            BlockStepKind.While => L("While: Value = condition expression, for example state(\"count\") < 3. Select it and click Enter Body to edit child steps.", "While: Value = condition expression, for example state(\"count\") < 3. Select it and click Enter Body to edit child steps."),
            BlockStepKind.Break => L("Break: exits the current while immediately. Other fields are ignored.", "Break: exits the current while immediately. Other fields are ignored."),
            BlockStepKind.Continue => L("Continue: skips the rest of the current loop iteration. Other fields are ignored.", "Continue: skips the rest of the current loop iteration. Other fields are ignored."),
            BlockStepKind.Stop => L("Stop: ends the current rule chain immediately. Other fields are ignored.", "Stop: ends the current rule chain immediately. Other fields are ignored."),
            BlockStepKind.VrchatParam => L("VRChat Param: Target = avatar parameter name, Value = value to write. Generates vrchat.param.", "VRChat Param: Target = avatar parameter name, Value = value to write. Generates vrchat.param."),
            BlockStepKind.VrchatInput => L("VRChat Input: Target = input name such as Jump / Vertical, Value = value to send.", "VRChat Input: Target = input name such as Jump / Vertical, Value = value to send."),
            BlockStepKind.VrchatChat => L("VRChat Chatbox: Value = text, Extra = options like send=true notify=false.", "VRChat Chatbox: Value = text, Extra = options like send=true notify=false."),
            BlockStepKind.VrchatTyping => L("VRChat Typing: Value = true or false.", "VRChat Typing: Value = true or false."),
            _ => L("Edit the selected step.", "Edit the selected step.")
        };
    }

    private void EnterSelectedStepBody()
    {
        if (SelectedStep is not { IsContainer: true } step)
        {
            return;
        }

        _stepContainerStack.Push(new StepContainerContext(step.Children, step));
        _stepBindingSource.DataSource = step.Children;
        _stepsListBox.DataSource = _stepBindingSource;
        RefreshStepDisplay();
        if (step.Children.Count > 0)
        {
            SelectStep(step.Children[0]);
        }
        else
        {
            BindSelectedStep();
        }
    }

    private void EnterSelectedStepElseBody()
    {
        if (SelectedStep is not { Kind: BlockStepKind.If } step)
        {
            return;
        }

        _stepContainerStack.Push(new StepContainerContext(step.ElseChildren, step));
        _stepBindingSource.DataSource = step.ElseChildren;
        _stepsListBox.DataSource = _stepBindingSource;
        RefreshStepDisplay();
        if (step.ElseChildren.Count > 0)
        {
            SelectStep(step.ElseChildren[0]);
        }
        else
        {
            BindSelectedStep();
        }
    }
    private void ExitStepBody()
    {
        if (IsAtRootStepContainer || _stepContainerStack.Count == 0)
        {
            return;
        }

        var previous = _stepContainerStack.Pop();
        var parent = _stepContainerStack.Peek();
        _stepBindingSource.DataSource = parent.Steps;
        _stepsListBox.DataSource = _stepBindingSource;
        RefreshStepDisplay();

        if (previous.Owner is not null)
        {
            SelectStep(previous.Owner);
        }
        else if (parent.Steps.Count > 0)
        {
            SelectStep(parent.Steps[0]);
        }
        else
        {
            BindSelectedStep();
        }
    }

    private void ResetToRuleStepContainer(BlockRule rule)
    {
        _stepContainerStack.Clear();
        _stepContainerStack.Push(new StepContainerContext(rule.Steps, null));
        _stepBindingSource.DataSource = rule.Steps;
        _stepsListBox.DataSource = _stepBindingSource;
        RefreshStepDisplay();
    }

    private void UpdateStepNavigationButtons()
    {
        _stepEnterBodyButton.Enabled = SelectedStep?.IsContainer == true;
        _stepEnterElseButton.Enabled = SelectedStep?.Kind == BlockStepKind.If;
        _stepBackButton.Enabled = !IsAtRootStepContainer;
    }

    private static void CommitGridEdit(DataGridView grid)
    {
        if (grid.IsCurrentCellDirty)
        {
            grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void PostToUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }

    private sealed class OptionItem<T>
    {
        public OptionItem(T value, string label)
        {
            Value = value;
            Label = label;
        }

        public T Value { get; }

        public string Label { get; }
    }
    private async Task SetBusyAsync(bool busy)
    {
        SetEditingEnabled(!busy);
        await Task.Yield();
    }

    private void SetEditingEnabled(bool enabled)
    {
        _pathTextBox.Enabled = enabled;
        _editorTabs.Enabled = enabled;
        _editorTextBox.Enabled = enabled;
        _blocksEditorRoot.Enabled = enabled;
        _reloadButton.Enabled = enabled;
        _saveButton.Enabled = enabled;
        _checkButton.Enabled = enabled;
        _packageButton.Enabled = enabled;
        _startButton.Enabled = enabled && !_controller.IsRunning;
        _stopButton.Enabled = _controller.IsRunning;
    }

    private void ResetBottomSplit()
    {
#if !OSCCONTROL_BLOCKLY_WEBVIEW2
        InitializeBlockSplitters();
#endif
    }

    private sealed record StepContainerContext(BindingList<BlockStep> Steps, BlockStep? Owner);
}
