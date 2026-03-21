using Automata.Application.Auth.Models;
using Automata.Desktop.ViewModels;

namespace Automata.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void SignOutCommand_InvokesCallback()
    {
        var signOutCalled = false;
        var viewModel = new MainWindowViewModel(
            null,
            null,
            null,
            null,
            null,
            () => signOutCalled = true,
            new AuthenticatedUser
            {
                Id = Guid.NewGuid(),
                DisplayName = "Администратор Тест",
                Email = "admin@test.local",
                RoleName = "Администратор",
            });

        viewModel.SignOutCommand.Execute(null);

        Assert.True(signOutCalled);
    }

    [Fact]
    public void ShowUsersCommand_NonAdmin_DoesNotSwitchSection()
    {
        var viewModel = new MainWindowViewModel(
            null,
            null,
            null,
            null,
            null,
            null,
            new AuthenticatedUser
            {
                Id = Guid.NewGuid(),
                DisplayName = "Инженер Тест",
                Email = "engineer@test.local",
                RoleName = "Инженер",
            });

        viewModel.ShowUsersCommand.Execute(null);

        Assert.Equal(DesktopSection.Dashboard, viewModel.CurrentSection);
    }
}
