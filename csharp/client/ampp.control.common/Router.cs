using ampp.control.common.Model;
using AmppControl.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ampp.control.common
{
    /// <summary>
    /// Class for Making Router Commands
    /// </summary>
    public class Router
    {
        // The connection to GV Platform
        private GVPlatform gvPlatform = null;

        // The Mailbox for receiving RouteMade notifications
        private GVMailbox mailbox;

        // Internal cache of data
        private List<Salvo> salvos = null;
        private List<FabricData> fabrics = null;
        private Dictionary<string, List<ProducerData>> producerCache = new Dictionary<string, List<ProducerData>>();
        private Dictionary<string, List<ConsumerData>> consumerCache = new Dictionary<string, List<ConsumerData>>();

        /// <summary>
        /// Event called when a route change event is received
        /// </summary>
        public event EventHandler<RouteChangedEvent> OnRouteChangedEvent;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseURL"></param>
        /// <param name="apiKey"></param>
        public Router(string baseURL, string apiKey)
        {
            gvPlatform = new GVPlatform(baseURL, apiKey);
            mailbox = new GVMailbox(gvPlatform);
        }

        /// <summary>
        /// Initialise the connection to GV Platform
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoginAsync()
        {
            var result = await gvPlatform.LoginAsync();
            return result;

        }

        /// <summary>
        /// Start the thread that polls the mailbox for routemade notifications
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StartListeningForRouteEvents()
        {
            if (gvPlatform == null || this.mailbox == null)
            {
                return false;
            }

            // Subscribe to RouteMade Notifications
            await this.mailbox.Subscribe("gv.cluster.matrix.*.routemade");
            this.mailbox.OnPlatformNotification += Mailbox_OnPlatformNotification;
            return this.mailbox.StartNotificationsListener();
        }

        /// <summary>
        /// Stop the thread that polls for routemade notifications and destroy the mailbox
        /// </summary>
        /// <returns></returns>
        public async Task StopListeningForRouteEvents()
        {
           await this.mailbox.StopNotificationsListener();
        }

        /// <summary>
        /// Get a List of all the Fabrics
        /// </summary>
        /// <returns></returns>
        public async Task<IList<FabricData>> GetFabrics()
        {
            if (this.fabrics != null)
            {
                return this.fabrics.ToArray();
            }

            var response = await this.gvPlatform.Get("/cluster/fabric/api/v1/fabrics");

            if (response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<FabricsResponse>(body);
                return data.Fabrics;
            }

            return null;
        }

        /// <summary>
        /// Get a List of all the Router Salvos
        /// </summary>
        /// <returns></returns>
        public async Task<IList<Salvo>> GetSalvos()
        {
            if (this.salvos != null)
            {
                return this.salvos.ToArray();
            }

            var response = await this.gvPlatform.Get("/cluster/matrix/api/v1/salvos");

            if (response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<IList<Salvo>>(body);
                this.salvos = data.ToList();
                return this.salvos;
            }

            return null;
        }

        /// <summary>
        /// Execute a Router Salvo
        /// </summary>
        /// <param name="salvoName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool> ExecuteSalvo(string salvoName)
        {
            var salvos = await this.GetSalvos();

            var salvo = salvos.FirstOrDefault(s => s.Name == salvoName);

            if (salvo == null)
            {
                throw new InvalidOperationException($"Salvo {salvo} not found");
            }

            var url = $"/cluster/matrix/api/v1/salvo/{salvo.Id}/execute";

            var response = await this.gvPlatform.Post(url, null);

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Cancel a Router Salvo
        /// </summary>
        /// <param name="salvoName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool> CancelSalvo(string salvoName)
        {
            var salvos = await this.GetSalvos();

            var salvo = salvos.FirstOrDefault(s => s.Name == salvoName);

            if (salvo == null)
            {
                throw new InvalidOperationException($"Salvo {salvo} not found");
            }

            var url = $"/cluster/matrix/api/v1/salvo/{salvo.Id}/cancel";

            var response = await this.gvPlatform.Post(url, null);

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get a List of all producers(sources) on a Fabric
        /// </summary>
        /// <param name="fabricId"></param>
        /// <returns></returns>
        public async Task<IList<ProducerData>> GetProducers(string fabricId)
        {
            if (this.producerCache.ContainsKey(fabricId))
            {
                return this.producerCache[fabricId];
            }

            var response = await this.gvPlatform.Get($"/cluster/matrix/api/v2/producers?fabricId={fabricId}&type=Fabric");

            if (response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<ProducerResponse>(body);
                return data.Producers;
            }

            return null;
        }

        /// <summary>
        /// Get a list of all Consumers(Destinations) on a Fabric
        /// </summary>
        /// <param name="fabricId"></param>
        /// <returns></returns>
        public async Task<IList<ConsumerData>> GetConsumers(string fabricId)
        {
            if (this.consumerCache.ContainsKey(fabricId))
            {
                return this.consumerCache[fabricId];
            }

            var response = await this.gvPlatform.Get($"/cluster/matrix/api/v1/consumers?fabricId={fabricId}&type=Fabric");

            if (response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<ConsumerResponse>(body);
                return data.Consumers;
            }

            return null;
        }


        /// <summary>
        /// Make a Route
        /// </summary>
        /// <param name="fabricId"></param>
        /// <param name="srcName"></param>
        /// <param name="dstName"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public async Task<string> MakeRoute(string fabricId, string srcName, string dstName)
        {
            string producerId;
            string consumerId;

            var producers = await this.GetProducers(fabricId);
            var consumers = await this.GetConsumers(fabricId);

            var producer = producers.FirstOrDefault(p => p.Producer.Name == srcName);
            if (producer == null)
            {
                throw new System.Exception($"Producer with name '{srcName}' not found");
            }

            var consumer = consumers.FirstOrDefault(c => c.Consumer.Name == dstName);
            if (consumer == null)
            {
                throw new System.Exception($"Consumer with name '{dstName}' not found");
            }

            producerId = producer.Producer.Id;
            consumerId = consumer.Consumer.Id;

            if (string.IsNullOrEmpty(producerId) || string.IsNullOrEmpty(consumerId))
            {
                throw new System.Exception("Source or Destination not found. Please check the name and try again.");
            }

            var requestBody = new
            {
                sourceId = producerId,
                destinationId = consumerId
            };

            var response = await gvPlatform.Post("/cluster/matrix/api/v1/routing/requestroute", requestBody);

            if (response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<RouteStatusResponse>(body);
                return data.RequestId;
            }

            return null;
        }

        /// <summary>
        /// Check the status of a route request
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public async Task<RouteStatusData> CheckRouteRequest(string requestId)
        {
            var url = $"cluster/matrix/api/v1/routing/routestatus/{requestId}";
            var response = await gvPlatform.Get(url);

            if (response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<RouteStatusData>(body);
                return data;
            }


            return null;
        }

        /// <summary>
        /// Set the alias of a Producer
        /// </summary>
        /// <param name="fabricId"></param>
        /// <param name="srcName"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public async Task<bool> SetProducerAlias(string fabricId, string srcName, string alias)
        {
            var producers = await this.GetProducers(fabricId);


            var producer = producers.FirstOrDefault(p => p.Producer.Name == srcName);
            if (producer == null)
            {
                throw new System.Exception($"Producer with name '{srcName}' not found");
            }

            var url = $"/cluster/matrix/api/v1/producer/{producer.Producer.Id}";

            var payload = new
            {
                alias = alias
            };

            var response = await gvPlatform.Put(url, payload);

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Set the alias of a Consumer
        /// </summary>
        /// <param name="fabricId"></param>
        /// <param name="dstName"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public async Task<bool> SetConsumerAlias(string fabricId, string dstName, string alias)
        {
            var consumers = await this.GetConsumers(fabricId);


            var consumer = consumers.FirstOrDefault(p => p.Consumer.Name == dstName);
            if (consumer == null)
            {
                throw new System.Exception($"Consumer with name '{dstName}' not found");
            }

            var url = $"/cluster/matrix/api/v1/consumer/{consumer.Consumer.Id}";

            var payload = new
            {
                alias = alias
            };

            var response = await gvPlatform.Put(url, payload);

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        private async Task OnNotification(PlatformNotification notification)
        {
            
            string[] parts = notification.Topic.Split('.');
            string fabricId = parts[3];

            RouteMadeEvent evt = JsonConvert.DeserializeObject<RouteMadeEvent>(notification.Content);
            string srcName = await this.GetProducerName(fabricId, evt.SourceId);
            string dstName = await this.GetConsumerName(fabricId, evt.DestinationId);

            // Fire a RouteChanged event
            this.OnRouteChangedEvent?.Invoke(this, 
                new RouteChangedEvent
                {
                    FabricId = fabricId,
                    SourceName = srcName,
                    DestinationName = dstName
                });
        }

        private async Task<string> GetProducerName(string fabricId, string sourceId)
        {
            var producers = await GetProducers(fabricId);
            if (producers == null) return null;

            var producer = producers.FirstOrDefault(p => p.Producer.Id == sourceId);
            if (producer == null) return null;

            return producer.Producer.Name;
        }

        private async Task<string> GetConsumerName(string fabricId, string destinationId)
        {
            var consumers = await GetConsumers(fabricId);
            if (consumers == null) return null;

            var consumer = consumers.FirstOrDefault(c => c.Consumer.Id == destinationId);
            return consumer?.Consumer.Name;
        }

        /// <summary>
        /// Event handler called when a notification is received on the mailbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mailbox_OnPlatformNotification(object sender, PlatformNotification e)
        {
            if (e.Topic.EndsWith("routemade"))
            {
                _ = this.OnNotification(e);
            }
        }

    }

    /// <summary>
    /// Definition of a RouteChangedEvent
    /// </summary>
    public class RouteChangedEvent
    {
        public string FabricId { get; set; }
        public string SourceName { get; set; }

        public string DestinationName { get; set; }
    }

    /// <summary>
    /// Definition of a RouteMadeEvent
    /// </summary>
    public class RouteMadeEvent
    {
        public string RequestId { get; set; }
        public string SourceId { get; set; }

        public string DestinationId { get; set; }
    }
}
