export interface IPlatformNotification {
  account: string;
  time: Date;
  topic: string;
  content: any;
  source: string;
  correlationId: string;
  ttl: number;
  binaryContent?: string;
}

export interface NotificationEvent {
  OnNotification: (notification: IPlatformNotification) => void;
}

export interface RouterEvents {
  RouteChangedEvent: (fabricId: string, src: string, dst: string) => void;
}

export interface IAmppControlNotification {
  topic: string;
  payload: any;
  reconKey: string;
}

export interface IAmppControlError {
  topic: string;
  reconKey: string;
  status: number;
  error: string;
  details: string;
}

export interface IAmppControlMacro {
  uuid: string;
  name: string;
  description: string;
  commands: [];
}

export interface Producer {
  producer: {
    id: string;
    name: string;
    alias: string;
    workloadId: string;
    nodeId: string;
    fabricId: string;
    groupName: string | null;
    type: string;
    stream: {
      streamId: string;
      flows: Array<{
        frameAge: number;
        maxFrameAge: number;
        minFrameAge: number;
        flowId: string;
        dataType: string;
        descriptor: {
          aspectRatio: {
            den: number;
            num: number;
          };
          colorSpace: string;
          height: number;
          pixelLayout: string;
          progressive: boolean;
          rate: {
            den: number;
            num: number;
          };
          transferCharacteristic: string;
          width: number;
        };
      }>;
    };
  };
}

export interface Consumer {
  consumer: {
    id: string;
    name: string;
    alias: string;
    workloadId: string;
    nodeId: string;
    fabricId: string;
    groupName: string | null;
    type: string;
    tallyState: string;
    locked: boolean;
    flags: string;
    enabled: boolean;
  };
}

export interface RouteStatus {
  routeStatus: string;
  routeErrorMessage: string;
}

export interface Fabric {
  fabric: {
    id: string;
    name: string;
    watermarking: boolean;
    type: string;
    nodes: Node[];
  };
  eTag: string;
}

export interface Node {
  id: string;
  name: string;
}

export interface RouteMadeEvent {
  requestId: string;
  sourceId: string;
  destinationId: string;
}
