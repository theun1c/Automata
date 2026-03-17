using System.ComponentModel;
using System.Threading.Tasks;
using Automata.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Automata.Desktop.Views;

public partial class MachinesView : UserControl
{
    private MachinesViewModel? _viewModel;
    private Window? _formWindow;
    private Window? _deleteWindow;
    private Window? _unbindWindow;
    private bool _openingForm;
    private bool _openingDelete;
    private bool _openingUnbind;

    public MachinesView()
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

        _viewModel = DataContext as MachinesViewModel;

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
        CloseWindow(ref _unbindWindow);
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.PropertyName == nameof(MachinesViewModel.IsMachineFormOpen))
        {
            if (_viewModel.IsMachineFormOpen)
            {
                await ShowMachineFormWindowAsync();
            }
            else
            {
                CloseWindow(ref _formWindow);
            }

            return;
        }

        if (e.PropertyName == nameof(MachinesViewModel.IsDeleteConfirmOpen))
        {
            if (_viewModel.IsDeleteConfirmOpen)
            {
                await ShowDeleteWindowAsync();
            }
            else
            {
                CloseWindow(ref _deleteWindow);
            }

            return;
        }

        if (e.PropertyName == nameof(MachinesViewModel.IsUnbindConfirmOpen))
        {
            if (_viewModel.IsUnbindConfirmOpen)
            {
                await ShowUnbindWindowAsync();
            }
            else
            {
                CloseWindow(ref _unbindWindow);
            }
        }
    }

    private async Task ShowMachineFormWindowAsync()
    {
        if (_viewModel is null || _formWindow is { IsVisible: true } || _openingForm)
        {
            return;
        }

        _openingForm = true;
        try
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var window = new MachineFormWindow { DataContext = _viewModel };

            window.Closed += (_, _) =>
            {
                _formWindow = null;
                if (_viewModel.IsMachineFormOpen)
                {
                    _viewModel.IsMachineFormOpen = false;
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
            var window = new MachineDeleteConfirmWindow { DataContext = _viewModel };

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

    private async Task ShowUnbindWindowAsync()
    {
        if (_viewModel is null || _unbindWindow is { IsVisible: true } || _openingUnbind)
        {
            return;
        }

        _openingUnbind = true;
        try
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var window = new MachineUnbindConfirmWindow { DataContext = _viewModel };

            window.Closed += (_, _) =>
            {
                _unbindWindow = null;
                if (_viewModel.IsUnbindConfirmOpen)
                {
                    _viewModel.IsUnbindConfirmOpen = false;
                }
            };

            _unbindWindow = window;

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
            _openingUnbind = false;
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
