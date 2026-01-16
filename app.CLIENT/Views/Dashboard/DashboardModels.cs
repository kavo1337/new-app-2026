using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace app.CLIENT;

public sealed class DashboardTile : INotifyPropertyChanged
{
    private string _statusText = "";

    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    public int EfficiencyPercent { get; set; }
    public int WorkingNum { get; set; }
    public int OfflineNum { get; set; }
    public int ServiceCount { get; set; }

    public decimal SalesNum { get; set; }
    public decimal CashNum{ get; set; }
    public int MaintenanceNum { get; set; }

    public ObservableCollection<ChartItem> ChartItems { get; } = new();
    public ObservableCollection<string> NewsItems { get; } = new();

    public string SelectedStatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ChartItem
{
    public string Day { get; init; } = string.Empty;
    public double BarHeight { get; init; }
    public string ValueText { get; init; } = string.Empty;
}
