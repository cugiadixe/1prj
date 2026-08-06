using Microsoft.Data.SqlClient;

namespace PTKD.IntegrationTests;

public static class TestDatabaseSafety
{
    public const string ApprovedDatabaseName = "PTKD_TEST_PHASE1A2";

    public const string DefaultConnectionString =
        "Server=localhost;Database=PTKD_TEST_PHASE1A2;Trusted_Connection=True;TrustServerCertificate=True;";

    public static string ResolveConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        return ValidateConnectionString(configured ?? DefaultConnectionString);
    }

    public static string ValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"A connection string with InitialCatalog={ApprovedDatabaseName} is required.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("The test connection string is invalid.", exception);
        }

        RequireApprovedDatabaseName(builder.InitialCatalog, "InitialCatalog");
        return builder.ConnectionString;
    }

    public static SqlConnection OpenVerifiedConnection(string connectionString)
    {
        var validatedConnectionString = ValidateConnectionString(connectionString);
        var connection = new SqlConnection(validatedConnectionString);

        try
        {
            connection.Open();
            VerifyOpenConnection(connection);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public static async Task<SqlConnection> OpenVerifiedConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var validatedConnectionString = ValidateConnectionString(connectionString);
        var connection = new SqlConnection(validatedConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            await VerifyOpenConnectionAsync(connection, cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public static string VerifyOpenConnection(SqlConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("DB_NAME() verification requires an open SQL connection.");
        }

        using var command = new SqlCommand("SELECT DB_NAME();", connection);
        var databaseName = command.ExecuteScalar() as string;
        RequireApprovedDatabaseName(databaseName, "DB_NAME()");
        return databaseName!;
    }

    public static async Task<string> VerifyOpenConnectionAsync(
        SqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("DB_NAME() verification requires an open SQL connection.");
        }

        await using var command = new SqlCommand("SELECT DB_NAME();", connection);
        var databaseName = await command.ExecuteScalarAsync(cancellationToken) as string;
        RequireApprovedDatabaseName(databaseName, "DB_NAME()");
        return databaseName!;
    }

    public static void RequireApprovedDatabaseName(string? databaseName, string source)
    {
        if (!string.Equals(databaseName, ApprovedDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{source} must equal {ApprovedDatabaseName}; resolved database was " +
                $"'{(string.IsNullOrEmpty(databaseName) ? "<empty>" : databaseName)}'.");
        }
    }
}
