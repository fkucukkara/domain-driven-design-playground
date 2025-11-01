var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database for DDD Playground
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var database = postgres.AddDatabase("dddplaygrounddb");

// Add API service with database dependency
var apiService = builder.AddProject<Projects.DDDPlayground_ApiService>("apiservice")
    .WithReference(database)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
