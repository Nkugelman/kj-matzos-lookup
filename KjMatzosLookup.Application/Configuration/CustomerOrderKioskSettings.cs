namespace KjMatzosLookup.Application.Configuration;

/// <summary>
/// Standalone KJ Matzos customer order kiosk — single-tenant scope.
/// </summary>
public class CustomerOrderKioskSettings
{
    public const string SectionName = "CustomerOrderKiosk";

    /// <summary>Tenant Customers.CustomerId for KJ Matzos.</summary>
    public int TenantCustomerId { get; set; } = 152;

    public string TenantDisplayName { get; set; } = "KJ Matzos";

    /// <summary>Dev/testing only — returned on setup when non-empty (omit in production).</summary>
    public List<KioskSampleCustomer> SampleCustomers { get; set; } = new();

    /// <summary>When true and <see cref="SampleCustomers"/> is empty, Development loads a few shoppers from the tenant DB.</summary>
    public bool AutoLoadSampleCustomersInDevelopment { get; set; } = true;

    /// <summary>User ID recorded as modifier when customers edit profile on the kiosk.</summary>
    public Guid ModifierUserId { get; set; } = Guid.Empty;
}

public class KioskSampleCustomer
{
    public string Label { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Note { get; set; }
}
