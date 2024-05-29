# config

Sets the configuration of the Demo Service

Expect notifications on the following topics:

- gv.ampp.control.{workload_id}.config.notify

## Parameters

- ConnectionString : The ConnectionString
- Port : Port Number

## Example Payload

```
{
	"ConnectionString" : "foo://demo",
	"Port" : 80085
}
```

## Example Response

```
- gv.ampp.control.{workload_id}.config.notify
{
	"ConnectionString" : "foo://demo",
	"Port" : 80085
}

```