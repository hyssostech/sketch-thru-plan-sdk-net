using StpSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetFrameworkSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Connect();
            Console.ReadLine();
        }

        private static async void Connect()
        {
            try
            {
                // Create an STP connection object - using STP's native pub/sub system via TCP or WebSockets
                IStpConnector stpConnector = new StpJsonRpcConnector(null, "localhost:9555");

                // Initialize the STP recognizer with the connector definition
                StpRecognizer _stpRecognizer = new StpRecognizer(stpConnector);

                // Hook up to the events _before_ connecting, so that the correct message subscriptions can be identified
                // A new symbol has been added, updated or removed
                _stpRecognizer.OnSymbolAdded += StpRecognizer_OnSymbolAdded;
                _stpRecognizer.OnSymbolModified += StpRecognizer_OnSymbolModified;
                _stpRecognizer.OnSymbolDeleted += StpRecognizer_OnSymbolDeleted;

                // Edit operations, including map commands
                _stpRecognizer.OnSymbolEdited += StpRecognizer_OnSymbolEdited;
                _stpRecognizer.OnMapOperation += StpRecognizer_OnMapOperation;

                // Speech recognition and ink feedback
                _stpRecognizer.OnSpeechRecognized += StpRecognizer_OnSpeechRecognized;
                _stpRecognizer.OnListeningStateChanged += StpRecognizer_OnListeningStateChanged;
                _stpRecognizer.OnSketchRecognized += StpRecognizer_OnSketchRecognized;
                _stpRecognizer.OnSketchIntegrated += StpRecognizer_OnSketchIntegrated;

                // Message from STP to be conveyed to user
                _stpRecognizer.OnStpMessage += StpRecognizer_OnStpMessage;

                // Connection error notification
                _stpRecognizer.OnConnectionError += StpRecognizer_OnConnectionError;

                // STP is being shutdown 
                _stpRecognizer.OnShutdown += StpRecognizer_OnShutdown;

                // Attempt to connect
                ShowStpMessage("---------------------------------");
                ShowStpMessage("Connecting...");
                string sessionId = await _stpRecognizer.ConnectAndRegisterAsync("EditSample");
                ShowStpMessage($"Connection established: {sessionId != null}");
            }
            catch (Exception ex)
            {
                ShowStpMessage($"Exception: {ex.Message}");
            }
        }

        private static void StpRecognizer_OnShutdown()
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnConnectionError(string msg, bool stpDisabled, Exception sce)
        {
            ShowStpMessage($"STP Connection Error: {msg} (Disabled: {stpDisabled})");
        }

        private static void StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel level, string msg)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnSketchIntegrated()
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnSketchRecognized(List<SketchRecoResult> sketchList)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnListeningStateChanged(bool IsListening)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnSpeechRecognized(List<string> speechList)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnMapOperation(string operation, Location location)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnSymbolEdited(string operation, Location location)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnSymbolDeleted(string poid, bool isUndo)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnSymbolModified(string poid, StpItem stpSymbol, bool isUndo)
        {
            throw new NotImplementedException();
        }

        private static void StpRecognizer_OnSymbolAdded(string poid, StpItem stpSymbol, bool isUndo)
        {
            throw new NotImplementedException();
        }

        private static void ShowStpMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
/*
// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// Create and run the C2SIM console service
// For an overview of the dependency injection, logging and configuration enacted
// behind the scenes, see for example:
// https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host 
// https://snede.net/get-started-with-net-generic-host/ is a simple intro
// but the mechanics it implements are supported more directly by
// BackgroundService - see https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice
// A few different usage patterns are shown here: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-3.1&tabs=netcore-cli
await CreateHostBuilder(args).RunConsoleAsync();// .Build.RunAsync();
/// <summary>
// Create the main console service, passing in Logger and C2SIM SDK object configured
// according to appsettings.json parameters, which can be overwritten by command line arguments
/// </summary>
static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services
                .AddHostedService<C2SIMConsole>();
            services.AddOptions<C2SIMSDKSettings>()
                .Bind(hostContext.Configuration.GetSection("App"));
        });
*/