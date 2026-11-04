param(
    [string]$BaseUrl = 'https://lab12-veterinaria-api.onrender.com'
)
$ErrorActionPreference = 'Stop'
if ($BaseUrl -notmatch '^https://[^/]+$' -or $BaseUrl -match 'localhost|127\.0\.0\.1') {
    throw 'Usa la URL HTTPS pública de la API, sin barra final.'
}
Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(180)
$script:passed = 0
$script:ownerId = $null
$script:petId = $null
$script:secondPetId = $null

function Request([string]$Name, [string]$Method, [string]$Path, [int]$Expected, $Body = $null, $Headers = @{}) {
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), "$BaseUrl$Path")
    if ($null -ne $Body) {
        $json = ConvertTo-Json -InputObject $Body -Depth 10 -Compress
        $request.Content = [System.Net.Http.StringContent]::new($json, [Text.Encoding]::UTF8, 'application/json')
    }
    foreach ($key in $Headers.Keys) { $request.Headers.TryAddWithoutValidation($key, [string]$Headers[$key]) | Out-Null }
    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $status = [int]$response.StatusCode
    if ($status -ne $Expected) { throw "$Name : esperado $Expected, recibido $status. $text" }
    $data = if ([string]::IsNullOrWhiteSpace($text)) { $null } else { ConvertFrom-Json -InputObject $text }
    $script:passed++
    Write-Host ('OK {0:00} | {1} {2} | {3} | {4}' -f $script:passed, $Method, $Path, $status, $Name)
    return [PSCustomObject]@{ Data=$data; Response=$response; Text=$text }
}
function Check([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
}

try {
    $null = Request 'Base de datos en nube conectada' GET '/health' 200
    $swagger = Request 'Documento OpenAPI público' GET '/openapi/v1.json' 200
    Check (@($swagger.Data.paths.PSObject.Properties).Count -ge 5) 'Faltan rutas en OpenAPI.'
    $owners = Request 'Listado de propietarios' GET '/api/propietarios' 200
    $seedOwner = @($owners.Data | Where-Object { $_.email -eq 'ana.torres@example.com' })[0]
    Check ($null -ne $seedOwner) 'No se encontró el propietario de ejemplo del SQL.'
    $seedId = $seedOwner.id
    $null = Request 'Detalle de propietario' GET "/api/propietarios/$seedId" 200
    $filtered = Request 'Filtro de propietarios por nombre' GET '/api/propietarios?nombre=Ana' 200
    Check (@($filtered.Data).Count -ge 1) 'El filtro de nombre no devolvió datos.'
    $nested = Request 'Ruta anidada de mascotas' GET "/api/propietarios/$seedId/mascotas" 200
    Check (@($nested.Data).Count -ge 2) 'Faltan mascotas de ejemplo.'
    $pets = Request 'Listado con Include sin ciclo JSON' GET '/api/mascotas' 200
    $seedPet = @($pets.Data | Where-Object { $_.nombre -eq 'Luna' })[0]
    Check ($null -ne $seedPet.propietario) 'Include no devolvió al propietario.'
    Check ($null -eq $seedPet.propietario.PSObject.Properties['mascotas']) 'La colección debe ignorarse en JSON.'
    $null = Request 'Detalle de mascota' GET "/api/mascotas/$($seedPet.id)" 200
    $filteredPets = Request 'Filtros combinados de mascotas' GET "/api/mascotas?especie=Perro&propietarioId=$seedId&edadMin=1&edadMax=10" 200
    Check (@($filteredPets.Data).Count -ge 1) 'Los filtros combinados no devolvieron datos.'
    $null = Request 'Propietario inexistente' GET '/api/propietarios/2147483647' 404
    $null = Request 'Mascota inexistente' GET '/api/mascotas/2147483647' 404
    $null = Request 'Ruta anidada inexistente' GET '/api/propietarios/2147483647/mascotas' 404
    $null = Request 'Required y EmailAddress automáticos' POST '/api/propietarios' 400 @{nombre='';email='invalido';telefono='900000000'}
    $null = Request 'StringLength automático' POST '/api/propietarios' 400 @{nombre=('A' * 101);email='longitud@example.com';telefono='900000000'}
    $null = Request 'Range automático de edad y peso' POST '/api/mascotas' 400 @{nombre='Invalida';especie='Perro';edad=-1;peso=0;propietarioId=$seedId}
    $null = Request 'Clave foránea inexistente' POST '/api/mascotas' 400 @{nombre='Invalida';especie='Perro';edad=2;peso=5;propietarioId=2147483647}
    $null = Request 'Rango de filtros inválido' GET '/api/mascotas?edadMin=10&edadMax=1' 400
    $null = Request 'Correo duplicado' POST '/api/propietarios' 409 @{nombre='Duplicado';email='ANA.TORRES@EXAMPLE.COM';telefono='900000000'}
    $null = Request 'Eliminar propietario con dependientes' DELETE "/api/propietarios/$seedId" 409
    $tag = [Guid]::NewGuid().ToString('N').Substring(0,12)
    $owner = @{nombre="Prueba $tag";email="prueba.$tag@example.com";telefono='900123456'}
    $created = Request 'Crear propietario' POST '/api/propietarios' 201 $owner
    $script:ownerId = $created.Data.id
    Check ($script:ownerId -gt 0) 'El POST no devolvió el id.'
    Check ($created.Response.Headers.Location.AbsoluteUri -eq "$BaseUrl/api/Propietarios/$script:ownerId" -or $created.Response.Headers.Location.AbsoluteUri -eq "$BaseUrl/api/propietarios/$script:ownerId") 'Location del propietario incorrecta.'
    $null = Request 'Recurso de Location disponible' GET "/api/propietarios/$script:ownerId" 200
    $pet = @{nombre="Nube $tag";especie='Gato';edad=2;peso=4.25;propietarioId=$script:ownerId}
    $createdPet = Request 'Crear mascota enviando solo la FK' POST '/api/mascotas' 201 $pet
    $script:petId = $createdPet.Data.id
    Check ($createdPet.Response.Headers.Location.AbsoluteUri -match "/api/[Mm]ascotas/$script:petId$") 'Location de mascota incorrecta.'
    $null = Request 'Mascota duplicada del mismo propietario' POST '/api/mascotas' 409 $pet
    $pet.id = $script:petId
    $pet.peso = 4.75
    $null = Request 'Actualizar mascota' PUT "/api/mascotas/$script:petId" 204 $pet
    $updated = Request 'Comprobar actualización de mascota' GET "/api/mascotas/$script:petId" 200
    Check ($updated.Data.peso -eq 4.75) 'El peso actualizado no se guardó.'
    $owner.id = $script:ownerId
    $owner.nombre = "Actualizado $tag"
    $null = Request 'Actualizar propietario' PUT "/api/propietarios/$script:ownerId" 204 $owner
    $updatedOwner = Request 'Comprobar actualización de propietario' GET "/api/propietarios/$script:ownerId" 200
    Check ($updatedOwner.Data.nombre -eq $owner.nombre) 'El propietario actualizado no se guardó.'
    $null = Request 'Id de propietario distinto de la ruta' PUT '/api/propietarios/2147483647' 400 $owner
    $null = Request 'Id de mascota distinto de la ruta' PUT '/api/mascotas/2147483647' 400 $pet
    $invalidOwner = @{id=2147483647;nombre='Inexistente';email='inexistente@example.com';telefono='900000000'}
    $null = Request 'Actualizar propietario inexistente' PUT '/api/propietarios/2147483647' 404 $invalidOwner
    $invalidPet = @{id=2147483647;nombre='Inexistente';especie='Gato';edad=1;peso=1;propietarioId=$seedId}
    $null = Request 'Actualizar mascota inexistente' PUT '/api/mascotas/2147483647' 404 $invalidPet
    $owner.email = $seedOwner.email
    $null = Request 'Correo duplicado al actualizar' PUT "/api/propietarios/$script:ownerId" 409 $owner
    $pet.propietarioId = 2147483647
    $null = Request 'FK inexistente al actualizar' PUT "/api/mascotas/$script:petId" 400 $pet
    $pet.propietarioId = $script:ownerId
    $otherPet = @{nombre="Sol $tag";especie='Gato';edad=1;peso=3;propietarioId=$script:ownerId}
    $secondPet = Request 'Crear segunda mascota temporal' POST '/api/mascotas' 201 $otherPet
    $script:secondPetId = $secondPet.Data.id
    $otherPet.id = $script:secondPetId
    $otherPet.nombre = $pet.nombre
    $null = Request 'Nombre duplicado al actualizar mascota' PUT "/api/mascotas/$script:secondPetId" 409 $otherPet
    $allowed = Request 'Preflight CORS del origen permitido' OPTIONS '/api/mascotas' 204 $null @{Origin=$BaseUrl;'Access-Control-Request-Method'='POST'}
    Check ($allowed.Response.Headers.Contains('Access-Control-Allow-Origin')) 'CORS no permitió el origen concreto.'
    $denied = Request 'Preflight CORS de un origen ajeno' OPTIONS '/api/mascotas' 204 $null @{Origin='https://origen-no-permitido.example';'Access-Control-Request-Method'='POST'}
    Check (!$denied.Response.Headers.Contains('Access-Control-Allow-Origin')) 'CORS permitió un origen ajeno.'
    $null = Request 'Eliminar segunda mascota temporal' DELETE "/api/mascotas/$script:secondPetId" 204
    $script:secondPetId = $null
    $null = Request 'Eliminar mascota temporal' DELETE "/api/mascotas/$script:petId" 204
    $deletedPetId = $script:petId
    $script:petId = $null
    $null = Request 'Mascota eliminada devuelve 404' GET "/api/mascotas/$deletedPetId" 404
    $null = Request 'Eliminar propietario sin dependientes' DELETE "/api/propietarios/$script:ownerId" 204
    $deletedOwnerId = $script:ownerId
    $script:ownerId = $null
    $null = Request 'Propietario eliminado devuelve 404' GET "/api/propietarios/$deletedOwnerId" 404
    $null = Request 'Eliminar propietario inexistente' DELETE '/api/propietarios/2147483647' 404
    $null = Request 'Eliminar mascota inexistente' DELETE '/api/mascotas/2147483647' 404
    Write-Host "RESULTADO: $script:passed solicitudes correctas sobre $BaseUrl. Códigos 200, 201, 204, 400, 404 y 409 verificados."
} finally {
    # Solo limpia los registros ficticios creados por esta ejecución.
    foreach ($id in @($script:secondPetId, $script:petId)) {
        if ($id) { try { $null = $client.DeleteAsync("$BaseUrl/api/mascotas/$id").GetAwaiter().GetResult() } catch {} }
    }
    if ($script:ownerId) { try { $null = $client.DeleteAsync("$BaseUrl/api/propietarios/$script:ownerId").GetAwaiter().GetResult() } catch {} }
    $client.Dispose()
}
