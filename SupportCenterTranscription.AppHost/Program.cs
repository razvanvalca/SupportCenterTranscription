var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TeamsStreamerBot>("teamsstreamerbot");

builder.Build().Run();
