using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using Automata.Desktop.ViewModels;

namespace Automata.Tests;

public class LoginViewModelTests
{
    [Fact]
    public async Task SignIn_WithEmptyCredentials_SetsErrorState()
    {
        var viewModel = CreateViewModel(new FakeAuthService(null));

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Укажите email и пароль.", viewModel.ErrorText);
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_ClearsErrorState()
    {
        var viewModel = CreateViewModel(new FakeAuthService(new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            Email = "operator@example.com",
            DisplayName = "Оператор",
            RoleName = "Оператор",
        }));

        viewModel.Email = "operator@example.com";
        viewModel.Password = "Passw0rd!";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorText);
    }

    [Fact]
    public async Task GoToRegistration_WhenOpenFails_ShowsErrorInsteadOfThrowing()
    {
        var viewModel = new LoginViewModel(
            new FakeAuthService(null),
            () => throw new InvalidOperationException("Окно не удалось открыть."),
            _ => { });

        await viewModel.GoToRegistrationCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Contains("Ошибка открытия регистрации", viewModel.ErrorText);
    }

    private static LoginViewModel CreateViewModel(IAuthService authService)
    {
        return new LoginViewModel(
            authService,
            () => Task.CompletedTask,
            _ => { });
    }

    private sealed class FakeAuthService : IAuthService
    {
        private readonly AuthenticatedUser? _signInResult;

        public FakeAuthService(AuthenticatedUser? signInResult)
        {
            _signInResult = signInResult;
        }

        public Task<AuthenticatedUser?> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_signInResult);
        }

        public Task<AuthenticatedUser> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
