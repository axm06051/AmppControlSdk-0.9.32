using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Gv.Ampp.Control.Sdk;
using AmppControl.Model;

namespace gv.ampp.control.demo.app
{
    public class Program
    {
        private static readonly AutoResetEvent _closing = new AutoResetEvent(false);


        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            ////////////////////////////////////////////////////
            // Environment Variable      | CommandLineParameter |
            // --------------------------|----------------------|
            // GVCLUSTER_WORKLOADNAME    | --WorkloadName       |
            // GVCLUSTER_PLATFORMURI     | --PlatformUri        |
            // GVCLUSTER_PLATFORMAPIKEY  | --ApiKey             |
            // --------------------------|----------------------|
            ///////////////////////////////////////////////////////

            // This class reads config from either environment variables, or  command line parameters
            // When running under node agent then will be injected as Environment variables

            AmppConfiguration amppConfiguration = new AmppConfiguration();

            string workloadName = amppConfiguration.GetworkloadName();
            string apiKey = amppConfiguration.GetAPIKey();
            string platformUrl = amppConfiguration.GetPlatformUrl();

            Console.WriteLine($"WorkloadName:\t\t{workloadName}");
            Console.WriteLine($"ApiKey:\t\t\t{apiKey}");
            Console.WriteLine($"PlatformUri:\t\t{platformUrl}");

            if (string.IsNullOrEmpty(platformUrl) || string.IsNullOrEmpty(apiKey))
            {
                Exit("WorkloadName, PlatformUri AND APIKey Must be provided");
            }

            // Read workloadId for this app from the application configuration
            // If one doesn't exists we create a new one and write it back to the appsettings.json file
            // so that we can reuse it
            string workloadId = amppConfiguration.GetWorkloadId(workloadName);
           
            if(workloadId == null)
            {
                Exit($"No WorkloadId for {workloadName}");
            }

            if (!Guid.TryParse(workloadId, out Guid result))
            {
                Exit($"WorkloadId {workloadId} is not a valid GUID");
            }

            Console.WriteLine($"WorkloadId:\t\t{workloadId}");

            Console.WriteLine();
            Console.Title = $"{workloadName} ({workloadId})";

           
            // Attempt to connect to GVPlatform
            Console.WriteLine("*******************************************");
            Console.WriteLine("Attempting to authenticate with platform...");

            // TODO: Enter your product code here
            // var productCode = "AE-A-MCUHDOUT16";
            // var productCode = "AE-A-MCUHDOUTX";
            string productCode = null;

            AmppControlService amppControl = new AmppControlService(platformUrl, apiKey, workloadName, productCode);
            if(!await amppControl.ConnectToGVPlatform())
            {
                Console.WriteLine("Error Connecting to GV Platform");
                Console.ReadLine();
                Environment.Exit(0);
            }

            Console.WriteLine("Connected Okay...");
            Console.WriteLine("*******************************************");

            if (!amppControl.IsLicensed())
            {
                Console.WriteLine("*******************************************");
                Console.WriteLine("Not Licensed");
                Console.WriteLine("*******************************************");
                Console.ReadLine();
                return;
            }

            amppControl.OnLicenseStatusChanged += AmppControl_OnLicenseStatusChanged;


            // Read Our Configuration from the Configuration Service.
            var config = await amppControl.GetConfigurationAsync();

            DemoConfiguration demoConfig = null;

            if(config == null)
            {
                demoConfig = new DemoConfiguration() { ConnectionString = "foo://abc/123", Port = 80085 };
                var jsonConfig = JsonConvert.SerializeObject(demoConfig);

                Console.WriteLine("Creating new Configuration");
                await amppControl.CreateConfigurationAsync(jsonConfig);
            }
            else
            {
                demoConfig = JsonConvert.DeserializeObject<DemoConfiguration>(config.Value);
                Console.WriteLine("Configuration ReadFromPlatform:");
                Console.WriteLine($"Connection String: {demoConfig.ConnectionString}");
                Console.WriteLine($"Port             : {demoConfig.Port}");
            }

            // Create our Application and Initialise it
            // This sets up all the method handlers and listens to the SignalR notifications
            // As well as writing details of our app to the AMPP Control Registry
            var demoApplication = new DemoApplication(demoConfig, amppControl);

            Console.WriteLine("Initialising Service...");
            await amppControl.InitializeWorkloadAsync("DemoService", "3rd Party", demoApplication);
            Console.WriteLine("...Initialised");

            string line = Console.ReadLine();

            while (!line.ToUpper().StartsWith("QUIT"))
            {

                line = Console.ReadLine();
            }

            await amppControl.ReleaseLicense();
            Console.WriteLine("Bye!");
            Environment.Exit(0);
        }

        private static void AmppControl_OnLicenseStatusChanged(object sender, Gv.Ampp.Control.Sdk.Model.LicenseStatusChangedEventArgs e)
        {
            Console.WriteLine("*******************************************");
            Console.WriteLine("License Status Changed");
            Console.WriteLine("*******************************************");

            if (!e.Licensed)
            {
                Console.WriteLine("No longer licensed: " + e.Error);
                Environment.Exit(0);
            }

        }

        private static void Usage()
        {
            Console.WriteLine("***************************gv.ampp.control.demo.app**********************************************");
            Console.WriteLine("Usage: ");
            Console.WriteLine("gv.ampp.control.demo.app --ApiKey {key} --PlatformUri {platformUri} --WorkloadName {workloadName}");
            Console.WriteLine();
            Console.WriteLine(" CommandLine          | Environment Variable      | Description            ");
            Console.WriteLine("----------------------|---------------------------|------------------------");
            Console.WriteLine(" --ApiKey             | GVCLUSTER_PLATFORMAPIKEY  | ApiKey for connecting to GVPlatform");
            Console.WriteLine(" --PlatformUri        | GVCLUSTER_PLATFORMURI     | Uri for for connecting to GVPlatform");
            Console.WriteLine(" --WorkloadName       | GVCLUSTER_WORKLOADNAME    | Unique WorkloadName");
            Console.WriteLine("*************************************************************************************************");
        }
        
        private static void Exit(string consoleLog)
        {
            Console.WriteLine(consoleLog);
            Usage();
            Console.ReadLine();
            Environment.Exit(0);
        }
    }
}
