using System.Net;

namespace TopCharge.Sdk;

public sealed class TopChargeApiException : Exception
{
    public TopChargeApiException(string message, HttpStatusCode statusCode, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }
}
