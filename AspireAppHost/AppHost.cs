using Aspire.Hosting;
using AspireAppHost;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// begin-snippet: MongoDB
var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume()
    .WithMongoExpress();

var mongodb = mongo.AddDatabase("petstore");
// end-snippet: MongoDB

// begin-snippet: PowerShellLoadData
var loadData = builder.AddExecutable("load-data", "pwsh", "../mongodb", "-noprofile", "./populate.ps1")
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithArgs("-connectionString")
    .WithArgs(new ConnectionStringReference(mongo.Resource, false));
//.WithExplicitStart();
// end-snippet: PowerShellLoadData

// Rust service
// begin-snippet: RustApi
var rust = builder.AddRustApp("rustpaymentapi", "../RustPaymentApi", [])
    .WithHttpEndpoint(env: "PAYMENT_API_PORT");
// end-snippet: RustApi

// Node.js App
// begin-snippet: NodeJokeApi
var nodeApp = builder.AddPnpmApp("node-joke-api", "../NodeApp")
    // If you are using fnm for Node.js version management, you might need to adjust the PATH
    .WithEnvironment("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\fnm\aliases\default"))
    .WithHttpEndpoint(env: "PORT")
    .WithOtlpExporter();

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];

if (builder.Environment.IsDevelopment() && launchProfile == "https")
{
    nodeApp.RunWithHttpsDevCertificate("HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE");
}
// end-snippet: NodeJokeApi

// Python API
// 1. dotnet add package CommunityToolkit.Aspire.Hosting.Python.Extensions
// 2. Add .AddPythonApp to the builder
// 3. Add pragma warning disable ASPIREHOSTINGPYTHON001
// 4. Use .AddUvApp
// 5. Mitigate character encoding issue

// begin-snippet: PythonApi
#pragma warning disable ASPIREHOSTINGPYTHON001
var pythonApp = builder.AddUvApp("python-api", "../PythonUv", "fastapi", "run", "src/api")
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(rust)
    .WaitFor(rust)
    .WithReference(nodeApp)
    .WaitFor(nodeApp)
    .WithEnvironment("PYTHONIOENCODING", "utf-8")
    .WithEnvironment("MONGO_CONNECTION_STRING", new ConnectionStringReference(mongo.Resource, false))
    .WithEnvironment("PAYMENT_API_BASE_URL", ReferenceExpression.Create($"{rust.Resource.GetEndpoint("http")}"))
    .WithEnvironment("NODE_APP_BASE_URL", ReferenceExpression.Create($"{nodeApp.Resource.GetEndpoint("http")}"))
    .WithHttpEndpoint(env: "PORT")
    .WithOtlpExporter();

#pragma warning restore ASPIREHOSTINGPYTHON001
// end-snippet: PythonApi

// Frontend
// 1. dotnet add package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions
// begin-snippet: ViteReactApp
var web = builder.AddViteApp("web", "../web-vite-react", "pnpm")
    // If you are using fnm for Node.js version management, you might need to adjust the PATH
    .WithEnvironment("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\fnm\aliases\default"))
    .WithExternalHttpEndpoints()
    .WithReference(pythonApp)
    .WaitFor(pythonApp)
    .WithEnvironment("VITE_API_BASE_URL", new EndpointReference(pythonApp.Resource, "http"));
// end-snippet: ViteReactApp

builder.Build().Run();
