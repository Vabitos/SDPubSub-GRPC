using Grpc.Core;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Server;
using Server.SistemaDB;

namespace Server.Services
{
    public class ReservaService : ReservaServ.ReservaServBase
    {
        private readonly ILogger<ReservaService> _logger;
        private SistemaDbContext _dbContext;
        private readonly RabbitMQConsumer _rabbitMQConsumer;

        public ReservaService(ILogger<ReservaService> logger, SistemaDbContext dbContext, RabbitMQConsumer rabbitMQConsumer)
        {
            _logger = logger;
            _dbContext = dbContext;
            _rabbitMQConsumer = rabbitMQConsumer;
        }

        public override Task<Contaguardada> Login(DadosConta request, ServerCallContext context)
        {
            Contaguardada c = new Contaguardada();

            if (_dbContext.Operadors.Contains(_dbContext.Operadors.FirstOrDefault(p => p.Username == request.Username && p.Password == request.Password && p.Admin == true)))
            {
                c.Admin = true;
                c.Logado = true;

                return Task.FromResult(c);
            }
            else if (_dbContext.Operadors.Contains(_dbContext.Operadors.FirstOrDefault(p => p.Username == request.Username && p.Password == request.Password && p.Admin == false)))
            {
                c.Admin = false;
                c.Logado = true;

                return Task.FromResult(c);
            }

            c.Admin = false;
            c.Logado = false;

            return Task.FromResult(c);
        }

        public override Task<Guardada> CriarReserva(DadosReserva request, ServerCallContext context)
        {
            var y = _dbContext.Operadors.FirstOrDefault(p => p.Username == request.Username);

            var x = _dbContext.Reservas.FirstOrDefault(p => p.Domicilio == request.Domicilio);

            if (x != null)
            {
                if (_dbContext.OperadorReservas.Contains(_dbContext.OperadorReservas.FirstOrDefault(p => p.UsernameOp == y.Username && p.IdAdministrativoR == x.IdAdministrativo &&
                (x.Estado == "RESERVED" || x.Estado == "ACTIVATED" || x.Estado == "DEACTIVATED"))))
                {
                    Guardada Error = new Guardada();

                    Error.Sucesso = false;

                    return Task.FromResult(Error);
                }

            }

            string randomString = Guid.NewGuid().ToString("N").Substring(0, 10);

            if (_dbContext.Reservas.FirstOrDefault(p => p.Domicilio.ToUpper() == request.Domicilio.ToUpper() && p.Estado == "TERMINATED") != null)
            {
                var xis = _dbContext.Reservas.FirstOrDefault(p => p.OpOrigem != y.Operadora && p.Domicilio.ToUpper() == request.Domicilio.ToUpper());
                if (xis == null)
                {
                    Reserva Reser = new Reserva(randomString, y.Operadora, y.Operadora, request.Domicilio, "RESERVED", request.Modalidade);
                    _dbContext.Reservas.Add(Reser);
                    _dbContext.SaveChanges();

                    OperadorReserva operadorReserva = new OperadorReserva(request.Username, randomString, Reser, y);
                    _dbContext.OperadorReservas.Add(operadorReserva);
                    _dbContext.SaveChanges(true);
                }
                else
                {
                    var z = _dbContext.Reservas.FirstOrDefault(p => p.Domicilio == request.Domicilio && p.OpOrigem != y.Operadora);

                    Reserva Reser = new Reserva(randomString, z.OpOrigem, y.Operadora, request.Domicilio, "RESERVED", request.Modalidade);
                    _dbContext.Reservas.Add(Reser);
                    _dbContext.SaveChanges();

                    OperadorReserva operadorReserva = new OperadorReserva(request.Username, randomString, Reser, y);
                    _dbContext.OperadorReservas.Add(operadorReserva);
                    _dbContext.SaveChanges(true);
                }
            }
            else
            {
                Reserva Reser = new Reserva(randomString, y.Operadora, y.Operadora, request.Domicilio, "RESERVED", request.Modalidade);
                _dbContext.Reservas.Add(Reser);
                _dbContext.SaveChanges();

                OperadorReserva operadorReserva = new OperadorReserva(request.Username, randomString, Reser, y);
                _dbContext.OperadorReservas.Add(operadorReserva);
                _dbContext.SaveChanges(true);
            }

            Guardada valid = new Guardada();

            valid.Sucesso = true;

            return Task.FromResult(valid);
        }

