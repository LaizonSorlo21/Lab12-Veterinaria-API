# Laboratorio 12 — Web API REST de veterinaria

API desarrollada con ASP.NET Core .NET 10 y Entity Framework Core. Permite administrar propietarios y mascotas, consultar su relación y validar los datos antes de guardarlos. La API está publicada en Render y la base PostgreSQL está alojada en Neon.

## Enlaces de entrega

- **Repositorio:** https://github.com/LaizonSorlo21/Lab12-Veterinaria-API
- **API pública y Swagger:** https://lab12-veterinaria-api.onrender.com/swagger/index.html
- **OpenAPI:** https://lab12-veterinaria-api.onrender.com/openapi/v1.json
- **Comprobación de conexión con la base en la nube:** https://lab12-veterinaria-api.onrender.com/health
- **Hosting de la API:** Render, Web Service con Docker, plan Free.
- **Base de datos:** Neon, PostgreSQL, proyecto `lab12-veterinaria`, plan Free.

La comprobación funcional del 4 de noviembre de 2026 completó **44 solicitudes correctas sobre la API pública**, cubriendo los códigos **200, 201, 204, 400, 404 y 409**. No se generaron capturas ni archivos de evidencias; las capturas de la entrega debes tomarlas tú siguiendo esta guía.

## Explicación del tema

Elegí una veterinaria porque necesita organizar los datos de sus clientes y de las mascotas que atiende. La API resuelve el registro, la consulta, la modificación y la eliminación de estos datos. Además, permite buscar mascotas por especie, propietario y rango de edad.

| Entidad | Campos | Función |
| --- | --- | --- |
| Propietario | Id, Nombre, Email, Telefono | Representa al cliente responsable de las mascotas. |
| Mascota | Id, Nombre, Especie, Edad, Peso, PropietarioId | Representa a una mascota y guarda la FK de su propietario. |

La relación es **Propietario 1 → N Mascotas**: un propietario puede tener varias mascotas y cada mascota pertenece a un propietario. `propietario_id` es una FK obligatoria. La propiedad de navegación `Propietario` admite nulo para que el POST de mascotas solo necesite enviar `propietarioId`.

La colección `Propietarios.Mascotas` tiene `[JsonIgnore]`. Así, cuando `Include` carga al propietario de una mascota, la serialización no vuelve a recorrer la colección y evita ciclos JSON. No se emplean DTOs.

### Reglas de negocio con respuesta 409

1. No se puede repetir el correo de un propietario. La validación normaliza espacios y mayúsculas/minúsculas.
2. No se puede eliminar un propietario que todavía tenga mascotas registradas.
3. Un mismo propietario no puede registrar dos mascotas con el mismo nombre, sin distinguir mayúsculas/minúsculas.

Las reglas se verifican con LINQ y también existen restricciones e índices en PostgreSQL. Los conflictos de unicidad o integridad que ocurran entre la consulta y la escritura también se controlan.

## Endpoints implementados

Las rutas no distinguen mayúsculas/minúsculas. Swagger las muestra con los nombres de los controladores.

| Método | Ruta | Resultado correcto | Otros resultados |
| --- | --- | --- | --- |
| GET | `/api/propietarios` | 200 | Filtro opcional `nombre` |
| GET | `/api/propietarios/{id}` | 200 | 404 |
| POST | `/api/propietarios` | 201 y cabecera Location | 400, 409 |
| PUT | `/api/propietarios/{id}` | 204 | 400, 404, 409 |
| DELETE | `/api/propietarios/{id}` | 204 | 404, 409 |
| GET | `/api/propietarios/{id}/mascotas` | 200 | 404 |
| GET | `/api/mascotas` | 200 | 400 para filtros inválidos |
| GET | `/api/mascotas/{id}` | 200 | 404 |
| POST | `/api/mascotas` | 201 y cabecera Location | 400, 409 |
| PUT | `/api/mascotas/{id}` | 204 | 400, 404, 409 |
| DELETE | `/api/mascotas/{id}` | 204 | 404 |

