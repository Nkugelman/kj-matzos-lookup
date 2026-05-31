using BackOffice.Domain.Entities.Tenant;
using BackOffice.Infrastructure.DBContext.Tenant;
using KjMatzosLookup.Application.Dtos;
using KjMatzosLookup.Application.Interfaces;
using KjMatzosLookup.Domain.Calendar;
using KjMatzosLookup.Domain.Customers;
using KjMatzosLookup.Domain.Phone;
using Microsoft.EntityFrameworkCore;

namespace KjMatzosLookup.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the kiosk persistence boundary, backed by the
/// KJ Matzos tenant database (<see cref="TenantDBContext"/>).
/// </summary>
public class CustomerOrderLookupRepository : ICustomerOrderLookupRepository
{
    private const int MaxTake = 100;

    private readonly TenantDBContext _db;

    public CustomerOrderLookupRepository(TenantDBContext db)
    {
        _db = db;
    }

    public async Task<List<KioskStoreOptionDto>> GetStoresAsync()
    {
        return await _db.StoreViews
            .AsNoTracking()
            .OrderBy(s => s.StoreName)
            .Select(s => new KioskStoreOptionDto
            {
                StoreId = s.StoreID,
                StoreName = s.StoreName ?? "Store",
            })
            .ToListAsync();
    }

    public async Task<List<KioskSampleCustomerDto>> GetSampleCustomersAsync(int take)
    {
        var rows = await _db.Set<CustomerView>()
            .AsNoTracking()
            .Where(c => c.Phone != null && c.Status > -1)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Take(take)
            .Select(c => new { c.FirstName, c.LastName, c.Phone })
            .ToListAsync();

        return rows
            .Select(c =>
            {
                var name = $"{c.FirstName} {c.LastName}".Trim();
                return new KioskSampleCustomerDto
                {
                    Label = string.IsNullOrWhiteSpace(name) ? c.Phone! : name,
                    Phone = c.Phone!,
                    Note = "Dev — from tenant DB",
                };
            })
            .ToList();
    }

    public async Task<ShopperMatch?> FindShopperByPhoneAsync(string phoneKey)
    {
        var fromView = await _db.Set<CustomerView>()
            .AsNoTracking()
            .Where(c => c.Phone != null && c.Status > -1)
            .Select(c => new { c.CustomerID, c.Phone, c.FirstName, c.LastName, c.Name })
            .ToListAsync();

        var match = fromView.FirstOrDefault(c => PhoneNormalizer.PhoneMatches(c.Phone, phoneKey));
        if (match != null)
        {
            return new ShopperMatch(
                match.CustomerID,
                DisplayName(match.FirstName, match.LastName, match.Name));
        }

        var phoneLinks = await _db.Set<CustomerToPhoneView>()
            .AsNoTracking()
            .Where(p => p.PhoneNumber != null && p.Status > -1 && p.CostumerID != null)
            .Select(p => new { p.CostumerID, p.PhoneNumber })
            .ToListAsync();

        var phoneLink = phoneLinks.FirstOrDefault(p => PhoneNormalizer.PhoneMatches(p.PhoneNumber, phoneKey));
        if (phoneLink?.CostumerID == null) return null;

        var customer = await _db.Set<CustomerView>()
            .AsNoTracking()
            .Where(c => c.CustomerID == phoneLink.CostumerID.Value)
            .Select(c => new { c.CustomerID, c.FirstName, c.LastName, c.Name })
            .FirstOrDefaultAsync();

        if (customer == null) return null;

        return new ShopperMatch(
            customer.CustomerID,
            DisplayName(customer.FirstName, customer.LastName, customer.Name));
    }

    public async Task<List<PurchaseYearDto>> GetPurchaseHebrewYearsAsync(Guid shopperCustomerId)
    {
        var dates = await _db.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.CustomerID == shopperCustomerId && t.Status == 1 && t.StartSaleTime != null)
            .Select(t => t.StartSaleTime!.Value)
            .ToListAsync();

