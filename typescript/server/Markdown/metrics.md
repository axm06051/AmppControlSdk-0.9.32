# metrics

Gets the metrics of the workload.

Expect notifications on the topic:

- gv.ampp.control.{workload_id}.metrics.notify

## Parameters

None.

## Example Payload

```
{
}
```

## Example Response

```
- gv.ampp.control.{workload_id}.metrics.notify
{
  "offset": 81,
  "gmid": "47687dbc-d13d-49d7-a2f2-e515df00d00d",
  "source": "Unlocked",
  "locked": true,
  "kernel_driver": false
}
```
