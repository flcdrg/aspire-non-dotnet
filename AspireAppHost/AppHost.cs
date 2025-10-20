var builder = DistributedApplication.CreateBuilder(args);

// MongoDB
var mongo = builder.AddMongoDB("mongo", 27017)
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

// Rust service

// Frontend

builder.Build().Run();
