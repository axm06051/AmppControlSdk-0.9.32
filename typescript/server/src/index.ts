import { AmppControlService, GVPlatform, LogProperty, GVLicensing, ConsoleLogger, LogLevel } from '@gv/amppsdk';
import 'dotenv/config'
import { exit } from 'process';
import { DemoService } from './DemoService';

/**
 * Read all the properties from the .env file
 */
const apiKey = process.env.GVCLUSTER_PLATFORMAPIKEY;
const platformUrl = process.env.GVCLUSTER_PLATFORMURI;
const workloadId = process.env.GVCLUSTER_WORKLOADID;
const workloadName = process.env.GVCLUSTER_WORKLOADNAME;
const applicationType = "SDKDemoApp";
const vendor = "GV 3rd Party";

/**
 * Entry Point
 * @returns 
 */
async function main() {

    console.log('Hello, World')

    // Test that the API key has been provided.
    if (apiKey == null || apiKey === ""){
        console.error("ERROR : Please provide an apiKey in the .env file")
        return;
    }

    // Test that the platformUrl has been provided
    if (platformUrl == null || platformUrl === ""){
        console.error("ERROR : Please provide a platformUrl in the .env file")
        return;
    }

       // Test that the workloadName has been provided
    if (workloadName == null || workloadName === ""){
        console.error("ERROR : Please provide a WORKLOAD_NAME in the .env file")
        return;
    }

        // Test that the workloadId has been provided
    if (workloadId == null || workloadId === ""){
        console.error("ERROR : Please provide a WORKLOAD_ID in the .env file")
        return;
    }

    if(!isValidGUID(workloadId)){
        console.error(`ERROR : WorkloadId ${workloadId} is not a valid GUID`)
        return;
    }

    // Initialize connection to AMPP
    console.log(`connecting to platform at '${platformUrl}'`);
    
    /// Adjust the LogLevel to see more or less logging
    const logger = new ConsoleLogger(LogLevel.Information);

    const platform = new GVPlatform(workloadName, platformUrl, apiKey, logger);
    const result = await platform.login()

    if(!result) {
        console.error('Error connecting to platform')
        return;
    }

    // Now you are connected you can use the logging method
    platform.logInfo(applicationType, "Connected to GVPlatform");

    console.log('***********************');
    console.log('connected to GVPlatform');
    console.log('***********************');

    /**
     * This service will register your app with the AMPP Control Registry as well as handling all the AMPP Control Messaging over the signalR connection
     * @platform - The platform you created above
     * @workloadId - A unique Id for this service, don't create a new one every time, but If you have multiple instances then each should have a unique Id
     * @workloadName - A unique name of this instance
     * @ApplicationType - Describes the type of App
     * @vendor - The vendor name
     * @
     */
    const amppControl = new AmppControlService(platform, workloadId, workloadName, applicationType, vendor);

    /**
     * Your code in this class
     * Add methods to this class that you want to be called when an Ampp Control Message is recieved.
     */
    const demoService = new DemoService(amppControl);

    console.log('Registering Workload');
    console.log(`NAME:\t\t${workloadName}`);
    console.log(`ID:\t\t${workloadId}`);
    console.log(`TYPE:\t\t${applicationType}`);

    const logProperties:  LogProperty[] = [
        {key : "WorkloadName", value : workloadName },
        {key : "WorkloadId",  value : workloadId},
        {key : "Type",  value : applicationType}
    ]

    // This is where you register your service
    // Method are registered (via reflection) with AMPP control
    // decorate methods with @amppCommand
    // This will do all the plumbing to map notifications to methods
    const registered = await amppControl.RegisterWorkloadAsync(demoService)

    if(!registered) {
        console.error('Error registering workload');
        amppControl.logError(applicationType, "Error Registering workload", logProperties)
        return;

    }

    console.log('************************************');
    console.log('Workload Registered with AMPP Control');
    console.log('*************************************');
    amppControl.logInfo(applicationType, "Workload Registered", logProperties)

    // This opens the SignalR connection and starts listening for AMPP Control messagges
    await amppControl.StartAsync()

    console.log('************************************');
    console.log('Started');
    console.log('*************************************');


    //const productCode = "AE-A-MCUHDOUT16";
    //const productCode = "AE-A-MCUHDOUT16X";
    const productCode = '';

    const licensing : GVLicensing  = new GVLicensing(platform, productCode);

    licensing.on('LicenseChangedEvent', ((productCode: string, licensed: boolean, executionType : string, error: string) => {

        
        console.log('************************************');
        console.log(`License Status Changed: "${productCode}" Licensed: ${licensed}, ExecutionType: ${executionType}, Error: ${error}`);
        console.log('*************************************');

        if(!licensed) {
            exit(1);
        }
        

    }));

    async function releaseLicense() {
        // Free any resources here
        console.log('Releasing License...');
        await licensing.ReleaseLicense();
      }

      //Register cleanup function to be called on process exit
      process.on('exit', () => {
        console.log('exit')
      }); 
      
      process.on('SIGINT', () => {
        console.log('Received SIGINT signal. Cleaning up...');
        releaseLicense().finally(() => {
          process.exit(0);
        });
      });
      

      // Handle uncaught exceptions
      process.on('uncaughtException', async (err) => {
            console.log("uncaughtException");
            console.log(err);
            await releaseLicense().finally( () => {
                exit(0);
            });
        });
        



    const licenseStatus = await licensing.StartLicenseCheck()

    // No license status means we are not attempting to license the application
    // Just continue as normal
    if(licenseStatus === undefined) {
        console.log('************************************');
        console.log('Application does not require a license.');
        console.log('*************************************');
    }
    // If we have a license status then we are attempting to license the application
    // If we are not licensed then we should exit
    else if(!licensing.IsLicensed())
    {
       
        console.log('************************************');
        console.log(`Failed to acquire a license for ${productCode}: `, licenseStatus.executionType, licenseStatus.noSubscriptionReason);
        console.log('*************************************');
        exit(1);
    }
    // If we have a license status and we are licensed then we can continue
    else
    {
        console.log('************************************');
        console.log(`License Acquired: for ${productCode}`, licenseStatus.executionType);
        console.log('*************************************');
    }

}

const isValidGUID = (value: string) : boolean => {
    if (value.length > 0) {
        if (new RegExp(/^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$/).test(value)) {
            return true;
        }
    }
    return false;
}

main()