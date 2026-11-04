using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Data;

public partial class VeterinariaContext : DbContext
{
    public VeterinariaContext(DbContextOptions<VeterinariaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Mascotas> Mascotas { get; set; }

    public virtual DbSet<Propietarios> Propietarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mascotas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mascotas_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();

            entity.HasOne(d => d.Propietario).WithMany(p => p.Mascotas)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_mascotas_propietarios");
        });

        modelBuilder.Entity<Propietarios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("propietarios_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
