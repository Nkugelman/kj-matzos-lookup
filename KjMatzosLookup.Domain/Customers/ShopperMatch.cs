namespace KjMatzosLookup.Domain.Customers;

/// <summary>
/// A customer resolved from a phone-number lookup — the minimal identity the
/// application needs to fetch orders and profile data.
/// </summary>
public sealed record ShopperMatch(Guid CustomerId, string DisplayName);
