using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AudioConverter.Services;
using Microsoft.Win32;

namespace AudioConverter;

public partial class MainWindow : Window
{
    readonly ObservableCollection<QueueItem> _items = new();
    IAudioEngine? _engine;
    CancellationTokenSource? _cts;
    DispatcherTimer? _timer;
    readonly Stopwatch _clock = new();
    int _finished;
    int _total;
    bool _busy;
    string? _lastOutputFolder;

    public MainWindow()
    {
        InitializeComponent();
        QueueGrid.ItemsSource = _items;
        Loaded += (_, _) => LoadFormats();
    }

    void SetStatus(string text) => StatusText.Text = text;

    // ---------------------------------------------------------------- startup

    void LoadFormats()
    {
        try
        {
            _engine = new FfmpegEngine();
            SupportedText.Text = "Reads: " + SupportedFormats.Describe();
        }
        catch (Exception ex)
        {
            _engine = null;
            SupportedText.Text = "Reads: " + SupportedFormats.Fallback;
            SetStatus("The audio engine didn't start properly: " + ex.Message);
        }

        FormatBox.ItemsSource = AudioTargetFormats.All;
        FormatBox.SelectedIndex = 0;   // MP3

        if (StatusText.Text.Length == 0)
            SetStatus("Drop audio files or folders onto the window, choose a format, then click Convert.");
    }

    // ---------------------------------------------------------------- adding files

    void Window_DragOver(object sender, DragEventArgs e)
    {
        bool ok = !_busy && e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = ok ? DragDropEffects.Copy : DragDropEffects.None;
        if (ok) DropRect.Fill = (Brush)FindResource("TintDarkBrush");
        e.Handled = true;
    }

    void Window_DragLeave(object sender, DragEventArgs e) => DropRect.Fill = (Brush)FindResource("TintBrush");

    async void Window_Drop(object sender, DragEventArgs e)
    {
        DropRect.Fill = (Brush)FindResource("TintBrush");
        if (_busy) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            await AddPathsAsync(paths);
    }

    async Task AddPathsAsync(IEnumerable<string> paths)
    {
        SetStatus("Scanning...");
        bool subfolders = SubfoldersBox.IsChecked == true;
        var existing = new HashSet<string>(_items.Select(i => i.Path), StringComparer.OrdinalIgnoreCase);
        var list = paths.ToList();

        List<QueueItem> added;
        try
        {
            added = await Task.Run(() => FileScanner.Expand(list, subfolders, existing));
        }
        catch (Exception ex)
        {
            SetStatus("Couldn't read that: " + ex.Message);
            return;
        }

        foreach (var item in added) _items.Add(item);
        SetStatus(added.Count == 0
            ? "No new audio files found there."
            : $"Added {added.Count} file(s). {_items.Count} in the list.");
        UpdateNotice();
    }

