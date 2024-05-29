# channelstate

Sets the channelstate of the application

Expect notifications on the following topics:

- gv.ampp.control.{workload_id}.channelstate.notify

## Parameters


## Example Payload

### Set channel 1 active
```
{
  "Index": 1,
  "Active": true
}
```

## Example Response

```
- gv.ampp.control.{workload_id}.channelstate.notify
{
  "Index": 1,
  "Volume": 0,
  "Label": "Channel:1",
  "Active": true
}

```