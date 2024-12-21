// Path: QuickTechSystems.WPF/ViewModels/SettingsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.WPF.Commands;

namespace QuickTechSystems.WPF.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IBusinessSettingsService _businessSettingsService;
        private readonly ISystemPreferencesService _systemPreferencesService;
        private ObservableCollection<BusinessSettingDTO> _businessSettings;
        private BusinessSettingDTO? _selectedBusinessSetting;
        private string _selectedGroup = "All";
        private bool _isEditing;

        public ObservableCollection<BusinessSettingDTO> BusinessSettings
        {
            get => _businessSettings;
            set => SetProperty(ref _businessSettings, value);
        }

        public BusinessSettingDTO? SelectedBusinessSetting
        {
            get => _selectedBusinessSetting;
            set
            {
                SetProperty(ref _selectedBusinessSetting, value);
                IsEditing = value != null;
            }
        }

        public string SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                SetProperty(ref _selectedGroup, value);
                _ = LoadSettingsByGroupAsync();
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public ObservableCollection<string> Groups { get; } = new ObservableCollection<string>
        {
            "All", "General", "Financial", "Inventory", "Sales"
        };

        // System Preferences Properties
        private string _currentTheme = "Light";
        private string _currentLanguage = "en-US";
        private bool _soundEffectsEnabled = true;
        private bool _notificationsEnabled = true;
        private string _dateFormat = "MM/dd/yyyy";
        private string _timeFormat = "HH:mm:ss";

        public string CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (SetProperty(ref _currentTheme, value))
                {
                    _ = SavePreferenceAsync("Theme", value);
                }
            }
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (SetProperty(ref _currentLanguage, value))
                {
                    _ = SavePreferenceAsync("Language", value);
                }
            }
        }

        public bool SoundEffectsEnabled
        {
            get => _soundEffectsEnabled;
            set
            {
                if (SetProperty(ref _soundEffectsEnabled, value))
                {
                    _ = SavePreferenceAsync("SoundEffects", value.ToString());
                }
            }
        }

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set
            {
                if (SetProperty(ref _notificationsEnabled, value))
                {
                    _ = SavePreferenceAsync("EnableNotifications", value.ToString());
                }
            }
        }

        public string DateFormat
        {
            get => _dateFormat;
            set
            {
                if (SetProperty(ref _dateFormat, value))
                {
                    _ = SavePreferenceAsync("DateFormat", value);
                }
            }
        }

        public string TimeFormat
        {
            get => _timeFormat;
            set
            {
                if (SetProperty(ref _timeFormat, value))
                {
                    _ = SavePreferenceAsync("TimeFormat", value);
                }
            }
        }

        public ObservableCollection<string> AvailableThemes { get; } = new()
        {
            "Light", "Dark", "System"
        };

        public ObservableCollection<string> AvailableLanguages { get; } = new()
        {
            "en-US", "es-ES", "fr-FR", "de-DE"
        };

        public ObservableCollection<string> DateFormats { get; } = new()
        {
            "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"
        };

        public ObservableCollection<string> TimeFormats { get; } = new()
        {
            "HH:mm:ss", "hh:mm:ss tt"
        };

        // Commands
        public ICommand LoadCommand { get; }
        public ICommand SaveBusinessSettingCommand { get; }
        public ICommand InitializeBusinessSettingsCommand { get; }
        public ICommand ResetPreferencesCommand { get; }

        public SettingsViewModel(
            IBusinessSettingsService businessSettingsService,
            ISystemPreferencesService systemPreferencesService)
        {
            _businessSettingsService = businessSettingsService ?? throw new ArgumentNullException(nameof(businessSettingsService));
            _systemPreferencesService = systemPreferencesService ?? throw new ArgumentNullException(nameof(systemPreferencesService));
            _businessSettings = new ObservableCollection<BusinessSettingDTO>();

            LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            SaveBusinessSettingCommand = new AsyncRelayCommand(async _ => await SaveBusinessSettingAsync());
            InitializeBusinessSettingsCommand = new AsyncRelayCommand(async _ => await InitializeBusinessSettingsAsync());
            ResetPreferencesCommand = new AsyncRelayCommand(async _ => await ResetPreferencesAsync());

            _ = LoadDataAsync();
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                await LoadBusinessSettingsAsync();
                await LoadSystemPreferencesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadBusinessSettingsAsync()
        {
            var settings = await _businessSettingsService.GetAllAsync();
            BusinessSettings = new ObservableCollection<BusinessSettingDTO>(settings);
        }

        private async Task LoadSettingsByGroupAsync()
        {
            try
            {
                if (SelectedGroup == "All")
                {
                    await LoadBusinessSettingsAsync();
                    return;
                }

                var settings = await _businessSettingsService.GetByGroupAsync(SelectedGroup);
                BusinessSettings = new ObservableCollection<BusinessSettingDTO>(settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveBusinessSettingAsync()
        {
            try
            {
                if (SelectedBusinessSetting == null) return;

                if (string.IsNullOrWhiteSpace(SelectedBusinessSetting.Value))
                {
                    MessageBox.Show("Setting value cannot be empty.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _businessSettingsService.UpdateSettingAsync(
                    SelectedBusinessSetting.Key,
                    SelectedBusinessSetting.Value,
                    "CurrentUser" // Replace with actual user info when authentication is implemented
                );

                await LoadSettingsByGroupAsync();
                MessageBox.Show("Setting updated successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving setting: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitializeBusinessSettingsAsync()
        {
            try
            {
                if (MessageBox.Show("This will initialize default business settings. Continue?",
                    "Confirm Initialize", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _businessSettingsService.InitializeDefaultSettingsAsync();
                    await LoadBusinessSettingsAsync();
                    MessageBox.Show("Default settings initialized successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadSystemPreferencesAsync()
        {
            try
            {
                const string userId = "default"; // Replace with actual user ID when authentication is implemented
                CurrentTheme = await _systemPreferencesService.GetPreferenceValueAsync(userId, "Theme", "Light");
                CurrentLanguage = await _systemPreferencesService.GetPreferenceValueAsync(userId, "Language", "en-US");
                SoundEffectsEnabled = bool.Parse(await _systemPreferencesService.GetPreferenceValueAsync(userId, "SoundEffects", "true"));
                NotificationsEnabled = bool.Parse(await _systemPreferencesService.GetPreferenceValueAsync(userId, "EnableNotifications", "true"));
                DateFormat = await _systemPreferencesService.GetPreferenceValueAsync(userId, "DateFormat", "MM/dd/yyyy");
                TimeFormat = await _systemPreferencesService.GetPreferenceValueAsync(userId, "TimeFormat", "HH:mm:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preferences: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SavePreferenceAsync(string key, string value)
        {
            try
            {
                const string userId = "default"; // Replace with actual user ID when authentication is implemented
                await _systemPreferencesService.SavePreferenceAsync(userId, key, value);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving preference: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ResetPreferencesAsync()
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to reset all preferences to default?",
                    "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    const string userId = "default";
                    await _systemPreferencesService.InitializeUserPreferencesAsync(userId);
                    await LoadSystemPreferencesAsync();
                    MessageBox.Show("Preferences have been reset to default values.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting preferences: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}