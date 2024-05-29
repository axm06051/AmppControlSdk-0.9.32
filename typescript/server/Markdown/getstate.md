# getstate

Gets the entire state of the application

Expect notifications on the following topics:

- gv.ampp.control.{workload_id}.channelstate.notify
- gv.ampp.control.{workload_id}.config.notify

## Parameters

None


## Example Response

```
- gv.ampp.control.{workload_id}.channelstate.notify
[
  {
    "index": 1,
    "volume": 86,
    "label": "Channel:1",
    "active": false
  },
  {
    "index": 2,
    "volume": 76,
    "label": "Channel:2",
    "active": false
  },
  {
    "index": 3,
    "volume": 94,
    "label": "Channel:3",
    "active": false
  },
  {
    "index": 4,
    "volume": 90,
    "label": "Channel:4",
    "active": false
  }
]
- gv.ampp.control.{workload_id}.config.notify
{
	"connectionString" : "foo://demo",
	"port" : 80085
}

```