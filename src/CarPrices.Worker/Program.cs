using CarPrices.Infrastructure;
using CarPrices.Scraping;
using CarPrices.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default"));
builder.Services.AddScraping(builder.Configuration);
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.AddHostedService<CollectorWorker>();

var host = builder.Build();
host.Run();
