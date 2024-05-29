import { randomUUID } from "crypto";
import EventEmitter from "events";
import { GVPlatform } from "./GVPlatform";
import { IPlatformNotification, NotificationEvent } from "./Model";

interface MailboxRequest {
    id: string;
    subscription: string;
    durable: boolean;
    maximumLength: number;
    mailboxTTL: number;
  }


interface MailboxResponse {
    id: string;
    subscription: string;
    durable: boolean;
    maximumLength: number;
    mailboxTTL: number;
    secret: string;
  }

  interface MailboxMessage {
    id: string;
    time: string;
    topic: string;
    source: string;
    correlationId: string;
    userOrClientId: string;
    content: string;
}

/**
 * Class for Getting Platform Notifications
 */
export class GVMailbox extends EventEmitter  implements NotificationEvent{

    private gvPlatform : GVPlatform;
    private secret : string;
    private mailboxId : string;

    /**
     * constructor
     */
    constructor(platform : GVPlatform) {
        super();
        this.gvPlatform = platform;
        this.secret = null;
        this.mailboxId = null;

    }

    public OnNotification (notification: IPlatformNotification) {
        this.emit('notification', notification);
    };

    /**
     * Subscribe to a notification
     * @param topic - Topic to subscribe to
     * @returns true/false
     */
    public async Subscribe(topic : string) : Promise<boolean> {

        if(!this.mailboxId) {
            await this.createMailbox();
        }

        if(!this.mailboxId) {
            throw new Error(`Error creating mailbox`);
        }

        const subscription = encodeURIComponent(topic);

        console.log(`Subscribing to: ${subscription}`)

        const url = `/notifications/api/v1/mailbox/${this.mailboxId}/subscribe/${subscription}`;

        const response = await this.gvPlatform.post(url, null);

        const ok = response.status === 204 || response.status === 200;

        console.log('subscription result: ' + ok);

        return ok;
    }

    /**
     * Create a new mailbox
     */
    private async createMailbox() : Promise<string> {

        // If we already have a mailbox, delete id
        if(this.secret)  {
            this.deleteMailbox();
        }

        const mailboxId = `ts-ampp-sdk-sampl--${randomUUID()}`

        const url = `/notifications/api/v1/mailbox`;

        let mailboxRequest : MailboxRequest = {
            durable : false,
            id : mailboxId,
            mailboxTTL : 60*2*1000,
            maximumLength : 1000,
            subscription : "gv",
        }

        const response = await this.gvPlatform.post(url, mailboxRequest);
        if(response.status === 200) {
            const mailboxResponse : MailboxResponse =  response.data;
            this.secret = mailboxResponse.secret;
            this.mailboxId = mailboxResponse.id;
            return mailboxResponse.id;
        }


        console.log(response.status)
        return null;

    }

    /**
     * 
     * @returns Delete an existing mailbox
     */
    private async deleteMailbox() : Promise<boolean> {

        const url = `/notifications/api/v1/mailbox/${this.mailboxId}/${this.secret}`;

        const result = await this.gvPlatform.delete(url)

        this.mailboxId = null;
        this.secret = null

        return result.status === 204
    }

    /**
     * Get Messages in Mailbox
     * @returns array of messages
     */
    private async getMailboxMessages() : Promise<MailboxMessage[]> {
        const url = `/notifications/api/v1/notifications/${this.mailboxId}?count=100&timeout=1000`;

        const result = await this.gvPlatform.get(url);

        if(result.status === 200) {
            const messages : MailboxMessage[] = result.data;
            return messages;
        }
        return []
    }

    /**
     * Start polling mailbox for messages
     * @returns 
     */
    public StartNotificationsListener() : boolean {

        if(!this.mailboxId) {
            // Mailbox hasn't been created
            return false;
        }

        setTimeout( () => {this.mailboxPoll()}, 1000);
        return true;
    }

    /**
     * Stop the Polling thread and delete the mailbox
     * @returns 
     */
    public async StopNotificationsListener() : Promise<boolean> {
        if(!this.mailboxId) {
            // Mailbox hasn't been created
            return false;
        }

        await this.deleteMailbox();
        return true;
    }

    /**
     * Method that periodically polls the mailbox for messages
     * @returns 
     */
    private async mailboxPoll() {
        if(!this.mailboxId) {
            // Mailbox hasn't been created or has been deleted
            return ;
        }

        const messages = await this.getMailboxMessages();

        messages.forEach((m) => {
            this.OnNotification( {
                account : m.userOrClientId,
                content : m.content,
                correlationId : m.correlationId,
                source : m.source,
                time :  new Date(m.time),
                topic : m.topic,
                ttl : 1000,
            })
        })

        setTimeout( () => {this.mailboxPoll()}, 1000);
    }
}