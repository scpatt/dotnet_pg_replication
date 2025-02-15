using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;

var connectionString = "Host=localhost;Username=postgres;Database=logical_replication;Port=5432";
var slotName = "test_slot";

var slotExists = await Initialise(connectionString, slotName);

await using var conn = new LogicalReplicationConnection(connectionString);
await conn.Open();

PgOutputReplicationSlot? slot = null;

if (slotExists)
{
    slot = new PgOutputReplicationSlot(slotName);
}
else
{
    slot = await conn.CreatePgOutputReplicationSlot(slotName);
}

var cancellationTokenSource = new CancellationTokenSource();

await foreach (var message in conn.StartReplication(slot!, new PgOutputReplicationOptions("test_pub", PgOutputProtocolVersion.V1, false, PgOutputStreamingMode.Off), cancellationTokenSource.Token))
{
    Console.WriteLine($"Received message type: {message.GetType().Name}");

    conn.SetReplicationStatus(message.WalEnd);
}

async Task<bool> Initialise(string connectionString, string slotName)
{
    await using var pgConn = new NpgsqlConnection(connectionString);
    await pgConn.OpenAsync();

    var slotExists = false;

    await using (var command = new NpgsqlCommand("SELECT 1 FROM pg_replication_slots WHERE slot_name = @slotName", pgConn))
    {
        command.Parameters.AddWithValue("slotName", slotName);
        var result = await command.ExecuteScalarAsync();

        slotExists = result != null;
    }

    await using (var command = new NpgsqlCommand(@"
        CREATE TABLE IF NOT EXISTS test_table(
            id SERIAL PRIMARY KEY,
            data TEXT NOT NULL
        )", pgConn))
    {
        await command.ExecuteNonQueryAsync();
    }

    await using (var cmd = new NpgsqlCommand(@"
        DO $$ 
        BEGIN 
            IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'test_pub') THEN 
                CREATE PUBLICATION test_pub FOR TABLE test_table;
            END IF; 
        END $$", pgConn))
    {
        await cmd.ExecuteNonQueryAsync();
    }

    await pgConn.CloseAsync();

    return slotExists;
}