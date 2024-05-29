import EventEmitter from "events";
import { Consumer, Fabric, IPlatformNotification, Producer, RouteMadeEvent, RouterEvents, RouteStatus } from "./Model";
import { GVPlatform } from "./GVPlatform";
import { GVMailbox } from "./GVMailbox";



interface Salvo {
    id: string;
    name: string;
    fabricId: string;
}

/**
 * Class for Handling Route Requests
 */
export class Router extends EventEmitter implements RouterEvents {

    private salvos: Salvo[] | null = null;
    private gvPlatform : GVPlatform | null;
    private producerCache = new Map<string, Producer[]>();
    private consumerCache = new Map<string, Consumer[]>();
    private mailbox : GVMailbox;
   
    /**
     * constructor
     * @param baseURL url for accessing GV Platform
     * @param apiKey The API Key (must have platform and cluster.readonly scopes) 
     */
    constructor(baseURL: string, apiKey: string) {
        super();
        this.gvPlatform = new GVPlatform(baseURL, apiKey);
        
        // This is the connection for listening for RouteMade notifications
        this.mailbox = new GVMailbox(this.gvPlatform);
    }


    /**
     * login
     * connects and authenticates with GVPlatform
     */
    public async login(): Promise < boolean > {
        return this.gvPlatform.login()
    }


    /**
     * Request a Route
     * @param fabricId 
     * @param srcName 
     * @param dstName 
     * @returns an Id
     */
    public async MakeRoute(fabricId: string, srcName: string, dstName: string) : Promise<string> {
        let producerId: string;
        let consumerId: string;

        const producers = await this.GetProducers(fabricId);
        const consumers = await this.GetConsumers(fabricId);
    
        const producer = producers.find(p => p.producer.name === srcName);
        if (!producer) {
            throw new Error(`Producer with name '${srcName}' not found`);
        }
    
        const consumer = consumers.find(c => c.consumer.name === dstName);
        if (!consumer) {
            throw new Error(`Producer with name '${srcName}' not found`);
        }

        producerId = producer.producer.id;
        consumerId = consumer.consumer.id;

        if (!producerId || !consumerId) {
            throw new Error(
                'Source or Destination not found. Please check the name and try again.'
            );
        }

        const requestBody = {
            sourceId : producerId,
            destinationId : consumerId,
        };

      const response = await this.gvPlatform.post('/cluster/matrix/api/v1/routing/requestroute', requestBody);
      return response.data.requestId;      
    }

    /**
     * Get the status of a route request
     * @param requestId Id returned from MakeRoute
     * @returns 
     */
    public async CheckRouteRequest(requestId: string) : Promise<RouteStatus> {
        const url = `cluster/matrix/api/v1/routing/routestatus/${requestId}`;
        const response = await this.gvPlatform.get(url)
        return response.data;
    }


    /**
     * Set the Alias for a Producer
     * @param fabricId 
     * @param srcName 
     * @param alias 
     * @returns 
     */
    public async SetProducerAlias(fabricId: string, srcName: string, alias: string)  : Promise<boolean> {

        const producers = await this.GetProducers(fabricId);

        const producer = producers.find(p => p.producer.name === srcName);
        if (!producer) {
            throw new Error(`Producer with name '${srcName}' not found`);
        }

        const url = `/cluster/matrix/api/v1/producer/${producer.producer.id}`;

        const payload = {
            alias,
        }

        const response = await this.gvPlatform.put(url, payload)

        return response.status === 204 || response.status === 200;
    }

    /**
     * Set the Router Alias for a Consumer
     * @param fabricId 
     * @param dstName 
     * @param alias 
     * @returns 
     */
    public async SetConsumerAlias(fabricId: string, dstName: string, alias: string) {
        const consumers = await this.GetConsumers(fabricId);

        const consumer = consumers.find(p => p.consumer.name === dstName);
        if (!consumer) {
            throw new Error(`Consumer with name '${dstName}' not found`);
        }

        const url = `cluster/matrix/api/v1/consumer/${consumer.consumer.id}`;

        const payload = {
            alias,
        }

        const response = await this.gvPlatform.put(url, payload)

        return response.status === 200;
    }

    /**
     * Execute a Salvo
     * @param salvoName 
     * @returns 
     */
    public async ExecuteSalvo(salvoName: string) : Promise<boolean>{

        const salvos = await this.GetSalvos();
        const salvo = salvos.find(s => s.name === salvoName);
        if (!salvo) {
            throw new Error(`Salvo not found: ${salvoName}`);
        }

        const url = `/cluster/matrix/api/v1/salvo/${salvo.id}/execute`

        // Execute the salvo
        const res = await this.gvPlatform.post(url, null)

        return res.status === 200
    }

