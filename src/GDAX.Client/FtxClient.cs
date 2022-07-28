using System.Text.Json;
using System.Text.Json.Serialization;
using GDAX.Client.Auth;
using GDAX.Client.Http;

namespace GDAX.Client;

/// <summary>
///     Client used to provide access to FTX endpoints.
/// </summary>
public class FtxClient
{
    public readonly GdaxRequestBuilder RequestBuilder;

    /// <summary>
    ///     JsonSerializeSettings configured specifically for working with FTX.
    /// </summary>
    public readonly JsonSerializerOptions SerializationSettings;

    public FtxClient(FtxCredentials credentials, FtxEndpoints endpoints)
    {
        Credentials = credentials;
        Endpoints = endpoints;
        RequestBuilder = new GdaxRequestBuilder(Credentials, TimestampProvider.Default, Endpoints);
        SerializationSettings = CreateSerializerSettings();
    }

    /// <summary>
    ///     The set of HTTP endpoints for working with FTX.
    /// </summary>
    public FtxEndpoints Endpoints { get; }

    /// <summary>
    ///     The authentication credentials for working with FTX.
    /// </summary>
    public FtxCredentials Credentials { get; }

    /// <summary>
    ///     INTERNAL API. Creates our custom JSON settings.
    /// </summary>
    /// <returns>A custom JsonSerializerSettings class.</returns>
    internal static JsonSerializerOptions CreateSerializerSettings()
    {
        return new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault, NumberHandling = JsonNumberHandling.AllowReadingFromString };
    }
}
