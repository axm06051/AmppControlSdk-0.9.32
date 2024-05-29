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
    "Index": 1,
    "Volume": 86,
    "Label": "Channel:1",
    "Active": false
  },
  {
    "Index": 2,
    "Volume": 76,
    "Label": "Channel:2",
    "Active": false
  },
  {
    "Index": 3,
    "Volume": 94,
    "Label": "Channel:3",
    "Active": false
  },
  {
    "Index": 4,
    "Volume": 90,
    "Label": "Channel:4",
    "Active": false
  },
  {
    "Index": 5,
    "Volume": 76,
    "Label": "Channel:5",
    "Active": false
  },
  {
    "Index": 6,
    "Volume": 0,
    "Label": "Channel:6",
    "Active": true
  },
  {
    "Index": 7,
    "Volume": 0,
    "Label": "Channel:7",
    "Active": true
  },
  {
    "Index": 8,
    "Volume": 0,
    "Label": "Channel:8",
    "Active": true
  },
  {
    "Index": 9,
    "Volume": 0,
    "Label": "Channel:9",
    "Active": true
  },
  {
    "Index": 10,
    "Volume": 0,
    "Label": "Channel:10",
    "Active": true
  }
]
- gv.ampp.control.{workload_id}.config.notify
{
	"ConnectionString" : "foo://demo",
	"Port" : 80085
}

```