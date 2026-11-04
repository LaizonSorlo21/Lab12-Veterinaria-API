using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropietariosController(VeterinariaContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Propietarios>>> GetAll([FromQuery] string? nombre, CancellationToken ct)
    {
        var query = db.Propietarios.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(p => p.Nombre.ToLower().Contains(nombre.Trim().ToLower()));
        return Ok(await query.OrderBy(p => p.Id).ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Propietarios>> GetById(int id, CancellationToken ct)
    {
        var propietario = await db.Propietarios.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return propietario is null ? NotFound() : Ok(propietario);
    }

    [HttpGet("{id:int}/mascotas")]
    public async Task<ActionResult<IEnumerable<Mascotas>>> GetMascotas(int id, CancellationToken ct)
    {
        if (!await db.Propietarios.AnyAsync(p => p.Id == id, ct)) return NotFound();
        return Ok(await db.Mascotas.AsNoTracking().Where(m => m.PropietarioId == id)
            .OrderBy(m => m.Id).ToListAsync(ct));
    }

    [HttpPost]
    public async Task<ActionResult<Propietarios>> Create(Propietarios propietario, CancellationToken ct)
    {
        if (propietario.Id != 0) return BadRequest(new { mensaje = "El id se genera automáticamente; envía 0 u omítelo." });
        propietario.Nombre = propietario.Nombre.Trim();
        propietario.Email = propietario.Email.Trim().ToLowerInvariant();
        if (await db.Propietarios.AnyAsync(p => p.Email.ToLower() == propietario.Email, ct))
            return Conflict(new { mensaje = "Ya existe un propietario con ese correo." });
        db.Propietarios.Add(propietario);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = propietario.Id }, propietario);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Propietarios propietario, CancellationToken ct)
    {
        if (id != propietario.Id) return BadRequest(new { mensaje = "El id de la ruta debe coincidir con el cuerpo." });
        var actual = await db.Propietarios.FindAsync([id], ct);
        if (actual is null) return NotFound();
        var email = propietario.Email.Trim().ToLowerInvariant();
        if (await db.Propietarios.AnyAsync(p => p.Id != id && p.Email.ToLower() == email, ct))
            return Conflict(new { mensaje = "Ya existe un propietario con ese correo." });
        actual.Nombre = propietario.Nombre.Trim();
        actual.Email = email;
        actual.Telefono = propietario.Telefono.Trim();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var propietario = await db.Propietarios.FindAsync([id], ct);
        if (propietario is null) return NotFound();
        if (await db.Mascotas.AnyAsync(m => m.PropietarioId == id, ct))
            return Conflict(new { mensaje = "No se puede eliminar un propietario que tiene mascotas registradas." });
        db.Propietarios.Remove(propietario);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
