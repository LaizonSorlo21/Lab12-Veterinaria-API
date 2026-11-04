using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Veterinaria.Api.Models;

[Table("propietarios")]
[Index("Email", Name = "uq_propietarios_email", IsUnique = true)]
public partial class Propietarios
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required(ErrorMessage = "El nombre del propietario es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [Column("email")]
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [Column("telefono")]
    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = null!;

    [InverseProperty("Propietario")]
    [JsonIgnore]
    public virtual ICollection<Mascotas> Mascotas { get; set; } = new List<Mascotas>();
}
