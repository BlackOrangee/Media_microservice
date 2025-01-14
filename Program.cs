using Autofac;
using Autofac.Extensions.DependencyInjection;
using Media_microservice;
using Media_microservice.Services;
using Media_microservice.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Minio;
using MySqlConnector;

DotNetEnv.Env.Load();

string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
string? minioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
string? bucketName = Environment.GetEnvironmentVariable("MINIO_BUCKET_NAME");
string? minioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
string? minioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("DB_CONNECTION_STRING environment variable is not set");
}

if (string.IsNullOrEmpty(minioEndpoint) || string.IsNullOrEmpty(minioAccessKey) 
    || string.IsNullOrEmpty(minioSecretKey) || string.IsNullOrEmpty(bucketName))
{
    throw new Exception("MINIO_ENDPOINT, MINIO_ACCESS_KEY, MINIO_SECRET_KEY" +
        " or MINIO_BUCKET_NAME environment variable is not set");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)), ServiceLifetime.Scoped);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Services.AddControllers();

var minioClient = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .Build();


builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterType<MinioService>()
                    .As<IMinioService>()
                    .WithParameter("minioClient", minioClient)
                    .WithParameter("bucketName", bucketName)
                    .InstancePerLifetimeScope();

    containerBuilder.RegisterType<CommunicationService>()
                    .As<ICommunicationService>()
                    .InstancePerLifetimeScope();
});

builder.Services.AddHostedService<InitialBackgroundService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var connection = new MySqlConnection(connectionString))
{
    connection.Open();
    using (var command = new MySqlCommand("SELECT 1", connection))
    {
        command.ExecuteScalar();
    }
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
    try
    {
        dbContext.Database.CanConnect();
        Console.WriteLine("Connected to database");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not connect to database. Message: {ex.Message}");
    }
}

app.UseAuthorization();

app.Run();
