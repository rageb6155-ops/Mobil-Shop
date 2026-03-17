using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string localConnStr = "Server=localhost;Database=MobileShopDB;Trusted_Connection=True;TrustServerCertificate=True;";
        string remoteConnStr = "workstation id=Mobile-shop.mssql.somee.com;packet size=4096;user id=mohamedragab222_SQLLogin_1;pwd=9wa9cyofwn;data source=Mobile-shop.mssql.somee.com;persist security info=False;initial catalog=Mobile-shop;TrustServerCertificate=True";

        Console.WriteLine("Starting Database Migration...");

        try
        {
            using (SqlConnection localConn = new SqlConnection(localConnStr))
            using (SqlConnection remoteConn = new SqlConnection(remoteConnStr))
            {
                localConn.Open();
                remoteConn.Open();
                Console.WriteLine("Connected to both databases.");

                // 1. Get all tables from local
                List<string> tables = new List<string>();
                using (SqlCommand cmdTables = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory'", localConn))
                using (SqlDataReader readerTables = cmdTables.ExecuteReader())
                {
                    while (readerTables.Read())
                    {
                        tables.Add(readerTables.GetString(0));
                    }
                }
                
                Console.WriteLine($"Found {tables.Count} tables to migrate.");

                // 2. Disable Foreign Keys on Remote
                Console.WriteLine("Disabling Foreign Keys on Remote DB...");
                using (SqlCommand cmdDisableFK = new SqlCommand("EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'", remoteConn))
                {
                    try { cmdDisableFK.ExecuteNonQuery(); } 
                    catch { Console.WriteLine("Fallback: disabling constraints manually won't use sp_msforeachtable if blocked."); }
                }

                // Actually try a safer explicit disable if sp_msforeachtable is blocked by host
                string disableFkSql = @"
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' NOCHECK CONSTRAINT ALL; '
FROM sys.foreign_keys;
EXEC sp_executesql @sql;";
                using (SqlCommand cmdExplicitDisable = new SqlCommand(disableFkSql, remoteConn))
                {
                    cmdExplicitDisable.ExecuteNonQuery();
                }

                // 3. Clear existing remote data to prevent duplicates (Warning: Destructive if data exists)
                Console.WriteLine("Clearing existing data on Remote DB...");
                foreach (var table in tables)
                {
                    try
                    {
                        using (SqlCommand cmdClear = new SqlCommand($"DELETE FROM [{table}]", remoteConn))
                        {
                            cmdClear.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not clear table {table} (mostly ignore if empty): {ex.Message}");
                    }
                }

                // 4. Migrate Data
                foreach (var table in tables)
                {
                    Console.WriteLine($"Migrating table {table}...");
                    try
                    {
                        // Get Destination Columns
                        var remoteCols = new List<(string Name, string Type, bool IsNullable)>();
                        using (var cmdCols = new SqlCommand($"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' ORDER BY ORDINAL_POSITION", remoteConn))
                        using (var rdrCols = cmdCols.ExecuteReader())
                        {
                            while (rdrCols.Read()) remoteCols.Add((rdrCols.GetString(0), rdrCols.GetString(1), rdrCols.GetString(2) == "YES"));
                        }

                        if (remoteCols.Count == 0) continue; // Table doesn't exist on remote

                        // Check Local Columns
                        var localCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        using (var cmdCols = new SqlCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}'", localConn))
                        using (var rdrCols = cmdCols.ExecuteReader())
                        {
                            while (rdrCols.Read()) localCols.Add(rdrCols.GetString(0));
                        }

                        // Build Dynamic Select Map
                        List<string> selectParts = new List<string>();
                        foreach (var rCol in remoteCols)
                        {
                            string def = "NULL";
                            if (rCol.Type.Contains("char") || rCol.Type.Contains("text")) def = "''";
                            else if (rCol.Type.Contains("int") || rCol.Type.Contains("decimal") || rCol.Type.Contains("float") || rCol.Type.Contains("money") || rCol.Type.Contains("bit")) def = "0";
                            else if (rCol.Type.Contains("date") || rCol.Type.Contains("time")) def = "GETDATE()";
                            else if (rCol.Type.Contains("uniqueidentifier")) def = "NEWID()";

                            if (localCols.Contains(rCol.Name))
                            {
                                if (!rCol.IsNullable)
                                {
                                    selectParts.Add($"ISNULL([{rCol.Name}], CAST({def} AS {rCol.Type})) AS [{rCol.Name}]");
                                }
                                else
                                {
                                    selectParts.Add($"[{rCol.Name}]");
                                }
                            }
                            else
                            {
                                selectParts.Add($"CAST({(rCol.IsNullable ? "NULL" : def)} AS {rCol.Type}) AS [{rCol.Name}]");
                            }
                        }

                        string selectSql = $"SELECT {string.Join(", ", selectParts)} FROM [{table}]";

                        using (SqlCommand cmdSelect = new SqlCommand(selectSql, localConn))
                        using (SqlDataReader readerData = cmdSelect.ExecuteReader())
                        {
                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(remoteConn, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls, null))
                            {
                                bulkCopy.DestinationTableName = $"[{table}]";
                                
                                // Explicit mapping
                                for (int i = 0; i < readerData.FieldCount; i++)
                                {
                                    string colName = readerData.GetName(i);
                                    bulkCopy.ColumnMappings.Add(colName, colName);
                                }

                                bulkCopy.WriteToServer(readerData);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error migrating table {table}: {ex.Message}");
                    }
                }

                // 5. Re-enable Foreign Keys
                Console.WriteLine("Re-enabling Foreign Keys on Remote DB...");
                string enableFkSql = @"
DECLARE @sql2 NVARCHAR(MAX) = N'';
SELECT @sql2 += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' WITH CHECK CHECK CONSTRAINT ALL; '
FROM sys.foreign_keys;
EXEC sp_executesql @sql2;";
                using (SqlCommand cmdEnableFK = new SqlCommand(enableFkSql, remoteConn))
                {
                    cmdEnableFK.ExecuteNonQuery();
                }

                Console.WriteLine("Migration completed successfully!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
