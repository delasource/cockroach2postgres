using Npgsql;

namespace CockroachDbDataMigrator;

internal class Runner(
    string sourceConnectionString,
    string destinationConnectionString,
    string tableNameParam,
    int    batchSize
)
    : IDisposable, IAsyncDisposable
{
    private readonly NpgsqlConnection _sourceConnection = new(sourceConnectionString);
    private readonly NpgsqlConnection _targetConnection = new(destinationConnectionString);

    public async Task RunAsync(CancellationToken ct = default)
    {
        var tableIdentifierStrings = new List<string>();
        var exactTableIdentifiers  = new List<(string schema, string tableName)>();

        if (tableNameParam.Contains(','))
        {
            // Array
            tableIdentifierStrings.AddRange(tableNameParam.Split(',').Select(se => se.Trim()));
        }
        else
        {
            tableIdentifierStrings.Add(tableNameParam);
        }


        foreach (var tIds in tableIdentifierStrings)
        {
            string schema, tableName;
            if (tIds.Contains('.'))
            {
                schema    = tIds.Split('.')[0].Trim('[', ']', ' ', '"', '\'');
                tableName = tIds.Split('.')[1].Trim('[', ']', ' ', '"', '\'');
            }
            else
            {
                schema    = "public";
                tableName = tIds.Trim('[', ']', ' ', '"', '\'');
            }

            if (tableName == "*")
            {
                // Introspect schema
                var tableNames = DatabaseHelper.GetTableNames(_sourceConnection, schema);
                foreach (var tn in tableNames)
                {
                    exactTableIdentifiers.Add((schema, tn));
                }
            }
            else
            {
                exactTableIdentifiers.Add((schema, tableName));
            }
        }

        // Open connections (may fail)
        await _sourceConnection.OpenAsync(ct);
        await _targetConnection.OpenAsync(ct);

        foreach (var tableIdentifier in exactTableIdentifiers)
        {
            ProcessSingleTable(tableIdentifier.schema, tableIdentifier.tableName, ct);
        }
    }

    private void ProcessSingleTable(string schema, string tableName, CancellationToken ct = default)
    {
        string tableIdentifier = $"\"{schema}\".\"{tableName}\"";
        Console.WriteLine($"Starting data transfer of from {tableIdentifier} with these columns:");

        // Introspect table
        var columns = DatabaseHelper.GetColumnNames(_sourceConnection, schema, tableName);
        foreach (var column in columns)
        {
            Console.WriteLine($"  > \"{column}\"");
        }

        Console.WriteLine();

        long offset = 0;
        while (true)
        {
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("Data transfer was cancelled.");
                return;
            }

            Console.WriteLine($"Fetching {offset} - {offset + batchSize}...");

            var rows = DatabaseHelper.FetchRows(_sourceConnection, tableIdentifier, columns, offset, batchSize);
            if (rows.Rows.Count == 0)
            {
                Console.WriteLine("All data has been transferred.");
                break;
            }

            Console.WriteLine("Inserting...");
            DatabaseHelper.InsertRows(_targetConnection, tableIdentifier, rows, columns);

            offset += batchSize;
        }

        Console.WriteLine($"Data transfer of table {tableIdentifier} completed.");
        Console.WriteLine();
    }

    public void Dispose()
    {
        _sourceConnection.Close();
        _targetConnection.Close();
        _sourceConnection.Dispose();
        _targetConnection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _sourceConnection.CloseAsync();
        await _targetConnection.CloseAsync();
        await _sourceConnection.DisposeAsync();
        await _targetConnection.DisposeAsync();
    }
}
