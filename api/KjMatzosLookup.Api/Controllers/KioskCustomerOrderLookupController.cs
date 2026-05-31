using KjMatzosLookup.Application.Dtos;
using KjMatzosLookup.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KjMatzosLookup.Api.Controllers;

/// <summary>
/// KJ Matzos standalone in-store kiosk — single tenant, no back-office login.
/// </summary>
[AllowAnonymous]
[Route("api/kiosk/customer-orders")]
[ApiController]
public class KioskCustomerOrderLookupController : ControllerBase
{
    private readonly ICustomerOrderLookupKioskService _service;

    public KioskCustomerOrderLookupController(ICustomerOrderLookupKioskService service)
    {
        _service = service;
    }

    [HttpGet("setup")]
    public async Task<IActionResult> GetSetup()
    {
        var result = await _service.GetSetupAsync();
        return ApiResponseActionResult.From(result);
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores([FromQuery] int customerId)
    {
        var result = await _service.GetStoresAsync(customerId);
        return ApiResponseActionResult.From(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> LookupOrders(
        [FromQuery] int customerId,
        [FromQuery] string phone,
        [FromQuery] int? year,
        [FromQuery] int? hebrewYear,
        [FromQuery] Guid? storeId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var result = await _service.LookupOrdersAsync(new CustomerOrderLookupQueryDto
        {
            CustomerId = customerId,
            StoreId = storeId.HasValue && storeId.Value != Guid.Empty ? storeId : null,
            Phone = phone ?? string.Empty,
            Year = year,
            HebrewYear = hebrewYear,
            Skip = skip,
            Take = take,
        });
        return ApiResponseActionResult.From(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] CustomerProfileUpdateDto update)
    {
        var result = await _service.UpdateProfileAsync(update);
        return ApiResponseActionResult.From(result);
    }

    [HttpGet("orders/{transactionId:guid}")]
    public async Task<IActionResult> GetOrderDetail(
        Guid transactionId,
        [FromQuery] int customerId,
        [FromQuery] Guid shopperCustomerId,
        [FromQuery] Guid? storeId = null)
    {
        var result = await _service.GetOrderDetailAsync(
            customerId,
            storeId ?? Guid.Empty,
            transactionId,
            shopperCustomerId);
        return ApiResponseActionResult.From(result);
    }
}
