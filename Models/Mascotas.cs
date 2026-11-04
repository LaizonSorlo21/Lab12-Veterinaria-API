using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Veterinaria.Api.Models;

[Table("mascotas")]
[Index("PropietarioId", "Nombre", Name = "uq_mascotas_propietario_nombre", IsUnique = true)]
public partial class Mascotas
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [StringLength(80)]
    public string Nombre { get; set; } = null!;

    [Column("especie")]
    [StringLength(40)]
    public string Especie { get; set; } = null!;

    [Column("edad")]
    public int Edad { get; set; }

    [Column("peso")]
    [Precision(6, 2)]
    public decimal Peso { get; set; }

    [Column("propietario_id")]
    public int PropietarioId { get; set; }

    [ForeignKey("PropietarioId")]
    [InverseProperty("Mascotas")]
    public virtual Propietarios Propietario { get; set; } = null!;
}
