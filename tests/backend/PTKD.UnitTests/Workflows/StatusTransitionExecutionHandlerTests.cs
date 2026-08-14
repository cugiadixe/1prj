using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Workflows;

/// <summary>
/// Kiểm các nhánh QUYẾT ĐỊNH của lớp nền (chạy trước khi chạm CSDL): sai loại đối tượng,
/// đã xử lý xong, sai trạng thái nguồn. Đây là chỗ quyết định hồ sơ có bị đánh dấu Thất bại
/// hay bỏ qua êm, nên sai ở đây là hỏng dữ liệu hoặc chôn lỗi.
/// </summary>
public class StatusTransitionExecutionHandlerTests
{
    private sealed class FakeEntity
    {
        public long Id { get; init; } = 7;
        public string Status { get; set; } = "PENDING_APPROVAL";
    }

    /// <summary>Handler thử nghiệm: mọi thứ trong bộ nhớ, không đụng CSDL.</summary>
    private sealed class TestHandler : StatusTransitionExecutionHandler<FakeEntity>
    {
        private readonly FakeEntity? _entity;
        public bool ApprovedApplied { get; private set; }

        public TestHandler(FakeEntity? entity)
            : base(BuildFactory(), Mock.Of<ITransactionalAuditWriter>())
        {
            _entity = entity;
        }

        private static IOrganizationDbContextFactory BuildFactory()
        {
            var factory = new Mock<IOrganizationDbContextFactory>();
            factory.Setup(f => f.CreateDbContext()).Returns(Mock.Of<IOrganizationDbContext>());
            return factory.Object;
        }

        public override string ProcessCode => "TEST_PROCESS";
        protected override string BusinessEntityType => "FakeEntity";
        protected override string RequiredStatus => "PENDING_APPROVAL";
        protected override IReadOnlyCollection<string> AlreadyDoneStatuses => ["APPROVED"];
        protected override string ExecutedAuditEventCode => "TEST_EXECUTED";

        protected override Task<FakeEntity?> LoadAsync(IOrganizationDbContext db, long entityId, CancellationToken ct)
            => Task.FromResult(_entity);

        protected override string GetStatus(FakeEntity entity) => entity.Status;
        protected override long GetEntityId(FakeEntity entity) => entity.Id;

        protected override void ApplyApproved(FakeEntity entity, WorkflowInstance instance)
        {
            ApprovedApplied = true;
            entity.Status = "APPROVED";
        }
    }

    /// <summary>WorkflowInstance có ctor nội bộ nên dựng bằng reflection cho test.</summary>
    private static WorkflowInstance MakeInstance(string entityType)
    {
        var instance = (WorkflowInstance)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(WorkflowInstance));

        typeof(WorkflowInstance).GetProperty(nameof(WorkflowInstance.BusinessEntityType))!
            .SetValue(instance, entityType);
        typeof(WorkflowInstance).GetProperty(nameof(WorkflowInstance.BusinessEntityId))!
            .SetValue(instance, 7L);
        return instance;
    }

    [Fact]
    public async Task WrongEntityType_Throws()
    {
        var handler = new TestHandler(new FakeEntity());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(MakeInstance("SomethingElse")));

        Assert.Contains("Sai loại đối tượng", ex.Message);
    }

    [Fact]
    public async Task EntityNotFound_Throws()
    {
        var handler = new TestHandler(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(MakeInstance("FakeEntity")));
    }

    [Fact]
    public async Task AlreadyDone_IsIdempotent_NoMutation()
    {
        // Chạy lại một hồ sơ đã thực thi không được làm gì thêm, và KHÔNG được ném lỗi.
        var entity = new FakeEntity { Status = "APPROVED" };
        var handler = new TestHandler(entity);

        await handler.ExecuteAsync(MakeInstance("FakeEntity"));

        Assert.False(handler.ApprovedApplied);
        Assert.Equal("APPROVED", entity.Status);
    }

    [Fact]
    public async Task UnexpectedStatus_Throws()
    {
        // Trạng thái lạ = bất thường thật → phải nổi lên hàng đợi Thất bại, không được bỏ qua êm.
        var entity = new FakeEntity { Status = "CANCELLED" };
        var handler = new TestHandler(entity);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(MakeInstance("FakeEntity")));

        Assert.Contains("CANCELLED", ex.Message);
        Assert.False(handler.ApprovedApplied);
    }

    [Fact]
    public async Task OnRejected_DoesNothing_WhenNotDeclared()
    {
        // Module đã tự hoàn tác ở tầng service thì handler không được làm lần thứ hai.
        var handler = new TestHandler(new FakeEntity());
        await handler.OnRejectedAsync(MakeInstance("FakeEntity"));
        Assert.False(handler.ApprovedApplied);
    }
}
