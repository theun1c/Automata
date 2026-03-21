using System.ComponentModel;
using System.Threading.Tasks;
using Automata.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Automata.Desktop.Views;

public partial class InventoryView : UserControl
{
    private InventoryViewModel? _viewModel;
    private Window? _formWindow;
    private bool _openingForm;

    public InventoryView()
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

        _viewModel = DataContext as InventoryViewModel;

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
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.PropertyName != nameof(InventoryViewModel.IsEditorOpen))
        {
            return;
        }

        if (_viewModel.IsEditorOpen)
        {
            await ShowProductFormWindowAsync();
        }
        else
        {
            CloseWindow(ref _formWindow);
        }
    }

    private async Task ShowProductFormWindowAsync()
    {
        if (_viewModel is null || _formWindow is { IsVisible: true } || _openingForm)
        {
            return;
        }

        _openingForm = true;
        try
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var window = new ProductFormWindow { DataContext = _viewModel };

            window.Closed += (_, _) =>
            {
                _formWindow = null;
                if (_viewModel.IsEditorOpen)
                {
                    _viewModel.IsEditorOpen = false;
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
