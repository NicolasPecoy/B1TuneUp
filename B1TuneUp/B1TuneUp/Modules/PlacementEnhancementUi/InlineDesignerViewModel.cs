using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Modules.ItemActionsUi;
using B1TuneUp.Modules.ValidationUi;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public class InlineDesignerViewModel : INotifyPropertyChanged
    {
        private readonly InlineDesignerSession _session;
        private InlineDesignerItem _selectedItem;
        private bool _snapToGrid = true;
        private double _gridSize = 5;
        private string _statusMessage;

        public InlineDesignerViewModel(InlineDesignerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Items = session.Items;
            if (!string.IsNullOrWhiteSpace(session.DefaultItemId))
            {
                SelectedItem = Items.FirstOrDefault(i => i.ItemId == session.DefaultItemId);
            }
            if (SelectedItem == null && Items.Count > 0)
            {
                SelectedItem = Items[0];
            }

            ApplyCommand = new RelayCommand(_ => Apply(false));
            PersistCommand = new RelayCommand(_ => Apply(true));
            CloseCommand = new RelayCommand(window => (window as Window)?.Close());
            OpenValidationCommand = new RelayCommand(_ => OpenValidation(), _ => SelectedItem != null);
            OpenActionCommand = new RelayCommand(_ => OpenActions(), _ => SelectedItem != null);
            ExportCommand = new RelayCommand(_ => ExportPackage());
            ImportCommand = new RelayCommand(_ => ImportPackage());
        }

        public ObservableCollection<InlineDesignerItem> Items { get; }
        public double FormWidth => _session.FormWidth;
        public double FormHeight => _session.FormHeight;
        public string FormTitle => _session.FormTitle;
        public string FormType => _session.FormType;

        public InlineDesignerItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                if (_selectedItem != null) _selectedItem.IsSelected = false;
                _selectedItem = value;
                if (_selectedItem != null) _selectedItem.IsSelected = true;
                OnPropertyChanged();
                OpenValidationCommand.RaiseCanExecuteChanged();
                OpenActionCommand.RaiseCanExecuteChanged();
            }
        }

        public bool SnapToGrid
        {
            get => _snapToGrid;
            set
            {
                if (_snapToGrid == value) return;
                _snapToGrid = value;
                OnPropertyChanged();
            }
        }

        public double GridSize
        {
            get => _gridSize;
            set
            {
                if (Math.Abs(_gridSize - value) < 0.1) return;
                _gridSize = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ApplyCommand { get; }
        public RelayCommand PersistCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand OpenValidationCommand { get; }
        public RelayCommand OpenActionCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand ImportCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void MoveItem(InlineDesignerItem item, double deltaX, double deltaY)
        {
            if (item == null) return;
            double nx = item.Left + deltaX;
            double ny = item.Top + deltaY;
            if (SnapToGrid)
            {
                nx = Math.Round(nx / GridSize) * GridSize;
                ny = Math.Round(ny / GridSize) * GridSize;
            }
            item.Left = Math.Max(0, nx);
            item.Top = Math.Max(0, ny);
        }

        public void ResizeItem(InlineDesignerItem item, double deltaX, double deltaY)
        {
            if (item == null) return;
            double nw = Math.Max(10, item.Width + deltaX);
            double nh = Math.Max(6, item.Height + deltaY);
            if (SnapToGrid)
            {
                nw = Math.Round(nw / GridSize) * GridSize;
                nh = Math.Round(nh / GridSize) * GridSize;
            }
            item.Width = nw;
            item.Height = nh;
        }

        private void Apply(bool persist)
        {
            try
            {
                _session.ApplyLayout(persist);
                StatusMessage = persist ? "Diseño guardado en BTUN_UI y aplicado." : "Diseño aplicado temporalmente.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void OpenValidation()
        {
            ValidationDesignerLauncher.Show(FormType, SelectedItem?.ItemId);
        }

        private void OpenActions()
        {
            ItemActionsLauncher.Show(FormType, SelectedItem?.ItemId);
        }

        private void ExportPackage()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "B1TuneUp UI Package (*.b1pkg)|*.b1pkg|JSON (*.json)|*.json",
                FileName = $"{FormType}_UiPackage.b1pkg"
            };
            if (dialog.ShowDialog() == true)
            {
                UICustomizerService.ExportPackage(FormType, dialog.FileName);
                StatusMessage = $"Paquete exportado: {Path.GetFileName(dialog.FileName)}";
            }
        }

        private void ImportPackage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "B1TuneUp UI Package (*.b1pkg;*.json)|*.b1pkg;*.json",
                Title = "Importar paquete UI"
            };
            if (dialog.ShowDialog() == true)
            {
                UICustomizerService.ImportPackage(dialog.FileName);
                StatusMessage = $"Paquete importado desde {Path.GetFileName(dialog.FileName)}";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
