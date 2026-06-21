using Microsoft.Extensions.Hosting;

// Azure Functions (.NET 8 isolated worker) for the Unified Rewards Platform.
// Hosts the serverless workloads from the deployment diagram: Blob:OCR, Timer:Reports, Queue:Payroll.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
