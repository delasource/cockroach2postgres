namespace CockroachDbDataMigrator;

/// <summary>
/// This class handles argument parsing, then starts the Runner class.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        // Args must not contain additional spaces.
        // If you need a space in your connectionString, then specify an ENV variable.
        var argsParsed = args
            .Select(arg => arg.Split('='))
            .ToDictionary(arg => arg[0], arg => arg[1]);

        var sourceConnectionString = argsParsed.GetValueOrDefault("SRC") ??
                                     Environment.GetEnvironmentVariable("SRC");

        var destinationConnectionString = argsParsed.GetValueOrDefault("DEST") ??
                                          Environment.GetEnvironmentVariable("DEST");

        var tableName = argsParsed.GetValueOrDefault("TABLE") ??
                        Environment.GetEnvironmentVariable("TABLE");

        var batchSize = int.Parse(argsParsed.GetValueOrDefault("BATCH") ??
                                  Environment.GetEnvironmentVariable("BATCH") ??
                                  "50");

        if (batchSize < 2)
        {
            Console.WriteLine("BATCH must be at least 2.");
            Environment.Exit(1);
            return;
        }

        if (batchSize > 1000)
        {
            Console.WriteLine("BATCH must be at most 1000.");
            Environment.Exit(1);
            return;
        }

        if (string.IsNullOrWhiteSpace(sourceConnectionString) ||
            string.IsNullOrWhiteSpace(destinationConnectionString) ||
            string.IsNullOrWhiteSpace(tableName))
        {
            Console.WriteLine("Variables SRC, DEST, and TABLE must be set.");
            Console.WriteLine("You can set them either as ENV variables or as command line arguments, " +
                              "in KEY=VAL format. When using command line arguments, separate them with " +
                              "spaces and have the connection string NOT contain any spaces.");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine("Postgres to Postgres Data Migrator");
        Console.WriteLine("  Copyright 2024 @delasource");
        Console.WriteLine();
        Console.WriteLine("Source: " + sourceConnectionString[..sourceConnectionString.IndexOf(';')]);
        Console.WriteLine("Destination: " + destinationConnectionString[..destinationConnectionString.IndexOf(';')]);
        Console.WriteLine("Tables: " + tableName);
        Console.WriteLine("Batch Size: " + batchSize);
        Console.WriteLine();

        try
        {
            await new Runner(sourceConnectionString, destinationConnectionString, tableName, batchSize)
                .RunAsync();
        }
        catch (Exception)
        {
            Console.WriteLine("Last Query Statement:");
            Console.WriteLine(DatabaseHelper.LastQuery);
            Console.WriteLine();
            throw;
        }
    }
}