    /**
     * Cancel a Salvo
     * @param salvoName 
     * @returns 
     */
    public async CancelSalvo(salvoName: string) : Promise<boolean>{

        const salvos = await this.GetSalvos();
        const salvo = salvos.find(s => s.name === salvoName);
        if (!salvo) {
            throw new Error(`Salvo not found: ${salvoName}`);
        }

        const url = `/cluster/matrix/api/v1/salvo/${salvo.id}/cancel`

        // Execute the salvo
        const res = await this.gvPlatform.post(url, null)

        return res.status === 200
    }

    public RouteChangedEvent(fabricId: string, src: string, dst: string) {
        this.emit('RouteChangedEvent', fabricId, src, dst);
    }

    /**
     * Get all Salvos
     * @returns Array of Salvos
     */
    public async GetSalvos() : Promise<Salvo[]> {
        if (this.salvos) {
            return this.salvos;
        }

        const response = await this.gvPlatform.get('/cluster/matrix/api/v1/salvos');
        this.salvos = response.data as Salvo[];
        return this.salvos;
    }

    /**
     * Get All Producers on a Fabric
     * @param fabricId 
     * @returns Array of Producers
     */
    public GetProducers = async (fabricId: string)  : Promise<Producer[]> => {
        if (this.producerCache.has(fabricId)) {
          return this.producerCache.get(fabricId);
        }
      
        const response = await this.gvPlatform.get(`/cluster/matrix/api/v2/producers?fabricId=${fabricId}&type=Fabric`) ;

        const producers : Producer[]= response.data.producers;
      
        this.producerCache.set(fabricId, producers);
        return producers;
      };
      
      /**
       * Get All Consumers on a Fabric
       * @param fabricId 
       * @returns Array of Consumers
       */
      public GetConsumers = async (fabricId: string) : Promise<Consumer[]> => {
        if (this.consumerCache.has(fabricId)) {
          return this.consumerCache.get(fabricId);
        }
      
        const response = await this.gvPlatform.get(`/cluster/matrix/api/v1/consumers?fabricId=${fabricId}&type=Fabric`);
        const consumers : Consumer[] = response.data.consumers;
      
        this.consumerCache.set(fabricId, consumers);
        return consumers;
      };

      /**
       * Get all fabrics
       * @returns Array of Fabrics
       */
      public GetFabrics = async () : Promise<Fabric[]> => {
        const response = await this.gvPlatform.get(`/cluster/fabric/api/v1/fabrics`);
        const fabrics : Fabric[] = response.data.fabrics;
      
        return fabrics;
      };

      /**
       * Start Listening for RouteMade notifications
       */
      async StartListeningForRouteEvents() {

        this.mailbox.OnNotification = this.OnNotification.bind(this);

        // Subscribe to RouteMade Notifications
        await this.mailbox.Subscribe("gv.cluster.matrix.*.routemade");

        // Start the thread that polls for notifications
        await this.mailbox.StartNotificationsListener();

      }

      /**
       * Stop Listening for RouteEvents. Delete the Mailbox
       */
      async StopListeningForRouteEvents() {
        await this.mailbox.StopNotificationsListener();
        
      }

      /**
       * Notification received from Mailbox
       * @param notification 
       */
      private async OnNotification(notification: IPlatformNotification) {

        const parts = notification.topic.split('.');
        const fabricId = parts[3];

        const event : RouteMadeEvent = JSON.parse(notification.content);
        const srcName = await this.getProducerName(fabricId, event.sourceId)
        const dstName = await this.getConsumerName(fabricId, event.destinationId);

        // Fire a RouteChanged event
        this.RouteChangedEvent(fabricId, srcName, dstName);
      }

     private async getProducerName(fabricId: string, sourceId: string): Promise<string | undefined> {
        const producers = await this.GetProducers(fabricId);
        if (!producers) return undefined;
      
        const producer = producers.find(p => p.producer.id === sourceId);
        if (!producer) return undefined;
      
        return producer.producer.name;
      }

      private async getConsumerName(fabricId: string, destinationId: string): Promise<string | undefined> {
        const consumers = await this.GetConsumers(fabricId);
        if (!consumers) return undefined;
      
        const consumer = consumers.find(c => c.consumer.id === destinationId);
        return consumer ? consumer.consumer.name : undefined;
      }
      
}
