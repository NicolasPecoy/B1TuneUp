using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.RuleBuilder
{
    public class RuleBuilderViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<B1Rule> _rules = new ObservableCollection<B1Rule>();
        private readonly ICollectionView _view;
        private B1Rule _selectedRule;
        private string _formFilter;
        private RuleType? _typeFilter;
        private string _searchFilter;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;
        private IReadOnlyList<string> _formTypes = new List<string>();

        public RuleBuilderViewModel()
        {
            _view = CollectionViewSource.GetDefaultView(_rules);
            _view.Filter = FilterRule;

            RuleTypeOptions = (RuleType[])Enum.GetValues(typeof(RuleType));
            EventSuggestions = new[]
            {
                "et_FORM_LOAD","et_ITEM_PRESSED","et_CLICK","et_FORM_DATA_ADD","et_FORM_DATA_UPDATE","et_KEY_DOWN"
            };

            NewCommand = new RelayCommand(NewRule);
            DuplicateCommand = new RelayCommand(DuplicateRule, () => SelectedRule != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => SelectedRule != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedRule != null);
            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            TestConditionCommand = new RelayCommand(TestCondition, () => SelectedRule != null);
            RunActionCommand = new RelayCommand(RunAction, () => SelectedRule != null);
        }

        public ObservableCollection<B1Rule> Rules => _rules;
        public ICollectionView FilteredRules => _view;
        public IReadOnlyList<RuleType> RuleTypeOptions { get; }
        public IReadOnlyList<string> EventSuggestions { get; }

        public IReadOnlyList<string> FormTypes
        {
            get => _formTypes;
            private set { _formTypes = value; OnPropertyChanged(); }
        }

        public B1Rule SelectedRule
        {
            get => _selectedRule;
            set
            {
                if (_selectedRule == value) return;
                _selectedRule = value;
                OnPropertyChanged();
                RaiseCommandStates();
            }
        }

        public string FormFilter
        {
            get => _formFilter;
            set
            {
                if (_formFilter == value) return;
                _formFilter = value;
                OnPropertyChanged();
                _view.Refresh();
            }
        }

        public RuleType? TypeFilter
        {
            get => _typeFilter;
            set
            {
                bool changed = !((_typeFilter.HasValue == value.HasValue) && (!_typeFilter.HasValue || _typeFilter.Value == value.Value));
                if (!changed) return;
                _typeFilter = value;
                OnPropertyChanged();
                _view.Refresh();
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter == value) return;
                _searchFilter = value;
                OnPropertyChanged();
                _view.Refresh();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            private set { if (_busyMessage == value) return; _busyMessage = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { if (_statusMessage == value) return; _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand TestConditionCommand { get; }
        public RelayCommand RunActionCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando reglas...", async () =>
            {
                var list = await Task.Run(() => RuleService.GetAll());
                var forms = await Task.Run(() => RuleService.GetDistinctFormTypes());

                _rules.Clear();
                foreach (var rule in list)
                {
                    _rules.Add(rule);
                }

                FormTypes = forms;

                if (_rules.Count == 0)
                {
                    SelectedRule = CreateDefaultRule();
                }
                else if (SelectedRule == null)
                {
                    SelectedRule = _rules.First();
                }
            });
        }

        private B1Rule CreateDefaultRule()
        {
            return new B1Rule
            {
                FormType = FormFilter,
                EventType = "et_FORM_LOAD",
                Type = RuleType.Macro,
                BeforeAction = false
            };
        }

        private void NewRule()
        {
            SelectedRule = CreateDefaultRule();
        }

        private void DuplicateRule()
        {
            if (SelectedRule == null) return;
            var copy = new B1Rule
            {
                ID = null,
                Name = SelectedRule.Name,
                FormType = SelectedRule.FormType,
                Type = SelectedRule.Type,
                EventType = SelectedRule.EventType,
                BeforeAction = SelectedRule.BeforeAction,
                Condition = SelectedRule.Condition,
                Action = SelectedRule.Action
            };
            _rules.Add(copy);
            SelectedRule = copy;
        }

        private async Task SaveAsync()
        {
            if (SelectedRule == null) return;
            await RunSafeAsync("Guardando regla...", async () =>
            {
                await Task.Run(() => RuleService.Save(SelectedRule));
                if (!_rules.Contains(SelectedRule)) _rules.Add(SelectedRule);
                StatusMessage = "Regla guardada correctamente.";
                _view.Refresh();
            });
        }

        private async Task DeleteAsync()
        {
            if (SelectedRule == null) return;
            var rule = SelectedRule;
            await RunSafeAsync("Eliminando regla...", async () =>
            {
                await Task.Run(() => RuleService.Delete(rule.ID));
                _rules.Remove(rule);
                SelectedRule = _rules.FirstOrDefault() ?? CreateDefaultRule();
                StatusMessage = "Regla eliminada.";
            });
        }

        private void TestCondition()
        {
            if (SelectedRule == null) return;
            bool result = RuleService.TestCondition(SelectedRule);
            StatusMessage = result ? "Condición TRUE." : "Condición FALSE.";
        }

        private void RunAction()
        {
            if (SelectedRule == null) return;
            RuleService.ExecuteAction(SelectedRule);
            StatusMessage = "Macro ejecutada.";
        }

        private bool FilterRule(object obj)
        {
            var rule = obj as B1Rule;
            if (rule == null) return false;
            if (!string.IsNullOrEmpty(FormFilter) && (rule.FormType ?? "").IndexOf(FormFilter, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            if (TypeFilter.HasValue && rule.Type != TypeFilter.Value)
                return false;
            if (string.IsNullOrEmpty(SearchFilter)) return true;
            return (rule.EventType ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (rule.Action ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (rule.Condition ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task RunSafeAsync(string message, Func<Task> work)
        {
            try
            {
                IsBusy = true;
                BusyMessage = message;
                await work();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
                RaiseCommandStates();
            }
        }

        private void RaiseCommandStates()
        {
            DuplicateCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            TestConditionCommand.RaiseCanExecuteChanged();
            RunActionCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
