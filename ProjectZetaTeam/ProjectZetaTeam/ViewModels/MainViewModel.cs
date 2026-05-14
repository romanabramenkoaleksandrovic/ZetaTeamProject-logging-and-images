using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectZetaTeam.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";
    }

    
}
