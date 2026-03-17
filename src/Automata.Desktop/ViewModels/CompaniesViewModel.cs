using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    public partial class CompanyRowViewModel : ObservableObject
    {
        [ObservableProperty] private int number;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string parent = string.Empty;
        [ObservableProperty] private string address = string.Empty;
        [ObservableProperty] private string contacts = string.Empty;
        [ObservableProperty] private string activeFrom = string.Empty;
        [ObservableProperty] private bool isOdd;

        public string RowBackground => IsOdd ? "#F8FAFC" : "#FFFFFF";

        partial void OnIsOddChanged(bool value)
        {
            OnPropertyChanged(nameof(RowBackground));
        }
    }

    public partial class CompanyEditorFormModel : ObservableObject
    {
        [ObservableProperty] private string parentCompany = string.Empty;
        [ObservableProperty] private string companyName = string.Empty;
        [ObservableProperty] private string companyAddress = string.Empty;
        [ObservableProperty] private string companyContacts = string.Empty;
        [ObservableProperty] private string notes = string.Empty;
    }

    public partial class CompaniesViewModel : ViewModelBase
    {
        private readonly List<CompanyRowViewModel> _allCompanies;

        [ObservableProperty]
        private string filterName = string.Empty;

        [ObservableProperty]
        private int selectedPageSize = 5;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private bool isTileView;

        [ObservableProperty]
        private string recordsCounterText = "0 из 0";

        [ObservableProperty]
        private string actionMessage = " ";

        [ObservableProperty]
        private bool isCompanyFormOpen;

        [ObservableProperty]
        private string companyFormTitle = "Добавление компании";

        [ObservableProperty]
        private CompanyEditorFormModel companyForm = new();

        [ObservableProperty]
        private bool isDeleteConfirmOpen;

        [ObservableProperty]
        private CompanyRowViewModel? selectedCompany;

        public CompaniesViewModel()
        {
            PageSizeOptions = new ObservableCollection<int> { 5, 10, 20 };
            VisibleCompanies = new ObservableCollection<CompanyRowViewModel>();
            _allCompanies = BuildMockCompanies();
            RefreshVisibleCompanies();
        }

        public ObservableCollection<int> PageSizeOptions { get; }
        public ObservableCollection<CompanyRowViewModel> VisibleCompanies { get; }

        public bool IsTableView => !IsTileView;
        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        public bool HasCompanies => VisibleCompanies.Count > 0;
        public string PageText => $"Страница {CurrentPage} из {TotalPages}";

        partial void OnFilterNameChanged(string value)
        {
            CurrentPage = 1;
            RefreshVisibleCompanies();
        }

        partial void OnSelectedPageSizeChanged(int value)
        {
            CurrentPage = 1;
            RefreshVisibleCompanies();
        }

        partial void OnCurrentPageChanged(int value)
        {
            RefreshVisibleCompanies();
        }

        partial void OnIsTileViewChanged(bool value)
        {
            OnPropertyChanged(nameof(IsTableView));
        }

        [RelayCommand]
        private void ApplyFilter()
        {
            CurrentPage = 1;
            RefreshVisibleCompanies();
            ActionMessage = "Фильтр компаний обновлен (mock).";
        }

        [RelayCommand]
        private void ClearFilter()
        {
            FilterName = string.Empty;
            CurrentPage = 1;
            RefreshVisibleCompanies();
            ActionMessage = "Фильтр компаний очищен.";
        }

        [RelayCommand]
        private void ToggleView()
        {
            IsTileView = !IsTileView;
        }

        [RelayCommand]
        private void PrevPage()
        {
            if (!CanGoPrev)
            {
                return;
            }

            CurrentPage--;
        }

        [RelayCommand]
        private void NextPage()
        {
            if (!CanGoNext)
            {
                return;
            }

            CurrentPage++;
        }

        [RelayCommand]
        private void ExportCsv()
        {
            ActionMessage = "Экспорт CSV: UI-заглушка (файл не создается).";
        }

        [RelayCommand]
        private void OpenCreateCompanyForm()
        {
            CompanyFormTitle = "Добавление компании";
            CompanyForm = new CompanyEditorFormModel();
            IsCompanyFormOpen = true;
        }

        [RelayCommand]
        private void OpenEditCompanyForm(CompanyRowViewModel row)
        {
            CompanyFormTitle = $"Редактирование компании «{row.Name}»";
            CompanyForm = new CompanyEditorFormModel
            {
                ParentCompany = row.Parent,
                CompanyName = row.Name,
                CompanyAddress = row.Address,
                CompanyContacts = row.Contacts,
                Notes = "Демо-режим формы компании",
            };
            IsCompanyFormOpen = true;
        }

        [RelayCommand]
        private void SaveCompanyForm()
        {
            IsCompanyFormOpen = false;
            ActionMessage = "Сохранение компании: UI-only режим, данные не отправляются.";
        }

        [RelayCommand]
        private void CloseCompanyForm()
        {
            IsCompanyFormOpen = false;
        }

        [RelayCommand]
        private void AskDeleteCompany(CompanyRowViewModel row)
        {
            SelectedCompany = row;
            IsDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelDeleteCompany()
        {
            IsDeleteConfirmOpen = false;
            SelectedCompany = null;
        }

        [RelayCommand]
        private void ConfirmDeleteCompany()
        {
            if (SelectedCompany is null)
            {
                return;
            }

            _allCompanies.Remove(SelectedCompany);
            IsDeleteConfirmOpen = false;
            ActionMessage = $"Компания «{SelectedCompany.Name}» удалена (mock).";
            SelectedCompany = null;
            RefreshVisibleCompanies();
        }

        private void RefreshVisibleCompanies()
        {
            var filtered = GetFilteredCompanies();
            var totalFiltered = filtered.Count;
            var safePageSize = Math.Max(1, SelectedPageSize);
            var pages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)safePageSize));

            if (CurrentPage > pages)
            {
                CurrentPage = pages;
                return;
            }

            var start = (CurrentPage - 1) * safePageSize;
            var pageRows = filtered.Skip(start).Take(safePageSize).ToList();

            VisibleCompanies.Clear();

            for (var index = 0; index < pageRows.Count; index++)
            {
                var row = pageRows[index];
                row.Number = start + index + 1;
                row.IsOdd = row.Number % 2 == 1;
                VisibleCompanies.Add(row);
            }

            RecordsCounterText = $"{VisibleCompanies.Count} из {totalFiltered}";
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageText));
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(HasCompanies));
        }

        private List<CompanyRowViewModel> GetFilteredCompanies()
        {
            IEnumerable<CompanyRowViewModel> query = _allCompanies;

            if (!string.IsNullOrWhiteSpace(FilterName))
            {
                query = query.Where(x => x.Name.Contains(FilterName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }

        private static List<CompanyRowViewModel> BuildMockCompanies()
        {
            return new List<CompanyRowViewModel>
            {
                new() { Name = "ООО ВендПарк", Parent = "АО Автомат Групп", Address = "Москва, Минская 2", Contacts = "+7 (495) 111-00-01", ActiveFrom = "03.03.2022" },
                new() { Name = "АО ГородАвтомат", Parent = "АО Автомат Групп", Address = "Москва, Тверская 12", Contacts = "+7 (495) 111-00-02", ActiveFrom = "14.04.2022" },
                new() { Name = "ООО АвтоСнаб", Parent = "АО Автомат Групп", Address = "Москва, Лужники 24", Contacts = "+7 (495) 111-00-03", ActiveFrom = "17.06.2022" },
                new() { Name = "ООО ВендЛогистик", Parent = "АО Автомат Групп", Address = "Балашиха, Индустриальный 4", Contacts = "+7 (495) 111-00-04", ActiveFrom = "11.08.2022" },
                new() { Name = "ООО СеверМаркет", Parent = "ООО ВендПарк", Address = "Москва, Дмитровское 7", Contacts = "+7 (495) 111-00-05", ActiveFrom = "19.10.2022" },
                new() { Name = "ООО ЦентрСервис", Parent = "АО ГородАвтомат", Address = "Москва, Ленинские горы 1", Contacts = "+7 (495) 111-00-06", ActiveFrom = "25.12.2022" },
                new() { Name = "ООО ТехноВенд", Parent = "ООО АвтоСнаб", Address = "Москва, Андропова 18", Contacts = "+7 (495) 111-00-07", ActiveFrom = "07.02.2023" },
            };
        }

        private int TotalPages
        {
            get
            {
                var filteredCount = GetFilteredCompanies().Count;
                return Math.Max(1, (int)Math.Ceiling(filteredCount / (double)Math.Max(1, SelectedPageSize)));
            }
        }
    }
}
