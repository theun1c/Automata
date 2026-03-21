using System.Globalization;
using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using Automata.Desktop.ViewModels;

namespace Automata.Tests;

public class RegistrationViewModelTests
{
    [Fact]
    public async Task Register_WithoutFranchiseCode_CompletesSuccessfully()
    {
        var authService = new CapturingAuthService();
        var windowClosed = false;
        var viewModel = new RegistrationViewModel(authService, () => windowClosed = true)
        {
            Email = "operator@example.com",
            Password = "Passw0rd!",
        };

        viewModel.OpenCodeModalCommand.Execute(null);
        viewModel.ConfirmationCode = viewModel.GeneratedCode;
        viewModel.CaptchaAnswer = SolveCaptcha(viewModel.CaptchaChallenge);

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.True(windowClosed);
        Assert.NotNull(authService.LastRegisterRequest);
        Assert.Equal("operator@example.com", authService.LastRegisterRequest!.Email);
    }

    [Fact]
    public void OpenCodeModal_GeneratesCodeOnlyOncePerSession()
    {
        var viewModel = new RegistrationViewModel(new CapturingAuthService(), () => { });

        viewModel.OpenCodeModalCommand.Execute(null);
        var firstCode = viewModel.GeneratedCode;

        viewModel.OpenCodeModalCommand.Execute(null);
        var secondCode = viewModel.GeneratedCode;

        Assert.False(string.IsNullOrWhiteSpace(firstCode));
        Assert.Equal(firstCode, secondCode);
    }

    private static string SolveCaptcha(string challenge)
    {
        var expression = challenge.Replace("= ?", string.Empty, StringComparison.Ordinal).Trim();
        var tokens = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var result = int.Parse(tokens[0], CultureInfo.InvariantCulture);
        for (var index = 1; index < tokens.Length; index += 2)
        {
            var operation = tokens[index];
            var value = int.Parse(tokens[index + 1], CultureInfo.InvariantCulture);
            result = operation switch
            {
                "+" => result + value,
                "-" => result - value,
                _ => throw new InvalidOperationException("Неизвестная операция CAPTCHA."),
            };
        }

        return result.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class CapturingAuthService : IAuthService
    {
        public RegisterUserRequest? LastRegisterRequest { get; private set; }

        public Task<AuthenticatedUser?> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        public Task<AuthenticatedUser> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            LastRegisterRequest = request;
            return Task.FromResult(new AuthenticatedUser
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                DisplayName = "Тестовый пользователь",
                RoleName = "Оператор",
            });
        }
    }
}
