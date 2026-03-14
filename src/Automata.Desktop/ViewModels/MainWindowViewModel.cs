using System.Collections.ObjectModel;

namespace Automata.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<PageViewModelBase> Pages { get; } = new();

        private PageViewModelBase? _selectedPage;

        public PageViewModelBase? SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value);
        }

        public MainWindowViewModel()
        {
            Pages.Add(new MainPageViewModel());
            Pages.Add(new AdministrationPageViewModel());

            SelectedPage = Pages[0];
        }
    }
}
