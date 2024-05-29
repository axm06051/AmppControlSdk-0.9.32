using AmppControl.Model;
using gv.ampp.control.demo.app.Model;
using Gv.Ampp.Control.Sdk;
using Gv.Ampp.Control.Sdk.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gv.ampp.control.demo.app
{
    internal class DemoApplication
    {
        private readonly AmppControlService amppControlService;
        private readonly DemoConfiguration configuration;
        

        private DemoState[] applicationState = new DemoState[10];


        /// <summary>
        /// Schema for settting some config
        /// </summary>
        private const string ConfigSchema = @"
        {
          'title': 'Updates Configuration',
          'type': 'object',
          'properties': {
            'ConnectionString': {
              'type': 'string',
              'title': 'Connection String'
            },
            'Port': {
              'type': 'integer',
              'title': 'Port Number',
              'minimum' : 1000,
              'maximum' : 65535
            }
          }
        }";

        /// <summary>
        /// Example Schema for settting State
        /// </summary>
        private const string StateSchema = @"
        {
          'title': 'Updates State',
          'type': 'object',
          'properties': {
            'Index': {
              'type': 'integer',
              'title': 'Channel Index',
              'minimum' : 1,
              'maximum' : 10
            },
            'Volume': {
              'type': 'integer',
              'title': 'Volume',
              'minimum' : 0,
              'maximum' : 100
            },
            'Active': {
              'type': 'boolean',
              'title': 'Active'
            },
            'Label': {
              'type': 'string',
              'title': 'Label'
            }
          }
        }";

        /// <summary>
        /// Schema for metrics
        /// </summary>
        private const string MetricsSchema = @"{
          'type': 'object',
          'properties': {
            'offset': {
              'type': 'integer',
              'description': 'Offset from master',
              'unit': 'us',
              'error_criteria': '> 15',
              'warn_criteria': '> 10',
              'minimum': 0,
              'maximum': 100
            },
            'gmid': {
              'type': 'string',
              'description': 'Grand Master ID'
            },
            'source': {
              'type': 'string',
              'enum': [
                'GPS',
                'Boundary Clock',
                'Unlocked'
              ],
              'description': 'Time Source',
              'error_criteria': '== Unlocked'
            },
            'locked': {
              'type': 'boolean',
              'description': 'PTP Locked',
              'error_criteria': '== false'
            },
            'kernel_driver': {
              'type': 'boolean',
              'description': 'Kernel Driver Mode',
              'error_criteria': '== false'
            }
          }
        }";

        public DemoApplication(DemoConfiguration configuration, AmppControlService amppControlService)
        {
            this.amppControlService = amppControlService;
            this.configuration = configuration;

            for(int i = 0; i < 10; i++)
            {
                this.applicationState[i] = new DemoState()
                {
                    Active = true,
                    Index = i + 1,
                    Label = $"Channel:{i + 1}",
                    Volume = 0,
                };
            }
        }


        /// <summary>
        /// All Apps Must have a getstate method that dumps out the entire state of the application
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="reconKey"></param>
        /// <returns></returns>
        [AMPPCommand("getstate", Schema = "{}", Version = "1.0", Markdown = "Markdown.getstate.md")]
        public async Task GetStateAsync(JObject payload, string reconKey)
        {
            Console.WriteLine("getstate");

            // Send Current Config
            await amppControlService.PushAmppControlMessageAsync("config", JObject.FromObject(configuration), reconKey);

            var appState = new List<JObject>();

            for (int i = 0; i < 10; i++)
            {
                var demoState = this.applicationState[i];

                appState.Add(JObject.FromObject(demoState));
            }

            await this.amppControlService.PushAmppControlMessageAsync("channelstate", JArray.FromObject(appState), reconKey);
        }

        /// <summary>
        /// An example method that sets the application cofig
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="reconKey"></param>
        /// <returns></returns>
        [AMPPCommand("config", Schema = ConfigSchema, Version = "1.0", Markdown ="Markdown.config.md")]
        public async Task ConfigureAsync(JObject payload, string reconKey)
        {
            Console.WriteLine("ConfigureAsync()");
            Console.WriteLine(JsonConvert.SerializeObject(payload, Formatting.Indented));

            var newConfig = payload.ToObject<DemoConfiguration>();

            // Allow for incomplete configuration
            // I.e. if only the port is set do not blank out the config string
            if (!string.IsNullOrEmpty(newConfig.ConnectionString))
            {
                configuration.ConnectionString = newConfig.ConnectionString;
            }

            if (newConfig.Port != 0)
            {
                configuration.Port = newConfig.Port;
            }

            // Save the Config back to Config service
            await amppControlService.SaveConfigurationAsync(JsonConvert.SerializeObject(configuration));

            // Inform all clients that the configuration has changed.
            await amppControlService.PushAmppControlMessageAsync("config", JObject.FromObject(configuration), reconKey);

        }

        /// <summary>
        /// An example method that updates the channel state of an application
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="reconKey"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        [AMPPCommand("channelstate", Schema = StateSchema, Version = "1.0", Markdown = "Markdown.channelstate.md")]
        public async Task SetChannelState(JObject payload, string reconKey)
        {
            Console.WriteLine("SetChannelState()");
            Console.WriteLine(JsonConvert.SerializeObject(payload, Formatting.Indented));

            // Do some work 
            var demoState = payload.ToObject<DemoState>();

            if(demoState.Index < 1 || demoState.Index > 10)
            {
                throw new IndexOutOfRangeException($"Invalid Channel Index {demoState.Index}");
            }

            // All parameters should be optional, so we can map a single button press to a single property
            if(!string.IsNullOrEmpty(demoState.Label))
            {
                this.applicationState[demoState.Index-1].Label = demoState.Label;
            }

            if (demoState.Volume.HasValue)
            {
                this.applicationState[demoState.Index - 1].Volume = demoState.Volume;
            }

            if (demoState.Active.HasValue)
            {
                this.applicationState[demoState.Index - 1].Active = demoState.Active;
            }

            // Inform all clients channelstate has changed
            // Pass the reconKey back so clients can see where the update originated from
            await amppControlService.PushAmppControlMessageAsync("channelstate", JObject.FromObject(this.applicationState[demoState.Index - 1]), reconKey);
        }

        /// <summary>
        /// Triggers the application to send its metrics.
        /// If not present then the default is used
        /// </summary>
        /// <param name="key">recon key</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AMPPCommand("metrics", Schema = "{}", Version = "1.0", Markdown = "metrics.md")]
        public async Task GetMetrics(string key)
        {
            // If you want to include the default netrics then call GetMetrics and extend the object
            // var payload = this.amppControlService.GetMetrics();
            var payload = new JObject();
            payload["offset"] = new Random().Next(0, 100);
            payload["gmid"] = this.amppControlService.WorkloadId;
            payload["source"] = "Unlocked";
            payload["locked"] = true;
            payload["kernel_driver"] = false;

            await this.amppControlService.PushAmppControlMessageAsync("metrics", payload, key);
        }

        /// <summary>
        /// This method is called (by reflection) in response to a metrics-schema ampp control request
        /// If not present the the default metrics schema will be returned
        /// </summary>
        /// <returns></returns>
        public string GetMetricsSchema()
        {
            return MetricsSchema;
        }


    }
}
