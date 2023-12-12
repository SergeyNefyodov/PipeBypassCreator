using CommunityToolkit.Mvvm.ComponentModel;

namespace PipeBypassCreator.ViewModels
{
    public sealed partial class PipeBypassCreatorViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isHorizontal;
        [ObservableProperty] private string _offset;
        [ObservableProperty] private string _corner;
    }
}