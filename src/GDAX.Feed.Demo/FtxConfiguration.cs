using Microsoft.Extensions.Configuration;

namespace FTX.Feed.Demo;

/// <summary>
///     INTERNAL. Used to resolve configuration from appSettings.json
/// </summary>
public class FtxConfiguration
{
    public static readonly FtxConfiguration Instance = new();

    private FtxConfiguration()
    {
        Config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").Build();
    }

    public IConfigurationRoot Config { get; }

    public string FtxApiKey => Config["FtxApiKey"];

    public string FtxApiSecret => Config["FtxApiSecret"];
}
