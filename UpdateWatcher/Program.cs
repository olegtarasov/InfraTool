using UpdateWatcher;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/versions", async (ILogger<Program> logger) =>
    {
        var config = WatcherConfig.Load();
        foreach (var item in config.Items)
        {
            logger.LogInformation($"Getting version for {item.Name}");
            var localVersion = await item.Local.GetVersion();
            var remoteVersion = await item.Remote.GetVersion();
        }
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();