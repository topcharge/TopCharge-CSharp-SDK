namespace TopCharge.Sdk;

public sealed record AccountBalanceResponse
{
    public string TotalBalance { get; init; } = "";
    public string ReservedBalance { get; init; } = "";
    public string LiabilityBalance { get; init; } = "";
    public string AvailableBalance { get; init; } = "";
    public string CashAvailableBalance { get; init; } = "";
}

public sealed record PaymentCreateRequest(
    string PaymentAmount,
    string? Description = null,
    string? RedirectUrl = null,
    string? CallbackUrl = null);

public sealed record PaymentCreateResponse
{
    public string PaymentId { get; init; } = "";
    public string PaymentUrl { get; init; } = "";
    public string RequestedAmount { get; init; } = "";
    public string FeeAmount { get; init; } = "";
    public string NetAmount { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record PaymentStatusResponse
{
    public string PaymentId { get; init; } = "";
    public string RequestedAmount { get; init; } = "";
    public string FeeAmount { get; init; } = "";
    public string NetAmount { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record PaymentCancelResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string PaymentId { get; init; } = "";
    public string Status { get; init; } = "";
}

public sealed record WithdrawalCreateRequest(string PhoneNumberCountryCode, string PhoneNumber, string Amount);

public sealed record WithdrawalRecipient
{
    public string Id { get; init; } = "";
    public string Username { get; init; } = "";
    public string PhoneNumberCountryCode { get; init; } = "";
    public string PhoneNumber { get; init; } = "";
}

public sealed record WithdrawalResponse
{
    public string WithdrawalId { get; init; } = "";
    public string RequestedAmount { get; init; } = "";
    public string FeePercent { get; init; } = "";
    public string FeeAmount { get; init; } = "";
    public string NetAmount { get; init; } = "";
    public string TransactionId { get; init; } = "";
    public string Status { get; init; } = "";
    public WithdrawalRecipient Recipient { get; init; } = new();
    public DateTimeOffset CompletedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

