using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using GDAX.Client;
using GDAX.Client.Auth;
using GDAX.Feed;
using GDAX.Feed.Msgs;
using GDAX.Messages;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace FTX.Feed.Demo;

internal class Program
{
    public static GdaxRealtimeFeedClient CreateLiveSocketFeed()
    {
        return new GdaxRealtimeFeedClient(new FtxClient(new FtxCredentials(FtxConfiguration.Instance.FtxApiKey,
                FtxConfiguration.Instance.FtxApiSecret),
            FtxEndpoints.Live));
    }

    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(theme: SystemConsoleTheme.Colored)
            .CreateLogger();

        using var system = ActorSystem.Create("FTXDemo");
        var client = CreateLiveSocketFeed();
        
        var feed = new GdaxFeed(system, client);
        var sub = feed.Subscribe("ticker", "BTC/USD", false, true);

        var consoleWriterActor = system.ActorOf(Props.Create(() => new ConsoleWriterActor()));
        var sink = Sink.ActorRef<IFeedMessage>(consoleWriterActor, "true");

        // begins running the graph
        using var materializer = system.Materializer();
        sink.RunWith(sub.source, materializer);

        Console.ReadLine();
    }

    public class ConsoleWriterActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            Log.Information(message.ToString());
            if (message is Error error)
            {
                Log.Information("ERROR | {@Data}", error);
                return;
            }

            if (message is Ticker ticker)
            {
                Log.Information("TICKER | {@Data}", ticker);
                return;
            }

            Console.WriteLine(message);
        }
    }
}
