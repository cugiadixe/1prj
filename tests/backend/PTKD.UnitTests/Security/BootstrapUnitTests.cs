using System;
using PTKD.Bootstrap;
using Xunit;

namespace PTKD.UnitTests.Security;

public class BootstrapUnitTests : IDisposable
{
    public BootstrapUnitTests()
    {
        // Ensure clean state before each test
        Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", null);
    }

    [Fact]
    public void Bootstrap_RejectsPassword_WhenProvidedAsArgument()
    {
        var args = new[] { "--BOOTSTRAP_ADMIN_PASSWORD=secret" };
        var result = PTKD.Bootstrap.Program.Main(args);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Bootstrap_RejectsConnectionString_WhenProvidedAsArgument()
    {
        var args = new[] { "--CONNECTION_STRING=dummy" };
        var result = PTKD.Bootstrap.Program.Main(args);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Bootstrap_FailsSafely_WhenMissingConnectionString()
    {
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", "secret");
        var args = new[] { "--BOOTSTRAP_ADMIN_EMAIL=admin@example.com" };
        var result = PTKD.Bootstrap.Program.Main(args);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Bootstrap_FailsSafely_WhenMissingEmail()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING", "dummy");
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", "secret");
        var args = Array.Empty<string>();
        var result = PTKD.Bootstrap.Program.Main(args);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Bootstrap_FailsSafely_WhenMissingPassword()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING", "dummy");
        var args = new[] { "--BOOTSTRAP_ADMIN_EMAIL=admin@example.com" };
        var result = PTKD.Bootstrap.Program.Main(args);
        Assert.Equal(1, result);
    }
}
