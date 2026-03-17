using System.ComponentModel;
using System.Threading.Tasks;
using Automata.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Automata.Desktop.Views;

public partial class CompaniesView : UserControl
{
    private CompaniesViewModel? _viewModel;
    private Window? _formWindow;
    private Window? _deleteWindow;
    private bool _openingForm;
    private bool _openingDelete;

    public CompaniesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as CompaniesViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        CloseWindow(ref _formWindow);
        CloseWindow(ref _deleteWindow);
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.PropertyName == nameof(CompaniesViewModel.IsCompanyFormOpen))
        {
            if (_viewModel.IsCompanyFormOpen)
            {
                await ShowCompanyFormWindowAsync();
            }
            else
            {
                CloseWindow(ref _formWindow);
            }

            return;
        }

        if (e.PropertyName == nameof(CompaniesViewModel.IsDeleteConfirmOpen))
        {
            if (_viewModel.IsDeleteConfirmOpen)
            {
                await ShowDeleteWindowAsync();
            }
            else
            {
                CloseWindow(ref _deleteWindow);
            }
        }
    }

    private async Task ShowCompanyFormWindowAsync()
    {
        if (_viewModel is null || _formWindow is { IsVisible: true } || _openingForm)
        {
            return;
        }

        _openingForm = true;
        try
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var window = new CompanyFormWindow { DataContext = _viewModel };

            window.Closed += (_, _) =>
            {
                _formWindow = null;
                if (_viewModel.IsCompanyFormOpen)
                {
                    _viewModel.IsCompanyFormOpen = false;
                }
            };

            _formWindow = window;

            if (owner is not null)
            {
                await window.ShowDialog(owner);
            }
            else
            {
                window.Show();
            }
        }
        finally
        {
            _openingForm = false;
        }
    }

    private async Task ShowDeleteWindowAsync()
    {
        if (_viewModel is null || _deleteWindow is { IsVisible: true } || _openingDelete)
        {
            return;
        }

        _openingDelete = true;
        try
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var window = new CompanyDeleteConfirmWindow { DataContext = _viewModel };

            window.Closed += (_, _) =>
            {
                _deleteWindow = null;
                if (_viewModel.IsDeleteConfirmOpen)
                {
                    _viewModel.IsDeleteConfirmOpen = false;
                }
            };

            _deleteWindow = window;

            if (owner is not null)
            {
                await window.ShowDialog(owner);
            }
            else
            {
                window.Show();
            }
        }
        finally
        {
            _openingDelete = false;
        }
    }

    private static void CloseWindow(ref Window? window)
    {
        if (window is null)
        {
            return;
        }

        window.Close();
        window = null;
    }
}
