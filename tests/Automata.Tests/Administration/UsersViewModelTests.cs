using Automata.Application.Common;
using Automata.Application.Users.Models;
using Automata.Application.Users.Services;
using Automata.Desktop.ViewModels;

namespace Automata.Tests;

public class UsersViewModelTests
{
    [Fact]
    public async Task CreateUser_FromDialog_CallsServiceAndReloadsList()
    {
        var service = new FakeUserAdministrationService();
        var viewModel = new UsersViewModel(service, Guid.NewGuid(), true);

        await WaitUntilAsync(() => viewModel.Roles.Count > 0);

        viewModel.StartCreateUserCommand.Execute(null);
        Assert.True(viewModel.IsCreateDialogOpen);

        viewModel.EditForm.LastName = "Тестов";
        viewModel.EditForm.FirstName = "Тест";
        viewModel.EditForm.Email = "test.user@example.com";
        viewModel.EditForm.SelectedRole = viewModel.Roles.First();
        viewModel.CreatePassword = "Passw0rd!";

        await viewModel.SaveUserCommand.ExecuteAsync(null);

        Assert.Equal(1, service.CreateCalls);
        Assert.False(viewModel.IsCreateDialogOpen);
        Assert.NotNull(viewModel.ActionMessage);
        Assert.Contains("успешно создан", viewModel.ActionMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(service.LastCreatedUserId, viewModel.SelectedUser?.Id);
    }

    [Fact]
    public async Task CloseCreateDialog_LeavesEditMode()
    {
        var service = new FakeUserAdministrationService();
        var viewModel = new UsersViewModel(service, Guid.NewGuid(), true);

        await WaitUntilAsync(() => viewModel.Roles.Count > 0);

        viewModel.StartCreateUserCommand.Execute(null);
        Assert.True(viewModel.IsCreateDialogOpen);

        viewModel.CloseCreateDialogCommand.Execute(null);

        Assert.False(viewModel.IsCreateDialogOpen);
        Assert.False(viewModel.IsCreateMode);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 3000)
    {
        var started = DateTime.UtcNow;
        while (!predicate())
        {
            if ((DateTime.UtcNow - started).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Не удалось дождаться готовности ViewModel.");
            }

            await Task.Delay(25);
        }
    }

    private sealed class FakeUserAdministrationService : IUserAdministrationService
    {
        private readonly List<LookupItem> _roles =
        [
            new LookupItem { Id = 1, Name = "Администратор" },
            new LookupItem { Id = 2, Name = "Оператор" },
        ];

        private readonly List<UserAdministrationListItem> _users = [];

        public int CreateCalls { get; private set; }
        public Guid LastCreatedUserId { get; private set; }

        public Task<IReadOnlyList<UserAdministrationListItem>> GetUsersAsync(
            string? search,
            int? roleId,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<UserAdministrationListItem> query = _users;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLowerInvariant();
                query = query.Where(user =>
                    user.DisplayName.ToLowerInvariant().Contains(normalizedSearch) ||
                    user.Email.ToLowerInvariant().Contains(normalizedSearch) ||
                    (user.Phone?.ToLowerInvariant().Contains(normalizedSearch) ?? false));
            }

            if (roleId.HasValue)
            {
                query = query.Where(user => user.RoleId == roleId.Value);
            }

            return Task.FromResult<IReadOnlyList<UserAdministrationListItem>>(query.ToList());
        }

        public Task<IReadOnlyList<LookupItem>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<LookupItem>>(_roles);
        }

        public Task<Guid> CreateUserAsync(
            UserEditModel model,
            string password,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastCreatedUserId = Guid.NewGuid();

            var roleName = _roles.First(role => role.Id == model.RoleId).Name;
            var displayName = string.Join(" ", new[]
            {
                model.LastName,
                model.FirstName,
                model.MiddleName ?? string.Empty,
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

            _users.Add(new UserAdministrationListItem
            {
                Id = LastCreatedUserId,
                LastName = model.LastName,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                DisplayName = displayName,
                Email = model.Email.ToLowerInvariant(),
                Phone = model.Phone,
                RoleId = model.RoleId,
                RoleName = roleName,
                CreatedAt = DateTimeOffset.UtcNow,
                HasMaintenanceRecords = false,
            });

            return Task.FromResult(LastCreatedUserId);
        }

        public Task UpdateUserAsync(
            UserEditModel model,
            Guid actingUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ChangePasswordAsync(
            Guid userId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(
            Guid userId,
            Guid actingUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
