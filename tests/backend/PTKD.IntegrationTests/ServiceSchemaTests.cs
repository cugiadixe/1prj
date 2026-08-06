using Microsoft.Data.SqlClient;

namespace PTKD.IntegrationTests;

[Collection("Sequential")]
public sealed class ServiceSchemaTests
{
    private readonly TestDatabaseFixture _fixture;

    public ServiceSchemaTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void V0011_ServiceTypes_TableExists()
    {
        _fixture.ResetToV0011();
        using var conn = _fixture.OpenVerifiedConnection();
        Assert.True(TableExists(conn, "Service_Types"));
    }

    [Fact]
    public void V0011_Services_TableExists()
    {
        _fixture.ResetToV0011();
        using var conn = _fixture.OpenVerifiedConnection();
        Assert.True(TableExists(conn, "Services"));
    }

    [Fact]
    public void V0011_ServicePriceHistory_TableExists()
    {
        _fixture.ResetToV0011();
        using var conn = _fixture.OpenVerifiedConnection();
        Assert.True(TableExists(conn, "Service_Price_History"));
    }

    [Fact]
    public void V0011_ServiceHistory_TableExists()
    {
        _fixture.ResetToV0011();
        using var conn = _fixture.OpenVerifiedConnection();
        Assert.True(TableExists(conn, "Service_History"));
    }

    [Fact]
    public void V0011_PermissionsSeeded()
    {
        _fixture.ResetToV0011();
        using var conn = _fixture.OpenVerifiedConnection();
        Assert.True(PermissionExists(conn, "SERVICE_VIEW"));
        Assert.True(PermissionExists(conn, "SERVICE_TYPE_MANAGE"));
        Assert.True(PermissionExists(conn, "SERVICE_CREATE_STANDARD"));
        Assert.True(PermissionExists(conn, "SERVICE_RENEW_STANDARD"));
        Assert.True(PermissionExists(conn, "SERVICE_PRICE_OVERRIDE_REQUEST"));
        Assert.True(PermissionExists(conn, "SERVICE_PRICE_OVERRIDE_APPROVE"));
    }

    [Fact]
    public void V0011_BusinessProcessCatalogSeeded()
    {
        _fixture.ResetToV0011();
        using var conn = _fixture.OpenVerifiedConnection();
        Assert.True(ProcessCodeExists(conn, "SERVICE_PRICE_OVERRIDE"));
        Assert.True(ProcessCodeExists(conn, "RENEW_SERVICE_STANDARD"));
    }

    [Fact]
    public void U0011_Rollback_DropsTablesAndDeactivatesPermissions()
    {
        _fixture.ResetToV0011();

        using var conn = _fixture.OpenVerifiedConnection();
        TestDatabaseFixture.ExecuteBatches(_fixture.ReadRollback("U0011__service_module_foundation.sql"), conn);

        Assert.False(TableExists(conn, "Service_Types"));
        Assert.False(TableExists(conn, "Services"));
        Assert.False(TableExists(conn, "Service_Price_History"));
        Assert.False(TableExists(conn, "Service_History"));
        Assert.False(PermissionIsActive(conn, "SERVICE_VIEW"));
        Assert.False(PermissionIsActive(conn, "SERVICE_TYPE_MANAGE"));
    }

    private static bool TableExists(SqlConnection conn, string tableName)
    {
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = @name", conn);
        cmd.Parameters.AddWithValue("@name", tableName);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static bool PermissionExists(SqlConnection conn, string permissionCode)
    {
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Permissions WHERE permission_code = @code", conn);
        cmd.Parameters.AddWithValue("@code", permissionCode);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static bool PermissionIsActive(SqlConnection conn, string permissionCode)
    {
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Permissions WHERE permission_code = @code AND is_active = 1", conn);
        cmd.Parameters.AddWithValue("@code", permissionCode);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static bool ProcessCodeExists(SqlConnection conn, string processCode)
    {
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Business_Process_Catalog WHERE process_code = @code AND is_active = 1", conn);
        cmd.Parameters.AddWithValue("@code", processCode);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
