using StructLink_X._0.Models;
using StructLink_X._0.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System;

namespace StructLink_X._0.ViewModels
{
    public class RebarEditorViewModel : ViewModelBase
    {
        private ObservableCollection<ColumnRCData> _columns;
        private ObservableCollection<BeamRCData> _beams;
        private readonly IEnumerable<ColumnRCData> _originalColumns;
        private readonly IEnumerable<BeamRCData> _originalBeams;

        public ObservableCollection<ColumnRCData> Columns
        {
            get => _columns;
            set { _columns = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BeamRCData> Beams
        {
            get => _beams;
            set { _beams = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public RebarEditorViewModel(IEnumerable<ColumnRCData> columns, IEnumerable<BeamRCData> beams)
        {
            if (columns == null || beams == null)
                throw new ArgumentNullException("Columns and Beams collections cannot be null.");

            _originalColumns = columns ?? new List<ColumnRCData>();
            _originalBeams = beams ?? new List<BeamRCData>();
            Columns = new ObservableCollection<ColumnRCData>(_originalColumns);
            Beams = new ObservableCollection<BeamRCData>(_originalBeams);

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Save(object parameter)
        {
            if (parameter is Window window)
            {
                // Basic validation (example: ensure no null values in critical fields)
                foreach (var column in Columns)
                {
                    if (column.ConcreteCover <= 0 || column.MainBarDiameter <= 0 || column.TieSpacing <= 0)
                    {
                        MessageBox.Show("Invalid data in Columns: Cover, Rebar Size, or Tie Spacing must be positive.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                foreach (var beam in Beams)
                {
                    if (beam.ConcreteCover <= 0 || beam.MainBarDiameter <= 0 || beam.TieSpacing <= 0)
                    {
                        MessageBox.Show("Invalid data in Beams: Cover, Rebar Size, or Tie Spacing must be positive.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Update original collections (if needed, pass back to parent view model)
                // This is a placeholder; you may want to implement a callback or event
                // For now, we just close the window with success
                window.DialogResult = true;
                window.Close();
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}