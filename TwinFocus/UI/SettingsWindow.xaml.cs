using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TwinFocus.Config;
using MessageBox = System.Windows.MessageBox;

namespace TwinFocus.UI;

/// <summary>
/// Settings window for TwinFocus configuration
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly AppConfig _config;

    public event EventHandler? SettingsSaved;

    public SettingsWindow(ConfigService configService, AppConfig config)
    {
        InitializeComponent();

        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        LoadSettings();
    }

    /// <summary>
    /// Load current settings into UI controls
    /// </summary>
    private void LoadSettings()
    {
        // Load hotkey modifiers
        if (_config.Hotkey.Modifiers != null)
        {
            ModCtrl.IsChecked = _config.Hotkey.Modifiers.Contains("CTRL");
            ModAlt.IsChecked = _config.Hotkey.Modifiers.Contains("ALT");
            ModShift.IsChecked = _config.Hotkey.Modifiers.Contains("SHIFT");
            ModWin.IsChecked = _config.Hotkey.Modifiers.Contains("WIN");
        }

        // Load hotkey key
        string key = _config.Hotkey.Key ?? "F4";
        foreach (ComboBoxItem item in KeyComboBox.Items)
        {
            if (item.Tag?.ToString() == key)
            {
                KeyComboBox.SelectedItem = item;
                break;
            }
        }

        UpdateCurrentHotkeyLabel();

        // Load ordering mode
        OrderScreen.IsChecked = _config.Ordering.Mode == "screenYThenX";
        OrderLastActive.IsChecked = _config.Ordering.Mode == "lastActive";
        OrderManual.IsChecked = _config.Ordering.Mode == "manual";

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
        OsdEnabledCheckBox.IsChecked = _config.Osd.Enabled;

        // Wire up events for live hotkey preview
        ModCtrl.Checked += (s, e) => UpdateCurrentHotkeyLabel();
        ModCtrl.Unchecked += (s, e) => UpdateCurrentHotkeyLabel();
        ModAlt.Checked += (s, e) => UpdateCurrentHotkeyLabel();
        ModAlt.Unchecked += (s, e) => UpdateCurrentHotkeyLabel();
        ModShift.Checked += (s, e) => UpdateCurrentHotkeyLabel();
        ModShift.Unchecked += (s, e) => UpdateCurrentHotkeyLabel();
        ModWin.Checked += (s, e) => UpdateCurrentHotkeyLabel();
        ModWin.Unchecked += (s, e) => UpdateCurrentHotkeyLabel();
        KeyComboBox.SelectionChanged += (s, e) => UpdateCurrentHotkeyLabel();
    }

    /// <summary>
    /// Update the "Current hotkey" preview label
    /// </summary>
    private void UpdateCurrentHotkeyLabel()
    {
        var parts = new System.Collections.Generic.List<string>();

        if (ModCtrl.IsChecked == true) parts.Add("Ctrl");
        if (ModAlt.IsChecked == true) parts.Add("Alt");
        if (ModShift.IsChecked == true) parts.Add("Shift");
        if (ModWin.IsChecked == true) parts.Add("Win");

        var selectedKey = (KeyComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "F4";
        parts.Add(selectedKey);

        CurrentHotkeyLabel.Text = $"Current: {string.Join("+", parts)}";
    }

    /// <summary>
    /// Save button clicked
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Build modifiers list
            var modifiers = new System.Collections.Generic.List<string>();
            if (ModCtrl.IsChecked == true) modifiers.Add("CTRL");
            if (ModAlt.IsChecked == true) modifiers.Add("ALT");
            if (ModShift.IsChecked == true) modifiers.Add("SHIFT");
            if (ModWin.IsChecked == true) modifiers.Add("WIN");
            if (modifiers.Count == 0) modifiers.Add("NONE");

            // Get selected key
            string key = (KeyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "F4";

            // Update config
            _config.Hotkey.Modifiers = modifiers;
            _config.Hotkey.Key = key;

            // Update ordering
            if (OrderScreen.IsChecked == true)
                _config.Ordering.Mode = "screenYThenX";
            else if (OrderLastActive.IsChecked == true)
                _config.Ordering.Mode = "lastActive";
            else if (OrderManual.IsChecked == true)
                _config.Ordering.Mode = "manual";

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
            _config.Osd.Enabled = OsdEnabledCheckBox.IsChecked ?? true;

            // Save to disk
            _configService.Save(_config);

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
}
