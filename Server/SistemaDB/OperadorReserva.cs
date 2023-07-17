using System;
using System.Collections.Generic;

namespace Server.SistemaDB;

public partial class OperadorReserva
{
    public string UsernameOp { get; set; } = null!;

    public string IdAdministrativoR { get; set; } = null!;

    public virtual Reserva IdAdministrativoRNavigation { get; set; } = null!;

    public virtual Operador UsernameOpNavigation { get; set; } = null!;


    public OperadorReserva()
    {

    }

    public OperadorReserva(string usernameOp, string idAdministrativoR, Reserva idAdministrativoRNavigation, Operador usernameOpNavigation)
    {
        UsernameOp = usernameOp;
        IdAdministrativoR = idAdministrativoR;
        IdAdministrativoRNavigation = idAdministrativoRNavigation;
        UsernameOpNavigation = usernameOpNavigation;
    }
    
}

