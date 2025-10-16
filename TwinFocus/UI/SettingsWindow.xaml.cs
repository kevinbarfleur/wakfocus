using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TwinFocus.Config;
using TwinFocus.Core;
using MessageBox = System.Windows.MessageBox;

namespace TwinFocus.UI;

/// <summary>
/// Settings window for TwinFocus configuration
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly AppConfig _config;
    private readonly ObservableCollection<WindowItem> _detectedWindows = new();
    private System.Windows.Point _dragStartPoint;

    // Hotkey capture state
    private List<string> _capturedModifiers = new();
    private string _capturedKey = "F4";
    private bool _isCapturingHotkey = false;

    public event EventHandler? SettingsSaved;
    public bool WasSaved { get; private set; } = false;

    public SettingsWindow(ConfigService configService, AppConfig config)
    {
        InitializeComponent();

        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        LoadSettings();

        // Bind detected windows list
        DetectedWindowsListBox.ItemsSource = _detectedWindows;

        // Always refresh windows on startup
        RefreshWindows();
    }

    /// <summary>
    /// Load current settings into UI controls
    /// </summary>
    private void LoadSettings()
    {
        // Load hotkey
        _capturedModifiers = _config.Hotkey.Modifiers?.ToList() ?? new List<string> { "NONE" };
        _capturedKey = _config.Hotkey.Key ?? "F4";
        UpdateHotkeyDisplay();

        // Load target filters - find first process target
        var processTarget = _config.Targets?.FirstOrDefault(t => t.Type == "process");
        if (processTarget != null)
        {
            ProcessNameTextBox.Text = processTarget.Match;
        }

        // Load first title target
        var titleTarget = _config.Targets?.FirstOrDefault(t => t.Type == "title");
        if (titleTarget != null)
        {
            TitleFilterTextBox.Text = titleTarget.Match;
        }

        // Load options
        IncludeMinimizedCheckBox.IsChecked = _config.IncludeMinimized;
        CompatFallbackCheckBox.IsChecked = _config.CompatFallback;
    }

    /// <summary>
    /// Update the hotkey display text
    /// </summary>
    private void UpdateHotkeyDisplay()
    {
        var parts = new System.Collections.Generic.List<string>();

        foreach (var mod in _capturedModifiers)
        {
            if (mod == "CTRL") parts.Add("Ctrl");
            else if (mod == "ALT") parts.Add("Alt");
            else if (mod == "SHIFT") parts.Add("Shift");
            else if (mod == "WIN") parts.Add("Win");
        }

        parts.Add(_capturedKey);

        HotkeyTextBox.Text = string.Join(" + ", parts);
    }

    /// <summary>
    /// Hotkey TextBox got focus - enter capture mode
    /// </summary>
    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = true;
        HotkeyTextBox.Text = "Press any key...";
        HotkeyTextBox.FontStyle = FontStyles.Italic;
    }

    /// <summary>
    /// Hotkey TextBox lost focus - exit capture mode
    /// </summary>
    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = false;
        HotkeyTextBox.FontStyle = FontStyles.Normal;
        UpdateHotkeyDisplay();
    }

    /// <summary>
    /// Capture key press for hotkey
    /// </summary>
    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        if (!_isCapturingHotkey)
            return;

        // Get the actual key (not modifier keys)
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore lone modifier keys
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // Capture modifiers
        _capturedModifiers.Clear();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            _capturedModifiers.Add("CTRL");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            _capturedModifiers.Add("ALT");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            _capturedModifiers.Add("SHIFT");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
            _capturedModifiers.Add("WIN");

        if (_capturedModifiers.Count == 0)
            _capturedModifiers.Add("NONE");

        // Convert WPF Key to string
        _capturedKey = ConvertKeyToString(key);

        // Update display and exit capture mode
        UpdateHotkeyDisplay();
        HotkeyTextBox.FontStyle = FontStyles.Normal;
        _isCapturingHotkey = false;

        // Move focus away to prevent further captures
        SaveButton.Focus();
    }

    /// <summary>
    /// Convert WPF Key to hotkey string
    /// </summary>
    private string ConvertKeyToString(Key key)
    {
        // Function keys
        if (key >= Key.F1 && key <= Key.F24)
        {
            int fNum = (int)key - (int)Key.F1 + 1;
            return $"F{fNum}";
        }

        // Number keys (D0-D9)
        if (key >= Key.D0 && key <= Key.D9)
        {
            int num = (int)key - (int)Key.D0;
            return num.ToString();
        }

        // Letter keys
        if (key >= Key.A && key <= Key.Z)
        {
            return key.ToString();
        }

        // Default - return the key name as-is
        return key.ToString();
    }

    /// <summary>
    /// Save button clicked
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update config with captured hotkey
            _config.Hotkey.Modifiers = _capturedModifiers;
            _config.Hotkey.Key = _capturedKey;

            // Always use manual ordering mode
            _config.Ordering.Mode = "manual";

            // Save manual order from the draggable list
            _config.Ordering.Manual = new System.Collections.Generic.List<TargetMatcher>();
            foreach (var windowItem in _detectedWindows)
            {
                // Create a matcher based on title (more specific than process)
                _config.Ordering.Manual.Add(new TargetMatcher
                {
                    Type = "title",
                    Match = System.Text.RegularExpressions.Regex.Escape(windowItem.Title)
                });
            }

            // Update targets
            _config.Targets.Clear();

            // Add process target if specified
            string processName = ProcessNameTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(processName))
            {
                _config.Targets.Add(new TargetMatcher
                {
                    Type = "process",
                    Match = processName
                });
            }

            // Add title target if specified
            string titleFilter = TitleFilterTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(titleFilter))
            {
                _config.Targets.Add(new TargetMatcher
                {
                    Type = "title",
                    Match = titleFilter
                });
            }

            // Add default class matcher for Java windows
            if (processName?.ToLower() == "java.exe")
            {
                _config.Targets.Add(new TargetMatcher
                {
                    Type = "class",
                    Match = "SunAwtFrame|LWJGL"
                });
            }

            // Update options
            _config.IncludeMinimized = IncludeMinimizedCheckBox.IsChecked ?? true;
            _config.CompatFallback = CompatFallbackCheckBox.IsChecked ?? false;

            // Save to disk
            _configService.Save(_config);

            // Mark as saved
            WasSaved = true;

            // Notify listeners
            SettingsSaved?.Invoke(this, EventArgs.Empty);

            MessageBox.Show("Settings saved successfully!\n\nThe new hotkey is now active.",
                "TwinFocus - Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings:\n\n{ex.Message}",
                "TwinFocus - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Cancel button clicked
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Refresh/scan for windows matching current filters
    /// </summary>
    private void RefreshWindowsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshWindows();
    }

    private void RefreshWindows()
    {
        try
        {
            _detectedWindows.Clear();

            // Create enumerator with current config
            var enumerator = new WindowEnumerator(_config);
            var windows = enumerator.FindTargetWindows();

            foreach (var window in windows)
            {
                _detectedWindows.Add(new WindowItem(window));
            }

            WindowCountLabel.Text = $"{_detectedWindows.Count} window(s) found";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to scan windows:\n\n{ex.Message}",
                "TwinFocus - Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Mouse down - record drag start point
    /// </summary>
    private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    /// <summary>
    /// Mouse move - initiate drag if threshold exceeded
    /// </summary>
    private void ListBox_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            System.Windows.Vector diff = _dragStartPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var listBox = sender as System.Windows.Controls.ListBox;
                if (listBox == null) return;

                var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                if (listBoxItem != null)
                {
                    var item = listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem) as WindowItem;
                    if (item != null)
                    {
                        System.Windows.DataObject dragData = new System.Windows.DataObject(typeof(WindowItem), item);
                        DragDrop.DoDragDrop(listBoxItem, dragData, System.Windows.DragDropEffects.Move);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Find ancestor of specific type in visual tree
    /// </summary>
    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T ancestor)
            {
                return ancestor;
            }

            try
            {
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            catch
            {
                // GetParent can fail for non-visual elements like Run
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Drag over - validate drop target
    /// </summary>
    private void ListBox_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(WindowItem)))
        {
            e.Effects = System.Windows.DragDropEffects.Move;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    /// <summary>
    /// Drop - reorder the list
    /// </summary>
    private void ListBox_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(WindowItem)))
        {
            var droppedData = e.Data.GetData(typeof(WindowItem)) as WindowItem;
            var target = GetListBoxItemUnderMouse(sender as System.Windows.Controls.ListBox, e.GetPosition(sender as System.Windows.Controls.ListBox));

            if (droppedData != null && target != null)
            {
                int removedIdx = DetectedWindowsListBox.Items.IndexOf(droppedData);
                int targetIdx = DetectedWindowsListBox.Items.IndexOf(target);

                if (removedIdx != targetIdx)
                {
                    _detectedWindows.Move(removedIdx, targetIdx);
                }
            }
        }
    }

    /// <summary>
    /// Get the ListBox item under the mouse cursor
    /// </summary>
    private WindowItem? GetListBoxItemUnderMouse(System.Windows.Controls.ListBox? listBox, System.Windows.Point point)
    {
        if (listBox == null) return null;

        var element = listBox.InputHitTest(point) as UIElement;
        while (element != null)
        {
            if (element == listBox) return null;

            var item = ItemsControl.ContainerFromElement(listBox, element);
            if (item != null)
            {
                return listBox.ItemContainerGenerator.ItemFromContainer(item) as WindowItem;
            }

            element = System.Windows.Media.VisualTreeHelper.GetParent(element) as UIElement;
        }

        return null;
    }
}

/// <summary>
/// View model for a window item in the list
/// </summary>
public class WindowItem
{
    public string Title { get; set; }
    public string ProcessName { get; set; }
    public string HandleHex { get; set; }
    public IntPtr Handle { get; set; }
    public string Subtitle => $"{ProcessName} • {HandleHex}";

    public WindowItem(WindowInfo window)
    {
        Title = string.IsNullOrEmpty(window.Title) ? "(No Title)" : window.Title;
        ProcessName = window.ProcessName;
        HandleHex = $"0x{window.Handle:X8}";
        Handle = window.Handle;
    }
}
