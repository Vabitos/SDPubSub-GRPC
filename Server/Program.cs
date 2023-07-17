using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Server.Services;
using Server.SistemaDB;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ;
using Microsoft.AspNetCore.Connections;
using static System.Formats.Asn1.AsnWriter;

static RabbitMQConsumer ConfigureRabbitMQConsumer(IServiceProvider provider)
{
    var factory = new ConnectionFactory { HostName = "localhost" };
    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();

    channel.ExchangeDeclare(exchange: "EVENTS", type: ExchangeType.Direct);

    channel.QueueDeclare(queue: "Client_Requests", durable: false, exclusive: false, autoDelete: false, arguments: null);

    var queuename = channel.QueueDeclare().QueueName;

    channel.QueueBind(queue: queuename, exchange: "EVENTS", routingKey: "request");

    using (var scope = provider.CreateScope())
    {
        var scopedProvider = scope.ServiceProvider;
        var scopeFactory = scopedProvider.GetRequiredService<IServiceScopeFactory>();
        var consumer = new RabbitMQConsumer("localhost", scopeFactory);
        consumer.StartConsuming();

        return consumer;
    }
}

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddDbContext<SistemaDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Data Source=(LocalDb)\\LocaldbTB2;Initial Catalog=SistemaDB;Integrated Security=True"));
}, ServiceLifetime.Transient);


builder.Services.AddSingleton(provider =>
{
    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    return ConfigureRabbitMQConsumer(provider);

});

var app = builder.Build();

// Get the RabbitMQ consumer from the service provider.
var rabbitMQConsumer = app.Services.GetRequiredService<RabbitMQConsumer>();


// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<ReservaService>();


app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();


