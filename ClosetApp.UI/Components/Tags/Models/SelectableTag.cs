using System.ComponentModel;
using System.Windows.Media;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Components.Tags.Models;

public class SelectableTag : INotifyPropertyChanged
{
    public Tag Tag { get; }
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
    }

    private double _unselectedOpacity = 1.0;
    public double UnselectedOpacity
    {
        get => _unselectedOpacity;
        set { if (Math.Abs(_unselectedOpacity - value) > 0.01) { _unselectedOpacity = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnselectedOpacity))); } }
    }

    public Brush SoftBackground => GetBrush(0.12);
    public Brush SelectedBackground => GetBrush(0.22);
    public Brush BorderBrush => GetBrush(0.85);

    private SolidColorBrush GetBrush(double opacity)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(Tag.Color);
            return new SolidColorBrush(Color.FromArgb(
                (byte)(opacity * 255), color.R, color.G, color.B));
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    public SelectableTag(Tag tag) => Tag = tag;
    public event PropertyChangedEventHandler? PropertyChanged;
}
