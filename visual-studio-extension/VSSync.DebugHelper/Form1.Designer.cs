namespace VSSync.DebugHelper;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 600);
        Text = "VSSync Debug Helper";
        MinimumSize = new Size(600, 500);

        // Main split container
        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 250
        };

        // Top panel - Instances
        var instancesGroup = new GroupBox
        {
            Text = "IDE Instances",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _listViewInstances = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };
        _listViewInstances.Columns.Add("IDE", 100);
        _listViewInstances.Columns.Add("Port", 60);
        _listViewInstances.Columns.Add("PID", 60);
        _listViewInstances.Columns.Add("Version", 80);
        _listViewInstances.Columns.Add("Workspace/Solution", 400);

        var instancesButtonPanel = new Panel
        {
            Height = 40,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, 5, 0, 0)
        };

        _btnRefresh = new Button
        {
            Text = "Refresh",
            Width = 100,
            Height = 30,
            Location = new Point(0, 5)
        };

        instancesButtonPanel.Controls.Add(_btnRefresh);
        instancesGroup.Controls.Add(_listViewInstances);
        instancesGroup.Controls.Add(instancesButtonPanel);

        // Command panel
        var commandGroup = new GroupBox
        {
            Text = "Send Open File Command",
            Dock = DockStyle.Top,
            Height = 130,
            Padding = new Padding(10)
        };

        var commandPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3
        };
        commandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        commandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        commandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        commandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        commandPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        commandPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        commandPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

        // File path row
        commandPanel.Controls.Add(new Label { Text = "File Path:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
        _txtFilePath = new TextBox { Dock = DockStyle.Fill };
        commandPanel.Controls.Add(_txtFilePath, 1, 0);
        commandPanel.SetColumnSpan(_txtFilePath, 3);

        // Line/Column row
        commandPanel.Controls.Add(new Label { Text = "Line:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
        _numLine = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100000, Value = 1 };
        commandPanel.Controls.Add(_numLine, 1, 1);
        commandPanel.Controls.Add(new Label { Text = "Column:", Anchor = AnchorStyles.Left, AutoSize = true }, 2, 1);
        _numColumn = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 10000, Value = 1 };
        commandPanel.Controls.Add(_numColumn, 3, 1);

        // Send button row
        _btnSend = new Button
        {
            Text = "Send Open File Command",
            Width = 200,
            Height = 30,
            Anchor = AnchorStyles.Left
        };
        commandPanel.Controls.Add(_btnSend, 1, 2);

        commandGroup.Controls.Add(commandPanel);

        // Bottom panel - Log
        var logGroup = new GroupBox
        {
            Text = "Log",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _txtLog = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9)
        };

        var logButtonPanel = new Panel
        {
            Height = 35,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, 5, 0, 0)
        };

        _btnClearLog = new Button
        {
            Text = "Clear Log",
            Width = 100,
            Height = 25,
            Location = new Point(0, 5)
        };

        logButtonPanel.Controls.Add(_btnClearLog);
        logGroup.Controls.Add(_txtLog);
        logGroup.Controls.Add(logButtonPanel);

        // Layout
        mainSplit.Panel1.Controls.Add(instancesGroup);
        
        var bottomPanel = new Panel { Dock = DockStyle.Fill };
        bottomPanel.Controls.Add(logGroup);
        bottomPanel.Controls.Add(commandGroup);
        mainSplit.Panel2.Controls.Add(bottomPanel);

        Controls.Add(mainSplit);
    }

    #endregion

    private ListView _listViewInstances = null!;
    private Button _btnRefresh = null!;
    private TextBox _txtFilePath = null!;
    private NumericUpDown _numLine = null!;
    private NumericUpDown _numColumn = null!;
    private Button _btnSend = null!;
    private TextBox _txtLog = null!;
    private Button _btnClearLog = null!;
}
