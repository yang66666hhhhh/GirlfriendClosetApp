using CommunityToolkit.Mvvm.ComponentModel;

namespace ClosetApp.UI.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected void NotifyPropertiesChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }
}
