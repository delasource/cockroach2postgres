using System.Data;

namespace CockroachDbDataMigrator;

public static class DatabaseHelper
{
    public static string LastQuery { get; private set; } = "";

    private static bool _notifiedDifferentPrimaryKey;

    public static string[] GetTableNames(IDbConnection connection, string schema = "public")
    {
        var query = @$"
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = '{schema}'
            ORDER BY table_name";

        var tableNames = new List<string>();

        using var command = connection.CreateCommand();
        command.CommandText = query;
        LastQuery           = command.CommandText;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames.ToArray();
    }

    public static string[] GetColumnNames(IDbConnection connection, string schema, string tableName)
    {
        var query = @$"
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = '{tableName}'
            AND table_schema = '{schema}'
            ORDER BY ordinal_position";

        var columnNames = new List<string>();

        using var command = connection.CreateCommand();
        command.CommandText = query;
        LastQuery           = command.CommandText;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columnNames.Add(reader.GetString(0));
        }

        return columnNames.ToArray();
    }

    public static DataTable FetchRows(IDbConnection connection,
                                      string        tableName,
                                      string[]      columns,
                                      long          offset,
                                      int           limit)
    {
        var primaryKeyColumnName = columns[0].Trim('"');
        if (primaryKeyColumnName != "Id" && !primaryKeyColumnName.ToLowerInvariant().EndsWith("id"))
        {
            // If the very first column appears not to be a primary key (i.e. name ends with -Id)
            // try to select the next best Id column.
            // -> We need a consistent orderable column, so that Limit and Offset work.
            primaryKeyColumnName = columns.FirstOrDefault(c => c.ToLowerInvariant().EndsWith("id")) ??
                                   primaryKeyColumnName;

            if (!_notifiedDifferentPrimaryKey)
            {
                Console.WriteLine("Warning:");
                Console.WriteLine($"The first column of table {tableName} does not appear to be a primary key. " +
                                  $"Using \"{primaryKeyColumnName}\" instead. This is required for a consistent " +
                                  $"ORDER-BY-statement for proper batching. If you think the selected column is " +
                                  $"invalid for proper ordering, consider altering the information schema of your " +
                                  $"table, so that the primary key of the table comes in first position and " +
                                  $"ends with 'id'.");
                Console.WriteLine();
                _notifiedDifferentPrimaryKey = true;
            }
        }

        primaryKeyColumnName = $"\"{primaryKeyColumnName}\"";
        var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));

        using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT {columnList}
            FROM {tableName}
            ORDER BY {primaryKeyColumnName}
            LIMIT @Limit OFFSET @Offset";
        LastQuery = command.CommandText;

        var limitParam = command.CreateParameter();
        limitParam.ParameterName = "@Limit";
        limitParam.Value         = limit;
        command.Parameters.Add(limitParam);

        var offsetParam = command.CreateParameter();
        offsetParam.ParameterName = "@Offset";
        offsetParam.Value         = offset;
        command.Parameters.Add(offsetParam);

        var dataTable = new DataTable();

        using var reader = command.ExecuteReader();
        dataTable.Load(reader);

        return dataTable;
    }

    public static void InsertRows(IDbConnection connection,
                                  string        tableName,
                                  DataTable     rows,
                                  string[]      columns)
    {
        var columnList        = string.Join(", ", columns.Select(c => $"\"{c}\""));
        var valuePlaceholders = string.Join(", ", columns.Select(c => $"@{c}"));

        var query = $@"
            INSERT INTO {tableName} ({columnList})
            VALUES ({valuePlaceholders})
            ON CONFLICT DO NOTHING";

        using var transaction = connection.BeginTransaction();
        foreach (DataRow row in rows.Rows)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = query;
            LastQuery           = command.CommandText;

            foreach (var column in columns)
            {
                command.Parameters.Add(CreateParameter(command, $"@{column}", row[column]));
            }

            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static IDbDataParameter CreateParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value         = value ?? DBNull.Value;
        return parameter;
    }
}
