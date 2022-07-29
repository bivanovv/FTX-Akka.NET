using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using FTX.Client;
using FTX.Client.Auth;
using FTX.Feed.Msgs;
using FTX.Messages;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace FTX.Feed.Demo;

internal class Program
{
    public static FtxRealtimeFeedClient CreateLiveSocketFeed()
    {
        return new FtxRealtimeFeedClient(new FtxClient(new FtxCredentials(FtxConfiguration.Instance.FtxApiKey,
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
        
        var feed = new FtxFeed(system, client);
        //var sub = feed.Subscribe("ticker", "BTC/USD", false, true);
        var sub = feed.Subscribe("fills", null, true, true);

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
            //Log.Information(message.ToString());
            if (message is Error error)
            {
                Log.Information("ERROR | {@Data}", error);
                return;
            }

            if (message is SubscribedResponse subscribedResponse)
            {
                Log.Information("SUBSCRIBED | {@Data}", subscribedResponse);
                return;
            }

            if (message is SocketResponse<Ticker> tickerResponse)
            {
                Log.Information("TICKER | {@Data}", tickerResponse.Data);
                return;
            }

            if (message is SocketResponse<UserFill> userFillResponse)
            {
                Log.Information("USER FILL | {@Data}", userFillResponse.Data);
                return;
            }

            Console.WriteLine(message);
        }
    }
}
