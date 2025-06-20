using System.Collections.ObjectModel;
using System.Linq;
using Auto_Sheets.Models;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Auto_Sheets.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly UIDocument _uiDocument;

        public ObservableCollection<ViewModel> AvailableViews { get; set; }
        public ObservableCollection<ViewModel> SelectedViews { get; set; }
        public ObservableCollection<ViewModel> SelectedAvailableViews { get; set; }

        public ObservableCollection<TitleBlockModel> TitleBlocks { get; set; }

        public RelayCommand AddSelectedViewsToGroupCommand { get; private set; }
        public RelayCommand ApplyGroupsCommand { get; private set; }
        public RelayCommand OkCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public ObservableCollection<ViewGroup> GroupsList { get; set; } = new ObservableCollection<ViewGroup>();

        private readonly Action _refreshAction;
        private readonly Action _closeAction;

        public string GroupName { get; set; }
        public string GroupNumber { get; set; }

        // We need to manually implement the property for selection to work correctly
        private TitleBlockModel _selectedTitleBlock;
        public TitleBlockModel SelectedTitleBlock
        {
            get => _selectedTitleBlock;
            set
            {
                if (_selectedTitleBlock != value)
                {
                    _selectedTitleBlock = value;
                    OnPropertyChanged(nameof(SelectedTitleBlock));

                    // Debug info to verify selection
                    if (_selectedTitleBlock != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Title block changed to: {_selectedTitleBlock.Name}, ID: {_selectedTitleBlock.Id.IntegerValue}");
                    }
                }
            }
        }

        public MainViewModel(UIDocument uiDocument, Action refreshAction, Action closeAction)
        {
            _uiDocument = uiDocument;
            _refreshAction = refreshAction;
            _closeAction = closeAction;

            AvailableViews = new ObservableCollection<ViewModel>();
            SelectedViews = new ObservableCollection<ViewModel>();
            SelectedAvailableViews = new ObservableCollection<ViewModel>();
            TitleBlocks = new ObservableCollection<TitleBlockModel>();

            OkCommand = new RelayCommand(ExecuteOk);
            CancelCommand = new RelayCommand(ExecuteCancel);
            AddSelectedViewsToGroupCommand = new RelayCommand(AddSelectedViewsToGroup);
            ApplyGroupsCommand = new RelayCommand(ApplyGroups);

            LoadViews();
            LoadTitleBlocks();
        }

        private void LoadViews()
        {
            var doc = _uiDocument.Document;

            var placedViewIds = new FilteredElementCollector(doc)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .Select(vp => vp.ViewId)
                .ToHashSet();

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v =>
                    !v.IsTemplate &&
                    v.ViewType != ViewType.ProjectBrowser &&
                    v.ViewType != ViewType.SystemBrowser &&
                    v.ViewType != ViewType.DrawingSheet &&
                    !placedViewIds.Contains(v.Id));

            foreach (var view in collector)
            {
                AvailableViews.Add(new ViewModel
                {
                    ViewName = view.Name,
                    ViewType = view.ViewType.ToString(),
                    ViewId = view.Id
                });
            }
        }

        private void LoadTitleBlocks()
        {
            var doc = _uiDocument.Document;

            // Clear existing title blocks first
            TitleBlocks.Clear();

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>();

            foreach (var symbol in collector)
            {
                // Double check that this Symbol belongs to a title block
                if (symbol != null && symbol.Category != null &&
                    symbol.Category.Id.IntegerValue == (int)BuiltInCategory.OST_TitleBlocks)
                {
                    // Include both family name and type name for better identification
                    string fullName = $"{symbol.Family.Name} : {symbol.Name}";

                    var titleBlockModel = new TitleBlockModel
                    {
                        Name = fullName,
                        Id = symbol.Id
                    };

                    TitleBlocks.Add(titleBlockModel);
                    System.Diagnostics.Debug.WriteLine($"Added title block: {fullName}, ID: {symbol.Id.IntegerValue}");
                }
            }

            if (TitleBlocks.Count > 0)
            {
                // Set the initial TitleBlock
                SelectedTitleBlock = TitleBlocks[0];
                System.Diagnostics.Debug.WriteLine($"Initially selected title block: {SelectedTitleBlock.Name}, ID: {SelectedTitleBlock.Id.IntegerValue}");
            }
            else
            {
                TaskDialog.Show("Warning", "No title blocks found in the project. Please load a title block family.");
            }
        }

        public void AddSelectedViewsToGroup()
        {
            if (string.IsNullOrEmpty(GroupName) || string.IsNullOrEmpty(GroupNumber))
            {
                TaskDialog.Show("Warning", "Please enter both Group Name and Group Number.");
                return;
            }

            var selected = SelectedAvailableViews.ToList();
            if (selected.Count == 0)
            {
                TaskDialog.Show("Warning", "Please select at least one view to add to the group.");
                return;
            }

            foreach (var view in selected)
            {
                AvailableViews.Remove(view);
                SelectedViews.Add(view);
            }

            SelectedAvailableViews.Clear();

            GroupsList.Add(new ViewGroup
            {
                GroupName = GroupName,
                GroupNumber = GroupNumber,
                Views = selected
            });

            // Clear input fields for next group
            GroupName = string.Empty;
            GroupNumber = string.Empty;
            OnPropertyChanged(nameof(GroupName));
            OnPropertyChanged(nameof(GroupNumber));
        }

        private void ApplyGroups()
        {
            var doc = _uiDocument.Document;

            if (SelectedTitleBlock == null)
            {
                TaskDialog.Show("Error", "Please select a Title Block.");
                return;
            }

            if (GroupsList.Count == 0)
            {
                TaskDialog.Show("Warning", "Please create at least one view group before applying.");
                return;
            }

            // Get the currently selected title block ID - this is crucial
            ElementId selectedTitleBlockId = SelectedTitleBlock.Id;

            System.Diagnostics.Debug.WriteLine($"Applying groups with title block: {SelectedTitleBlock.Name}, ID: {selectedTitleBlockId.IntegerValue}");

            using (Transaction trans = new Transaction(doc, "Create Sheets"))
            {
                trans.Start();

                try
                {
                    // Get the selected title block family symbol by ID
                    var titleBlockSymbol = doc.GetElement(selectedTitleBlockId) as FamilySymbol;

                    if (titleBlockSymbol == null)
                    {
                        TaskDialog.Show("Error", $"Title Block '{SelectedTitleBlock.Name}' not found. ID: {selectedTitleBlockId.IntegerValue}");
                        trans.RollBack();
                        return;
                    }

                    // Verify we found the correct title block
                    System.Diagnostics.Debug.WriteLine($"Retrieved title block: {titleBlockSymbol.Name} from family {titleBlockSymbol.Family.Name}");

                    // Make sure the Symbol is activated
                    if (!titleBlockSymbol.IsActive)
                    {
                        titleBlockSymbol.Activate();
                        doc.Regenerate();
                    }

                    List<string> createdSheets = new List<string>();
                    List<string> errors = new List<string>();

                    foreach (var group in GroupsList)
                    {
                        try
                        {
                            // Create a new sheet with the selected title block ID
                            ViewSheet sheet = ViewSheet.Create(doc, selectedTitleBlockId);

                            // Set sheet properties
                            sheet.Name = group.GroupName;
                            sheet.SheetNumber = group.GroupNumber;

                            createdSheets.Add($"{sheet.SheetNumber} - {sheet.Name}");
                            System.Diagnostics.Debug.WriteLine($"Created sheet: {sheet.SheetNumber} - {sheet.Name} with title block ID: {selectedTitleBlockId.IntegerValue}");

                            // Starting point for view placement
                            XYZ point = new XYZ(1, 1, 0);

                            // Place each view on the sheet
                            foreach (var viewModel in group.Views)
                            {
                                var view = doc.GetElement(viewModel.ViewId) as View;

                                if (view != null)
                                {
                                    // If the View is a Schedule
                                    if (view.ViewType == ViewType.Schedule)
                                    {
                                        try
                                        {
                                            ScheduleSheetInstance.Create(doc, sheet.Id, view.Id, point); // Here we add the schedule to the sheet
                                            point = new XYZ(point.X + 0.3, point.Y - 0.2, 0);
                                        }
                                        catch (Exception ex)
                                        {
                                            errors.Add($"Could not add the schedule '{view.Name}' to the sheet '{sheet.Name}': {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        // If not a schedule, use Viewport as usual
                                        if (Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
                                        {
                                            Viewport.Create(doc, sheet.Id, view.Id, point);
                                            point = new XYZ(point.X + 0.3, point.Y - 0.2, 0);
                                        }
                                        else
                                        {
                                            errors.Add($"Could not add the View '{view.Name}' to the sheet '{sheet.Name}'.");
                                        }
                                    }
                                }
                                else
                                {
                                    errors.Add($"The View '{viewModel.ViewName}' was not found in the project.");
                                }
                            }


                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Failed to create sheet for group '{group.GroupName}':\n{ex.Message}");
                        }
                    }

                    trans.Commit();

                    // Prepare result message
                    string message = "Sheets created successfully:\n";
                    foreach (var sheet in createdSheets)
                    {
                        message += $"• {sheet}\n";
                    }

                    if (errors.Count > 0)
                    {
                        message += "\nErrors encountered:\n";
                        foreach (var error in errors.Take(5))
                        {
                            message += $"• {error}\n";
                        }

                        if (errors.Count > 5)
                        {
                            message += $"...and {errors.Count - 5} more errors.";
                        }
                    }

                    TaskDialog.Show("Success", message);
                }
                catch (Exception ex)
                {
                    if (trans.HasStarted() && !trans.HasEnded())
                    {
                        trans.RollBack();
                    }
                    TaskDialog.Show("Error", $"Failed to create sheets:\n{ex.Message}");
                }
            }

            // Clear lists after operation
            GroupsList.Clear();
            SelectedViews.Clear();
            SelectedAvailableViews.Clear();
        }

        private void ExecuteOk()
        {
            _refreshAction?.Invoke();
        }

        private void ExecuteCancel()
        {
            _closeAction?.Invoke();
        }
    }
}