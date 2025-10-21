using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// MongoDB
var mongo = builder.AddMongoDB("mongo")
    //.WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithMongoExpress();

var mongodb = mongo.AddDatabase("petstore");

var loadData = builder.AddExecutable("load-data", "pwsh", "../mongodb", "-noprofile", "./populate.ps1")
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithArgs("-connectionString")
    .WithArgs(new ConnectionStringReference(mongo.Resource, false));
//.WithExplicitStart();

// Rust service
var rust = builder.AddRustApp("rustpaymentapi", "../RustPaymentApi", [])
    .WithHttpEndpoint(env: "PAYMENT_API_PORT");

// Python API
// 1. dotnet add package CommunityToolkit.Aspire.Hosting.Python.Extensions
// 2. Add .AddPythonApp to the builder
// 3. Add pragma warning disable ASPIREHOSTINGPYTHON001
// 4. Use .AddUvApp
// 5. Mitigate character encoding issue

#pragma warning disable ASPIREHOSTINGPYTHON001
var pythonApp = builder.AddUvApp("python-api", "../PythonUv", "fastapi", "dev", "src/api")
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(rust)
    .WaitFor(rust)
    .WithEnvironment("PYTHONIOENCODING", "utf-8")
    .WithEnvironment("MONGO_CONNECTION_STRING", new ConnectionStringReference(mongo.Resource, false))
    .WithEnvironment("PAYMENT_API_BASE_URL", ReferenceExpression.Create($"{rust.Resource.GetEndpoint("http")}"))
    .WithHttpEndpoint(env: "PORT");

#pragma warning restore ASPIREHOSTINGPYTHON001

// Frontend
// 1. dotnet add package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions
var web = builder.AddViteApp("web", "../web-vite-react", "pnpm")
    // If you are using fnm for Node.js version management, you might need to adjust the PATH
    .WithEnvironment("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\fnm\aliases\default"))
    .WithExternalHttpEndpoints()
    .WithReference(pythonApp)
    .WaitFor(pythonApp)
    .WithReference(rust)
    .WaitFor(rust)
    .WithEnvironment("VITE_API_BASE_URL", new EndpointReference(pythonApp.Resource, "http"));

builder.Build().Run();
