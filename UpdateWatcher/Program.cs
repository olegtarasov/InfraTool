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

app.MapGet("/versions", async () =>
    {
        var config = WatcherConfig.Load();
        foreach (var item in config.Items)
        {
            var localVersion = await item.Local.GetVersion();
        }
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();