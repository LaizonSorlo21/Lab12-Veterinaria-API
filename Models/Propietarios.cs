using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Veterinaria.Api.Models;

[Table("propietarios")]
[Index("Email", Name = "uq_propietarios_email", IsUnique = true)]
public partial class Propietarios
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [Column("email")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [Column("telefono")]
    [StringLength(20)]
    public string Telefono { get; set; } = null!;

    [InverseProperty("Propietario")]
    public virtual ICollection<Mascotas> Mascotas { get; set; } = new List<Mascotas>();
}
