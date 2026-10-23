using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TopCharge.Sdk;

public sealed class TopChargeClient : IDisposable
{
    private const string DefaultBaseUrl = "https://api.topcharge.app";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;
    private readonly bool ownsHttpClient;
    private readonly string clientId;
    private readonly string? secretKey;
    private readonly string baseUrl;

    public TopChargeClient(
        string clientId,
        string? secretKey = null,
        string baseUrl = DefaultBaseUrl,
        HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("TopCharge client ID must not be empty.", nameof(clientId));
        }
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("TopCharge base URL must not be empty.", nameof(baseUrl));
        }

        this.clientId = clientId;
        this.secretKey = secretKey;
        this.baseUrl = baseUrl.TrimEnd('/');
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        ownsHttpClient = httpClient is null;

        Account = new TopChargeAccountService(this);
        Payments = new TopChargePaymentService(this);
        Withdrawals = new TopChargeWithdrawalService(this);
    }

    public TopChargeAccountService Account { get; }
    public TopChargePaymentService Payments { get; }
    public TopChargeWithdrawalService Withdrawals { get; }

    internal async Task SendVoidAsync(
        HttpMethod method,
        string path,
        object? payload = null,
        IReadOnlyDictionary<string, string?>? query = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(method, path, payload, query, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload = null,
        IReadOnlyDictionary<string, string?>? query = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(method, path, payload, query, cancellationToken).ConfigureAwait(false);
        var body = await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions)
                ?? throw new InvalidOperationException("TopCharge returned an empty JSON response.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("TopCharge returned an invalid JSON response.", exception);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? payload,
        IReadOnlyDictionary<string, string?>? query,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(path, query);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("User-Agent", "topcharge-dotnet-sdk/1.0");
        request.Headers.TryAddWithoutValidation("x-api-key", clientId);
        if (!string.IsNullOrWhiteSpace(secretKey))
        {
            request.Headers.TryAddWithoutValidation("x-api-secret", secretKey);
        }
        if (payload is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        return await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private string BuildUrl(string path, IReadOnlyDictionary<string, string?>? query)
    {
        var url = $"{baseUrl}/{path.Trim('/')}/";
        if (query is null)
        {
            return url;
        }

        var values = query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}")
            .ToArray();
        return values.Length == 0 ? url : $"{url}?{string.Join("&", values)}";
    }

    private static async Task<string> EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return body;
        }

        throw new TopChargeApiException(ExtractErrorMessage(body, response.StatusCode), response.StatusCode, body);
    }

    private static string ExtractErrorMessage(string body, HttpStatusCode statusCode)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString()!;
            }
            if (root.TryGetProperty("err", out var error)
                && error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty("message", out message)
                && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Use the stable fallback below for non-JSON error payloads.
        }

        return $"TopCharge API request failed with HTTP status {(int)statusCode}.";
    }

    public void Dispose()
    {
        if (ownsHttpClient)
        {
            httpClient.Dispose();
        }
    }
}

public sealed class TopChargeAccountService(TopChargeClient client)
{
    public Task VerifyCredentialsAsync(CancellationToken cancellationToken = default) =>
        client.SendVoidAsync(HttpMethod.Get, "/api/v1/account/verify-credentials", cancellationToken: cancellationToken);

    public Task<AccountBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default) =>
        client.SendAsync<AccountBalanceResponse>(HttpMethod.Get, "/api/v1/account/balance", cancellationToken: cancellationToken);
}

public sealed class TopChargePaymentService(TopChargeClient client)
{
    public Task<PaymentCreateResponse> CreateAsync(PaymentCreateRequest request, CancellationToken cancellationToken = default) =>
        client.SendAsync<PaymentCreateResponse>(HttpMethod.Post, "/api/v1/payment/create", request, cancellationToken: cancellationToken);

    public Task<PaymentStatusResponse> GetStatusAsync(string paymentId, CancellationToken cancellationToken = default) =>
        client.SendAsync<PaymentStatusResponse>(HttpMethod.Get, $"/api/v1/payment/status/{Uri.EscapeDataString(paymentId)}", cancellationToken: cancellationToken);

    public Task<PaymentCancelResponse> CancelAsync(string paymentId, CancellationToken cancellationToken = default) =>
        client.SendAsync<PaymentCancelResponse>(HttpMethod.Post, $"/api/v1/payment/cancel/{Uri.EscapeDataString(paymentId)}", cancellationToken: cancellationToken);
}

public sealed class TopChargeWithdrawalService(TopChargeClient client)
{
    public Task<WithdrawalResponse> CreateAsync(WithdrawalCreateRequest request, CancellationToken cancellationToken = default) =>
        client.SendAsync<WithdrawalResponse>(HttpMethod.Post, "/api/v1/withdrawal/create", request, cancellationToken: cancellationToken);
}

