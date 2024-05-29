using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using AmppControl;
using AmppControl.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace cloudwatch.bridge
{
    class Program
    {
        static string reconKey = "CloudWatchBridge";
        static string? systemWorkloadId;
        static CloudwatchSender? cwSender;

        static async Task Main(string[] args)
        {
          
            /*
             * Access the application configuration.  
             * 
             * First  priority : command line
             * Second priority : key=values found as environment variables
             * Third  priority : key values and sections found in the 'appsettings.json' file
             * 
             */
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            /*
             * Display usage information if requested by the user
             */
            if (config.GetValue<string>("help") != null || config.GetValue<string>("?") != null)
            {
                PrintUsage();
                Environment.Exit(0);
            }

            /*
             * Configure the logger based on user provided minimum level. logs to the console
             */
            var levelSwitch = new LoggingLevelSwitch();
            var logLevel = config.GetValue("LOG_LEVEL", "INFO");
            switch (logLevel)
            {
                case "DEBUG":
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
                    break;
                case "ERROR":
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Error;
                    break;
                case "INFO":
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                    break;
                case "WARNING":
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Warning;
                    break;
                case "VERBOSE":
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                    break;
                default:
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                    break;
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Console()
                .CreateLogger();

            /*
             *  Read the PlatformUrl and ApiKey values required for connecting to the platform
             */
            var platformUrl = GetConfigValue(config, "PlatformUrl");
            if (!Uri.IsWellFormedUriString(platformUrl, UriKind.Absolute))
            {
                Log.Error("PlatformUrl is malformed.");
                Environment.Exit(1);
            }

            var apiKey = GetConfigValue(config, "ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Log.Error("Invalid ApiKey.");
                Environment.Exit(1);
            }

            /*
             * Connect to the platform (obtain tokens, etc)
             */
            Log.Information("Connecting to " + platformUrl);
            var amppControl = new AmppControlClient(platformUrl, apiKey);
            if (!await amppControl.LoginAsync())
            {
                Log.Error("Error connecting to AMPP.");
                Environment.Exit(1);
            }

            Log.Information("Connected to AMPP.");

            /*
             * Configure the Cloudwatch sender and connect to cloudwatch 
             */
            var logGroupName = GetConfigValue(config, "CloudWatchLogGroupName");
            var metricsNameSpace = GetConfigValue(config, "CloudWatchMetricsNamespace");
            cwSender = new CloudwatchSender(logGroupName, metricsNameSpace);
            await cwSender.Connect();

            /*
             * Get the system workload id.  This is the workload we will listen to for health notifications
             */
            systemWorkloadId = GetConfigValue(config, "SystemWorkloadId");
            Guid tmp;
            if (!Guid.TryParse(systemWorkloadId, out tmp))
            {
                Log.Error("Malformed system workloadId (not a UUID).");
                Environment.Exit(1);
            }


            amppControl.OnAmppControlNotifyEvent += AmppControl_OnAmppControlNotifyEvent;
            amppControl.OnAmppControlErrorEvent += AmppControl_OnAmppControlErrorEvent;
            await amppControl.SubscribeToWorkload(systemWorkloadId);

            amppControl.OnSignalRReconnected += (sender, arg) =>{
                amppControl.SubscribeToWorkload(systemWorkloadId);
            };


            Log.Information($"Listening for Status updates for workload `{systemWorkloadId}`");
            bool isOnline = await amppControl.PingAsync(systemWorkloadId, 1000);
            if (isOnline)
            {
                Log.Information($"System workload {systemWorkloadId} is online.");
            }
            else
            {
                Log.Information($"System workload {systemWorkloadId} is offline.");
            }

            // All Workloads support a getstate message.
            // This command is used to get the entire state of the workload when you first connect
            // Do not poll using this call
            // Listen for specific notification messages for state updates.
            await amppControl.GetStateAsync(systemWorkloadId, reconKey);

            Log.Information("Press Enter to Exit...");
            Console.ReadLine();
        }

        private static void PrintUsage()
        {
            var usage = @"
                This application requires configuration keys that can provided in mulitple ways:
                - appsettings.json (lowest priority)
                - environment variables 
                - command line arguements (/key=value )

                Configuration keys:
                    PlatformUrl                 Required. The absolute URL of the ampp platform to connect to.
                    ApiKey                      Required. Any API Key allowing the application to connect to your ampp account.
                    SystemWorkloadId            Required. The UUID of a workload of type <System> providing health statistics.  This workload should be in the running state.
                    CloudWatchLogGroupName      Required. The name of the CloudWatch Log Group where the log streams will be created.  If the log group does not exist it will be created.
                    CloudWatchMetricsNamespace  Required. A namespace for CloudWatch Metrics.  If the namespace does not exist it will be created.
                    LOG_LEVEL                   Optional. The minimal log level to display on the console.  Can be : INFO, ERROR, WARNING, DEBUG, VERBOSE
                ";

            Console.WriteLine(usage);
        }


        /// <summary>
        /// Get a string value for a key 
        /// </summary>
        /// <param name="config">the configuration store</param>
        /// <param name="key">the key to look up</param>
        /// <param name="throwIfNotFound>Throw if the key returns a null value</param>
        /// <returns>The value associated to the key</returns>
        /// <exception cref="KeyNotFoundException">If the key is not found</exception>
        private static string GetConfigValue(IConfigurationRoot config, string key)
        {
            var value = config.GetValue<string>(key);
            if (value == null)
            {
                throw new KeyNotFoundException($"{key} not defined.  Please define it as an environment variable or in appsettings.json");
            }
            return value;
        }

        private static void AmppControl_OnAmppControlErrorEvent(object? sender, AmppControl.Model.AmppControlErrorEventArgs e)
        {
            // Only report errors that have originated from our requests
            if (e.Key == reconKey)
            {
                Log.Error("************Error Notification**************");
                Log.Error($"Workload:\t{e.Workload}");
                Log.Error($"Command:\t{e.Command}");
                Log.Error($"Error:\t{e.Error}");
                Log.Error($"Details:\t{e.Details}");
                Log.Error("**************************************");
            }
        }

        private static async void AmppControl_OnAmppControlNotifyEvent(object? sender, AmppControl.Model.AmppControlNotificationEventArgs e)
        {
            try
            {
                Log.Debug("On AMPP Control Notify Event " + e.Command);
                if (cwSender != null && e.Workload == systemWorkloadId)
                {
                    if (e.Command != null && e.Command == "logstatistics")
                    {
                        await cwSender.PublishMetrics(e.Notification);
                    }
                    else if (e.Command != null && e.Command != "ping")
                    {
                        await cwSender.PublishLogs(e.Command, e.Notification);
                    }
                }
            } catch (Exception ex)
            {
                Log.Error($"Failed to send CloudWatch data. {ex.Message}");
            }
        }
    }
}
