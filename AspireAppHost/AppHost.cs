var builder = DistributedApplication.CreateBuilder(args);

// MongoDB
var mongo = builder.AddMongoDB("mongo", 27017, null, null)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables.Remove("MONGO_INITDB_ROOT_USERNAME");
        context.EnvironmentVariables.Remove("MONGO_INITDB_ROOT_PASSWORD");
    })

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
    .WithEnvironment("PYTHONIOENCODING", "utf-8")
    .WithHttpEndpoint(env: "PORT", port: 8000);

#pragma warning restore ASPIREHOSTINGPYTHON001

// Rust service
var rust = builder.AddRustApp("rustpaymentapi", "../RustPaymentApi", [])
    .WithHttpEndpoint(port: 8000, isProxied: false)
    .WithExternalHttpEndpoints();

// Frontend

builder.Build().Run();
