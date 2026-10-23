# TopCharge .NET SDK

The TopCharge client for .NET 8+. It uses `HttpClient`, sends `x-api-key` and `x-api-secret`, and exposes Account, Payment, and Withdrawal services.

## Release status

This is a local source project until the NuGet package is published. Its future package identity must not be treated as an available public package yet.

## Add the local project

```xml
<ItemGroup>
  <ProjectReference Include="path/to/sdk/csharp/src/TopCharge.Sdk/TopCharge.Sdk.csproj" />
</ItemGroup>
```

Build and view the individual integration commands with:

```bash
dotnet build src/TopCharge.Sdk
dotnet run --project tests/IntegrationExamples -- help
```

## Quick start

```csharp
using TopCharge.Sdk;

using var client = new TopChargeClient(
    clientId: Environment.GetEnvironmentVariable("TOPCHARGE_CLIENT_ID")!,
    secretKey: Environment.GetEnvironmentVariable("TOPCHARGE_SECRET_KEY")!);

var payment = await client.Payments.CreateAsync(new PaymentCreateRequest(
    PaymentAmount: "25000",
    Description: "Invoice #1001",
    RedirectUrl: "https://merchant.example/success",
    CallbackUrl: "https://merchant.example/webhooks/payment"));

Console.WriteLine(payment.PaymentUrl);
```

The default API host is `https://api.topcharge.app`. Pass `baseUrl` to use another environment. Successful calls return typed models; unsuccessful API calls throw `TopChargeApiException`, which provides `StatusCode` and `ResponseBody`.

`clientId` is sent as `x-api-key` and `secretKey` as `x-api-secret`. Keep both only in server-side secret storage or environment variables; never commit them or send them to a client application.

## Services

```csharp
await client.Account.VerifyCredentialsAsync();
var balance = await client.Account.GetBalanceAsync();

var payment = await client.Payments.CreateAsync(new PaymentCreateRequest("25000"));
var status = await client.Payments.GetStatusAsync(payment.PaymentId);
await client.Payments.CancelAsync(payment.PaymentId);

await client.Withdrawals.CreateAsync(new WithdrawalCreateRequest("964", "7701234567", "50000"));

```

## Integration examples

Each command tests exactly one API operation, except `payment:smoke`, which deliberately runs create → status → cancel as an end-to-end check.

```bash
export TOPCHARGE_RUN_INTEGRATION=1
export TOPCHARGE_CLIENT_ID='mch_...'
export TOPCHARGE_SECRET_KEY='sk_...'

dotnet run --project tests/IntegrationExamples -- help
dotnet run --project tests/IntegrationExamples -- payment:create
dotnet run --project tests/IntegrationExamples -- payment:smoke
```

### Account

```bash
dotnet run --project tests/IntegrationExamples -- account:verify
dotnet run --project tests/IntegrationExamples -- account:balance
```

### Payment

```bash
# Creates a PENDING payment and prints paymentId and paymentUrl.
dotnet run --project tests/IntegrationExamples -- payment:create

# Use the paymentId from create for each independent operation.
export TOPCHARGE_PAYMENT_ID='01...'
dotnet run --project tests/IntegrationExamples -- payment:status
dotnet run --project tests/IntegrationExamples -- payment:cancel

# End-to-end test: creates, checks, then cancels automatically.
dotnet run --project tests/IntegrationExamples -- payment:smoke
```

`payment:create` defaults to `50000` IQD. Override it with `TOPCHARGE_TEST_PAYMENT_AMOUNT`; use `TOPCHARGE_REDIRECT_URL` and `TOPCHARGE_CALLBACK_URL` to test your own URLs.

### Withdrawal

This action transfers value and cannot be automatically undone.

```bash
export TOPCHARGE_ALLOW_MONEY_MOVEMENT=1
export TOPCHARGE_PHONE_COUNTRY_CODE='964'
export TOPCHARGE_PHONE_NUMBER='7701234567'
export TOPCHARGE_WITHDRAWAL_AMOUNT='50000'
dotnet run --project tests/IntegrationExamples -- withdrawal:create
```

Never put credentials in source code or commit them to Git.

## Reliability

Every method accepts a `CancellationToken`; the SDK does not retry calls automatically. Retrying reads is normally safe. Do not blindly retry payment creation or withdrawals after an unknown network result; resolve the original operation first.

## License

[MIT License](LICENSE)