        return dates
            .Select(HebrewYearHelper.FromDate)
            .Distinct()
            .OrderByDescending(hy => hy)
            .Select(hy => new PurchaseYearDto
            {
                HebrewYear = hy,
                HebrewLabel = HebrewYearHelper.FormatLabel(hy),
            })
            .ToList();
    }

    public async Task<(List<CustomerOrderSummaryDto> Orders, int TotalCount)> QueryOrdersAsync(
        ShopperMatch shopper,
        Guid? storeId,
        int? year,
        int? hebrewYear,
        int skip,
        int take)
    {
        var txQuery = _db.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.CustomerID == shopper.CustomerId && t.Status == 1);

        if (storeId.HasValue && storeId.Value != Guid.Empty)
            txQuery = txQuery.Where(t => t.StoreID == storeId);

        if (year.HasValue && !hebrewYear.HasValue)
        {
            var y = year.Value;
            txQuery = txQuery.Where(t =>
                t.StartSaleTime.HasValue && t.StartSaleTime.Value.Year == y);
        }

        List<(Guid TransactionID, string TransactionNo, DateTime? StartSaleTime, Guid? StoreID, decimal? Debit, bool? PhoneOrder, short? Status)> transactions;

        int totalCount;

        if (hebrewYear.HasValue)
        {
            var hy = hebrewYear.Value;
            var raw = await txQuery
                .Where(t => t.StartSaleTime != null)
                .Select(t => new
                {
                    t.TransactionID,
                    t.TransactionNo,
                    t.StartSaleTime,
                    t.StoreID,
                    t.Debit,
                    t.PhoneOrder,
                    t.Status,
                })
                .ToListAsync();

            var filtered = raw
                .Where(t => HebrewYearHelper.FromDate(t.StartSaleTime!.Value) == hy)
                .OrderByDescending(t => t.StartSaleTime)
                .ToList();

            totalCount = filtered.Count;
            var clampedTake = Math.Clamp(take <= 0 ? 50 : take, 1, MaxTake);
            transactions = filtered
                .Skip(Math.Max(0, skip))
                .Take(clampedTake)
                .Select(t => (t.TransactionID, t.TransactionNo, t.StartSaleTime, t.StoreID, t.Debit, t.PhoneOrder, t.Status))
                .ToList();
        }
        else
        {
            totalCount = await txQuery.CountAsync();
            var clampedTake = Math.Clamp(take <= 0 ? 50 : take, 1, MaxTake);

            transactions = await txQuery
                .OrderByDescending(t => t.StartSaleTime)
                .Skip(Math.Max(0, skip))
                .Take(clampedTake)
                .Select(t => ValueTuple.Create(
                    t.TransactionID,
                    t.TransactionNo,
                    t.StartSaleTime,
                    t.StoreID,
                    t.Debit,
                    t.PhoneOrder,
                    t.Status))
                .ToListAsync();
        }

        var storeIds = transactions
            .Where(t => t.StoreID.HasValue)
            .Select(t => t.StoreID!.Value)
            .Distinct()
            .ToList();

        var storeNames = storeIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Set<StoreView>()
                .AsNoTracking()
                .Where(s => storeIds.Contains(s.StoreID))
                .ToDictionaryAsync(s => s.StoreID, s => s.StoreName ?? "Store");

        var transactionIds = transactions.Select(t => t.TransactionID).ToList();
        var itemCounts = transactionIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.Set<TransactionEntryItem>()
                .AsNoTracking()
                .Where(e => transactionIds.Contains(e.TransactionID))
                .GroupBy(e => e.TransactionID)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

        var orders = transactions.Select(t =>
        {
            storeNames.TryGetValue(t.StoreID ?? Guid.Empty, out var storeName);
            itemCounts.TryGetValue(t.TransactionID, out var count);
            return new CustomerOrderSummaryDto
            {
                TransactionId = t.TransactionID,
                TransactionNo = t.TransactionNo,
                SaleDate = t.StartSaleTime,
                StoreId = t.StoreID,
                StoreName = storeName,
                Total = t.Debit ?? 0m,
                ItemCount = count,
                IsPhoneOrder = t.PhoneOrder == true,
                StatusLabel = ResolveStatusLabel(t.Status, t.PhoneOrder),
            };
        }).ToList();

        return (orders, totalCount);
    }

    public async Task<CustomerOrderDetailDto?> GetOrderDetailAsync(Guid transactionId, Guid shopperCustomerId)
    {
        var transaction = await _db.Set<Transaction>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TransactionID == transactionId && t.CustomerID == shopperCustomerId);

        if (transaction == null) return null;

        string? storeName = null;
        if (transaction.StoreID.HasValue)
        {
            storeName = await _db.Set<StoreView>()
                .AsNoTracking()
                .Where(s => s.StoreID == transaction.StoreID.Value)
                .Select(s => s.StoreName)
                .FirstOrDefaultAsync();
        }

        var lines = await _db.Set<TransactionEntryItem>()
            .AsNoTracking()
            .Where(e => e.TransactionID == transactionId)
            .OrderBy(e => e.Name)
            .Select(e => new CustomerOrderLineDto
            {
                Description = string.IsNullOrWhiteSpace(e.Name) ? e.ParentName : e.Name,
                Qty = e.QTY ?? 0m,
                LineTotal = e.Total ?? e.ExtPrice ?? 0m,
            })
            .ToListAsync();

        var subtotal = lines.Sum(l => l.LineTotal);
        var tax = transaction.Tax ?? 0m;

        return new CustomerOrderDetailDto
        {
            TransactionId = transaction.TransactionID,
            TransactionNo = transaction.TransactionNo,
            SaleDate = transaction.StartSaleTime,
            StoreName = storeName,
            Subtotal = subtotal,
            Tax = tax,
            Total = transaction.Debit ?? subtotal + tax,
            StatusLabel = ResolveStatusLabel(transaction.Status, transaction.PhoneOrder),
            Lines = lines,
        };
    }

    public async Task<Guid?> GetTransactionStoreIdAsync(Guid transactionId)
    {
        return await _db.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.TransactionID == transactionId)
            .Select(t => t.StoreID)
            .FirstOrDefaultAsync();
    }

    public async Task<CustomerProfileDto?> GetCustomerProfileAsync(Guid customerId, string? lookupPhoneFormatted)
    {
        var customer = await _db.Set<CustomerView>()
            .AsNoTracking()
            .Where(c => c.CustomerID == customerId && c.Status > -1)
            .Select(c => new
            {
                c.FirstName,
                c.LastName,
                c.Name,
                c.Phone,
                c.Email,
                c.Address,
                c.Address2,
                c.HouseNo,
                c.StreetName,
            })
            .FirstOrDefaultAsync();

        if (customer == null) return null;

        var extraPhones = await _db.Set<CustomerToPhoneView>()
            .AsNoTracking()
            .Where(p => p.CostumerID == customerId && p.PhoneNumber != null && p.Status > -1)
            .Select(p => p.PhoneNumber!)
            .ToListAsync();

        var phones = new List<string>();
        void AddPhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (phones.Any(p => PhoneNormalizer.PhoneMatches(p, PhoneNormalizer.MatchKey(value))))
                return;
            phones.Add(value.Trim());
        }

        AddPhone(lookupPhoneFormatted);
        AddPhone(customer.Phone);
        foreach (var p in extraPhones) AddPhone(p);

        var address = customer.Address?.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            var parts = new[] { customer.HouseNo, customer.StreetName, customer.Address2 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim());
            address = string.Join(" ", parts);
        }

        var primaryPhone = string.IsNullOrWhiteSpace(customer.Phone) ? null : customer.Phone.Trim();
        var secondaryLink = await _db.Set<CustomerToPhoneView>()
            .AsNoTracking()
            .Where(p => p.CostumerID == customerId && p.PhoneNumber != null && p.Status > -1)
            .OrderBy(p => p.SortOrder)
            .Select(p => new { p.CostumerToPhoneID, p.PhoneNumber })
            .ToListAsync();

        string? secondaryPhone = null;
        Guid? secondaryPhoneLinkId = null;
        foreach (var link in secondaryLink)
        {
            if (primaryPhone != null && PhoneNormalizer.PhoneMatches(link.PhoneNumber, primaryPhone))
                continue;
            secondaryPhone = link.PhoneNumber?.Trim();
            secondaryPhoneLinkId = link.CostumerToPhoneID;
            break;
        }

        return new CustomerProfileDto
        {
            FirstName = customer.FirstName?.Trim() ?? string.Empty,
            LastName = customer.LastName?.Trim() ?? string.Empty,
            DisplayName = DisplayName(customer.FirstName, customer.LastName, customer.Name),
            Phones = phones,
            PrimaryPhone = primaryPhone,
            SecondaryPhone = secondaryPhone,
            SecondaryPhoneLinkId = secondaryPhoneLinkId,
            Email = string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.ToUpperInvariant(),
        };
    }

    public async Task<CustomerProfileDto?> UpdateCustomerProfileAsync(
        Guid shopperCustomerId,
        CustomerProfileUpdateDto update,
        Guid modifierId)
    {
        var customerRow = await _db.Set<CustomerView>()
            .AsNoTracking()
            .Where(c => c.CustomerID == shopperCustomerId && c.Status > -1)
            .FirstOrDefaultAsync();
        if (customerRow == null) return null;

        var customerEntity = await _db.Set<Customer>()
            .AsNoTracking()
            .Where(c => c.CustomerID == shopperCustomerId)
            .FirstOrDefaultAsync();
        if (customerEntity == null) return null;

        var firstName = string.IsNullOrWhiteSpace(update.FirstName)
            ? customerRow.FirstName
            : update.FirstName.Trim();
        var lastName = string.IsNullOrWhiteSpace(update.LastName)
            ? customerRow.LastName
            : update.LastName.Trim();
        var address = update.Address?.Trim() ?? customerRow.Address?.Trim() ?? string.Empty;
        var primaryPhone = string.IsNullOrWhiteSpace(update.PrimaryPhone)
            ? customerRow.Phone?.Trim() ?? string.Empty
            : PhoneNormalizer.Format(update.PrimaryPhone);
        var email = update.Email?.Trim() ?? customerRow.Email?.Trim() ?? string.Empty;

        await _db.Procedures.Sync_CustomerUpdateAsync(
            shopperCustomerId,
            customerRow.CustomerNo ?? customerEntity.CustomerNo ?? string.Empty,
            firstName,
            lastName,
            address,
            customerRow.Address2 ?? string.Empty,
            customerRow.City ?? string.Empty,
            customerRow.State ?? string.Empty,
            customerRow.Zip ?? string.Empty,
            primaryPhone,
            customerRow.FaxNumber ?? string.Empty,
            customerRow.PriceLevelID?.ToString() ?? string.Empty,
            DateTime.UtcNow,
            modifierId);

        await _db.Procedures.SP_CustomerUpdatePOSAsync(
            shopperCustomerId,
            customerRow.CustomerNo ?? customerEntity.CustomerNo ?? string.Empty,
            firstName,
            lastName,
            customerRow.MainAddressID,
            customerRow.BirthDay,
            customerRow.CustomerType,
            customerRow.Credit,
            customerRow.PriceLevelID,
            customerRow.TaxExempt,
            email,
            customerRow.LoyaltyMembertype,
            customerRow.TaxNumber ?? string.Empty,
            customerRow.Status,
            modifierId,
            null);

        if (update.SecondaryPhone != null)
        {
            var secondaryFormatted = string.IsNullOrWhiteSpace(update.SecondaryPhone)
                ? string.Empty
                : PhoneNormalizer.Format(update.SecondaryPhone);

            var existingLink = await _db.Set<CustomerToPhoneView>()
                .AsNoTracking()
                .Where(p => p.CostumerID == shopperCustomerId && p.Status > -1)
                .OrderBy(p => p.SortOrder)
                .Select(p => new { p.CostumerToPhoneID, p.PhoneNumber, p.PhoneType, p.SortOrder })
                .ToListAsync();

            var secondaryLink = existingLink.FirstOrDefault(p =>
                primaryPhone.Length == 0 || !PhoneNormalizer.PhoneMatches(p.PhoneNumber, primaryPhone));

            if (string.IsNullOrWhiteSpace(secondaryFormatted))
            {
                if (secondaryLink != null)
                {
                    await _db.Procedures.SP_CustomerToPhoneDeleteAsync(
                        secondaryLink.CostumerToPhoneID,
                        modifierId);
                }
            }
            else if (secondaryLink != null)
            {
                await _db.Procedures.SP_CustomerToPhoneUpdateAsync(
                    secondaryLink.CostumerToPhoneID,
                    shopperCustomerId,
                    secondaryLink.PhoneType ?? 2,
                    secondaryFormatted,
                    secondaryLink.SortOrder,
                    1,
                    DateTime.UtcNow,
                    modifierId);
            }
            else
            {
                await _db.Procedures.SP_CustomerToPhoneInsertAsync(
                    Guid.NewGuid(),
                    shopperCustomerId,
                    2,
                    secondaryFormatted,
                    2,
                    1,
                    modifierId);
            }
        }

        return await GetCustomerProfileAsync(shopperCustomerId, primaryPhone);
    }

    private static string DisplayName(string? first, string? last, string? name)
    {
        var combined = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? (name ?? string.Empty) : combined;
    }

    private static string ResolveStatusLabel(short? status, bool? phoneOrder)
    {
        if (status is null or < 1) return "Void";
        if (phoneOrder == true) return "Phone order";
        return "Completed";
    }
}
