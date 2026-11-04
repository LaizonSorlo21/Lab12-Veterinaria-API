using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MascotasController(VeterinariaContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Mascotas>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Mascotas>>> GetAll(
        [FromQuery] string? especie, [FromQuery] int? propietarioId,
        [FromQuery] int? edadMin, [FromQuery] int? edadMax, CancellationToken ct)
    {
        if (edadMin is < 0 or > 50 || edadMax is < 0 or > 50 || edadMin > edadMax || propietarioId is <= 0)
            return BadRequest(new { mensaje = "Revisa el propietario y el rango de edad (0 a 50)." });
        var query = db.Mascotas.AsNoTracking().Include(m => m.Propietario).AsQueryable();
        if (!string.IsNullOrWhiteSpace(especie))
            query = query.Where(m => m.Especie.ToLower() == especie.Trim().ToLower());
        if (propietarioId.HasValue) query = query.Where(m => m.PropietarioId == propietarioId);
        if (edadMin.HasValue) query = query.Where(m => m.Edad >= edadMin);
        if (edadMax.HasValue) query = query.Where(m => m.Edad <= edadMax);
        return Ok(await query.OrderBy(m => m.Id).ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Mascotas), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Mascotas>> GetById(int id, CancellationToken ct)
    {
        var mascota = await db.Mascotas.AsNoTracking().Include(m => m.Propietario)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
        return mascota is null ? NotFound() : Ok(mascota);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Mascotas), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Mascotas>> Create(Mascotas mascota, CancellationToken ct)
    {
        if (mascota.Id != 0) return BadRequest(new { mensaje = "El id se genera automáticamente; envía 0 u omítelo." });
        if (!await db.Propietarios.AnyAsync(p => p.Id == mascota.PropietarioId, ct))
            return BadRequest(new { mensaje = "El propietario indicado no existe." });
        mascota.Nombre = mascota.Nombre.Trim();
        mascota.Especie = mascota.Especie.Trim();
        if (await db.Mascotas.AnyAsync(m => m.PropietarioId == mascota.PropietarioId &&
            m.Nombre.ToLower() == mascota.Nombre.ToLower(), ct))
            return Conflict(new { mensaje = "Ese propietario ya tiene una mascota registrada con el mismo nombre." });
        mascota.Propietario = null;
        db.Mascotas.Add(mascota);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = mascota.Id }, mascota);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, Mascotas mascota, CancellationToken ct)
    {
        if (id != mascota.Id) return BadRequest(new { mensaje = "El id de la ruta debe coincidir con el cuerpo." });
        var actual = await db.Mascotas.FindAsync([id], ct);
        if (actual is null) return NotFound();
        if (!await db.Propietarios.AnyAsync(p => p.Id == mascota.PropietarioId, ct))
            return BadRequest(new { mensaje = "El propietario indicado no existe." });
        var nombre = mascota.Nombre.Trim();
        if (await db.Mascotas.AnyAsync(m => m.Id != id && m.PropietarioId == mascota.PropietarioId &&
            m.Nombre.ToLower() == nombre.ToLower(), ct))
            return Conflict(new { mensaje = "Ese propietario ya tiene una mascota registrada con el mismo nombre." });
        actual.Nombre = nombre;
        actual.Especie = mascota.Especie.Trim();
        actual.Edad = mascota.Edad;
        actual.Peso = mascota.Peso;
        actual.PropietarioId = mascota.PropietarioId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var mascota = await db.Mascotas.FindAsync([id], ct);
        if (mascota is null) return NotFound();
        db.Mascotas.Remove(mascota);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
