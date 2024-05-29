# channelstate

Sets the channelstate of the application

Expect notifications on the following topics:

- gv.ampp.control.{workload_id}.channelstate.notify

## Parameters


## Example Payload

### Set channel 1 active
```
{
  "index": 1,
  "active": true
}
```

## Example Response

```
- gv.ampp.control.{workload_id}.channelstate.notify
{
  "index": 1,
  "volume": 0,
  "label": "Channel:1",
  "active": true
}

```