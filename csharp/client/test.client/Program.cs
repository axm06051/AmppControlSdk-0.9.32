using AmppControl;
using AmppControl.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace test.client
{
    class Program
    {
        // Define a ReconKey for you app, that is unique, but also shows where requests originated from.
        // The key can be any string, but just a GUID would make it hard to tell the origin.
        static string reconKey = "AmppControlSdk";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();


            var section = config.GetSection(nameof(PlatformSettings));
            var platformSettings = section.Get<PlatformSettings>();

            Console.WriteLine("Connecting to AMPP");
            Console.WriteLine("URL: " + platformSettings.PlatformUrl);
            Console.WriteLine("APIKey: " + platformSettings.ApiKey);

            var amppControl = new AmppControlClient(platformSettings.PlatformUrl, platformSettings.ApiKey);

            bool connected = await amppControl.LoginAsync();

            if (!connected)
            {
                Console.WriteLine("Error Connecting to AMPP");
                Console.WriteLine("Press Any Key to Continue...");

                Environment.Exit(0);
            }

            Console.WriteLine("Connected to AMPP!");

            var apps = await amppControl.GetApplicationTypesAsync();

            Console.WriteLine("**************************");
            Console.WriteLine();
            Console.WriteLine("AMPP can be used to control or configure the following application types:");

            foreach (var app in apps)
            {
                Console.WriteLine(app.Name);
            }

            Console.WriteLine("**************************");
            Console.WriteLine("You can see what commands are supported by an application...");

            var miniMixer = apps.FirstOrDefault(a => a.Name == "MiniMixer");

            if (miniMixer != null)
            {
                Console.WriteLine("MiniMixer Supports the following commands:");
                foreach (var command in miniMixer.Commands)
                {
                    Console.WriteLine(command.Name);
                }

                var miniMixerControlStateSchema = miniMixer.Commands.FirstOrDefault(a => a.Name == "controlstate");

                if (miniMixerControlStateSchema != null)
                {
                    Console.WriteLine("**************************");
                    Console.WriteLine("You can see the JSON7 Schema for this command...");
                    Console.WriteLine(miniMixerControlStateSchema.Schema);

                    Console.WriteLine("**************************");
                    Console.WriteLine("And Even Get a Markdown Documentation File...");
                    Console.WriteLine(miniMixerControlStateSchema.Markdown);
                }

                Console.WriteLine("**************************");
                Console.WriteLine();
            }


            Console.WriteLine("The Following Workloads are registered as MiniMixer");

            var workloads = await amppControl.GetWorkloadsForApplicationTypeAsync("MiniMixer");

            foreach (var w in workloads)
            {
                Console.WriteLine(w);
            }

            Console.WriteLine("**************************");


            // Control Groups can be used to control Multiple workloads at the same time
            // Use the AMPP Control UI to define which workloads are in the group
            // Then use the GroupId in place of the workloadId when sending a control command
            var groups = await amppControl.GetControlGroupsForApplicationTypeAsync("MiniMixer");

            if (groups != null)
            {
                Console.WriteLine("The Following ControlGroups are registered for MiniMixer");
                foreach (var g in groups)
                {
                    Console.WriteLine(g.Name);
                }
            }

            Console.WriteLine("**************************");

            Console.WriteLine("AMPP Control has the following Macros defined");

            var macros = await amppControl.GetMacrosAsync();

            foreach (var macro in macros)
            {
                Console.WriteLine(macro.Name);
            }

            Console.WriteLine("**************************");
            Console.WriteLine();

            // Use the AMPP Control UI to define a Macro, then use the following to execute:
            string myMacroName = "SdkDemo";

            var macroToExecute = macros.FirstOrDefault(m => m.Name == myMacroName);

            if (macroToExecute != null)
            {
                Console.WriteLine("Executing Macro " + myMacroName);
                await amppControl.ExecuteMacroAsync(macroToExecute.Uuid, reconKey);
            }


            // Enter the workloadId of a workload you want to receive notifications for
            string myWorkloadId = "c8725b2d-0bb2-49e5-8f31-fa49e5da6495";

            amppControl.OnAmppControlNotifyEvent += AmppControl_OnAmppControlNotifyEvent;
            amppControl.OnAmppControlErrorEvent += AmppControl_OnAmppControlErrorEvent;
            amppControl.SubscribeToWorkload(myWorkloadId);

            Console.WriteLine($"Listening for Status updates for workload `{myWorkloadId}`");

            Console.WriteLine($"Pinging `{myWorkloadId}`");
            bool isOnline = await amppControl.PingAsync(myWorkloadId, 1000);

            if (isOnline)
            {
                Console.WriteLine($"Ping Okay");
            }
            else
            {
                Console.WriteLine($"Ping Timeout");
            }

            // All Workloads support a getstate message.
            // This command is used to get the entire state of the workload when you first connect
            // Do not poll using this call
            // Listen for specific notification messages for state updates.
            Console.WriteLine($"Calling GetState on `{myWorkloadId}`");
            await amppControl.GetStateAsync(myWorkloadId, reconKey);

            /// Wait for the responses from GetState()
            await Task.Delay(1000);
            Console.WriteLine("**************************");

            Console.WriteLine("Sending Request to Mute a Channel on an AudioMixer");

            var payload = new
            {
                index = 1,
                mute = true,
            };

            await amppControl.SendAmppControlMessageAsync(myWorkloadId, "AudioMixer", "channelstate", JObject.FromObject(payload), reconKey);

            await Task.Delay(1000);
            Console.WriteLine("**************************");

            Console.WriteLine("Sending Invalid Payload to trigger error response");

            var invalidPayload = new
            {
                index = 0,
                mute = true,
            };

            await amppControl.SendAmppControlMessageAsync(myWorkloadId, "AudioMixer", "channelstate", JObject.FromObject(invalidPayload), reconKey);

            await Task.Delay(1000);
            Console.WriteLine("**************************");

            Console.WriteLine("Sending Requests to Update Channel By PushNotifications...Preferred Method");

            for (int i = 0; i < 10; i++)
            {
                var setLevelPayload = new
                {
                    index = i + 1,
                    level = i * 10,
                };

                await amppControl.PushAmppControlMessageAsync(myWorkloadId, "AudioMixer", "channelstate", JObject.FromObject(setLevelPayload), reconKey);
            }

            await Task.Delay(2000);
            Console.WriteLine("**************************");

            Console.WriteLine("Press Enter to Exit...");
            Console.ReadLine();
        }

        private static void AmppControl_OnAmppControlErrorEvent(object? sender, AmppControl.Model.AmppControlErrorEventArgs e)
        {
            // Only report errors that have originated from our requests
            if (e.Key == reconKey)
            {
                Console.WriteLine("************Error Notification**************");
                Console.WriteLine($"Workload:\t{e.Workload}");
                Console.WriteLine($"Command:\t{e.Command}");
                Console.WriteLine($"Error:\t{e.Error}");
                Console.WriteLine($"Details:\t{e.Details}");
                Console.WriteLine("**************************************");
            }

        }

        private static void AmppControl_OnAmppControlNotifyEvent(object? sender, AmppControl.Model.AmppControlNotificationEventArgs e)
        {
            Console.WriteLine("************Notification**************");

            // If the Key matches our ReconKey then this message is in response to something we have sent
            if (e.Notification.Key == reconKey)
            {
                Console.WriteLine("Application Status Update From This:");
            }
            // Otherwise it is in response to another app, or a user interacting from the UI
            else
            {
                Console.WriteLine("Application Status Update From Other: " + e.Notification.Key);
            }

            Console.WriteLine($"Workload:\t{e.Workload}");
            Console.WriteLine($"Command:\t{e.Command}");
            Console.WriteLine($"Payload:\t{JsonConvert.SerializeObject(e.Notification.Payload)}");
            Console.WriteLine("**************************************");
        }
    }
}
