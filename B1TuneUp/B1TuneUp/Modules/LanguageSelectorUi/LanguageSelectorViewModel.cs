using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.LanguageSelectorUi
{
    public class LanguageSelectorViewModel : INotifyPropertyChanged
    {
        private string _selectedLanguage;
        private string _statusMessage;

        public LanguageSelectorViewModel()
        {
            Languages = new ObservableCollection<string>();
            LoadLanguages();
            SaveCommand = new RelayCommand(SaveLanguage, () => !string.IsNullOrWhiteSpace(SelectedLanguage));
        }

        public ObservableCollection<string> Languages { get; }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;
                _selectedLanguage = value;
                OnPropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveCommand { get; }

        private void LoadLanguages()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var langFolder = Path.Combine(basePath, "B1TuneUp", "Resources", "lang");
                if (!Directory.Exists(langFolder))
                {
                    langFolder = Path.Combine(basePath, "Resources", "lang");
                }
                if (!Directory.Exists(langFolder)) return;
                foreach (var file in Directory.GetFiles(langFolder, "*.json"))
                {
                    Languages.Add(Path.GetFileNameWithoutExtension(file));
                }
                var current = LocalizationManager.CurrentLanguage;
                if (!string.IsNullOrEmpty(current) && Languages.Contains(current))
                {
                    SelectedLanguage = current;
                }
                else if (Languages.Count > 0)
                {
                    SelectedLanguage = Languages[0];
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cargando idiomas: {ex.Message}";
            }
        }

        private void SaveLanguage()
        {
            try
            {
                SettingsManager.SetSetting("Language", SelectedLanguage);
                SettingsManager.SyncToDatabase();
                LocalizationManager.Init(SelectedLanguage);
                StatusMessage = LocalizationManager.GetString("LanguageSelector.Saved");
                Logger.Info("Language changed to " + SelectedLanguage);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error guardando idioma: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
