using System.ComponentModel;

namespace ClosetApp.UI.Components.Shared;

/// <summary>
/// 非泛型接口，供 WPF 转换器使用。
/// </summary>
public interface IEnumRadioGroup : INotifyPropertyChanged
{
    bool IsSelected(object? value);
    bool IsAllSelected { get; set; }
    void Select(object? value);
    object? SelectedValue { get; set; }
}

/// <summary>
/// 泛型 RadioButton 选择组，将 nullable enum 映射为一组 IsXxxSelected 布尔属性。
/// null 值对应"全部"选项。
/// </summary>
public sealed class EnumRadioGroup<TEnum> : IEnumRadioGroup where TEnum : struct, Enum
{
    private static readonly EqualityComparer<TEnum?> Comparer = EqualityComparer<TEnum?>.Default;

    private TEnum? _selected;
    private readonly Action<TEnum?>? _onChanged;

    public EnumRadioGroup(Action<TEnum?>? onChanged = null, TEnum? initial = null)
    {
        _onChanged = onChanged;
        _selected = initial;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TEnum? Selected
    {
        get => _selected;
        set
        {
            if (Comparer.Equals(_selected, value))
                return;

            _selected = value;
            RaiseSelectionChanged();
            _onChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// 对应"全部"选项（null 值）。
    /// </summary>
    public bool IsAllSelected
    {
        get => _selected == null;
        set { if (value) Selected = null; }
    }

    /// <summary>
    /// 判断指定枚举值是否被选中。
    /// </summary>
    public bool IsSelected(TEnum value)
    {
        return Comparer.Equals(_selected, value);
    }

    /// <summary>
    /// 选中指定枚举值。
    /// </summary>
    public void Select(TEnum value)
    {
        Selected = value;
    }

    // IEnumRadioGroup 显式实现
    bool IEnumRadioGroup.IsSelected(object? value)
    {
        if (value is TEnum typed)
            return IsSelected(typed);
        if (value == null)
            return IsAllSelected;
        return false;
    }

    void IEnumRadioGroup.Select(object? value)
    {
        if (value is TEnum typed)
            Select(typed);
        else if (value == null)
            Selected = null;
    }

    object? IEnumRadioGroup.SelectedValue
    {
        get => _selected;
        set
        {
            if (value == null)
                Selected = null;
            else if (value is TEnum typed)
                Selected = typed;
        }
    }

    private void RaiseSelectionChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAllSelected)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}