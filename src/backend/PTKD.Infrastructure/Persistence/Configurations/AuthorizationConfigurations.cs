using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Security.Authorization;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "dbo", t => t.HasTrigger("trg_Permissions"));
        builder.HasKey(p => p.PermissionCode);
        
        builder.Property(p => p.PermissionCode).HasColumnName("permission_code").IsRequired().HasMaxLength(100);
        builder.Property(p => p.ModuleCode).HasColumnName("module_code").IsRequired().HasMaxLength(50);
        builder.Property(p => p.ActionCode).HasColumnName("action_code").IsRequired().HasMaxLength(50);
        builder.Property(p => p.DataScope).HasColumnName("data_scope").IsRequired().HasMaxLength(30);
        builder.Property(p => p.IsSensitive).HasColumnName("is_sensitive").IsRequired();
        builder.Property(p => p.RequiresReason).HasColumnName("requires_reason").IsRequired();
        builder.Property(p => p.IsDelegable).HasColumnName("is_delegable").IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired().HasConversion(
            v => v.Value,
            v => PTKD.Domain.ValueObjects.RowVersion.FromByteArray(v));
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo", t => t.HasTrigger("trg_Roles"));
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.RoleCode).HasColumnName("role_code").IsRequired().HasMaxLength(100);
        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(p => p.ScopeType).HasColumnName("scope_type").IsRequired().HasMaxLength(30);
        builder.Property(p => p.CompanyId).HasColumnName("company_id");
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired().HasConversion(
            v => v.Value,
            v => PTKD.Domain.ValueObjects.RowVersion.FromByteArray(v));
            
        builder.HasMany(r => r.Permissions).WithOne().HasForeignKey(rp => rp.RoleId);
    }
}

public class AdminGroupConfiguration : IEntityTypeConfiguration<AdminGroup>
{
    public void Configure(EntityTypeBuilder<AdminGroup> builder)
    {
        builder.ToTable("Admin_Groups", "dbo", t => t.HasTrigger("trg_Admin_Groups"));
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.GroupCode).HasColumnName("group_code").IsRequired().HasMaxLength(100);
        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(p => p.ScopeType).HasColumnName("scope_type").IsRequired().HasMaxLength(30);
        builder.Property(p => p.CompanyId).HasColumnName("company_id");
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired().HasConversion(
            v => v.Value,
            v => PTKD.Domain.ValueObjects.RowVersion.FromByteArray(v));
            
        builder.HasMany(g => g.Permissions).WithOne().HasForeignKey(gp => gp.AdminGroupId);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("Role_Permissions", "dbo", t => t.HasTrigger("trg_Role_Permissions"));
        builder.HasKey(p => new { p.RoleId, p.PermissionCode });
        
        builder.Property(p => p.RoleId).HasColumnName("role_id");
        builder.Property(p => p.PermissionCode).HasColumnName("permission_code").IsRequired().HasMaxLength(100);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
    }
}

public class AdminGroupPermissionConfiguration : IEntityTypeConfiguration<AdminGroupPermission>
{
    public void Configure(EntityTypeBuilder<AdminGroupPermission> builder)
    {
        builder.ToTable("Admin_Group_Permissions", "dbo", t => t.HasTrigger("trg_Admin_Group_Permissions"));
        builder.HasKey(p => new { p.AdminGroupId, p.PermissionCode });
        
        builder.Property(p => p.AdminGroupId).HasColumnName("admin_group_id");
        builder.Property(p => p.PermissionCode).HasColumnName("permission_code").IsRequired().HasMaxLength(100);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
    }
}

public class DepartmentPermissionConfiguration : IEntityTypeConfiguration<DepartmentPermission>
{
    public void Configure(EntityTypeBuilder<DepartmentPermission> builder)
    {
        builder.ToTable("Department_Permissions", "dbo", t => t.HasTrigger("trg_Department_Permissions"));
        builder.HasKey(p => new { p.DepartmentId, p.PermissionCode });
        
        builder.Property(p => p.DepartmentId).HasColumnName("department_id");
        builder.Property(p => p.PermissionCode).HasColumnName("permission_code").IsRequired().HasMaxLength(100);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
    }
}

public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("User_Role_Assignments", "dbo", t => t.HasTrigger("trg_User_Role_Assignments"));
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(p => p.AssignmentStatus).HasColumnName("assignment_status").IsRequired().HasMaxLength(30);
        builder.Property(p => p.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(p => p.EffectiveTo).HasColumnName("effective_to");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired().HasConversion(
            v => v.Value,
            v => PTKD.Domain.ValueObjects.RowVersion.FromByteArray(v));
            
        builder.HasOne(a => a.Role).WithMany().HasForeignKey(a => a.RoleId);
    }
}

public class UserAdminGroupAssignmentConfiguration : IEntityTypeConfiguration<UserAdminGroupAssignment>
{
    public void Configure(EntityTypeBuilder<UserAdminGroupAssignment> builder)
    {
        builder.ToTable("User_Admin_Group_Assignments", "dbo", t => t.HasTrigger("trg_User_Admin_Group_Assignments"));
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.AdminGroupId).HasColumnName("admin_group_id").IsRequired();
        builder.Property(p => p.AssignmentStatus).HasColumnName("assignment_status").IsRequired().HasMaxLength(30);
        builder.Property(p => p.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(p => p.EffectiveTo).HasColumnName("effective_to");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired().HasConversion(
            v => v.Value,
            v => PTKD.Domain.ValueObjects.RowVersion.FromByteArray(v));
            
        builder.HasOne(a => a.AdminGroup).WithMany().HasForeignKey(a => a.AdminGroupId);
    }
}

public class UserIndividualPermissionConfiguration : IEntityTypeConfiguration<UserIndividualPermission>
{
    public void Configure(EntityTypeBuilder<UserIndividualPermission> builder)
    {
        builder.ToTable("User_Individual_Permissions", "dbo", t => t.HasTrigger("trg_User_Individual_Permissions"));
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.PermissionCode).HasColumnName("permission_code").IsRequired().HasMaxLength(100);
        builder.Property(p => p.ScopeType).HasColumnName("scope_type").IsRequired().HasMaxLength(30);
        builder.Property(p => p.CompanyId).HasColumnName("company_id");
        builder.Property(p => p.GrantType).HasColumnName("grant_type").IsRequired().HasMaxLength(10);
        builder.Property(p => p.AssignmentStatus).HasColumnName("assignment_status").IsRequired().HasMaxLength(30);
        builder.Property(p => p.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(p => p.EffectiveTo).HasColumnName("effective_to");
        builder.Property(p => p.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired().HasConversion(
            v => v.Value,
            v => PTKD.Domain.ValueObjects.RowVersion.FromByteArray(v));
    }
}

public class AuthorizationPolicyStateConfiguration : IEntityTypeConfiguration<AuthorizationPolicyState>
{
    public void Configure(EntityTypeBuilder<AuthorizationPolicyState> builder)
    {
        builder.ToTable("Authorization_Policy_State", "dbo", t => t.HasTrigger("trg_Authorization_Policy_State"));
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.PolicyVersion).HasColumnName("policy_version").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired().HasConversion(
            v => v.Value,
            v => PTKD.Domain.ValueObjects.RowVersion.FromByteArray(v));
    }
}
