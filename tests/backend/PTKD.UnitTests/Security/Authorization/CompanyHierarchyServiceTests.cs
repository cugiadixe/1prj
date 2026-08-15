using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.Services;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Security.Authorization;

public class CompanyHierarchyServiceTests
{
    private readonly Mock<IAuthorizationDbContext> _dbContextMock = new();
    private readonly CompanyHierarchyService _sut;

    public CompanyHierarchyServiceTests()
    {
        _sut = new CompanyHierarchyService(_dbContextMock.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    private static Company Company(long id, long? parentId)
    {
        var c = new Company($"C{id}", parentId, $"Company {id}", null);
        typeof(Company).GetProperty("Id")!.SetValue(c, id);
        return c;
    }

    private void SetupTree(params Company[] companies)
        => _dbContextMock.Setup(x => x.Companies).ReturnsDbSet(companies);

    // Cây: 1 (tập đoàn) → {2, 3}; 2 → {4}
    private void SetupStandardGroup() => SetupTree(
        Company(1, null),
        Company(2, 1),
        Company(3, 1),
        Company(4, 2));

    [Fact]
    public async Task Expand_Parent_ReturnsSelfAndAllDescendants()
    {
        SetupStandardGroup();

        var result = await _sut.ExpandWithDescendantsAsync(new long[] { 1 });

        Assert.Equal(new long[] { 1, 2, 3, 4 }, result.OrderBy(x => x));
    }

    [Fact]
    public async Task Expand_MidLevel_ReturnsOnlyItsOwnSubtree()
    {
        SetupStandardGroup();

        var result = await _sut.ExpandWithDescendantsAsync(new long[] { 2 });

        Assert.Equal(new long[] { 2, 4 }, result.OrderBy(x => x));
    }

    [Fact]
    public async Task Expand_Leaf_ReturnsOnlyItself()
    {
        SetupStandardGroup();

        var result = await _sut.ExpandWithDescendantsAsync(new long[] { 3 });

        Assert.Equal(new long[] { 3 }, result);
    }

    [Fact]
    public async Task Expand_UnknownCompany_ReturnsItselfUnchanged()
    {
        SetupStandardGroup();

        var result = await _sut.ExpandWithDescendantsAsync(new long[] { 999 });

        Assert.Equal(new long[] { 999 }, result);
    }

    [Fact]
    public async Task Expand_EmptyInput_ReturnsEmpty()
    {
        SetupStandardGroup();

        var result = await _sut.ExpandWithDescendantsAsync(Array.Empty<long>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task Expand_MultipleSeeds_AreUnionedWithoutDuplicates()
    {
        SetupStandardGroup();

        var result = await _sut.ExpandWithDescendantsAsync(new long[] { 2, 3 });

        Assert.Equal(new long[] { 2, 3, 4 }, result.OrderBy(x => x));
    }

    [Fact]
    public async Task Expand_CyclicData_Terminates()
    {
        // Dữ liệu hỏng: 5 là con của 6 và 6 là con của 5. Phép nở phải dừng, không lặp vô hạn.
        SetupTree(Company(5, 6), Company(6, 5));

        var result = await _sut.ExpandWithDescendantsAsync(new long[] { 5 });

        Assert.Equal(new long[] { 5, 6 }, result.OrderBy(x => x));
    }
}
