using Npgsql;
await using var conn = new NpgsqlConnection(Environment.GetEnvironmentVariable("LAB12_DB"));
await conn.OpenAsync();
await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(args[0]), conn);
await command.ExecuteNonQueryAsync();
Console.WriteLine("Script SQL ejecutado en PostgreSQL de Neon.");
