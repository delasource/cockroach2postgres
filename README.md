# CockroachDB to Postgres Data Migrator

This project aims to provide a simple yet complete functionality to transfer data from a CockroachDB instance 
into a Postgres server, because CockroachDB lacks any export or dump functionality and makes it hard to
move away.

This app connects to two postgres-compatible servers, reads batches of data from one and inserts them into another server. 
Both use the Npgsql library, so it works with any postgres-compatible database.

## Features 🎉

- **Dynamic Column Mapping**: Automatically fetches table schema to copy all columns without hardcoding.
- **Batch Processing**: Handles large datasets efficiently by processing rows in batches. Works great with binary data!
- **Conflict Resolution**: Skips rows with existing primary keys to avoid insertion errors.
- **Configurable**: Simplifies setup for your environment.
- **Built with [dotnet 8](https://dotnet.microsoft.com/en-us/download)**: So it's fast and robust.

---

## Run 🚀

You can either directly run the docker script from [here](https://hub.docker.com/r/delasrc/cockroach2postgres)
or build it from source.

### Example docker run command:

   ```bash
   docker run --rm --name cockroach2postgres \
       -e SRC="Host=cockroachdb;Port=26257;Database=source_db;Username=user;Password=pass" \
       -e DEST="Host=postgresdb;Port=5432;Database=dest_db;Username=user;Password=pass" \
       -e TABLE="MyTable" \
       --network captain-overlay-network \
       delasrc/cockroach2postgres
   ```

_In this example, I'm connecting the container to the "captain-overlay-network" because my two databases run inside
CapRover. Adjust to your requirements._

---

## Configuration 🔧

You can either specify environment variables, or commandline arguments.
Commandline arguments must not contain additional spaces in your connection strings!
If you need a space in your connection string, then specify an ENV variable.

### Parameter 

| Variable | Description                                      | Example                                                                      | Required |
|----------|--------------------------------------------------|------------------------------------------------------------------------------|----------|
| `SRC`    | Connection string for the source CockroachDB     | `Host=cockroachdb;Port=26257;Database=source_db;Username=user;Password=pass` | yes      |
| `DEST`   | Connection string for the destination PostgreSQL | `Host=postgresdb;Port=5432;Database=dest_db;Username=user;Password=pass`     | yes      |
| `TABLE`  | Name of the table to migrate (see Note below)    | `MyTable`                                                                    | yes      |
| `BATCH`  | Number of results to copy at a time              | `50` (default)                                                               | optional |

Note on `TABLE` variable:

- Format: `simple string` => expects the public schema, results in queries with `"public"."simple string"`
- Format: `public.mytable` => a dot represents a specific schema, results in queries with `"public"."mytable"`
  (you must only have one dot)
- Format: `public.*` => performs a lookup of all tables in the public schema (same as just `*`, see rule 1)

Wrapping the names in square brackets, double quotes, or single quotes is optional. We do it for you.

You can specifiy **multiple tables** at once, separated by commas. Example:
`public.table1, public.table2, table3, table4`

---

## Permissions 🛡️

The database user you are accessing the source database with has to have read access to the tables in question,
as well as `information_schema.columns` and `information_schema.tables`.
On the target database, the user must have INSERT-access to the tables in question.

> ❕ We only transfer data, we do not create the tables for you ❕

In case of a conflict (of any primary or unique key) the row is silently ignored.



---

## License 📜

This project is licensed under the [MIT License](LICENSE).

---

## Contributions 🤝

Feel free to fork this repository and submit pull requests. Contributions are welcome!

---

## Author 👤

- **Dela**
  - GitHub: [@delasource](https://github.com/delasource)
  - X: [@denkbox](https://x.com/denkbox)
