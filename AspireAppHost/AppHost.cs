var builder = DistributedApplication.CreateBuilder(args);

// MongoDB
// begin-snippet: MongoDB
var mongo = builder.AddMongoDB("mongo", 27017, null, null)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables.Remove("MONGO_INITDB_ROOT_USERNAME");
        context.EnvironmentVariables.Remove("MONGO_INITDB_ROOT_PASSWORD");
    })
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
    .WithHttpEndpoint(port: 8080, isProxied: false);
// end-snippet: RustApi

// Node.js App

// Python API
// begin-snippet: PythonApi
var pythonApp = builder.AddPythonExecutable("python-api", "../PythonUv", "fastapi")
    .WithArgs(["dev", "src/api"])
    .WithUv()
    .WaitFor(mongo)
	.WaitFor(rust)
    .WithEnvironment("PYTHONIOENCODING", "utf-8")
    .WithHttpEndpoint(env: "PORT", port: 8000);
// end-snippet: PythonApi

// Frontend
// begin-snippet: ViteReactApp
var web = builder.AddViteApp("web", "../web-vite-react")
    .WithPnpm()
    // If you are using fnm for Node.js version management, you might need to adjust the PATH
    .WithEnvironment("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\fnm\aliases\default"))
    .WaitFor(pythonApp);
// end-snippet: ViteReactApp

builder.Build().Run();
