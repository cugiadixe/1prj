using System;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Application.Security.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string PermissionCode { get; }
    public PermissionScope Scope { get; }

    public RequirePermissionAttribute(string permissionCode, PermissionScope scope = PermissionScope.Company)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("Permission code cannot be null or whitespace.", nameof(permissionCode));
        }

        PermissionCode = permissionCode;
        Scope = scope;
    }
}
