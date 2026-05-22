using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClosetApp.UI.Components.Shared.States;

public partial class EmptyState : UserControl
{
    public static readonly RoutedEvent ActionClickedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ActionClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(EmptyState));

    public static readonly RoutedEvent SecondaryActionClickedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(SecondaryActionClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(EmptyState));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(EmptyState), new PropertyMetadata("✨", OnIconChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata("暂无内容", OnTitleChanged));

    public static readonly DependencyProperty BodyProperty =
        DependencyProperty.Register(nameof(Body), typeof(string), typeof(EmptyState), new PropertyMetadata("先创建一项内容吧", OnBodyChanged));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyState), new PropertyMetadata("开始创建", OnActionTextChanged));

    public static readonly DependencyProperty ActionVisibleProperty =
        DependencyProperty.Register(nameof(ActionVisible), typeof(Visibility), typeof(EmptyState), new PropertyMetadata(Visibility.Visible, OnActionVisibleChanged));

    public static readonly DependencyProperty IsActionEnabledProperty =
        DependencyProperty.Register(nameof(IsActionEnabled), typeof(bool), typeof(EmptyState), new PropertyMetadata(true, OnIsActionEnabledChanged));

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyState), new PropertyMetadata(null));

    public static readonly DependencyProperty ActionCommandParameterProperty =
        DependencyProperty.Register(nameof(ActionCommandParameter), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryActionTextProperty =
        DependencyProperty.Register(nameof(SecondaryActionText), typeof(string), typeof(EmptyState), new PropertyMetadata("辅助操作", OnSecondaryActionTextChanged));

    public static readonly DependencyProperty SecondaryActionVisibleProperty =
        DependencyProperty.Register(nameof(SecondaryActionVisible), typeof(Visibility), typeof(EmptyState), new PropertyMetadata(Visibility.Collapsed, OnSecondaryActionVisibleChanged));

    public static readonly DependencyProperty IsSecondaryActionEnabledProperty =
        DependencyProperty.Register(nameof(IsSecondaryActionEnabled), typeof(bool), typeof(EmptyState), new PropertyMetadata(true, OnIsSecondaryActionEnabledChanged));

    public static readonly DependencyProperty SecondaryActionCommandProperty =
        DependencyProperty.Register(nameof(SecondaryActionCommand), typeof(ICommand), typeof(EmptyState), new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryActionCommandParameterProperty =
        DependencyProperty.Register(nameof(SecondaryActionCommandParameter), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    public EmptyState()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler ActionClicked
    {
        add => AddHandler(ActionClickedEvent, value);
        remove => RemoveHandler(ActionClickedEvent, value);
    }

    public event RoutedEventHandler SecondaryActionClicked
    {
        add => AddHandler(SecondaryActionClickedEvent, value);
        remove => RemoveHandler(SecondaryActionClickedEvent, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public Visibility ActionVisible
    {
        get => (Visibility)GetValue(ActionVisibleProperty);
        set => SetValue(ActionVisibleProperty, value);
    }

    public bool IsActionEnabled
    {
        get => (bool)GetValue(IsActionEnabledProperty);
        set => SetValue(IsActionEnabledProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public object? ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    public string SecondaryActionText
    {
        get => (string)GetValue(SecondaryActionTextProperty);
        set => SetValue(SecondaryActionTextProperty, value);
    }

    public Visibility SecondaryActionVisible
    {
        get => (Visibility)GetValue(SecondaryActionVisibleProperty);
        set => SetValue(SecondaryActionVisibleProperty, value);
    }

    public bool IsSecondaryActionEnabled
    {
        get => (bool)GetValue(IsSecondaryActionEnabledProperty);
        set => SetValue(IsSecondaryActionEnabledProperty, value);
    }

    public ICommand? SecondaryActionCommand
    {
        get => (ICommand?)GetValue(SecondaryActionCommandProperty);
        set => SetValue(SecondaryActionCommandProperty, value);
    }

    public object? SecondaryActionCommandParameter
    {
        get => GetValue(SecondaryActionCommandParameterProperty);
        set => SetValue(SecondaryActionCommandParameterProperty, value);
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).IconText.Text = e.NewValue?.ToString() ?? "";
    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).TitleText.Text = e.NewValue?.ToString() ?? "";
    private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).BodyText.Text = e.NewValue?.ToString() ?? "";
    private static void OnActionTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).ActionButton.Content = e.NewValue?.ToString() ?? "";
    private static void OnActionVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).ActionButton.Visibility = (Visibility)e.NewValue;
    private static void OnIsActionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).ActionButton.IsEnabled = (bool)e.NewValue;
    private static void OnSecondaryActionTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).SecondaryActionButton.Content = e.NewValue?.ToString() ?? "";
    private static void OnSecondaryActionVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).SecondaryActionButton.Visibility = (Visibility)e.NewValue;
    private static void OnIsSecondaryActionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).SecondaryActionButton.IsEnabled = (bool)e.NewValue;

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (ActionCommand?.CanExecute(ActionCommandParameter) == true)
            ActionCommand.Execute(ActionCommandParameter);

        RaiseEvent(new RoutedEventArgs(ActionClickedEvent, this));
    }

    private void SecondaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (SecondaryActionCommand?.CanExecute(SecondaryActionCommandParameter) == true)
            SecondaryActionCommand.Execute(SecondaryActionCommandParameter);

        RaiseEvent(new RoutedEventArgs(SecondaryActionClickedEvent, this));
    }
}
