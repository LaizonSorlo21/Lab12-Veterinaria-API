$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$ownerPath = Join-Path $root 'Models/Propietarios.cs'
$petPath = Join-Path $root 'Models/Mascotas.cs'
$owner = Get-Content -LiteralPath $ownerPath -Raw
$pet = Get-Content -LiteralPath $petPath -Raw
# Se ejecuta después de Scaffold, que sobrescribe las personalizaciones manuales.
if ($owner -notmatch 'JsonIgnore') {
    $owner = $owner.Replace('using Microsoft.EntityFrameworkCore;', "using Microsoft.EntityFrameworkCore;`nusing System.Text.Json.Serialization;")
    $owner = $owner.Replace('[Column("nombre")]', '[Column("nombre")]' + "`n    [Required(ErrorMessage = `"El nombre del propietario es obligatorio.`")]")
    $owner = $owner.Replace('[Column("email")]', '[Column("email")]' + "`n    [Required(ErrorMessage = `"El correo es obligatorio.`")]`n    [EmailAddress(ErrorMessage = `"El correo no tiene un formato válido.`")]")
    $owner = $owner.Replace('[Column("telefono")]', '[Column("telefono")]' + "`n    [Required(ErrorMessage = `"El teléfono es obligatorio.`")]")
    $owner = $owner.Replace('[InverseProperty("Propietario")]', '[InverseProperty("Propietario")]' + "`n    [JsonIgnore]")
}
if ($pet -notmatch '\[Range') {
    $pet = $pet.Replace('[Column("nombre")]', '[Column("nombre")]' + "`n    [Required(ErrorMessage = `"El nombre de la mascota es obligatorio.`")]")
    $pet = $pet.Replace('[Column("especie")]', '[Column("especie")]' + "`n    [Required(ErrorMessage = `"La especie es obligatoria.`")]")
    $pet = $pet.Replace('[Column("edad")]', '[Column("edad")]' + "`n    [Range(0, 50, ErrorMessage = `"La edad debe estar entre 0 y 50 años.`")]")
    $pet = $pet.Replace('[Precision(6, 2)]', '[Precision(6, 2)]' + "`n    [Range(typeof(decimal), `"0.01`", `"999.99`", ErrorMessage = `"El peso debe estar entre 0.01 y 999.99 kg.`")]")
    $pet = $pet.Replace('[Column("propietario_id")]', '[Column("propietario_id")]' + "`n    [Range(1, int.MaxValue, ErrorMessage = `"Indica un propietario válido.`")]")
    $pet = $pet.Replace('public virtual Propietarios Propietario { get; set; } = null!;', 'public virtual Propietarios? Propietario { get; set; }')
}
[IO.File]::WriteAllText($ownerPath, $owner)
[IO.File]::WriteAllText($petPath, $pet)
Write-Output 'Validaciones, navegación nullable y JsonIgnore aplicados.'