        public override Task<ListShowOp> MostrarReservasOp(Null request, ServerCallContext context)
        {
            List<Reserva> dados = _dbContext.Reservas.ToList();
            ListShowOp lista = new ListShowOp();

            foreach (Reserva r in dados)
            {
                lista.List.Add(new ReservasShowOpModel()
                {
                    Domicilio = r.Domicilio,
                    Estado = r.Estado,
                    DataReserva = r.DataReserva.ToString(),
                    Username = _dbContext.OperadorReservas.FirstOrDefault(p => p.IdAdministrativoR == r.IdAdministrativo).UsernameOp
                });
            }

            return Task.FromResult(lista);
        }

        public override Task<ListaReservs> LerReservas(Pedido request, ServerCallContext context)
        {

            if (request.Mensagem.ToUpper() == "RESERVADOS")
            {
                List<Reserva> dados = _dbContext.Reservas.Where(p => p.Estado == "RESERVED").ToList();
                ListaReservs reservs = new ListaReservs();

                foreach (Reserva r in dados)
                {
                    reservs.Repeats.Add(new ReservaModel()
                    {
                        IdAdministrativo = r.IdAdministrativo,
                        OpOrigem = r.OpOrigem,
                        Operadora = r.Operadora,
                        Domicilio = r.Domicilio,
                        Estado = r.Estado,
                        Modalidade = r.Modalidade,
                        DataReserva = r.DataReserva.ToString()
                    });
                }

                return Task.FromResult(reservs);
            }
            else if (request.Mensagem.ToUpper() == "ATIVADOS")
            {
                List<Reserva> dados = _dbContext.Reservas.Where(p => p.Estado == "ACTIVATED").ToList();
                ListaReservs reservs = new ListaReservs();

                foreach (Reserva r in dados)
                {
                    reservs.Repeats.Add(new ReservaModel()
                    {
                        IdAdministrativo = r.IdAdministrativo,
                        OpOrigem = r.OpOrigem,
                        Operadora = r.Operadora,
                        Domicilio = r.Domicilio,
                        Estado = r.Estado,
                        Modalidade = r.Modalidade,
                        DataReserva = r.DataReserva.ToString()
                    });
                }

                return Task.FromResult(reservs);
            }
            else if (request.Mensagem.ToUpper() == "DESATIVADOS")
            {
                List<Reserva> dados = _dbContext.Reservas.Where(p => p.Estado == "DEACTIVATED").ToList();
                ListaReservs reservs = new ListaReservs();

                foreach (Reserva r in dados)
                {
                    reservs.Repeats.Add(new ReservaModel()
                    {
                        IdAdministrativo = r.IdAdministrativo,
                        OpOrigem = r.OpOrigem,
                        Operadora = r.Operadora,
                        Domicilio = r.Domicilio,
                        Estado = r.Estado,
                        Modalidade = r.Modalidade,
                        DataReserva = r.DataReserva.ToString()
                    });
                }

                return Task.FromResult(reservs);

            }
            else
            {
                List<Reserva> dados = _dbContext.Reservas.Where(p => p.Estado == "TERMINATED").ToList();
                ListaReservs reservs = new ListaReservs();

                foreach (Reserva r in dados)
                {
                    reservs.Repeats.Add(new ReservaModel()
                    {
                        IdAdministrativo = r.IdAdministrativo,
                        OpOrigem = r.OpOrigem,
                        Operadora = r.Operadora,
                        Domicilio = r.Domicilio,
                        Estado = r.Estado,
                        Modalidade = r.Modalidade,
                        DataReserva = r.DataReserva.ToString()
                    });
                }

                return Task.FromResult(reservs);
            }

        }
        public override Task<ListaOps> LerOperadores(Null request, ServerCallContext context)
        {
            List<Operador> dados = _dbContext.Operadors.ToList();
            ListaOps opera = new ListaOps();

            foreach (Operador o in dados)
            {
                opera.Repeats.Add(new OperadorModel() { Username = o.Username, Operadora = o.Operadora, Admin = o.Admin });
            }

            return Task.FromResult(opera);

        }
    }
}

        



