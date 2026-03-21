using Automata.Application.Auth.Models;
using Automata.Desktop.ViewModels;

namespace Automata.Tests;

public class RegistrationViewModelTests
{
    [Fact]
    public async Task Register_ValidData_InvokesAuthServiceAndClosesWindow()
    {
        RegisterUserRequest? capturedRequest = null;
        var windowClosed = false;

        var authService = new StubAuthService(
            register: (request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(new AuthenticatedUser
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    DisplayName = "Новый пользователь",
                    RoleName = "Оператор",
                });
            });

        var viewModel = CreateReadyForRegistrationViewModel(authService, () => windowClosed = true);

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.True(windowClosed);
        Assert.NotNull(capturedRequest);
        Assert.Equal("operator@example.com", capturedRequest!.Email);
        Assert.Equal("Operator", capturedRequest.FirstName);
        Assert.Equal("Пользователь", capturedRequest.LastName);
    }

    [Fact]
    public async Task Register_InvalidEmail_SetsErrorStateAndDoesNotCloseWindow()
    {
        var windowClosed = false;
        var viewModel = CreateReadyForRegistrationViewModel(new StubAuthService(), () => windowClosed = true);
        viewModel.Email = "invalid";

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.False(windowClosed);
        Assert.Equal("Укажите корректный email.", viewModel.StateMessage);
        Assert.Equal("#B91C1C", viewModel.StateColor);
    }

    [Fact]
    public async Task Register_WeakPassword_SetsErrorState()
    {
        var viewModel = CreateReadyForRegistrationViewModel(new StubAuthService(), () => { });
        viewModel.Password = "Password1";

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Пароль должен содержать цифру и специальный символ.", viewModel.StateMessage);
        Assert.Equal("#B91C1C", viewModel.StateColor);
    }

    [Fact]
    public async Task Register_WithoutGeneratedCode_SetsErrorState()
    {
        var viewModel = new RegistrationViewModel(new StubAuthService(), () => { })
        {
            Email = "operator@example.com",
            Password = "Passw0rd!",
        };

        viewModel.CaptchaAnswer = AuthTestHelpers.SolveCaptcha(viewModel.CaptchaChallenge);

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Сначала сгенерируйте код подтверждения.", viewModel.StateMessage);
        Assert.Equal("#B91C1C", viewModel.StateColor);
    }

    [Fact]
    public async Task Register_WithWrongConfirmationCode_SetsErrorState()
    {
        var viewModel = CreateReadyForRegistrationViewModel(new StubAuthService(), () => { });
        viewModel.ConfirmationCode = "000000";

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Неверный код подтверждения.", viewModel.StateMessage);
        Assert.Equal("#B91C1C", viewModel.StateColor);
    }

    [Fact]
    public async Task Register_WithWrongCaptcha_SetsErrorState()
    {
        var viewModel = CreateReadyForRegistrationViewModel(new StubAuthService(), () => { });
        viewModel.CaptchaAnswer = "9999";

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Неверная CAPTCHA.", viewModel.StateMessage);
        Assert.Equal("#B91C1C", viewModel.StateColor);
    }

    [Fact]
    public async Task Register_WhenAuthServiceThrows_ShowsServiceMessage()
    {
        var authService = new StubAuthService(
            register: (_, _) => throw new InvalidOperationException("Тестовая ошибка регистрации."));

        var viewModel = CreateReadyForRegistrationViewModel(authService, () => { });

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Тестовая ошибка регистрации.", viewModel.StateMessage);
        Assert.Equal("#B91C1C", viewModel.StateColor);
    }

    [Fact]
    public void OpenCodeModal_GeneratesCodeOnlyOncePerSession()
    {
        var viewModel = new RegistrationViewModel(new StubAuthService(), () => { });

        viewModel.OpenCodeModalCommand.Execute(null);
        var firstCode = viewModel.GeneratedCode;

        viewModel.OpenCodeModalCommand.Execute(null);
        var secondCode = viewModel.GeneratedCode;

        Assert.False(string.IsNullOrWhiteSpace(firstCode));
        Assert.Equal(firstCode, secondCode);
    }

    [Fact]
    public void OpenAndCloseCodeModal_TogglesVisibilityFlag()
    {
        var viewModel = new RegistrationViewModel(new StubAuthService(), () => { });

        viewModel.OpenCodeModalCommand.Execute(null);
        Assert.True(viewModel.IsCodeModalOpen);

        viewModel.CloseCodeModalCommand.Execute(null);
        Assert.False(viewModel.IsCodeModalOpen);
    }

    [Fact]
    public void RegenerateCaptcha_ChangesChallengeAndClearsAnswer()
    {
        var viewModel = new RegistrationViewModel(new StubAuthService(), () => { });
        var before = viewModel.CaptchaChallenge;

        viewModel.CaptchaAnswer = "123";
        viewModel.RegenerateCaptchaCommand.Execute(null);

        Assert.NotEqual(before, viewModel.CaptchaChallenge);
        Assert.Equal(string.Empty, viewModel.CaptchaAnswer);
    }

    [Fact]
    public void GoToLogin_ClosesWindow()
    {
        var closed = false;
        var viewModel = new RegistrationViewModel(new StubAuthService(), () => closed = true);

        viewModel.GoToLoginCommand.Execute(null);

        Assert.True(closed);
    }

    private static RegistrationViewModel CreateReadyForRegistrationViewModel(StubAuthService authService, Action closeWindow)
    {
        var viewModel = new RegistrationViewModel(authService, closeWindow)
        {
            Email = "operator@example.com",
            Password = "Passw0rd!",
        };

        viewModel.OpenCodeModalCommand.Execute(null);
        viewModel.ConfirmationCode = viewModel.GeneratedCode;
        viewModel.CaptchaAnswer = AuthTestHelpers.SolveCaptcha(viewModel.CaptchaChallenge);

        return viewModel;
    }
}
