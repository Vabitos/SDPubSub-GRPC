using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;

namespace Server.SistemaDB;

public partial class Reserva
{
    public string IdAdministrativo { get; set; } = null!;

    public string OpOrigem { get; set; } = null!;

    public string Operadora { get; set; } = null!;

    public string Domicilio { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string Modalidade { get; set; } = null!;

    public DateTime DataReserva { get; set; }

    public Reserva()
    {

    }

    public Reserva(string idAdministrativo, string opOrigem, string operadora, string domicilio, string estado, string modalidade)
    {
        IdAdministrativo = idAdministrativo;
        OpOrigem = opOrigem;
        Operadora = operadora;
        Domicilio = domicilio;
        Estado = estado;
        Modalidade = modalidade;
        DataReserva = DateTime.Now;
    }
}
