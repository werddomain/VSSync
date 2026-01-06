namespace VSSync.DebugHelper;

public partial class Form1 : Form
{
    private readonly IpcClient _ipcClient;
    private List<IdeInstance> _instances = [];

    public Form1()
    {
        InitializeComponent();

        _ipcClient = new IpcClient();
        _ipcClient.LogMessage += (_, msg) => AppendLog(msg);

        _btnRefresh.Click += BtnRefresh_Click;
        _btnSend.Click += BtnSend_Click;
        _btnClearLog.Click += (_, _) => _txtLog.Clear();

        Load += async (_, _) => await RefreshInstancesAsync();
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e)
    {
        await RefreshInstancesAsync();
    }

    private async Task RefreshInstancesAsync()
    {
        _btnRefresh.Enabled = false;
        _listViewInstances.Items.Clear();
        AppendLog("Discovering IDE instances...");

        try
        {
            _instances = await _ipcClient.DiscoverAllInstancesAsync();

            foreach (var instance in _instances)
            {
                var item = new ListViewItem(instance.Ide);
                item.SubItems.Add(instance.Port.ToString());
                item.SubItems.Add(instance.Pid.ToString());
                item.SubItems.Add(instance.Version);
                item.SubItems.Add(instance.SolutionPath ?? instance.WorkspacePath);
                item.Tag = instance;
                _listViewInstances.Items.Add(item);
            }

            if (_instances.Count == 0)
            {
                AppendLog("No IDE instances found. Make sure VS Code or Visual Studio is running with the VSSync extension.");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error during discovery: {ex.Message}");
        }
        finally
        {
            _btnRefresh.Enabled = true;
        }
    }

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        if (_listViewInstances.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select an IDE instance first.", "No Instance Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var filePath = _txtFilePath.Text.Trim();
        if (string.IsNullOrEmpty(filePath))
        {
            MessageBox.Show("Please enter a file path.", "No File Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var instance = (IdeInstance)_listViewInstances.SelectedItems[0].Tag!;
        var line = (int)_numLine.Value;
        var column = (int)_numColumn.Value;

        _btnSend.Enabled = false;
        AppendLog($"Sending OPEN_FILE to {instance.Ide} (Port {instance.Port})...");
        AppendLog($"  File: {filePath}");
        AppendLog($"  Line: {line}, Column: {column}");

        try
        {
            var (success, error) = await _ipcClient.OpenFileAsync(instance, filePath, line > 0 ? line : null, column > 0 ? column : null);

            if (success)
            {
                AppendLog("✓ File opened successfully!");
            }
            else
            {
                AppendLog($"✗ Failed to open file: {error}");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            _btnSend.Enabled = true;
        }
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendLog(message));
            return;
        }

        _txtLog.AppendText(message + Environment.NewLine);
        _txtLog.SelectionStart = _txtLog.Text.Length;
        _txtLog.ScrollToCaret();
    }
}
