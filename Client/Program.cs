using Google.Protobuf.Collections;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Server;
using Server.SistemaDB;
using System.Diagnostics.Eventing.Reader;
using System.Text;


namespace client
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var chl = connection.CreateModel();

            bool v;
            string pass;
            string user;

            chl.ExchangeDeclare(exchange: "EVENTS", type: ExchangeType.Direct);

            chl.QueueDeclare(queue: "Server_Responses",
                                       durable: false,
                                       exclusive: false,
                                       autoDelete: false,
                                       arguments: null);

            var channel = GrpcChannel.ForAddress("https://localhost:7130");
            var client = new ReservaServ.ReservaServClient(channel);

            

            while (true)
            {

                Console.Write("Escreva seu username:");
                user = Console.ReadLine();

                Console.Write("Escreva a sua password:");
                pass = Console.ReadLine();

                DadosConta conta = new DadosConta { Username = user, Password = pass };

                var log = client.Login(conta);

                if (log.Logado)
                {
                    v = log.Admin;
                    break;
                }
                else
                {
                    Console.WriteLine("username/Password errada.");
                }
            }

            if (v)
            {
                Console.WriteLine($"Bem vindo Admin {user}!!");

                Console.WriteLine("Reservas:");

                Null nu = new Null();

                var lista = client.MostrarReservasOp(nu);

                foreach (var r in lista.List)
                {
                    Console.WriteLine($"User: {r.Username} | Domicilio: {r.Domicilio} | Estado: {r.Estado} | Data da Reserva: {r.DataReserva}");
                }

                Console.WriteLine();

                chl.QueueBind(queue: "Server_Responses", exchange: "EVENTS", routingKey: "Admin");

                var consumer = new EventingBasicConsumer(chl);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    char[] entryDelimiters = { ';', ':' };

                    string[] entries = message.Split(entryDelimiters);

                    string userRec = entries[1];

                    var resposta = entries[2];

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($"Notificacao - {userRec}: {resposta}");

                };

                chl.BasicConsume(queue: "Server_Responses",
                                 autoAck: true,
                                 consumer: consumer);
            }
            else
            {
                Console.WriteLine($"Bem vindo Operador {user}");
                Console.WriteLine("Reservas:");

                Null nu = new Null();

                var lista = client.MostrarReservasOp(nu);

                foreach (var r in lista.List)
                {
                    Console.WriteLine($"User: {r.Username} | Domicilio: {r.Domicilio} | Estado: {r.Estado} | Data da Reserva: {r.DataReserva}");
                }

                Console.WriteLine();

                chl.QueueBind(queue: "Server_Responses",
                                exchange: "EVENTS",
                                routingKey: user.ToUpper());

                var consumer = new EventingBasicConsumer(chl);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    char[] entryDelimiters = { ';', ':' };

                    string[] entries = message.Split(entryDelimiters);

                    string userRec = null;

                    foreach (string entry in entries)
                    {
                        if (entry.StartsWith("Username"))
                        {
                            userRec = entry.Split(':')[1];
                        }
                    }
                    if (userRec.ToUpper() == user.ToUpper())
                    {
                        var resposta = message.Replace("Username:" + userRec + ";", "");

                        Console.WriteLine($"Notificacao: {resposta}");
                    }
                };
                chl.BasicConsume(queue: "Server_Responses",
                                 autoAck: true,
                                 consumer: consumer);
            }

            if (v)
            {

                while (true)
                {
                    string dom = "";
                    string pedido = "";

                    Console.Write("Para listar com mais detalhe as reservas ou operadores escreva: Reservas/Operadores. \n" +
                        "Para alterar estado de uma Reserva escreva o domicilio. \n" +
                        "Escreva 'exit' para sair:");

                    var escolha = Console.ReadLine();

                    if (escolha.ToUpper() == "RESERVAS")
                    {
                        Console.Write("Escreva o estado das reservas que quer ver(Reservados/Ativados/Desativados/Terminados):");
                        var tipos = Console.ReadLine();

                        if (tipos.ToUpper() == "RESERVADOS" || tipos.ToUpper() == "ATIVADOS" || tipos.ToUpper() == "DESATIVADOS" || tipos.ToUpper() == "TERMINADOS")
                        {
                            Pedido ped = new Pedido() { Mensagem = tipos };

                            Console.WriteLine($"Lista de Reservas no estado - {tipos.ToUpper()}:");

                            var ListReservs = client.LerReservas(ped);

                            foreach (var Reser in ListReservs.Repeats)
                            {
                                Console.WriteLine($"Domicilio: {Reser.Domicilio} | Modalidade: {Reser.Modalidade} | Operadora de origem: {Reser.OpOrigem} | Operadora atual: {Reser.Operadora} | Data de Reserva: {Reser.DataReserva}");
                            }

                        }

                    }
                    else if (escolha.ToUpper() == "OPERADORES")
                    {
                        Console.WriteLine($"Lista de Operadores:");

                        Null n = new Null();

                        var Listops = client.LerOperadores(n);

                        foreach (var op in Listops.Repeats)
                        {
                            Console.WriteLine($"Username: {op.Username} | Operadora: {op.Operadora} | Admin: {op.Admin}");
                        }

                    }
                    else if (escolha.ToUpper() == "EXIT")
                    {
                        break;
                    }
                    else
                    {
                        dom = escolha;

                        Console.Write("Escreva a mudança de estado que quer efetuar (Ativar/Desativar/Terminar):");

                        pedido = Console.ReadLine();

                        if(pedido.ToUpper() == "ATIVAR" || pedido.ToUpper() == "DESATIVAR" || pedido.ToUpper() == "TERMINAR")
                        { 
                        string message = $"Username:{user};Domicilio:{dom};pedido:{pedido}";
                        var body = Encoding.UTF8.GetBytes(message);

                            chl.BasicPublish(exchange: "EVENTS",
                                routingKey: "request",
                                basicProperties: null,
                                body: body);
                        }
                    }

                }
            }
            else
            {
                while (true)
                {
                    Console.Write("Escreva o Domicilio(Escreva 'exit' para fazer logout):");
                    var dom = Console.ReadLine();

                    if (dom.ToUpper() == "EXIT")
                    {
                        break;
                    }

                    Console.Write("Escreva que ação quer fazer (Reservar/Ativar/Desativar/Terminar):");
                    var pedido = Console.ReadLine();

                    if (pedido.ToUpper() == "RESERVAR")
                    {
                        Console.Write("Escreva a modalidade do Domicilio a reservar ( *downstream*_*upstream* ):");
                        var modal = Console.ReadLine();

                        DadosReserva DR = new DadosReserva() { Domicilio = dom, Modalidade = modal, Username = user, Password = pass };

                        var EfectReser = client.CriarReserva(DR);

                        if (EfectReser.Sucesso)
                        {
                            Console.WriteLine("Domicilio Reservado com Sucesso!!");
                        }
                        else
                        {
                            Console.WriteLine("Não foi possível reservar o Domicilio..");
                        }
                    }
                    else if (pedido.ToUpper() == "ATIVAR" || pedido.ToUpper() == "DESATIVAR" || pedido.ToUpper() == "TERMINAR")
                    {
                        string message = $"Username:{user};Domicilio:{dom};pedido:{pedido}";
                        var body = Encoding.UTF8.GetBytes(message);

                        chl.BasicPublish(exchange: "EVENTS",
                                routingKey: "Request",
                                basicProperties: null,
                                body: body);

                        var x = Console.Read();
                    }
                }
            }
        }
    }
}
