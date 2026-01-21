using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DietSentry
{
    public partial class WeightTablePage : ContentPage
    {
        private const string WeightDateFormat = "d-MMM-yy";
        private readonly FoodDatabaseService _databaseService = new();
        private WeightEntry? _selectedWeight;
        private bool _showAddPanel;
        private bool _showEditPanel;
        private bool _showDeletePanel;

        public ObservableCollection<WeightEntry> WeightEntries { get; } = new();

        public WeightEntry? SelectedWeight
        {
            get => _selectedWeight;
            private set
            {
                if (_selectedWeight == value)
                {
                    return;
                }

                _selectedWeight = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSelectionPanel));
                OnPropertyChanged(nameof(SelectedWeightDateDisplay));
                OnPropertyChanged(nameof(SelectedWeightValueDisplay));
            }
        }

        public bool ShowSelectionPanel => SelectedWeight != null;

        public string SelectedWeightDateDisplay => SelectedWeight == null
            ? string.Empty
            : string.IsNullOrWhiteSpace(SelectedWeight.DateWeight)
                ? "Unknown date"
                : SelectedWeight.DateWeight;

        public string SelectedWeightValueDisplay => SelectedWeight == null
            ? string.Empty
            : $"{FormatWeight(SelectedWeight.Weight)} kg";

        public bool ShowAddPanel
        {
            get => _showAddPanel;
            private set
            {
                if (_showAddPanel == value)
                {
                    return;
                }

                _showAddPanel = value;
                OnPropertyChanged();
            }
        }

        public bool ShowEditPanel
        {
            get => _showEditPanel;
            private set
            {
                if (_showEditPanel == value)
                {
                    return;
                }

                _showEditPanel = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDeletePanel
        {
            get => _showDeletePanel;
            private set
            {
                if (_showDeletePanel == value)
                {
                    return;
                }

                _showDeletePanel = value;
                OnPropertyChanged();
            }
        }

        public WeightTablePage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadWeightsAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            if (ShowDeletePanel)
            {
                ShowDeletePanel = false;
                ClearSelection();
                return true;
            }

            if (ShowEditPanel)
            {
                ShowEditPanel = false;
                ClearSelection();
                return true;
            }

            if (ShowAddPanel)
            {
                ShowAddPanel = false;
                ClearSelection();
                return true;
            }

            if (SelectedWeight != null)
            {
                ClearSelection();
                return true;
            }

            return base.OnBackButtonPressed();
        }

        private async Task LoadWeightsAsync()
        {
            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                var entries = await _databaseService.GetWeightEntriesAsync();
                var ordered = entries
                    .OrderByDescending(entry => ParseWeightDate(entry.DateWeight) ?? DateTime.MinValue)
                    .ThenByDescending(entry => entry.WeightId)
                    .ToList();

                WeightEntries.Clear();
                foreach (var entry in ordered)
                {
                    WeightEntries.Add(entry);
                }

                ClearSelection();
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Error", "Unable to load weight records.", "OK");
            }
        }

        private void ClearSelection()
        {
            SelectedWeight = null;
            if (WeightCollectionView != null)
            {
                WeightCollectionView.SelectedItem = null;
            }
        }

        private void OnWeightSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedWeight = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as WeightEntry : null;
        }

        private void OnAddClicked(object? sender, EventArgs e)
        {
            AddDatePicker.Date = DateTime.Today;
            AddWeightEntry.Text = string.Empty;
            AddCommentsEditor.Text = string.Empty;
            ShowAddPanel = true;
        }

        private async void OnAddConfirmClicked(object? sender, EventArgs e)
        {
            if (!TryParseWeight(AddWeightEntry.Text, out var weightValue) || weightValue <= 0)
            {
                await DisplayAlertAsync("Invalid weight", "Enter a valid weight in kg.", "OK");
                return;
            }

            var selectedDate = AddDatePicker.Date ?? DateTime.Today;
            var dateText = selectedDate.ToString(WeightDateFormat, CultureInfo.CurrentCulture);
            if (WeightEntries.Any(entry =>
                    string.Equals(entry.DateWeight, dateText, StringComparison.OrdinalIgnoreCase)))
            {
                await DisplayAlertAsync("Date exists", "A weight record already exists for that date.", "OK");
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var saved = await _databaseService.InsertWeightAsync(
                dateText,
                weightValue,
                AddCommentsEditor.Text?.Trim() ?? string.Empty);
            if (!saved)
            {
                await DisplayAlertAsync("Error", "Unable to save the weight record.", "OK");
                return;
            }

            ShowAddPanel = false;
            ClearSelection();
            await LoadWeightsAsync();
        }

        private void OnAddCancelClicked(object? sender, EventArgs e)
        {
            ShowAddPanel = false;
            ClearSelection();
        }

        private void OnAddBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowAddPanel = false;
            ClearSelection();
        }

        private void OnEditSelectedClicked(object? sender, EventArgs e)
        {
            if (SelectedWeight == null)
            {
                return;
            }

            EditDateLabel.Text = string.IsNullOrWhiteSpace(SelectedWeight.DateWeight)
                ? "-"
                : SelectedWeight.DateWeight;
            EditWeightEntry.Text = FormatWeight(SelectedWeight.Weight);
            EditCommentsEditor.Text = SelectedWeight.Comments;
            ShowEditPanel = true;
        }

        private async void OnEditConfirmClicked(object? sender, EventArgs e)
        {
            if (SelectedWeight == null)
            {
                ShowEditPanel = false;
                ClearSelection();
                return;
            }

            if (!TryParseWeight(EditWeightEntry.Text, out var weightValue) || weightValue <= 0)
            {
                await DisplayAlertAsync("Invalid weight", "Enter a valid weight in kg.", "OK");
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var saved = await _databaseService.UpdateWeightAsync(
                SelectedWeight.WeightId,
                SelectedWeight.DateWeight,
                weightValue,
                EditCommentsEditor.Text?.Trim() ?? string.Empty);
            if (!saved)
            {
                await DisplayAlertAsync("Error", "Unable to update the weight record.", "OK");
                return;
            }

            ShowEditPanel = false;
            ClearSelection();
            await LoadWeightsAsync();
        }

        private void OnEditCancelClicked(object? sender, EventArgs e)
        {
            ShowEditPanel = false;
            ClearSelection();
        }

        private void OnEditBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowEditPanel = false;
            ClearSelection();
        }

        private void OnDeleteSelectedClicked(object? sender, EventArgs e)
        {
            if (SelectedWeight == null)
            {
                return;
            }

            DeleteDetailsLabel.Text = $"{SelectedWeightDateDisplay}    {SelectedWeightValueDisplay}";
            ShowDeletePanel = true;
        }

        private async void OnDeleteConfirmClicked(object? sender, EventArgs e)
        {
            if (SelectedWeight == null)
            {
                ShowDeletePanel = false;
                ClearSelection();
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var deleted = await _databaseService.DeleteWeightAsync(SelectedWeight.WeightId);
            if (!deleted)
            {
                await DisplayAlertAsync("Error", "Unable to delete the weight record.", "OK");
                return;
            }

            ShowDeletePanel = false;
            ClearSelection();
            await LoadWeightsAsync();
        }

        private void OnDeleteCancelClicked(object? sender, EventArgs e)
        {
            ShowDeletePanel = false;
            ClearSelection();
        }

        private void OnDeleteBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowDeletePanel = false;
            ClearSelection();
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//foodSearch");
        }

        private static bool TryParseWeight(string? input, out double weightValue)
        {
            var normalized = (input ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace(',', '.');
            return double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out weightValue);
        }

        private static DateTime? ParseWeightDate(string? dateText)
        {
            if (string.IsNullOrWhiteSpace(dateText))
            {
                return null;
            }

            if (DateTime.TryParseExact(
                    dateText,
                    WeightDateFormat,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return date;
            }

            if (DateTime.TryParseExact(
                    dateText,
                    WeightDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date))
            {
                return date;
            }

            if (DateTime.TryParse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }

        private static string FormatWeight(double weight)
        {
            return weight.ToString("0.0", CultureInfo.CurrentCulture);
        }
    }
}
