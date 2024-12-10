using Autofac;
using Autofac.Extensions.DependencyInjection;
using Confluent.Kafka;
using Media_microservice.Services;
using Media_microservice.Services.Impl;
using Minio;

DotNetEnv.Env.Load();

string? topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC");
string? groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID");
string? bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
string? minioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
string? bucketName = Environment.GetEnvironmentVariable("MINIO_BUCKET_NAME");
string? minioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
string? minioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

if (string.IsNullOrEmpty(bootstrapServers) || string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(topic))
{
    throw new Exception("KAFKA_BOOTSTRAP_SERVERS, KAFKA_GROUP_ID or KAFKA_TOPIC environment variable is not set");
}

if (string.IsNullOrEmpty(minioEndpoint) || string.IsNullOrEmpty(minioAccessKey) 
    || string.IsNullOrEmpty(minioSecretKey) || string.IsNullOrEmpty(bucketName))
{
    throw new Exception("MINIO_ENDPOINT, MINIO_ACCESS_KEY, MINIO_SECRET_KEY" +
        " or MINIO_BUCKET_NAME environment variable is not set");
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Services.AddControllers();

var minioClient = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .Build();

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = bootstrapServers,
    GroupId = groupId,
    AutoOffsetReset = AutoOffsetReset.Earliest
};

var producerConfig = new ProducerConfig
{
    BootstrapServers = bootstrapServers
};

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{

    containerBuilder.RegisterInstance(producerConfig)
                    .As<ProducerConfig>()
                    .SingleInstance();

    containerBuilder.RegisterInstance(consumerConfig)
                    .As<ConsumerConfig>()
                    .SingleInstance();

    containerBuilder.RegisterType<MinioService>()
                    .As<IMinioService>()
                    .WithParameter("minioClient", minioClient)
                    .WithParameter("bucketName", bucketName)
                    .SingleInstance();

    containerBuilder.RegisterType<KafkaService>()
                    .As<IKafkaService>()
                    .WithParameter("consumerConfig", consumerConfig)
                    .WithParameter("producerConfig", producerConfig)
                    .WithParameter("topic", topic)
                    .InstancePerLifetimeScope();
});

builder.Services.AddHostedService<KafkaBackgroundService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseAuthorization();

app.Run();
