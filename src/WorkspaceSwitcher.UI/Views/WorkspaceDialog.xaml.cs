using System;
using System.Windows;
using WorkspaceSwitcher.Core.Hotkeys;
using WorkspaceSwitcher.Core.Localization;
using WorkspaceSwitcher.UI.ViewModels;

namespace WorkspaceSwitcher.UI.Views;

public partial class WorkspaceDialog : Window
{
    public string WorkspaceName { get; private set; } = string.Empty;
    public string WorkspaceDescription { get; private set; } = string.Empty;
    public string WorkspaceIcon { get; private set; } = "💻";
    public string HotkeyModifier { get; private set; } = "Ctrl + Alt";
    public string HotkeyKey { get; private set; } = "Auto (1-5)";
    public bool CaptureTaskbar { get; private set; } = false;

    public WorkspaceDialog(
        string? initialName = null, 
        string? initialDescription = null, 
        string? initialIcon = null, 
        string? initialModifier = null, 
        string? initialKey = null, 
        bool isEditMode = false)
    {
        InitializeComponent();

        var localizer = Localizer.Current;

        NameLabelText.Text = localizer.Get("WorkspaceDialog.NameLabel");
        DescriptionLabelText.Text = localizer.Get("WorkspaceDialog.DescriptionLabel");
        IconLabelText.Text = localizer.Get("WorkspaceDialog.IconLabel");
        HotkeyLabelText.Text = localizer.Get("WorkspaceDialog.HotkeyLabel");
        TaskbarTitleText.Text = localizer.Get("WorkspaceDialog.TaskbarLabel");
        TaskbarDescriptionText.Text = localizer.Get("WorkspaceDialog.TaskbarDescription");
        CancelButton.Content = localizer.Get("WorkspaceDialog.Cancel");

        IconListBox.ItemsSource = ProfileItemViewModel.AvailableIcons;
        var modifierOptions = HotkeyDisplay.CreateModifierOptions(localizer);
        ModifierComboBox.ItemsSource = modifierOptions;
        var keyOptions = HotkeyDisplay.CreateKeyOptions(localizer);
        KeyComboBox.ItemsSource = keyOptions;

        if (isEditMode)
        {
            Title = localizer.Get("WorkspaceDialog.Title.Edit");
            HeaderIconText.Text = "✏️";
            HeaderTitleText.Text = localizer.Get("WorkspaceDialog.Title.Edit");
            HeaderSubtitleText.Text = localizer.Get("WorkspaceDialog.Description.Edit");
            PrimaryButtonIconText.Text = "💾";
            PrimaryButtonText.Text = localizer.Get("WorkspaceDialog.Save.Edit");
            TaskbarOptionBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            Title = localizer.Get("WorkspaceDialog.Title.Create");
            HeaderIconText.Text = "📷";
            HeaderTitleText.Text = localizer.Get("WorkspaceDialog.Title.Create");
            HeaderSubtitleText.Text = localizer.Get("WorkspaceDialog.Description.Create");
            PrimaryButtonIconText.Text = "📸";
            PrimaryButtonText.Text = localizer.Get("WorkspaceDialog.Save.Create");
        }

        NameTextBox.Text = initialName ?? string.Empty;
        DescriptionTextBox.Text = initialDescription ?? string.Empty;
        
        string iconToSelect = string.IsNullOrWhiteSpace(initialIcon) ? "💻" : initialIcon;
        IconListBox.SelectedItem = iconToSelect;
        if (IconListBox.SelectedItem == null && ProfileItemViewModel.AvailableIcons.Count > 0)
        {
            IconListBox.SelectedItem = ProfileItemViewModel.AvailableIcons[0];
        }

        // Stored internal values select the matching option; display text never round-trips.
        ModifierComboBox.SelectedItem = HotkeyDisplay.Resolve(
            modifierOptions, string.IsNullOrWhiteSpace(initialModifier) ? "Ctrl + Alt" : initialModifier);
        KeyComboBox.SelectedItem = HotkeyDisplay.Resolve(
            keyOptions, string.IsNullOrWhiteSpace(initialKey) ? "Auto (1-5)" : initialKey);

        Loaded += (s, e) =>
        {
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        };
    }

    private void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        string name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show(
                Localizer.Current.Get("WorkspaceDialog.NameRequired"),
                Localizer.Current.Get("WorkspaceDialog.ValidationError"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        WorkspaceName = name;
        WorkspaceDescription = DescriptionTextBox.Text.Trim();
        WorkspaceIcon = IconListBox.SelectedItem?.ToString() ?? "💻";
        HotkeyModifier = (ModifierComboBox.SelectedItem as HotkeyOption)?.Value ?? "Ctrl + Alt";
        HotkeyKey = (KeyComboBox.SelectedItem as HotkeyOption)?.Value ?? "Auto (1-5)";
        CaptureTaskbar = CaptureTaskbarCheckBox.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
