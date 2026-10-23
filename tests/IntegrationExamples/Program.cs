using System.Text.Json;
using TopCharge.Sdk;

var action = args.FirstOrDefault() ?? "help";
if (action == "help")
{
    PrintHelp();
    return;
}
if (Environment.GetEnvironmentVariable("TOPCHARGE_RUN_INTEGRATION") != "1")
{
    throw new InvalidOperationException("Refusing to call the API. Set TOPCHARGE_RUN_INTEGRATION=1.");
}

using var client = new TopChargeClient(
    Required("TOPCHARGE_CLIENT_ID"),
    Required("TOPCHARGE_SECRET_KEY"),
    Environment.GetEnvironmentVariable("TOPCHARGE_BASE_URL") ?? "https://api.topcharge.app");

switch (action)
{
    case "account:verify":
        await client.Account.VerifyCredentialsAsync();
        Console.WriteLine("Credentials are valid.");
        break;
    case "account:balance":
        Print(await client.Account.GetBalanceAsync());
        break;
    case "payment:create":
        Print(await client.Payments.CreateAsync(new PaymentCreateRequest(
            Environment.GetEnvironmentVariable("TOPCHARGE_TEST_PAYMENT_AMOUNT") ?? "50000",
            Environment.GetEnvironmentVariable("TOPCHARGE_PAYMENT_DESCRIPTION") ?? "TopCharge .NET SDK manual test",
            Environment.GetEnvironmentVariable("TOPCHARGE_REDIRECT_URL") ?? "https://example.com/topcharge-sdk-test",
            Environment.GetEnvironmentVariable("TOPCHARGE_CALLBACK_URL") ?? "https://example.com/topcharge-sdk-webhook")));
        break;
    case "payment:status":
        Print(await client.Payments.GetStatusAsync(Required("TOPCHARGE_PAYMENT_ID")));
        break;
    case "payment:cancel":
        Print(await client.Payments.CancelAsync(Required("TOPCHARGE_PAYMENT_ID")));
        break;
    case "payment:smoke":
        await PaymentSmokeTest(client);
        break;
    case "withdrawal:create":
        RequireConfirmation("TOPCHARGE_ALLOW_MONEY_MOVEMENT");
        Print(await client.Withdrawals.CreateAsync(new WithdrawalCreateRequest(
            Required("TOPCHARGE_PHONE_COUNTRY_CODE"), Required("TOPCHARGE_PHONE_NUMBER"), Required("TOPCHARGE_WITHDRAWAL_AMOUNT"))));
        break;
    default:
        throw new InvalidOperationException($"Unknown action '{action}'. Run with help.");
}

static async Task PaymentSmokeTest(TopChargeClient client)
{
    string? paymentId = null;
    try
    {
        var payment = await client.Payments.CreateAsync(new PaymentCreateRequest(
            Environment.GetEnvironmentVariable("TOPCHARGE_TEST_PAYMENT_AMOUNT") ?? "50000",
            $"TopCharge .NET SDK smoke {Guid.NewGuid():N}",
            "https://example.com/topcharge-sdk-test",
            "https://example.com/topcharge-sdk-webhook"));
        paymentId = payment.PaymentId;
        var status = await client.Payments.GetStatusAsync(paymentId);
        if (status.PaymentId != paymentId) throw new InvalidOperationException("Payment status did not match the created payment.");
        Console.WriteLine($"Payment link created and verified: {payment.PaymentUrl}");
    }
    finally
    {
        if (!string.IsNullOrWhiteSpace(paymentId))
        {
            Print(await client.Payments.CancelAsync(paymentId));
            Console.WriteLine("Test payment cancelled.");
        }
    }
}

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"{name} is required.");

static void RequireConfirmation(string name)
{
    if (Environment.GetEnvironmentVariable(name) != "1")
        throw new InvalidOperationException($"This action changes money or value. Set {name}=1 to continue.");
}

static void Print<T>(T value) => Console.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));

static void PrintHelp() => Console.WriteLine("""
TopCharge .NET integration examples

Set TOPCHARGE_RUN_INTEGRATION=1, TOPCHARGE_CLIENT_ID, and TOPCHARGE_SECRET_KEY first.

Account:
  account:verify
  account:balance

Payment:
  payment:create       Optional: TOPCHARGE_TEST_PAYMENT_AMOUNT, TOPCHARGE_PAYMENT_DESCRIPTION, TOPCHARGE_REDIRECT_URL, TOPCHARGE_CALLBACK_URL
  payment:status       Required: TOPCHARGE_PAYMENT_ID
  payment:cancel       Required: TOPCHARGE_PAYMENT_ID
  payment:smoke        Creates, reads, then cancels a test payment automatically

Withdrawal:
  withdrawal:create    Required: TOPCHARGE_ALLOW_MONEY_MOVEMENT=1, TOPCHARGE_PHONE_COUNTRY_CODE, TOPCHARGE_PHONE_NUMBER, TOPCHARGE_WITHDRAWAL_AMOUNT

Example:
  dotnet run --project tests/IntegrationExamples -- payment:smoke
""");
