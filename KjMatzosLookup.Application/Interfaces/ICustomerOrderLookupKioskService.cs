using BackOffice.Common;
using KjMatzosLookup.Application.Dtos;

namespace KjMatzosLookup.Application.Interfaces;

public interface ICustomerOrderLookupKioskService
{
    Task<ApiResponse<KioskSetupDto>> GetSetupAsync();
    Task<ApiResponse<List<KioskStoreOptionDto>>> GetStoresAsync(int customerId);
    Task<ApiResponse<CustomerOrderLookupResultDto>> LookupOrdersAsync(CustomerOrderLookupQueryDto query);
    Task<ApiResponse<CustomerOrderDetailDto>> GetOrderDetailAsync(
        int customerId,
        Guid storeId,
        Guid transactionId,
        Guid shopperCustomerId);

    Task<ApiResponse<CustomerProfileDto>> UpdateProfileAsync(CustomerProfileUpdateDto update);
}