    async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        string patterns = string.Join(";", AudioKinds.Readable.OrderBy(x => x).Select(x => "*" + x));
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Choose audio files to convert",
            Filter = $"Audio files|{patterns}|All files|*.*"
        };
        if (dialog.ShowDialog() == true) await AddPathsAsync(dialog.FileNames);
    }

    async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose a folder of audio files" };
        if (dialog.ShowDialog() == true) await AddPathsAsync(new[] { dialog.FolderName });
    }

    void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _items.Clear();
        ResetStats();
        UpdateNotice();
        SetStatus("List cleared.");
    }

    void QueueGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || _busy) return;
        foreach (var item in QueueGrid.SelectedItems.Cast<QueueItem>().ToList())
            _items.Remove(item);
        UpdateNotice();
        e.Handled = true;
    }

    // ---------------------------------------------------------------- options

    void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BitratePanel == null || QualityPanel == null || SampleRatePanel == null || BitDepthPanel == null) return;
        if (FormatBox.SelectedItem is not AudioTargetFormat target) return;

        BitratePanel.Visibility = target.HasBitrate ? Visibility.Visible : Visibility.Collapsed;
        QualityPanel.Visibility = target.HasQuality ? Visibility.Visible : Visibility.Collapsed;
        SampleRatePanel.Visibility = target.HasSampleRate ? Visibility.Visible : Visibility.Collapsed;
        BitDepthPanel.Visibility = target.HasBitDepth ? Visibility.Visible : Visibility.Collapsed;

        if (target.HasBitrate) SelectBitrate(target.DefaultBitrateKbps);
        if (target.HasQuality) QualitySlider.Value = target.DefaultQuality;
        if (target.HasBitDepth) BitDepthBox.SelectedIndex = target.DefaultBitDepth >= 24 ? 1 : 0;

        UpdateNotice();
    }

    void SelectBitrate(int kbps)
    {
        foreach (var item in BitrateBox.Items)
        {
            if (item is ComboBoxItem c && c.Tag is string s && int.TryParse(s, out int value) && value == kbps)
            {
                BitrateBox.SelectedItem = item;
                return;
            }
        }
    }

    void Quality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QualityValue == null) return;
        QualityValue.Text = ((int)QualitySlider.Value).ToString();
    }

    /// <summary>Warns about the two conversions people expect more from than they get.</summary>
    void UpdateNotice()
    {
        if (NoticeText == null) return;
        if (FormatBox.SelectedItem is not AudioTargetFormat target) { NoticeText.Text = ""; return; }

        bool hasLossySource = _items.Any(i => AudioKinds.IsLossy(i.Path));
        bool sameFormat = _items.Any(i => string.Equals(Path.GetExtension(i.Path), target.Extension, StringComparison.OrdinalIgnoreCase));

        if (target.IsLossless && hasLossySource)
            NoticeText.Text = $"Heads up: the source is already lossy, so converting to {target.Name} won't restore quality. " +
                              "The file gets bigger; the audio stays the same.";
        else if (!target.IsLossless && sameFormat)
            NoticeText.Text = $"Re-encoding to {target.Name} loses a little quality each time. Converting from the original file is always better.";
        else
            NoticeText.Text = "";
    }

    void OutputBox_GotFocus(object sender, RoutedEventArgs e) => FolderRadio.IsChecked = true;

    void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose where to save the converted audio" };
        if (dialog.ShowDialog() != true) return;
        OutputBox.Text = dialog.FolderName;
        FolderRadio.IsChecked = true;
    }

    static int TagInt(ComboBox box, int fallback) =>
        box.SelectedItem is ComboBoxItem item && item.Tag is string s && int.TryParse(s, out int value) ? value : fallback;

    ChannelMode SelectedChannels()
    {
        string? tag = (ChannelsBox.SelectedItem as ComboBoxItem)?.Tag as string;
        return tag switch
        {
            "mono" => ChannelMode.Mono,
            "stereo" => ChannelMode.Stereo,
            _ => ChannelMode.Keep
        };
    }

    // ---------------------------------------------------------------- converting

    void SetBusy(bool busy)
    {
        _busy = busy;
        ConvertButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        AddFilesButton.IsEnabled = !busy;
        AddFolderButton.IsEnabled = !busy;
        ClearButton.IsEnabled = !busy;
        SubfoldersBox.IsEnabled = !busy;
        FormatBox.IsEnabled = !busy;
        BitrateBox.IsEnabled = !busy;
        QualitySlider.IsEnabled = !busy;
        SampleRateBox.IsEnabled = !busy;
        BitDepthBox.IsEnabled = !busy;
        ChannelsBox.IsEnabled = !busy;
        KeepTagsBox.IsEnabled = !busy;
        SameFolderRadio.IsEnabled = !busy;
        FolderRadio.IsEnabled = !busy;
        OutputBox.IsEnabled = !busy;
        BrowseButton.IsEnabled = !busy;
    }

    async void Convert_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0) { SetStatus("Add some audio files first: drop them onto the window."); return; }
        if (FormatBox.SelectedItem is not AudioTargetFormat target) { SetStatus("Choose a format to convert to."); return; }
        var engine = _engine;
        if (engine == null) { SetStatus("The audio engine isn't available: ffmpeg.exe could not be found."); return; }

        string? outputFolder = null;
        if (FolderRadio.IsChecked == true)
        {
            outputFolder = OutputBox.Text.Trim();
            if (outputFolder.Length == 0)
            {
                SetStatus("Choose an output folder, or pick \"Next to the originals\".");
                return;
            }
        }

        var options = new AudioConvertOptions(
            target,
            TagInt(BitrateBox, target.DefaultBitrateKbps),
            (int)QualitySlider.Value,
            TagInt(SampleRateBox, 0),
            TagInt(BitDepthBox, 16),
            SelectedChannels(),
            KeepTagsBox.IsChecked == true);

        var items = _items.ToList();

        _cts = new CancellationTokenSource();
        _total = items.Count;
        _finished = 0;
        SetBusy(true);
        OpenFolderButton.IsEnabled = false;
        RunProgress.Value = 0;
        StartStats();
        SetStatus($"Converting {_total} file(s) to {target.Name}...");

        string? dialogText = null;
        var icon = MessageBoxImage.Information;
        try
        {
            var progress = new Progress<int>(n =>
            {
                _finished = n;
                RunProgress.Value = _total == 0 ? 0 : n * 100.0 / _total;
            });
            var summary = await new BatchRunner().RunAsync(items, options, outputFolder, engine, progress, _cts.Token);

            _lastOutputFolder = summary.FirstOutputFolder;
            if (summary.Cancelled)
            {
                SetStatus($"Cancelled. {summary.Converted} converted before stopping.");
            }
            else
            {
                string text = $"Converted {summary.Converted} track(s) to {target.Name}.";
                if (summary.Failed > 0) text += $"  {summary.Failed} failed.";
                if (summary.Skipped > 0) text += $"  {summary.Skipped} skipped.";
                SetStatus(text);

                dialogText = text;
                if (summary.Converted > 0)
                    dialogText += $"\n\nSize: {QueueItem.FormatSize(summary.BytesIn)} to {QueueItem.FormatSize(summary.BytesOut)}." +
                                  $"\nTime: {FormatTime(summary.Elapsed.TotalSeconds)}.";
                if (_lastOutputFolder != null) dialogText += $"\n\nSaved to:\n{_lastOutputFolder}";
                if (summary.Failed > 0)
                {
                    dialogText += "\n\nThe files that failed are marked in red in the list, with the reason.";
                    icon = MessageBoxImage.Warning;
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus("Error: " + ex.Message);
        }
        finally
        {
            _cts = null;
            StopStats();
            SetBusy(false);
            OpenFolderButton.IsEnabled = _lastOutputFolder != null && Directory.Exists(_lastOutputFolder);
            RunProgress.Value = 0;
            if (dialogText != null)
                MessageBox.Show(this, dialogText, "Conversion complete", MessageBoxButton.OK, icon);
        }
    }

    void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelButton.IsEnabled = false;
        SetStatus("Cancelling...");
    }

    void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_lastOutputFolder != null && Directory.Exists(_lastOutputFolder))
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_lastOutputFolder}\"") { UseShellExecute = true });
    }

    // ---------------------------------------------------------------- live readout

    void StartStats()
    {
        _clock.Restart();
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _timer.Tick += (_, _) => UpdateStats();
        _timer.Start();
        UpdateStats();
    }

    void StopStats()
    {
        _timer?.Stop();
        _clock.Stop();
        UpdateStats();   // leave the final figures on screen
    }

    void UpdateStats()
    {
        double seconds = _clock.Elapsed.TotalSeconds;
        double speed = seconds > 0.2 ? _finished / seconds : 0;
        int failed = _items.Count(i => i.State == ItemState.Failed);

        // each figure sits in its own fixed-width cell, so the line never shifts as the numbers change
        DoneText.Text = $"Done {_finished} of {_total}";
        ElapsedText.Text = $"Elapsed {FormatTime(seconds)}";
        SpeedText.Text = $"Speed {speed:0.0} files/second";
        FailedText.Text = $"Failed {failed}";
    }

    void ResetStats()
    {
        _finished = 0;
        _total = 0;
        DoneText.Text = ElapsedText.Text = SpeedText.Text = FailedText.Text = "";
        RunProgress.Value = 0;
    }

    static string FormatTime(double seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";

    // ---------------------------------------------------------------- about and links

    void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();

    void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }

    /// <summary>Opens a web address in the user's default browser.</summary>
    public static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* no browser available: nothing useful to do */ }
    }
}
