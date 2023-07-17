using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Server.SistemaDB;

public partial class SistemaDbContext : DbContext
{
    public SistemaDbContext()
    {
    }

    public SistemaDbContext(DbContextOptions<SistemaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Operador> Operadors { get; set; }

    public virtual DbSet<OperadorReserva> OperadorReservas { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=(LocalDb)\\LocaldbTB2;Initial Catalog=SistemaDB;Integrated Security=True");

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    optionsBuilder.EnableSensitiveDataLogging();
    //    optionsBuilder.UseSqlServer("Data Source=(LocalDb)\\LocaldbTB2;Initial Catalog=SistemaDB;Integrated Security=True");
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Operador>(entity =>
        {
            entity.HasKey(e => e.Username);

            entity.ToTable("Operador");

            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Operadora)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<OperadorReserva>(entity =>
        {
            entity.HasKey(e => e.IdAdministrativoR);
                
            entity.ToTable("Operador_Reserva");

            entity.Property(e => e.IdAdministrativoR)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Id_Administrativo_R");
            entity.Property(e => e.UsernameOp)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Username_Op");

            entity.HasOne(d => d.IdAdministrativoRNavigation).WithMany()
                .HasForeignKey(d => d.IdAdministrativoR)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Operador___Id_Ad__3B75D760");

            entity.HasOne(d => d.UsernameOpNavigation).WithMany()
                .HasForeignKey(d => d.UsernameOp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Operador___Usern__3A81B327");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.IdAdministrativo);

            entity.ToTable("Reserva");

            entity.Property(e => e.IdAdministrativo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Id_Administrativo");
            entity.Property(e => e.DataReserva)
                .HasColumnType("smalldatetime")
                .HasColumnName("Data_Reserva");
            entity.Property(e => e.Domicilio)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Modalidade)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OpOrigem)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Operadora)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
