using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace StpSDKSample
{
    class Program
    {
        static ILogger _logger;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddNLog(new NLogLoggingConfiguration(hostContext.Configuration.GetSection("NLog")));
                        //.SetMinimumLevel(LogLevel.Trace);
                    });
                    services.AddSingleton<Form1>();
                    services.AddOptions<AppParams>()
                        .Bind(hostContext.Configuration.GetSection("App"));
                })
                .Build();

            // Get the logger to use here
            var lf = host.Services.GetRequiredService<ILoggerFactory>();
            //_logger = host.Services.GetRequiredService<ILogger<Program>>();
            _logger = lf.CreateLogger<Program>();
            _logger.LogWarning("Starting...");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                var form1 = services.GetRequiredService<Form1>();
                Application.Run(form1);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string msg = "Unhandled exception: " + e.ExceptionObject.ToString();
            Console.WriteLine(msg);
            _logger.LogCritical(msg);
            _logger?.LogCritical(e.ToString());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string msg = "Thread exception: " + e.Exception.ToString();
            Console.WriteLine(msg);
            _logger.LogCritical(msg);
            _logger?.LogCritical(e.ToString());
        }
    }
}
