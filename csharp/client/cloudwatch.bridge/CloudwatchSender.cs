using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using AmppControl.Model;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cloudwatch.bridge
{
    internal class CloudwatchSender
    {
        private string logGroup;
        private string metricsNamespace;
        private IAmazonCloudWatch cwClient;
        private IAmazonCloudWatchLogs cwLogClient;
        private ConcurrentDictionary<string, string> tokens = new();


        public CloudwatchSender(string logGroup, string metricsNamespace)
        {
            this.logGroup = logGroup;
            this.metricsNamespace = metricsNamespace;
            cwClient = new AmazonCloudWatchClient();
            cwLogClient = new AmazonCloudWatchLogsClient();
        }

        public async Task Connect() { 

            try
            {
                var request = new CreateLogGroupRequest
                {
                    LogGroupName = logGroup
                };
                await cwLogClient.CreateLogGroupAsync(request);
            }
            catch (ResourceAlreadyExistsException)
            {}

            try
            {
                DescribeLogStreamsRequest req = new();
                req.LogGroupIdentifier = logGroup;
                var resp = await cwLogClient.DescribeLogStreamsAsync(req);
                foreach (var stream in resp.LogStreams)
                {
                    if (stream.UploadSequenceToken != null)
                    {
                        tokens[stream.LogStreamName] = stream.UploadSequenceToken;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get a list of logstreams from cloudwatch.");
            }
        }

        public async Task PublishMetrics(AmppControlNotification notif)
        {
            Log.Debug("Publish Metrics");

            var now = DateTime.UtcNow;

            PutMetricDataRequest req = new PutMetricDataRequest();
            req.Namespace = this.metricsNamespace;
            var data = new List<MetricDatum>();

            dynamic d = notif.Payload;
            var totalLogs = d.TotalLogs;
            if (totalLogs != null)
            {
                data.Add(new MetricDatum() { MetricName = "Total Logs", Value = totalLogs.Value, TimestampUtc = now });
            }

            var totalInfo = d.TotalInfo;
            if (totalInfo != null)
            {
                data.Add(new MetricDatum() { MetricName = "Total Info", Value = totalInfo, TimestampUtc = now });
            }

            var totalWarning = d.TotalWarning;
            if (totalWarning != null)
            {
                data.Add(new MetricDatum() { MetricName = "Total Warning", Value = totalWarning, TimestampUtc = now });
            }

            var totalError = d.TotalError;
            if (totalError != null)
            {
                data.Add(new MetricDatum() { MetricName = "Total Error", Value = totalError, TimestampUtc = now });
            }

            var totalFatal = d.TotalFatal;
            if (totalFatal != null)
            {
                data.Add(new MetricDatum() { MetricName = "Total Fatal", Value = totalFatal, TimestampUtc = now });
            }

            var totalDebug = d.TotalDebug;
            if (totalDebug != null)
            {
                data.Add(new MetricDatum() { MetricName = "Total Debug", Value = totalDebug, TimestampUtc = now });
            }

            var totalTrace = d.TotalTrace;
            if (totalTrace != null)
            {
                data.Add(new MetricDatum() { MetricName = "Total Trace", Value = totalTrace, TimestampUtc = now });
            }

            var OverallHealth = d.OverallHealth;
            if (OverallHealth != null)
            {
                data.Add(new MetricDatum() { MetricName = "Overall Health", Value = OverallHealth, TimestampUtc = now });
            }

            req.MetricData = data;
            await cwClient.PutMetricDataAsync(req);

        }


        public async Task PublishLogs(string command, AmppControlNotification notif)
        {
            Log.Debug("Publish logs for command " + command);

            var now = DateTime.UtcNow;

            if (!tokens.ContainsKey(command))
            {
                Log.Warning($"Token not found for command : {command}");

                var logStreamRequest = new CreateLogStreamRequest
                {
                    LogGroupName = this.logGroup,
                    LogStreamName = command
                };

                try
                {
                    await cwLogClient.CreateLogStreamAsync(logStreamRequest);
                } catch (ResourceAlreadyExistsException) 
                {
                    //ignore
                }
            }

            PutLogEventsRequest request = new PutLogEventsRequest();
            request.LogStreamName = command;
            request.LogGroupName = logGroup;
            if (tokens.ContainsKey(command))
            {
                request.SequenceToken = tokens[command];
            }
            request.LogEvents = new List<InputLogEvent>();
            request.LogEvents.Add(new InputLogEvent()
            {
                Message = notif.Payload.ToString(),
                Timestamp = now
            });

            try
            {
                var resp = await cwLogClient.PutLogEventsAsync(request);
                tokens[command] = resp.NextSequenceToken;
            } 
            catch (Exception e)
            {
                Log.Error($"PutEventsAsync failed. Command is {command}, token sent= {request.SequenceToken}.  Error {e.Message}");
            }
        }
    }
}
