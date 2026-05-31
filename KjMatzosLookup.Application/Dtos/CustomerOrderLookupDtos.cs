using Newtonsoft.Json;

namespace KjMatzosLookup.Application.Dtos;

public class KioskSetupDto
{
    [JsonProperty("customerId")]
    public int CustomerId { get; set; }

    [JsonProperty("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonProperty("stores")]
    public List<KioskStoreOptionDto> Stores { get; set; } = new();

    [JsonProperty("sampleCustomers")]
    public List<KioskSampleCustomerDto> SampleCustomers { get; set; } = new();
}

public class KioskSampleCustomerDto
{
    [JsonProperty("label")]
    public string Label { get; set; } = string.Empty;

    [JsonProperty("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonProperty("note")]
    public string? Note { get; set; }
}

public class KioskTenantOptionDto
{
    [JsonProperty("customerId")]
    public int CustomerId { get; set; }

    [JsonProperty("customerName")]
    public string CustomerName { get; set; } = string.Empty;
}

public class KioskStoreOptionDto
{
    [JsonProperty("storeId")]
    public Guid StoreId { get; set; }

    [JsonProperty("storeName")]
    public string StoreName { get; set; } = string.Empty;
}

public class CustomerOrderLookupQueryDto
{
    [JsonProperty("customerId")]
    public int CustomerId { get; set; }

    [JsonProperty("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonProperty("year")]
    public int? Year { get; set; }

    [JsonProperty("hebrewYear")]
    public int? HebrewYear { get; set; }

    [JsonProperty("storeId")]
    public Guid? StoreId { get; set; }

    [JsonProperty("skip")]
    public int Skip { get; set; }

    [JsonProperty("take")]
    public int Take { get; set; } = 50;
}

public class PurchaseYearDto
{
    [JsonProperty("hebrewYear")]
    public int HebrewYear { get; set; }

    [JsonProperty("hebrewLabel")]
    public string HebrewLabel { get; set; } = string.Empty;
}

public class CustomerOrderLookupResultDto
{
    [JsonProperty("found")]
    public bool Found { get; set; }

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    [JsonProperty("maskedPhone")]
    public string? MaskedPhone { get; set; }

    [JsonProperty("customerId")]
    public Guid? CustomerId { get; set; }

    [JsonProperty("customerName")]
    public string? CustomerName { get; set; }

    [JsonProperty("profile")]
    public CustomerProfileDto? Profile { get; set; }

    [JsonProperty("purchaseYears")]
    public List<PurchaseYearDto> PurchaseYears { get; set; } = new();

    [JsonProperty("orders")]
    public List<CustomerOrderSummaryDto> Orders { get; set; } = new();

    [JsonProperty("totalCount")]
    public int TotalCount { get; set; }
}

public class CustomerProfileDto
{
    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("phones")]
    public List<string> Phones { get; set; } = new();

    [JsonProperty("primaryPhone")]
    public string? PrimaryPhone { get; set; }

    [JsonProperty("secondaryPhone")]
    public string? SecondaryPhone { get; set; }

    [JsonProperty("secondaryPhoneLinkId")]
    public Guid? SecondaryPhoneLinkId { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("address")]
    public string? Address { get; set; }
}

public class CustomerProfileUpdateDto
{
    [JsonProperty("customerId")]
    public int CustomerId { get; set; }

    [JsonProperty("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonProperty("shopperCustomerId")]
    public Guid ShopperCustomerId { get; set; }

    [JsonProperty("firstName")]
    public string? FirstName { get; set; }

    [JsonProperty("lastName")]
    public string? LastName { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("address")]
    public string? Address { get; set; }

    [JsonProperty("primaryPhone")]
    public string? PrimaryPhone { get; set; }

    [JsonProperty("secondaryPhone")]
    public string? SecondaryPhone { get; set; }
}

public class CustomerOrderSummaryDto
{
    [JsonProperty("transactionId")]
    public Guid TransactionId { get; set; }

    [JsonProperty("transactionNo")]
    public string TransactionNo { get; set; } = string.Empty;

    [JsonProperty("saleDate")]
    public DateTime? SaleDate { get; set; }

    [JsonProperty("storeId")]
    public Guid? StoreId { get; set; }

    [JsonProperty("storeName")]
    public string? StoreName { get; set; }

    [JsonProperty("total")]
    public decimal Total { get; set; }

    [JsonProperty("itemCount")]
    public int ItemCount { get; set; }

    [JsonProperty("statusLabel")]
    public string StatusLabel { get; set; } = string.Empty;

    [JsonProperty("isPhoneOrder")]
    public bool IsPhoneOrder { get; set; }
}

public class CustomerOrderDetailDto
{
    [JsonProperty("transactionId")]
    public Guid TransactionId { get; set; }

    [JsonProperty("transactionNo")]
    public string TransactionNo { get; set; } = string.Empty;

    [JsonProperty("saleDate")]
    public DateTime? SaleDate { get; set; }

    [JsonProperty("storeName")]
    public string? StoreName { get; set; }

    [JsonProperty("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonProperty("tax")]
    public decimal Tax { get; set; }

    [JsonProperty("total")]
    public decimal Total { get; set; }

    [JsonProperty("statusLabel")]
    public string StatusLabel { get; set; } = string.Empty;

    [JsonProperty("lines")]
    public List<CustomerOrderLineDto> Lines { get; set; } = new();
}

public class CustomerOrderLineDto
{
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("qty")]
    public decimal Qty { get; set; }

    [JsonProperty("lineTotal")]
    public decimal LineTotal { get; set; }
}
