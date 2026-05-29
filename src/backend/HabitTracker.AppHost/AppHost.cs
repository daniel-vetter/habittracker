var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", true);
var postgres = builder
    .AddPostgres("Postgres", password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent);
var db = postgres.AddDatabase("db", "habittracker");

var backend = builder.AddProject<Projects.HabitTracker_WebApp>("Backend")
    .WithReference(db, "db")
    .WaitFor(db);

var frontend = builder
    .AddNpmApp("Frontend", "../../frontend")
    .WithHttpEndpoint(name: "http", port: 4200)
    .WithHttpHealthCheck("/")
    .WithReference(backend)
    .WaitFor(backend);

frontend.WithArgs(context =>
{
    context.Args.Add("--");
    context.Args.Add("--port");
    context.Args.Add(frontend.GetEndpoint("http").Property(EndpointProperty.TargetPort));
});

builder.Build().Run();
