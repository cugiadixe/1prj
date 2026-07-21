using System;
using PTKD.Application.Security.Audit;

namespace PTKD.UnitTests.Security.Audit;

public sealed class SecurityAuditEventRecordSanitizationTests
{
    private static SecurityAuditEventRecord ValidRecord(
        string? changedFieldsJson = null,
        string? beforeStateJson = null,
        string? afterStateJson = null,
        string? requestMetadataJson = null) =>
        new()
        {
            EventCode = "TEST_EVENT",
            EntityType = "TEST_ENTITY",
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid(),
            ChangedFieldsJson = changedFieldsJson,
            BeforeStateJson = beforeStateJson,
            AfterStateJson = afterStateJson,
            RequestMetadataJson = requestMetadataJson
        };

    [Fact]
    public void ThrowIfContainsSensitiveData_AllNullJsonFields_DoesNotThrow()
    {
        var record = ValidRecord();
        record.ThrowIfContainsSensitiveData();
    }

    [Fact]
    public void ThrowIfContainsSensitiveData_SafeJson_DoesNotThrow()
    {
        var record = ValidRecord(
            changedFieldsJson: """{"user_id": 42, "display_name": "Test User"}""",
            beforeStateJson: """{"status": "active"}""",
            afterStateJson: """{"status": "inactive"}""",
            requestMetadataJson: """{"ip": "127.0.0.1", "user_agent": "test"}""");
        record.ThrowIfContainsSensitiveData();
    }

    [Theory]
    [InlineData("""{"password": "hunter2"}""", "ChangedFieldsJson")]
    [InlineData("""{"token": "abc123"}""", "ChangedFieldsJson")]
    [InlineData("""{"secret": "shh"}""", "ChangedFieldsJson")]
    [InlineData("""{"signing_key": "keydata"}""", "ChangedFieldsJson")]
    [InlineData("""{"private_key": "keydata"}""", "ChangedFieldsJson")]
    [InlineData("""{"api_key": "apidata"}""", "ChangedFieldsJson")]
    [InlineData("""{"auth_key": "keydata"}""", "ChangedFieldsJson")]
    [InlineData("""{"access_key": "keydata"}""", "ChangedFieldsJson")]
    public void ThrowIfContainsSensitiveData_BlockedKeyInChangedFields_Throws(string json, string expectedParam)
    {
        var record = ValidRecord(changedFieldsJson: json);
        var ex = Assert.Throws<ArgumentException>(() => record.ThrowIfContainsSensitiveData());
        Assert.Equal(expectedParam, ex.ParamName);
    }

    [Fact]
    public void ThrowIfContainsSensitiveData_BlockedKeyInBeforeState_Throws()
    {
        var record = ValidRecord(beforeStateJson: """{"password": "old"}""");
        var ex = Assert.Throws<ArgumentException>(() => record.ThrowIfContainsSensitiveData());
        Assert.Equal("BeforeStateJson", ex.ParamName);
    }

    [Fact]
    public void ThrowIfContainsSensitiveData_BlockedKeyInAfterState_Throws()
    {
        var record = ValidRecord(afterStateJson: """{"token": "new-token"}""");
        var ex = Assert.Throws<ArgumentException>(() => record.ThrowIfContainsSensitiveData());
        Assert.Equal("AfterStateJson", ex.ParamName);
    }

    [Fact]
    public void ThrowIfContainsSensitiveData_BlockedKeyInRequestMetadata_Throws()
    {
        var record = ValidRecord(requestMetadataJson: """{"api_key": "secret"}""");
        var ex = Assert.Throws<ArgumentException>(() => record.ThrowIfContainsSensitiveData());
        Assert.Equal("RequestMetadataJson", ex.ParamName);
    }

    [Theory]
    [InlineData("""{"password_reset_count": 3}""")]
    [InlineData("""{"total_tokens_issued": 5}""")]
    [InlineData("""{"has_secret_question": true}""")]
    public void ThrowIfContainsSensitiveData_SensitiveWordInValue_DoesNotThrow(string json)
    {
        // Checks match JSON key syntax ("key":), not substrings of values or other keys.
        var record = ValidRecord(changedFieldsJson: json);
        record.ThrowIfContainsSensitiveData();
    }

    [Fact]
    public void ThrowIfContainsSensitiveData_CaseInsensitiveBlockedKey_Throws()
    {
        var record = ValidRecord(changedFieldsJson: """{"PASSWORD": "secret"}""");
        var ex = Assert.Throws<ArgumentException>(() => record.ThrowIfContainsSensitiveData());
        Assert.Equal("ChangedFieldsJson", ex.ParamName);
    }
}
