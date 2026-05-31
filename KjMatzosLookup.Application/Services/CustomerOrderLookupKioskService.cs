using BackOffice.Common;
using KjMatzosLookup.Application.Configuration;
using KjMatzosLookup.Application.Dtos;
using KjMatzosLookup.Application.Interfaces;
using KjMatzosLookup.Domain.Phone;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KjMatzosLookup.Application.Services;

/// <summary>
/// Application use cases for the KJ Matzos kiosk. Performs validation and shapes
/// responses; all data access goes through <see cref="ICustomerOrderLookupRepository"/>.
/// </summary>
public class CustomerOrderLookupKioskService : ICustomerOrderLookupKioskService
{
    private readonly ICustomerOrderLookupRepository _repository;
    private readonly CustomerOrderKioskSettings _kioskSettings;
    private readonly IHostEnvironment _hostEnvironment;

    public CustomerOrderLookupKioskService(
        ICustomerOrderLookupRepository repository,
        IOptions<CustomerOrderKioskSettings> kioskSettings,
        IHostEnvironment hostEnvironment)
    {
        _repository = repository;
        _kioskSettings = kioskSettings.Value;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<ApiResponse<KioskSetupDto>> GetSetupAsync()
    {
        try
        {
            var sampleCustomers = await ResolveSampleCustomersAsync();

            return ApiResponseFactory.Success(new KioskSetupDto
            {
                CustomerId = _kioskSettings.TenantCustomerId,
                CustomerName = _kioskSettings.TenantDisplayName,
                SampleCustomers = sampleCustomers,
            }, $"{_kioskSettings.TenantDisplayName} kiosk ready.");
        }
        catch (Exception ex)
        {
            return ApiResponseFactory.InternalError<KioskSetupDto>(
                "Could not load kiosk setup.",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<KioskStoreOptionDto>>> GetStoresAsync(int customerId)
    {
        if (customerId <= 0)
        {
            return ApiResponseFactory.BadRequest<List<KioskStoreOptionDto>>("customerId is required.");
        }

        if (!IsAllowedTenant(customerId))
        {
            return ApiResponseFactory.Forbidden<List<KioskStoreOptionDto>>(
                "This kiosk is configured for KJ Matzos only.");
        }

        try
        {
            var stores = await _repository.GetStoresAsync();
            return ApiResponseFactory.Success(stores, "Stores loaded.");
        }
        catch (Exception ex)
        {
            return ApiResponseFactory.InternalError<List<KioskStoreOptionDto>>(
                "Could not load stores. Check that the API can reach the KJ Matzos database.",
                TenantDbErrors(ex));
        }
    }

    public async Task<ApiResponse<CustomerOrderLookupResultDto>> LookupOrdersAsync(
        CustomerOrderLookupQueryDto query)
    {
        if (!IsAllowedTenant(query.CustomerId))
            return ApiResponseFactory.Forbidden<CustomerOrderLookupResultDto>(
                "This kiosk is for KJ Matzos only.");

        try
        {
            var phoneKey = PhoneNormalizer.MatchKey(query.Phone);
            if (phoneKey.Length < 7)
                return ApiResponseFactory.BadRequest<CustomerOrderLookupResultDto>("Enter a valid phone number.");

            var shopper = await _repository.FindShopperByPhoneAsync(phoneKey);
            if (shopper == null)
            {
                return ApiResponseFactory.Success(new CustomerOrderLookupResultDto
                {
                    Found = false,
                    Phone = PhoneNormalizer.Format(query.Phone),
                    MaskedPhone = PhoneNormalizer.Format(query.Phone),
                    Orders = new List<CustomerOrderSummaryDto>(),
                    TotalCount = 0,
                }, "No customer found for this phone number.");
            }

            var profile = await _repository.GetCustomerProfileAsync(shopper.CustomerId, query.Phone);
            var purchaseYears = await _repository.GetPurchaseHebrewYearsAsync(shopper.CustomerId);

            var take = Math.Clamp(query.Take <= 0 ? 50 : query.Take, 1, 100);
            var (orders, totalCount) = await _repository.QueryOrdersAsync(
                shopper,
                query.StoreId,
                query.Year,
                query.HebrewYear,
                query.Skip,
                take);

            var formattedPhone = PhoneNormalizer.Format(query.Phone);
            return ApiResponseFactory.Success(new CustomerOrderLookupResultDto
            {
                Found = true,
                Phone = formattedPhone,
                MaskedPhone = formattedPhone,
                CustomerId = shopper.CustomerId,
                CustomerName = profile?.DisplayName ?? shopper.DisplayName,
                Profile = profile,
                PurchaseYears = purchaseYears,
                Orders = orders,
                TotalCount = totalCount,
            }, "Orders loaded.");
        }
        catch (Exception ex)
        {
            return ApiResponseFactory.InternalError<CustomerOrderLookupResultDto>(
                "Could not connect to the KJ Matzos database.",
                TenantDbErrors(ex));
        }
    }

    public async Task<ApiResponse<CustomerProfileDto>> UpdateProfileAsync(CustomerProfileUpdateDto update)
    {
        if (!IsAllowedTenant(update.CustomerId))
            return ApiResponseFactory.Forbidden<CustomerProfileDto>("This kiosk is for KJ Matzos only.");

        try
        {
            var phoneKey = PhoneNormalizer.MatchKey(update.Phone);
            if (phoneKey.Length < 7)
                return ApiResponseFactory.BadRequest<CustomerProfileDto>("Enter a valid phone number.");

            if (update.ShopperCustomerId == Guid.Empty)
                return ApiResponseFactory.BadRequest<CustomerProfileDto>("shopperCustomerId is required.");

            var shopper = await _repository.FindShopperByPhoneAsync(phoneKey);
            if (shopper == null || shopper.CustomerId != update.ShopperCustomerId)
                return ApiResponseFactory.Forbidden<CustomerProfileDto>("Phone number does not match this customer.");

            var profile = await _repository.UpdateCustomerProfileAsync(
                update.ShopperCustomerId,
                update,
                _kioskSettings.ModifierUserId);

            if (profile == null)
                return ApiResponseFactory.NotFound<CustomerProfileDto>("Customer not found.");

            return ApiResponseFactory.Success(profile, "Profile updated.");
        }
        catch (Exception ex)
        {
            return ApiResponseFactory.InternalError<CustomerProfileDto>(
                "Could not update profile.",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<CustomerOrderDetailDto>> GetOrderDetailAsync(
        int customerId,
        Guid storeId,
        Guid transactionId,
        Guid shopperCustomerId)
    {
        if (!IsAllowedTenant(customerId))
            return ApiResponseFactory.Forbidden<CustomerOrderDetailDto>("This kiosk is for KJ Matzos only.");

        try
        {
            if (shopperCustomerId == Guid.Empty)
                return ApiResponseFactory.BadRequest<CustomerOrderDetailDto>("shopperCustomerId is required.");

            var detail = await _repository.GetOrderDetailAsync(transactionId, shopperCustomerId);

            if (detail == null)
                return ApiResponseFactory.NotFound<CustomerOrderDetailDto>("Order not found.");

            if (storeId != Guid.Empty)
            {
                var txStore = await _repository.GetTransactionStoreIdAsync(transactionId);
                if (txStore != storeId)
                    return ApiResponseFactory.NotFound<CustomerOrderDetailDto>("Order not found for this store.");
            }

            return ApiResponseFactory.Success(detail, "Order detail loaded.");
        }
        catch (Exception ex)
        {
            return ApiResponseFactory.InternalError<CustomerOrderDetailDto>(
                "Could not load order detail.",
                new List<string> { ex.Message });
        }
    }

    private bool IsAllowedTenant(int customerId) =>
        customerId > 0 && customerId == _kioskSettings.TenantCustomerId;

    private List<KioskSampleCustomerDto> MapConfiguredSampleCustomers() =>
        _kioskSettings.SampleCustomers
            .Where(s => !string.IsNullOrWhiteSpace(s.Phone))
            .Select(s => new KioskSampleCustomerDto
            {
                Label = string.IsNullOrWhiteSpace(s.Label) ? s.Phone.Trim() : s.Label.Trim(),
                Phone = s.Phone.Trim(),
                Note = s.Note,
            })
            .ToList();

    private async Task<List<KioskSampleCustomerDto>> ResolveSampleCustomersAsync()
    {
        var configured = MapConfiguredSampleCustomers();
        if (configured.Count > 0)
            return configured;

        if (!_hostEnvironment.IsDevelopment() || !_kioskSettings.AutoLoadSampleCustomersInDevelopment)
            return configured;

        try
        {
            return await _repository.GetSampleCustomersAsync(8);
        }
        catch
        {
            return configured;
        }
    }

    private static List<string> TenantDbErrors(Exception ex)
    {
        var errors = new List<string> { ex.Message };
        var inner = ex.InnerException?.Message;
        if (!string.IsNullOrWhiteSpace(inner) && inner != ex.Message)
            errors.Add(inner);

        return errors;
    }
}
