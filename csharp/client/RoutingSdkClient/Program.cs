// See https://aka.ms/new-console-template for more information
using ampp.control.common;
using AmppControl.Model;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, World!");

// Read Platform settings from appsettings.json
var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();


var section = config.GetSection(nameof(PlatformSettings));
var platformSettings = section.Get<PlatformSettings>();

Console.WriteLine("Connecting to AMPP");
Console.WriteLine("URL: " + platformSettings.PlatformUrl);
Console.WriteLine("APIKey: " + platformSettings.ApiKey);

// Create RouterSDK object
Router routerSdk = new Router(platformSettings.PlatformUrl, platformSettings.ApiKey);

// Connect to AMPP
var connected = await routerSdk.LoginAsync();

if (!connected)
{
    Console.WriteLine("Error Connecting to AMPP");
    Console.WriteLine("Press Any Key to Continue...");

    Environment.Exit(0);
}

Console.WriteLine("**************************");
Console.WriteLine();
Console.WriteLine("Start Listening for Routed Events");
await routerSdk.StartListeningForRouteEvents();

routerSdk.OnRouteChangedEvent += (sender, e) => {
    Console.WriteLine("**********Route Made***************");
    Console.WriteLine($"Fabric: {e.FabricId}\tSrc: {e.SourceName}\t-- >\t{e.DestinationName}");
    Console.WriteLine("***********************************");
};

Console.WriteLine("**************************");
Console.WriteLine();
Console.WriteLine("List Fabrics");

var fabrics = await routerSdk.GetFabrics();

foreach (var fabricResponse in fabrics)
{
    Console.WriteLine($"{fabricResponse.Fabric.Id}\t{fabricResponse.Fabric.Name}");
}


// TODO: 
// Enter the FabricId of a your Fabric for the next series of tests
var fabridId = "468243a8-1a3c-41ea-ac83-1f5f7eb18df8";


Console.WriteLine("**************************");
Console.WriteLine();
Console.WriteLine($"Getting Producers for Fabric: {fabridId}");

var producers = await routerSdk.GetProducers(fabridId);

foreach (var prod in producers)
{
    Console.WriteLine($"{prod.Producer.Id}\t{prod.Producer.Name}");
}

Console.WriteLine("**************************");
Console.WriteLine();
Console.WriteLine($"Getting Consumers for Fabric: {fabridId}");

var consumers = await routerSdk.GetConsumers(fabridId);

foreach (var cons in consumers)
{
    Console.WriteLine($"{cons.Consumer.Id}\t{cons.Consumer.Name}");
}

Console.WriteLine("**************************");
Console.WriteLine();
//var srcName = "ColinF4:Keyer";
//var srcName = "ColinF4:TSG";
var srcName = "ColinF4:ClipPlayer";
var dstName = "ColinF4:Flow";
Console.WriteLine($"Making Route ${srcName} --> {dstName}");
var requestId = await routerSdk.MakeRoute(fabridId, srcName, dstName);
Console.WriteLine($"RouteRequest: {requestId}");

Thread.Sleep(1000);

var routeStatus = await routerSdk.CheckRouteRequest(requestId);

Console.WriteLine($"RouteStatus: {routeStatus}");

Console.WriteLine("**************************");
Console.WriteLine();
Console.WriteLine("List Salvos");

var salvos = await routerSdk.GetSalvos();

foreach (var salvo in salvos)
{
    Console.WriteLine($"{salvo.Id}\t{salvo.Name}");
}


Console.WriteLine("**************************");
Console.WriteLine();
var salvoName = "ColinSalvo1";
Console.WriteLine($"Execute Salvo:  {salvoName}");
var executeSalvo = await routerSdk.ExecuteSalvo(salvoName);
Console.WriteLine(executeSalvo ? "Success" : "Failure");

Thread.Sleep(1000);
Console.WriteLine($"Cancel Salvo:  {salvoName}");
var cancelSalvo = await routerSdk.CancelSalvo(salvoName);
Console.WriteLine(cancelSalvo ? "Success" : "Failure");

// Set a Router Alias for a Producer
{
    Console.WriteLine("**************************");
    var producerName = "ColinF4:ClipPlayer";
    var alias = "CF4:Clip1";
    Console.WriteLine($"Set Producer Alias: {producerName} = {alias}");
    var setAlias = await routerSdk.SetProducerAlias(fabridId, producerName, alias);
    Console.WriteLine(setAlias ? "Success" : "Failure");
}


// Set a Router Alias for a Consumer
{
    Console.WriteLine("**************************");
    var consumerName = "ColinF4:Flow";
    var alias = "CF4:FM";
    Console.WriteLine($"Set Consumer Alias: {consumerName} = {alias}");
    var setAlias = await routerSdk.SetConsumerAlias(fabridId, consumerName, alias);
    Console.WriteLine(setAlias ? "Success" : "Failure");
}


Console.WriteLine("**************************");
Console.WriteLine("Press enter to exit");
Console.ReadLine();

await routerSdk.StopListeningForRouteEvents();







