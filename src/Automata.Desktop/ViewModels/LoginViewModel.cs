using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly Action _openRegistration;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool showPassword;

        [ObservableProperty]
        private string errorText = " ";

        public LoginViewModel(Action openRegistration)
        {
            _openRegistration = openRegistration;
        }

        public bool IsPasswordMasked => !ShowPassword;

        partial void OnShowPasswordChanged(bool value)
        {
            OnPropertyChanged(nameof(IsPasswordMasked));
        }

        [RelayCommand]
        private void SignIn()
        {
            ErrorText = "Демо-режим: визуальная заглушка входа, проверка отключена.";
        }

        [RelayCommand]
        private void GoToRegistration()
        {
            _openRegistration();
        }
    }
}
