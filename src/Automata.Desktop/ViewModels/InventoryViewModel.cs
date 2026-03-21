using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automata.Application.Inventory.Models;
using Automata.Application.Inventory.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// Форма создания/редактирования товара в разделе учета ТМЦ.
    /// </summary>
    public partial class ProductEditFormModel : ObservableObject
    {
        [ObservableProperty]
        private Guid? id;

        [ObservableProperty]
        private MachineLookupItem? selectedMachine;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string? description;

        [ObservableProperty]
        private decimal price;

        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private int minStock;

        [ObservableProperty]
        private decimal avgDailySales;
    }

    /// <summary>
    /// ViewModel экрана "Учет ТМЦ" (режим просмотра и режим управления).
    /// </summary>
    public partial class InventoryViewModel : ViewModelBase
    {
        private readonly IProductInventoryService _productInventoryService;
        private readonly MachineLookupItem _allMachinesItem = new() { Id = Guid.Empty, Name = "Все автоматы" };
        private CancellationTokenSource? _autoFilterDelayTokenSource;
        private bool _suppressAutoFiltering;
        private int _reloadRequestVersion;

        private static readonly TimeSpan AutoFilterDelay = TimeSpan.FromMilliseconds(250);

        public bool CanManage { get; }

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private MachineLookupItem? selectedMachineFilter;

        [ObservableProperty]
        private ProductListItem? selectedProduct;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? actionMessage;

        [ObservableProperty]
        private string recordsCounterText = "0";

        [ObservableProperty]
        private bool isEditorOpen;

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private string editorTitle = "Новый товар";

        [ObservableProperty]
        private ProductEditFormModel editForm = new();

        [ObservableProperty]
        private bool isDeleteConfirmOpen;

        public InventoryViewModel()
            : this(new DesignProductInventoryService(), true)
        {
        }

        public InventoryViewModel(IProductInventoryService productInventoryService, bool canManage = true)
        {
            _productInventoryService = productInventoryService;
            CanManage = canManage;
            Products = new ObservableCollection<ProductListItem>();
            Machines = new ObservableCollection<MachineLookupItem> { _allMachinesItem };
            EditMachines = new ObservableCollection<MachineLookupItem>();

            _suppressAutoFiltering = true;
            SelectedMachineFilter = _allMachinesItem;
            _suppressAutoFiltering = false;

            _ = LoadAsync();
        }

        public ObservableCollection<ProductListItem> Products { get; }
        public ObservableCollection<MachineLookupItem> Machines { get; }
        public ObservableCollection<MachineLookupItem> EditMachines { get; }

        public bool IsReadOnly => !CanManage;
        public bool HasProducts => Products.Count > 0;
        public bool HasNoProducts => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Products.Count == 0;
        public bool CanEditSelectedProduct => CanManage && SelectedProduct is not null;
        public bool CanDeleteSelectedProduct => CanManage && SelectedProduct is not null;
        public string ModeHint => CanManage
            ? "Режим управления: доступны создание, редактирование и удаление."
            : "Режим просмотра: операции изменения отключены.";
        public string EditorHint => IsEditorOpen
            ? "Заполните поля и нажмите «Сохранить»."
            : "Для открытия формы нажмите «Добавить» или «Редактировать».";

        partial void OnSearchTextChanged(string? value)
        {
            TriggerAutoFiltering();
        }

        partial void OnSelectedMachineFilterChanged(MachineLookupItem? value)
        {
            TriggerAutoFiltering();
        }

        partial void OnSelectedProductChanged(ProductListItem? value)
        {
            OpenEditProductFormCommand.NotifyCanExecuteChanged();
            AskDeleteProductCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsEditorOpenChanged(bool value)
        {
            OnPropertyChanged(nameof(EditorHint));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            // Инициализация фильтров и списка товаров.
            IsLoading = true;
            ErrorMessage = null;
            CancelPendingAutoFiltering();

            try
            {
                var machines = await _productInventoryService.GetMachinesAsync();

                _suppressAutoFiltering = true;
                try
                {
                    Machines.Clear();
                    Machines.Add(_allMachinesItem);

                    EditMachines.Clear();

                    foreach (var machine in machines)
                    {
                        Machines.Add(machine);
                        EditMachines.Add(machine);
                    }

                    if (SelectedMachineFilter is null || !Machines.Any(item => item.Id == SelectedMachineFilter.Id))
                    {
                        SelectedMachineFilter = _allMachinesItem;
                    }
                }
                finally
                {
                    _suppressAutoFiltering = false;
                }

                var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);
                await ReloadProductsAsync(requestVersion);
            }
            catch (Exception ex)
            {
                Products.Clear();
                Machines.Clear();
                EditMachines.Clear();
                RecordsCounterText = "0";
                ErrorMessage = $"Не удалось загрузить товары: {ex.Message}";
                OnPropertyChanged(nameof(HasProducts));
                OnPropertyChanged(nameof(HasNoProducts));
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoProducts));
            }
        }

        [RelayCommand]
        private async Task ApplyFilterAsync()
        {
            await ReloadProductsSafeAsync();
        }

        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            _suppressAutoFiltering = true;
            SearchText = null;
            SelectedMachineFilter = _allMachinesItem;
            _suppressAutoFiltering = false;

            ErrorMessage = null;
            ActionMessage = null;

            await ReloadProductsSafeAsync();
        }

        [RelayCommand]
        private void OpenCreateProductForm()
        {
            if (!CanManage)
            {
                return;
            }

            ErrorMessage = null;
            ActionMessage = null;
            IsEditMode = false;
            EditorTitle = "Новый товар";

            var preselectedMachine = SelectedMachineFilter is not null && SelectedMachineFilter.Id != Guid.Empty
                ? SelectedMachineFilter
                : null;

            EditForm = new ProductEditFormModel
            {
                SelectedMachine = preselectedMachine,
                Price = 0,
                Quantity = 0,
                MinStock = 0,
                AvgDailySales = 0,
            };

            IsEditorOpen = true;
        }

        [RelayCommand(CanExecute = nameof(CanEditSelectedProduct))]
        private void OpenEditProductForm()
        {
            if (!CanManage)
            {
                return;
            }

            if (SelectedProduct is null)
            {
                return;
            }

            ErrorMessage = null;
            ActionMessage = null;
            IsEditMode = true;
            EditorTitle = $"Редактирование товара «{SelectedProduct.Name}»";

            var selectedMachine = EditMachines.FirstOrDefault(machine => machine.Id == SelectedProduct.MachineId);

            EditForm = new ProductEditFormModel
            {
                Id = SelectedProduct.Id,
                SelectedMachine = selectedMachine,
                Name = SelectedProduct.Name,
                Description = SelectedProduct.Description,
                Price = SelectedProduct.Price,
                Quantity = SelectedProduct.Quantity,
                MinStock = SelectedProduct.MinStock,
                AvgDailySales = SelectedProduct.AvgDailySales,
            };

            IsEditorOpen = true;
        }

        [RelayCommand]
        private void CancelEditProductForm()
        {
            IsEditorOpen = false;
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task SaveProductAsync()
        {
            if (!CanManage)
            {
                return;
            }

            // Единый save для create/edit карточки товара.
            var validationError = ValidateEditForm();
            if (validationError is not null)
            {
                ErrorMessage = validationError;
                return;
            }

            var model = new ProductEditModel
            {
                Id = EditForm.Id,
                MachineId = EditForm.SelectedMachine!.Id,
                Name = EditForm.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(EditForm.Description) ? null : EditForm.Description.Trim(),
                Price = EditForm.Price,
                Quantity = EditForm.Quantity,
                MinStock = EditForm.MinStock,
                AvgDailySales = EditForm.AvgDailySales,
            };

            try
            {
                Guid savedProductId;
                if (IsEditMode)
                {
                    await _productInventoryService.UpdateProductAsync(model);
                    savedProductId = model.Id!.Value;
                    ActionMessage = "Товар успешно обновлен.";
                }
                else
                {
                    savedProductId = await _productInventoryService.CreateProductAsync(model);
                    ActionMessage = "Товар успешно создан.";
                }

                ErrorMessage = null;
                IsEditorOpen = false;

                await ReloadProductsSafeAsync(savedProductId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось сохранить товар: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedProduct))]
        private void AskDeleteProduct()
        {
            if (!CanManage)
            {
                return;
            }

            if (SelectedProduct is null)
            {
                return;
            }

            ErrorMessage = null;
            IsDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelDeleteProduct()
        {
            IsDeleteConfirmOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmDeleteProductAsync()
        {
            if (!CanManage)
            {
                return;
            }

            if (SelectedProduct is null)
            {
                IsDeleteConfirmOpen = false;
                return;
            }

            var deletedProductName = SelectedProduct.Name;
            var deletedProductId = SelectedProduct.Id;

            try
            {
                await _productInventoryService.DeleteProductAsync(deletedProductId);
                IsDeleteConfirmOpen = false;
                ActionMessage = $"Товар «{deletedProductName}» удален.";
                ErrorMessage = null;
                IsEditorOpen = false;
                await ReloadProductsSafeAsync();
            }
            catch (Exception ex)
            {
                IsDeleteConfirmOpen = false;
                ErrorMessage = $"Не удалось удалить товар: {ex.Message}";
            }
        }

        private async Task ReloadProductsSafeAsync(Guid? preferredSelectedProductId = null)
        {
            var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                await ReloadProductsAsync(requestVersion, preferredSelectedProductId);
            }
            catch (Exception ex)
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    Products.Clear();
                    RecordsCounterText = "0";
                    ErrorMessage = $"Не удалось загрузить товары: {ex.Message}";
                    OnPropertyChanged(nameof(HasProducts));
                    OnPropertyChanged(nameof(HasNoProducts));
                }
            }
            finally
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    IsLoading = false;
                    OnPropertyChanged(nameof(HasNoProducts));
                }
            }
        }

        private async Task ReloadProductsAsync(
            int requestVersion,
            Guid? preferredSelectedProductId = null,
            CancellationToken cancellationToken = default)
        {
            // Сбор фильтра по выбранному автомату.
            Guid? machineIdFilter = SelectedMachineFilter is not null && SelectedMachineFilter.Id != Guid.Empty
                ? SelectedMachineFilter.Id
                : null;

            var items = await _productInventoryService.GetProductsAsync(
                SearchText,
                machineIdFilter,
                cancellationToken);

            if (requestVersion != _reloadRequestVersion)
            {
                return;
            }

            var selectedProductId = preferredSelectedProductId ?? SelectedProduct?.Id;

            Products.Clear();
            foreach (var item in items)
            {
                Products.Add(item);
            }

            RecordsCounterText = Products.Count.ToString();

            if (selectedProductId.HasValue)
            {
                SelectedProduct = Products.FirstOrDefault(product => product.Id == selectedProductId.Value);
            }
            else
            {
                SelectedProduct = null;
            }

            OnPropertyChanged(nameof(HasProducts));
            OnPropertyChanged(nameof(HasNoProducts));
        }

        private void TriggerAutoFiltering()
        {
            if (_suppressAutoFiltering)
            {
                return;
            }

            // Debounce фильтров, чтобы не вызывать запросы на каждый ввод символа.
            StartAutoFiltering();
        }

        private void StartAutoFiltering()
        {
            _autoFilterDelayTokenSource?.Cancel();
            _autoFilterDelayTokenSource?.Dispose();

            var tokenSource = new CancellationTokenSource();
            _autoFilterDelayTokenSource = tokenSource;
            _ = RunDelayedAutoFilteringAsync(tokenSource);
        }

        private async Task RunDelayedAutoFilteringAsync(CancellationTokenSource tokenSource)
        {
            try
            {
                await Task.Delay(AutoFilterDelay, tokenSource.Token);
                await ApplyFilterAsync();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(_autoFilterDelayTokenSource, tokenSource))
                {
                    _autoFilterDelayTokenSource = null;
                }

                tokenSource.Dispose();
            }
        }

        private void CancelPendingAutoFiltering()
        {
            _autoFilterDelayTokenSource?.Cancel();
            _autoFilterDelayTokenSource?.Dispose();
            _autoFilterDelayTokenSource = null;
        }

        private string? ValidateEditForm()
        {
            if (EditForm.SelectedMachine is null || EditForm.SelectedMachine.Id == Guid.Empty)
            {
                return "Выберите автомат.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.Name))
            {
                return "Название товара обязательно.";
            }

            if (EditForm.Price < 0)
            {
                return "Цена не может быть отрицательной.";
            }

            if (EditForm.Quantity < 0)
            {
                return "Количество не может быть отрицательным.";
            }

            if (EditForm.MinStock < 0)
            {
                return "Минимальный остаток не может быть отрицательным.";
            }

            if (EditForm.AvgDailySales < 0)
            {
                return "Средние дневные продажи не могут быть отрицательными.";
            }

            return null;
        }

        private sealed class DesignProductInventoryService : IProductInventoryService
        {
            public Task<IReadOnlyList<ProductListItem>> GetProductsAsync(
                string? search,
                Guid? machineId,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<ProductListItem> items = Array.Empty<ProductListItem>();
                return Task.FromResult(items);
            }

            public Task<IReadOnlyList<MachineLookupItem>> GetMachinesAsync(CancellationToken cancellationToken = default)
            {
                IReadOnlyList<MachineLookupItem> items = Array.Empty<MachineLookupItem>();
                return Task.FromResult(items);
            }

            public Task<Guid> CreateProductAsync(ProductEditModel model, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Guid.NewGuid());
            }

            public Task UpdateProductAsync(ProductEditModel model, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