Ejemplo de filtros combinados: `/api/mascotas?especie=Perro&propietarioId=1&edadMin=1&edadMax=10`.

Todos los endpoints de las entidades están en controladores con `[ApiController]` y `api/[controller]`. Las acciones son asíncronas y acceden directamente al DbContext mediante `ToListAsync`, `FindAsync`, `AnyAsync`, `FirstOrDefaultAsync`, `Include`, `AsNoTracking` y consultas LINQ.

Las entidades contienen `[Required]`, `[StringLength]` y `[Range]`; además, el correo usa `[EmailAddress]`. `[ApiController]` responde automáticamente 400 cuando falla la validación. Los POST devuelven `CreatedAtAction`, los GET usan `Ok` y las modificaciones y eliminaciones correctas usan `NoContent`.

## Elección de servicios gratuitos

Se revisaron estas opciones con la documentación de sus proveedores el 4 de noviembre de 2026:

| Alternativa | Evaluación y elección |
| --- | --- |
| Render Free para la API | Permite ejecutar la aplicación .NET mediante Docker y publica una URL HTTPS. Se eligió para alojar la API. Se suspende tras 15 minutos sin tráfico y el siguiente inicio puede tardar aproximadamente un minuto. [Documentación de Render](https://render.com/docs/free). |
| Render Free para PostgreSQL | Permitiría tener ambos servicios en Render, pero la base gratuita vence 30 días después de su creación. Por ese límite no se eligió para los datos del laboratorio. [Límites de Render](https://render.com/docs/free). |
| Neon Free para PostgreSQL | Ofrece PostgreSQL sin límite de tiempo del plan gratuito, sujeto a sus cuotas. Se eligió para mantener la base separada del contenedor y conservar los datos. [Planes de Neon](https://neon.com/pricing). |

Se cambió SQL Server por PostgreSQL usando **`Npgsql.EntityFrameworkCore.PostgreSQL`** y **`UseNpgsql`**. El SQL usa `GENERATED ALWAYS AS IDENTITY`, `varchar`, `numeric`, claves foráneas e índices compatibles con PostgreSQL. La base está en Neon; la API publicada no depende de una base local.

## Base de datos primero y Scaffold

El archivo `database/01_veterinaria.sql` crea las dos tablas, sus restricciones y los datos ficticios de ejemplo. No contiene contraseñas ni borra los datos existentes. Sus inserciones evitan duplicar los ejemplos cuando se vuelve a ejecutar.

Se ejecutó el script en Neon y después se generaron `Models/Mascotas.cs`, `Models/Propietarios.cs` y `Data/VeterinariaContext.cs` mediante Scaffold. La generación quedó registrada en [GitHub Actions](https://github.com/LaizonSorlo21/Lab12-Veterinaria-API/actions/runs/41918961297).

Comando de generación, con la conexión ya configurada mediante secretos:

```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:Veterinaria" Npgsql.EntityFrameworkCore.PostgreSQL --context VeterinariaContext --context-dir Data --output-dir Models --table propietarios --table mascotas --data-annotations --no-onconfiguring --no-pluralize --force
powershell -NoProfile -File tools/Apply-Validations.ps1
```

El segundo comando restaura las validaciones, `[JsonIgnore]` y la navegación nullable después de regenerar el modelo. El workflow `database-first.yml` incluye ambos pasos y compila el resultado. La generación se realiza desde un ejecutor en la nube porque la conexión TCP directa desde esta PC a Neon agotó el tiempo de espera. Referencia técnica: [Scaffolding en EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/).

## Configuración y despliegue

`Dockerfile` compila con el SDK de .NET 10 y ejecuta la API en una imagen ASP.NET 10. El contenedor escucha en el puerto 8080. `render.yaml` describe la configuración del servicio gratuito y su comprobación `/health`.

La variable **`ConnectionStrings__Veterinaria`** está configurada en Render. La conexión también está almacenada como secreto de GitHub Actions bajo **`VETERINARIA_DB`**. En desarrollo se usa Secret Manager. El código y el repositorio no contienen el valor de la conexión.

CORS permite únicamente el origen concreto **`https://lab12-veterinaria-api.onrender.com`**. Se puede cambiar mediante `Cors__AllowedOrigin` para un futuro cliente HTTPS. No se usa `AllowAnyOrigin`. Swagger y el documento OpenAPI están habilitados en producción.

Para compilar localmente:

```powershell
dotnet restore
dotnet build -c Release
```

Para ejecutar localmente se necesita configurar la conexión en Secret Manager o en `ConnectionStrings__Veterinaria` y tener acceso de red a Neon. Después se puede ejecutar `dotnet run --launch-profile https`. Para las capturas del laboratorio, usa siempre la API pública.

## Pruebas y cómo repetirlas

El archivo **`tests/Test-Api.ps1`** comprueba los once endpoints, los seis códigos solicitados, las validaciones, los conflictos de POST y PUT, las cabeceras Location, la persistencia de cambios, la relación sin ciclos JSON y CORS. Crea datos ficticios con identificadores únicos y al terminar elimina únicamente los registros que creó esa ejecución. No guarda capturas ni reportes.

Desde la carpeta del proyecto:

```powershell
powershell -NoProfile -File tests/Test-Api.ps1
```

El resultado esperado es `RESULTADO: 44 solicitudes correctas ...`. Se comprobó también la compilación Release sin errores ni advertencias y la consulta de paquetes no encontró vulnerabilidades conocidas en los orígenes consultados.

**`Veterinaria.Api.http`** contiene 37 solicitudes para ejecutarlas individualmente en un cliente compatible. Las solicitudes `createOwner` y `createPet` permiten reutilizar sus ids con REST Client. Si tu editor no interpreta estas variables, copia los ids del JSON recibido o usa Swagger.

## README de las fotos que debes tomar

Abre [Swagger de la API publicada](https://lab12-veterinaria-api.onrender.com/swagger/index.html). Para cada operación, pulsa **Try it out**, completa los campos y pulsa **Execute**. La foto debe mostrar la **URL pública**, el **método y la ruta**, los **datos enviados cuando correspondan** y la sección **Server response** con el **código real** y el cuerpo. La lista de respuestas documentadas de Swagger, por sí sola, no demuestra que ejecutaste una solicitud.

Para una respuesta 201, muestra también **Response headers → Location**. Para una respuesta 204, muestra el código aunque el cuerpo esté vacío. Si una operación ocupa más de una pantalla, toma dos fotos para que se puedan leer la solicitud y la respuesta. No muestres contraseñas ni el valor de la conexión.

### Fotos de contexto y configuración

| Foto | Qué mostrar |
| --- | --- |
| A | Repositorio de GitHub con `Controllers`, `Models`, `Data`, `database`, `Dockerfile` y README. |
| B | Script SQL con las tablas, FK, índices y datos de ejemplo. |
| C | Workflow de Scaffold finalizado correctamente y el comando usado. |
| D | Modelos en el editor: Data Annotations, `Propietarios? Propietario` y `[JsonIgnore]`. |
| E | Controladores con `[ApiController]`, `api/[controller]` y ejemplos de consultas LINQ asíncronas. |
| F | `Program.cs`: `UseNpgsql`, origen concreto de CORS y OpenAPI/Swagger habilitados. |
| G | Panel de Render mostrando `lab12-veterinaria-api`, despliegue exitoso y plan Free. |
| H | Panel de Neon mostrando el proyecto del laboratorio y sus dos tablas con datos. |
| I | Swagger con la URL pública y ambos grupos de controladores. |
| J | `/health` público devolviendo `{"estado":"ok","baseDeDatos":"PostgreSQL"}`. |

### Orden de fotos para probar cada endpoint y los seis códigos

Los propietarios y mascotas de ejemplo pueden consultarse libremente. Para los PUT y DELETE correctos, usa los registros temporales que crearás en los pasos 8 y 9. Los ids se generan automáticamente: **P** significa el id del propietario creado por ti y **M** el id de su mascota. No envíes las letras P o M: reemplázalas por los números recibidos.

| Paso | Solicitud y preparación | Código de la foto |
| --- | --- | --- |
| 1 | GET `/api/propietarios` | 200 |
| 2 | GET `/api/propietarios/1` | 200 |
| 3 | GET `/api/propietarios?nombre=Ana` | 200 |
| 4 | GET `/api/propietarios/1/mascotas` | 200 |
| 5 | GET `/api/mascotas`; muestra el propietario incluido sin ciclos | 200 |
| 6 | GET `/api/mascotas/{id}` con un id del listado anterior | 200 |
| 7 | GET `/api/mascotas?especie=Perro&propietarioId=1&edadMin=1&edadMax=10` | 200 |
| 8 | POST `/api/propietarios` con el cuerpo A; anota su id como P y muestra Location | 201 |
| 9 | POST `/api/mascotas` con el cuerpo B, reemplazando P; anota su id como M y muestra Location | 201 |
| 10 | Repite el POST de la misma mascota del paso 9 | 409 |
| 11 | PUT `/api/propietarios/P` con el cuerpo C y luego un GET del mismo propietario | 204 y 200 |
| 12 | PUT `/api/mascotas/M` con el cuerpo D y luego un GET de la misma mascota | 204 y 200 |
| 13 | POST propietario con `nombre` vacío y un correo inválido, como en el .http | 400 |
| 14 | POST mascota con `edad: -1` y `peso: 0` | 400 |
| 15 | POST mascota válida, pero con `propietarioId: 2147483647` | 400 |
| 16 | PUT propietario: ruta con id `2147483647`, pero `id: P` en el cuerpo válido | 400 |
| 17 | PUT mascota: ruta con id `2147483647`, pero `id: M` en el cuerpo válido | 400 |
| 18 | GET `/api/propietarios/2147483647` y GET `/api/mascotas/2147483647` | 404 en ambos |
| 19 | GET `/api/propietarios/2147483647/mascotas` | 404 |
| 20 | POST propietario con email `ana.torres@example.com` y los otros campos válidos | 409 |
| 21 | DELETE `/api/propietarios/1`, que tiene mascotas | 409 |
| 22 | GET `/api/mascotas?edadMin=10&edadMax=1` | 400 |
| 23 | PUT de cada entidad con id `2147483647` tanto en la ruta como en un cuerpo válido | 404 en ambos |
| 24 | DELETE `/api/mascotas/M`; después GET del mismo id | 204 y 404 |
| 25 | DELETE `/api/propietarios/P`; después GET del mismo id | 204 y 404 |
| 26 | DELETE `/api/propietarios/2147483647` y DELETE `/api/mascotas/2147483647` | 404 en ambos |

Los pasos 24 y 25 limpian tus datos de prueba. Elimina primero la mascota para que el propietario quede sin dependientes.

**Cuerpo A — crear propietario.** Si ya usaste este correo, cambia el número antes de repetirlo.

```json
{"nombre":"Pedro de prueba","email":"pedro.captura.01@example.com","telefono":"900123456"}
```

**Cuerpo B — crear mascota.** Sustituye `123` por P. Envía solo la FK, sin objeto `propietario`.

```json
{"nombre":"Nube de prueba","especie":"Gato","edad":2,"peso":4.25,"propietarioId":123}
```

**Cuerpo C — actualizar propietario.** Sustituye `123` por P también en la ruta.

```json
{"id":123,"nombre":"Pedro actualizado","email":"pedro.captura.01@example.com","telefono":"900987654"}
```

**Cuerpo D — actualizar mascota.** Sustituye `456` por M y `123` por P.

```json
{"id":456,"nombre":"Nube de prueba","especie":"Gato","edad":3,"peso":4.75,"propietarioId":123}
```

### Fotos adicionales de las 44 verificaciones automáticas

Ejecuta `tests/Test-Api.ps1` en la terminal del editor y toma las fotos necesarias para leer todas las líneas `OK 01` hasta `OK 44`, junto con la URL pública del comando o del resultado final. Incluye el mensaje final de las 44 solicitudes correctas. Así documentas también StringLength, conflictos al actualizar, limpieza de datos y los dos casos de CORS que amplían las pruebas manuales de la tabla.

Si documentas CORS en el cliente .http, fotografía tanto la solicitud OPTIONS permitida con `Access-Control-Allow-Origin` como la solicitud de un origen ajeno sin esa cabecera. Una respuesta OPTIONS 204 por sí sola no demuestra que se permitió el origen.

## Cinco observaciones

1. **Observé** que `[ApiController]` devuelve 400 automáticamente cuando envío un nombre vacío, un correo inválido o valores fuera del rango permitido. Esto me ayudó a reconocer cómo las Data Annotations validan una solicitud antes de ejecutar la acción.
2. **Noté** que el POST de mascotas funciona enviando únicamente `propietarioId`, porque la propiedad de navegación admite nulo. También observé que un propietario inexistente produce una respuesta 400.
3. **Comprobé** que las reglas de negocio responden con 409 cuando repito un correo, repito el nombre de una mascota del mismo propietario o intento eliminar un propietario con mascotas.
4. **Observé** que las consultas con `Include` muestran al propietario de cada mascota y que `[JsonIgnore]` evita que el JSON vuelva a recorrer la colección de mascotas.
5. **Identifiqué** que la API y la base de datos funcionan en servicios diferentes de la nube. La conexión directa desde la PC agotó el tiempo de espera, por lo que ejecuté el SQL y Scaffold desde GitHub Actions y comprobé la aplicación desde su URL pública.

## Cinco conclusiones

1. **Aprendí** a desarrollar una Web API REST con controladores en ASP.NET Core .NET 10 y a utilizar GET, POST, PUT y DELETE para administrar dos entidades relacionadas.
2. **Comprendí** el enfoque de base de datos primero: primero creé las tablas y los datos con SQL, después generé las entidades y el DbContext con Scaffold, y finalmente agregué las validaciones necesarias.
3. **Aprendí** a diferenciar los códigos 200, 201, 204, 400, 404 y 409 según el resultado de una operación. Las pruebas sobre la API publicada me permitieron comprobar tanto los casos correctos como los errores esperados.
4. **Comprendí** que Entity Framework Core puede trabajar con distintos proveedores. En este laboratorio adapté el script a PostgreSQL y utilicé el paquete Npgsql junto con `UseNpgsql` para conectar la API con Neon en lugar de SQL Server.
5. **Concluí** que el despliegue incluye la aplicación, la persistencia de datos y la configuración. Aprendí a publicar la API en Render, mantener la base en Neon y guardar la conexión como secreto o variable de entorno, fuera del repositorio.

## Archivos principales

| Ruta | Contenido |
| --- | --- |
| `Controllers/` | CRUD de propietarios y mascotas, filtros y ruta anidada. |
| `Models/` | Entidades generadas con Scaffold y validadas con anotaciones. |
| `Data/VeterinariaContext.cs` | DbContext generado desde PostgreSQL. |
| `database/01_veterinaria.sql` | Tablas, relación, reglas y datos de ejemplo. |
| `Program.cs` | Proveedor EF Core, CORS, OpenAPI, Swagger y control de errores de integridad. |
| `Dockerfile` y `render.yaml` | Configuración del alojamiento. |
| `Veterinaria.Api.http` | Solicitudes individuales contra la URL pública. |
| `tests/Test-Api.ps1` | Prueba funcional de 44 solicitudes sin generar evidencias. |
| `tools/DbSetup/` | Herramienta para ejecutar el SQL mediante una conexión suministrada por secreto. |
| `tools/Apply-Validations.ps1` | Restaura validaciones y configuración JSON después de Scaffold. |
| `.github/workflows/` | Generación del modelo y compilación del proyecto. |
