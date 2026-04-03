using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using B1TuneUp.Modules;
using B1TuneUp.Modules.IntegrationUi;
using SAPbouiCOM;

namespace B1TuneUp.Modules.ProcessStepsUi
{
    public class ProcessStepsViewModel : INotifyPropertyChanged
    {
        private readonly ProcessInfo _processInfo;
        private readonly Form _sapForm;
        private readonly string _processEntry;
        private ProcessStep _selectedStep;
        private string _description;
        private string _progressText;
        private int _progressValue;

        public ProcessStepsViewModel(ProcessInfo info, System.Collections.Generic.List<ProcessStep> steps, Form sapForm, string processEntry)
        {
            _processInfo = info;
            _sapForm = sapForm;
            _processEntry = processEntry;
            Steps = new ObservableCollection<ProcessStep>(steps ?? new System.Collections.Generic.List<ProcessStep>());
            RefreshProgress();
            ExecuteCommand = new RelayCommand(ExecuteStep, () => SelectedStep != null && !string.IsNullOrEmpty(SelectedStep.Action));
            RefreshCommand = new RelayCommand(RefreshSteps);
        }

        public ObservableCollection<ProcessStep> Steps { get; }

        public string ProcessTitle => string.IsNullOrWhiteSpace(_processInfo?.Name) ? "Process Steps" : _processInfo.Name;

        public ProcessStep SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (_selectedStep == value) return;
                _selectedStep = value;
                Description = _selectedStep?.Desc ?? "Seleccione un paso para ver su descripción.";
                OnPropertyChanged();
                ExecuteCommand.RaiseCanExecuteChanged();
            }
        }

        public string Description
        {
            get => _description;
            private set
            {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public string ProgressText
        {
            get => _progressText;
            private set
            {
                if (_progressText == value) return;
                _progressText = value;
                OnPropertyChanged();
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            private set
            {
                if (_progressValue == value) return;
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ExecuteCommand { get; }
        public RelayCommand RefreshCommand { get; }

        private void ExecuteStep()
        {
            if (SelectedStep == null) return;
            ProcessStepsManager.ExecuteStepAction(SelectedStep, _sapForm);
            RefreshSteps();
        }

        private void RefreshSteps()
        {
            var latest = ProcessStepsManager.LoadSteps(_processEntry, _sapForm);
            Steps.Clear();
            foreach (var step in latest)
            {
                Steps.Add(step);
            }
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            if (Steps.Count == 0)
            {
                ProgressValue = 0;
                ProgressText = "Sin pasos configurados.";
                return;
            }
            int done = 0;
            foreach (var step in Steps)
            {
                if (step.IsDone) done++;
            }
            int pct = (int)((double)done / Steps.Count * 100);
            ProgressValue = pct;
            ProgressText = $"{done}/{Steps.Count} pasos completados ({pct}%).";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
