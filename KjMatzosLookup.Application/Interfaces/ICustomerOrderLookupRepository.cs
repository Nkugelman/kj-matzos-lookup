using KjMatzosLookup.Application.Dtos;
using KjMatzosLookup.Domain.Customers;

namespace KjMatzosLookup.Application.Interfaces;

/// <summary>
/// Persistence boundary for the kiosk. The application layer depends only on this
/// abstraction; the EF Core / tenant-database implementation lives in Infrastructure.
/// </summary>
public interface ICustomerOrderLookupRepository
{
    Task<List<KioskStoreOptionDto>> GetStoresAsync();

    Task<List<KioskSampleCustomerDto>> GetSampleCustomersAsync(int take);

    Task<ShopperMatch?> FindShopperByPhoneAsync(string phoneKey);

    Task<CustomerProfileDto?> GetCustomerProfileAsync(Guid customerId, string? lookupPhoneFormatted);

    Task<List<PurchaseYearDto>> GetPurchaseHebrewYearsAsync(Guid shopperCustomerId);

    Task<(List<CustomerOrderSummaryDto> Orders, int TotalCount)> QueryOrdersAsync(
        ShopperMatch shopper,
        Guid? storeId,
        int? year,
        int? hebrewYear,
        int skip,
        int take);

    Task<CustomerOrderDetailDto?> GetOrderDetailAsync(Guid transactionId, Guid shopperCustomerId);

    Task<Guid?> GetTransactionStoreIdAsync(Guid transactionId);

    Task<CustomerProfileDto?> UpdateCustomerProfileAsync(
        Guid shopperCustomerId,
        CustomerProfileUpdateDto update,
        Guid modifierId);
}
