using FluentAssertions;
using PTKD.Application.Security.Authentication.Services;
using Xunit;

namespace PTKD.UnitTests.Security.Authentication;

public class RefreshTokenMaterialServiceTests
{
    private readonly RefreshTokenMaterialService _service = new();

    [Fact]
    public void Generate_ProducesDifferentMaterialAndHash()
    {
        var result = _service.Generate();

        result.RawMaterial.Should().NotBeNullOrWhiteSpace();
        result.Hash.Should().NotBeNullOrWhiteSpace();
        result.RawMaterial.Should().NotBe(result.Hash);
        
        // Hash should be SHA-256 uppercase hex
        result.Hash.Length.Should().Be(64);
        result.Hash.Should().MatchRegex("^[A-F0-9]{64}$");
    }

    [Fact]
    public void ComputeHash_VerifyWorks()
    {
        var (raw, generatedHash) = _service.Generate();
        var computedHash = _service.ComputeHash(raw);

        computedHash.Should().Be(generatedHash);
    }
}
