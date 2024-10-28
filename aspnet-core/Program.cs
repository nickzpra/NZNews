
using Microsoft.Extensions.Configuration;
using NZNewsApi.Services;
using NZNewsApi.Services.Interfaces;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder => builder
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Configure Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "munisoft.redis.cache.windows.net:6379"; // Replace with your Redis server configuration
    options.InstanceName = "NZNewsInstance:"; // Optional: Specify an instance name
});

// Get the connection string from appsettings
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

// Initialize Redis connection and register it as a singleton
var redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Register HttpClient
builder.Services.AddHttpClient();

builder.Services.AddControllers(); // Make sure this line is included

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<INewsStoryService, NewsStoryService>();
builder.Services.AddScoped<ICacheService, CacheService>();

var app = builder.Build();

// Add CORS to the middleware pipeline
app.UseCors("AllowAngularApp");

app.MapControllers();


    app.UseSwagger();
    app.UseSwaggerUI();




app.Run();

