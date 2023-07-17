using System;
using System.Collections.Generic;

namespace Server.SistemaDB;

public partial class Operador
{
    public string Username { get; set; } = null!;

    public string? Operadora { get; set; }

    public string Password { get; set; } = null!;

    public bool Admin { get; set; }

    public Operador()
    {

    }

    public Operador(string _username, string _operadora, string _password, bool _admin)
    {
        Username = _username;
        Operadora = _operadora;
        Password = _password;
        Admin = _admin;
    }
}
