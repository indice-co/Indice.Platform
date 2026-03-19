using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.ActivityLogs;

/// <summary>Standard categories for activity logging.</summary>
public static class ActivityLogCategories
{
    /// <summary>For authentication related activities</summary>
    public const string Authentication = "Authentication";
    /// <summary>For authorization related activities</summary>
    public const string Authorization = "Authorization";
    /// <summary>For security related activities</summary>
    public const string Security = "Security";
    /// <summary>For data access related activities</summary>
    public const string DataAccess = "DataAccess";
    /// <summary>For data modification related activities</summary>
    public const string DataModification = "DataModification";
    /// <summary>For user management related activities</summary>
    public const string UserManagement = "UserManagement";
    /// <summary>For business process related activities</summary>
    public const string BusinessProcess = "BusinessProcess";
    /// <summary>For system related activities</summary>
    public const string System = "System";
}