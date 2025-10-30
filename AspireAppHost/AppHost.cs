var builder = DistributedApplication.CreateBuilder(args);

// MongoDB
// begin-snippet: MongoDB
var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume()
    .WithMongoExpress();

var mongodb = mongo.AddDatabase("petstore");
// end-snippet: MongoDB

// begin-snippet: PowerShellLoadData
var loadData = builder.AddExecutable("load-data", "pwsh", "../mongodb", "-noprofile", "./populate.ps1")
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
var nodeApp = builder.AddJavaScriptApp("node-joke-api", "../NodeApp", "start")
    .WithPnpm()
    // If you are using fnm for Node.js version management, you might need to adjust the PATH
    .WithEnvironment("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\fnm\aliases\default"))
    .WithHttpEndpoint(env: "PORT")
    .WithOtlpExporter();
// end-snippet: NodeJokeApi

// begin-snippet: PythonApi
var pythonApp = builder.AddUvicornApp("python-api", "../PythonUv", "src.api:app")
    .WithUv()
    .WaitFor(mongo)
	.WaitFor(rust)
    .WaitFor(nodeApp)
    .WithEnvironment("PYTHONIOENCODING", "utf-8")
    .WithEnvironment("MONGO_CONNECTION_STRING", new ConnectionStringReference(mongo.Resource, false))
    .WithEnvironment("PAYMENT_API_BASE_URL", new EndpointReference(rust.Resource, "http"))
    .WithEnvironment("NODE_APP_BASE_URL", ReferenceExpression.Create($"{nodeApp.Resource.GetEndpoint("http")}"))
    .WithHttpHealthCheck("/")
    .WithExternalHttpEndpoints();
// end-snippet: PythonApi
// Frontend
// begin-snippet: ViteReactApp
var web = builder.AddViteApp("web", "../web-vite-react")
    .WithPnpm()
    // If you are using fnm for Node.js version management, you might need to adjust the PATH
    .WithEnvironment("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\fnm\aliases\default"))
    .WaitFor(pythonApp)
    .WithEnvironment("VITE_API_BASE_URL", new EndpointReference(pythonApp.Resource, "http"));
// end-snippet: ViteReactApp

builder.Build().Run();
