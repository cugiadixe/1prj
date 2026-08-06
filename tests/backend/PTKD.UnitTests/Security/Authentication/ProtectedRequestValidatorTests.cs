using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;
using Xunit;

namespace PTKD.UnitTests.Security.Authentication;

public class ProtectedRequestValidatorTests
{
    private readonly Mock<IAuthenticationDbContextFactory> _dbContextFactoryMock;
    private readonly Mock<IAuthenticationDbContext> _dbContextMock;
    private readonly Mock<ILogger<ProtectedRequestValidator>> _loggerMock;
    private readonly AuthenticationAccountPolicy _policy;
    private readonly ProtectedRequestValidator _validator;

    public ProtectedRequestValidatorTests()
    {
        _dbContextFactoryMock = new Mock<IAuthenticationDbContextFactory>();
        _dbContextMock = new Mock<IAuthenticationDbContext>();
        _loggerMock = new Mock<ILogger<ProtectedRequestValidator>>();
        _policy = new AuthenticationAccountPolicy();

        _dbContextFactoryMock
            .Setup(f => f.CreateDbContext())
            .Returns(_dbContextMock.Object);

        _validator = new ProtectedRequestValidator(
            _dbContextFactoryMock.Object,
            _policy,
            _loggerMock.Object);
    }

    private UserAuthAccount SetupAccount(
        long userId,
        bool activeAccount = true,
        string employmentStatus = "ACTIVE",
        string userAccountStatus = "ACTIVE",
        Guid? securityStamp = null,
        DateTime? invalidatedAt = null)
    {
        var stamp = securityStamp ?? Guid.NewGuid();
        
        var user = new User("user_" + userId, "User", null, employmentStatus, userAccountStatus);
        var idProp = typeof(User).GetProperty(nameof(User.Id));
        idProp?.SetValue(user, userId);

        var account = UserAuthAccount.CreateInternal(
            userId,
            "sub",
            "hash",
            DateTime.UtcNow);

        var stampProp = typeof(UserAuthAccount).GetProperty(nameof(UserAuthAccount.SecurityStamp));
        stampProp?.SetValue(account, stamp);

        var invalProp = typeof(UserAuthAccount).GetProperty(nameof(UserAuthAccount.SessionsInvalidatedAt));
        invalProp?.SetValue(account, invalidatedAt);

        if (!activeAccount)
        {
            account.Disable(DateTime.UtcNow, 1);
        }

        var userProp = typeof(UserAuthAccount).GetProperty(nameof(UserAuthAccount.User));
        userProp?.SetValue(account, user);

        var list = new[] { account };
        _dbContextMock.Setup(c => c.UserAuthAccounts).ReturnsDbSet(list);

        return account;
    }

    [Fact]
    public async Task ValidateAsync_ActiveAccount_EligibleUser_MatchingStamp_AfterCutoff_Passes()
    {
        var stamp = Guid.NewGuid();
        var account = SetupAccount(1, securityStamp: stamp, invalidatedAt: DateTime.UtcNow.AddMinutes(-5));

        var result = await _validator.ValidateAsync(1, stamp, DateTime.UtcNow);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AccountNotFound_Fails()
    {
        _dbContextMock.Setup(c => c.UserAuthAccounts).ReturnsDbSet(Array.Empty<UserAuthAccount>());

        var result = await _validator.ValidateAsync(1, Guid.NewGuid(), DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_AccountDisabled_Fails()
    {
        var stamp = Guid.NewGuid();
        SetupAccount(1, activeAccount: false, securityStamp: stamp);

        var result = await _validator.ValidateAsync(1, stamp, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_LinkedUserMissing_Fails()
    {
        var stamp = Guid.NewGuid();
        var account = SetupAccount(1, securityStamp: stamp);
        
        var userProp = typeof(UserAuthAccount).GetProperty(nameof(UserAuthAccount.User));
        userProp?.SetValue(account, null);

        var result = await _validator.ValidateAsync(1, stamp, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_EmploymentStatusNotActiveOrProbation_Fails()
    {
        var stamp = Guid.NewGuid();
        SetupAccount(1, employmentStatus: "TERMINATED", securityStamp: stamp);

        var result = await _validator.ValidateAsync(1, stamp, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_SecurityStampMismatches_Fails()
    {
        var stamp = Guid.NewGuid();
        SetupAccount(1, securityStamp: stamp);

        var result = await _validator.ValidateAsync(1, Guid.NewGuid(), DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_TokenIssuedAtCutoff_Fails()
    {
        var stamp = Guid.NewGuid();
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        SetupAccount(1, securityStamp: stamp, invalidatedAt: cutoff);

        var result = await _validator.ValidateAsync(1, stamp, cutoff);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_TokenIssuedBeforeCutoff_Fails()
    {
        var stamp = Guid.NewGuid();
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        SetupAccount(1, securityStamp: stamp, invalidatedAt: cutoff);

        var result = await _validator.ValidateAsync(1, stamp, cutoff.AddSeconds(-1));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_InfrastructureException_FailsClosed()
    {
        _dbContextFactoryMock
            .Setup(f => f.CreateDbContext())
            .Throws(new Exception("Database down"));

        var result = await _validator.ValidateAsync(1, Guid.NewGuid(), DateTime.UtcNow);

        result.Should().BeFalse();
    }
}
