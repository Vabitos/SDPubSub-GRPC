using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Server;
using Server.Services;
using Server.SistemaDB;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;

public class RabbitMQConsumer
{
    private readonly ConnectionFactory _connectionFactory;
    private IConnection _connection;
    private IModel _channel;
    private readonly IServiceScopeFactory _scopeFactory;

    public RabbitMQConsumer(string hostName, IServiceScopeFactory scopeFactory)
    {
        _connectionFactory = new ConnectionFactory { HostName = hostName };
        _scopeFactory = scopeFactory;
    }

    public async Task StartConsuming()
    {
        try
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();


            _channel.ExchangeDeclare(exchange: "EVENTS", type: ExchangeType.Direct);

            _channel.QueueDeclare(queue: "Client_Requests", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var queuename = _channel.QueueDeclare().QueueName;

            _channel.QueueBind(queue: queuename, exchange: "EVENTS", routingKey: "request");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (sender, eventArgs) =>
            {
                var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                char[] entryDelimiters = { ';', ':' };
                string[] entries = message.Split(entryDelimiters);

                string user = entries[1];
                string dom = entries[3];
                string pedido = entries[5];

                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<SistemaDbContext>();

                    var op = dbContext.Operadors.FirstOrDefault(o => o.Username.ToUpper() == user.ToUpper());
                    var reser = dbContext.Reservas.FirstOrDefault(r => r.Domicilio.ToUpper() == dom.ToUpper());

                    if (reser != null)
                    {
                        var op_reser = dbContext.OperadorReservas.FirstOrDefault(or => or.UsernameOpNavigation == op && or.IdAdministrativoRNavigation == reser);

                        if (op_reser != null || op.Admin)
                        {
                            if (pedido.ToUpper() == "ATIVAR")
                            {
                                if (reser.Estado == "RESERVED" || reser.Estado == "DEACTIVATED")
                                {
                                    reser.Estado = "ACTIVATED";
                                    dbContext.Reservas.Update(reser);
                                    await dbContext.SaveChangesAsync();

                                    var body = Encoding.UTF8.GetBytes($"Username:{op.Username};A Reserva do/a {reser.Domicilio} foi ativada");

                                    _channel.BasicPublish(exchange: "EVENTS",
                                        routingKey: user.ToUpper(),
                                        basicProperties: null,
                                        body: body);

                                    _channel.BasicPublish(exchange: "EVENTS",
                                       routingKey: "Admin",
                                       basicProperties: null,
                                       body: body);


                                }
                                else
                                {
                                    var body = Encoding.UTF8.GetBytes($"Username:{op.Username};Nao foi possivel ativar a reserva do/a {reser.Domicilio}");

                                    _channel.BasicPublish(exchange: "EVENTS",
                                        routingKey: user.ToUpper(),
                                        basicProperties: null,
                                        body: body);
                                }
                            }
                            else if (pedido.ToUpper() == "DESATIVAR")
                            {
                                if (reser.Estado == "ACTIVATED")
                                {
                                    reser.Estado = "DEACTIVATED";
                                    dbContext.Reservas.Update(reser);
                                    await dbContext.SaveChangesAsync();

                                    var body = Encoding.UTF8.GetBytes($"Username:{op.Username};A Reserva do/a {reser.Domicilio} foi desativada");

                                    _channel.BasicPublish(exchange: "EVENTS",
                                        routingKey: user.ToUpper(),
                                        basicProperties: null,
                                        body: body);

                                    _channel.BasicPublish(exchange: "EVENTS",
                                      routingKey: "Admin",
                                      basicProperties: null,
                                      body: body);
                                }
                                else
                                {
                                    var body = Encoding.UTF8.GetBytes($"Username:{op.Username};Nao foi possivel desativar a reserva do/a {reser.Domicilio}");

                                    _channel.BasicPublish(exchange: "EVENTS",
                                        routingKey: user.ToUpper(),
                                        basicProperties: null,
                                        body: body);

                                    _channel.BasicPublish(exchange: "EVENTS",
                                      routingKey: "Admin",
                                      basicProperties: null,
                                      body: body);
                                }
                            }
                            else if (pedido.ToUpper() == "TERMINAR")
                            {
                                if (reser.Estado == "DEACTIVATED")
                                {
                                    reser.Estado = "TERMINATED";
                                    dbContext.Reservas.Update(reser);
                                    await dbContext.SaveChangesAsync();

                                    var body = Encoding.UTF8.GetBytes($"Username:{op.Username};A Reserva do/a {reser.Domicilio} foi terminado");

                                    _channel.BasicPublish(exchange: "EVENTS",
                                        routingKey: user.ToUpper(),
                                        basicProperties: null,
                                        body: body);

                                    _channel.BasicPublish(exchange: "EVENTS",
                                      routingKey: "Admin",
                                      basicProperties: null,
                                      body: body);

                                    await Task.Delay(1000);

                                    dbContext.OperadorReservas.Remove(op_reser);
                                }
                                else
                                {
                                    var body = Encoding.UTF8.GetBytes($"Username:{op.Username};Nao foi possivel terminar a reserva do/a {reser.Domicilio}");

                                    _channel.BasicPublish(exchange: "EVENTS",
                                        routingKey: user.ToUpper(),
                                        basicProperties: null,
                                        body: body);

                                    _channel.BasicPublish(exchange: "EVENTS",
                                      routingKey: "Admin",
                                      basicProperties: null,
                                      body: body);

                                }
                            }
                        }
                    }
                    dbContext.Dispose();
                }
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };
            _channel.BasicConsume(queuename, false, consumer);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}