using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using Automata.Desktop.ViewModels;

namespace Automata.Tests;

public class LoginViewModelTests
{
    [Fact]
    public async Task SignIn_WithEmptyCredentials_SetsErrorState()
    {
        var viewModel = CreateViewModel(new StubAuthService());

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Укажите email и пароль.", viewModel.ErrorText);
    }

    [Fact]
    public async Task SignIn_WithWrongCredentials_ShowsInvalidCredentialsMessage()
    {
        var viewModel = CreateViewModel(new StubAuthService());
        viewModel.Email = "operator@example.com";
        viewModel.Password = "wrong";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Неверный email или пароль.", viewModel.ErrorText);
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_ClearsErrorState()
    {
        var expectedUser = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            Email = "operator@example.com",
            DisplayName = "Оператор",
            RoleName = "Оператор",
        };

        AuthenticatedUser? capturedUser = null;

        var viewModel = new LoginViewModel(
            new StubAuthService(signIn: (_, _, _) => Task.FromResult<AuthenticatedUser?>(expectedUser)),
            () => Task.CompletedTask,
            user => capturedUser = user);

        viewModel.Email = "operator@example.com";
        viewModel.Password = "Passw0rd!";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorText);
        Assert.NotNull(capturedUser);
        Assert.Equal(expectedUser.Id, capturedUser!.Id);
    }

    [Fact]
    public async Task SignIn_WhenServiceThrows_ShowsErrorAndResetsLoading()
    {
        var viewModel = CreateViewModel(new StubAuthService(
            signIn: (_, _, _) => throw new InvalidOperationException("Тестовая ошибка.")));

        viewModel.Email = "operator@example.com";
        viewModel.Password = "Passw0rd!";

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Ошибка входа: Тестовая ошибка.", viewModel.ErrorText);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task SignIn_WhenAlreadyLoading_DoesNotCallService()
    {
        var calls = 0;
        var viewModel = CreateViewModel(new StubAuthService(
            signIn: (_, _, _) =>
            {
                calls++;
                return Task.FromResult<AuthenticatedUser?>(null);
            }));

        viewModel.Email = "operator@example.com";
        viewModel.Password = "Passw0rd!";
        viewModel.IsLoading = true;

        await viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GoToRegistration_WhenOpenFails_ShowsErrorInsteadOfThrowing()
    {
        var viewModel = new LoginViewModel(
            new StubAuthService(),
            () => throw new InvalidOperationException("Окно не удалось открыть."),
            _ => { });

        await viewModel.GoToRegistrationCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Contains("Ошибка открытия регистрации", viewModel.ErrorText);
    }

    [Fact]
    public async Task GoToRegistration_OnSuccess_ClearsOldError()
    {
        var viewModel = CreateViewModel(new StubAuthService(), () => Task.CompletedTask);
        viewModel.ErrorText = "Старая ошибка";

        await viewModel.GoToRegistrationCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorText);
    }

    [Fact]
    public void ShowPassword_TogglesIsPasswordMasked()
    {
        var viewModel = CreateViewModel(new StubAuthService());

        Assert.True(viewModel.IsPasswordMasked);

        viewModel.ShowPassword = true;
        Assert.False(viewModel.IsPasswordMasked);
    }

    private static LoginViewModel CreateViewModel(IAuthService authService)
    {
        return CreateViewModel(authService, () => Task.CompletedTask);
    }

    private static LoginViewModel CreateViewModel(IAuthService authService, Func<Task> openRegistrationWindow)
    {
        return new LoginViewModel(authService, openRegistrationWindow, _ => { });
    }
}
