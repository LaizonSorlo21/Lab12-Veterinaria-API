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
    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = null!;

    [Column("especie")]
    [Required(ErrorMessage = "La especie es obligatoria.")]
    [StringLength(40)]
    public string Especie { get; set; } = null!;

    [Column("edad")]
    [Range(0, 50, ErrorMessage = "La edad debe estar entre 0 y 50 años.")]
    public int Edad { get; set; }

    [Column("peso")]
    [Precision(6, 2)]
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "El peso debe estar entre 0.01 y 999.99 kg.")]
    public decimal Peso { get; set; }

    [Column("propietario_id")]
    [Range(1, int.MaxValue, ErrorMessage = "Indica un propietario válido.")]
    public int PropietarioId { get; set; }

    [ForeignKey("PropietarioId")]
    [InverseProperty("Mascotas")]
    public virtual Propietarios? Propietario { get; set; }
}
