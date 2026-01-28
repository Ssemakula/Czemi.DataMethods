using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Czemi.DataMethods
{
    public static partial class SqlDataAccess
    {
        /// <summary>
        /// Performs a bulk insert of data from the specified DataTable into a target SQL Server table using a single
        /// transaction.
        /// </summary>
        /// <remarks>The operation is performed within a single transaction. If any error occurs during
        /// the insert, all changes are rolled back to maintain data integrity. Enabling internal safety may impact
        /// performance due to the enforcement of triggers and constraints. This method automatically maps columns by
        /// name between the DataTable and the destination table, excluding any columns specified in
        /// columnsToSkip.</remarks>
        /// <param name="dataTable">The DataTable containing the data to insert. Each row represents a record to be added to the destination
        /// table. Cannot be null.</param>
        /// <param name="_connectionString">The connection string used to establish a connection to the target SQL Server database. Cannot be null or
        /// empty.</param>
        /// <param name="destinationTableName">The name of the table in the database where the data will be inserted. Must be a valid table name and cannot
        /// be null or empty.</param>
        /// <param name="columnsToSkip">An optional collection of column names to exclude from the insert operation. Columns listed here will not be
        /// mapped or written to the destination table. If null, all columns in the DataTable are included.</param>
        /// <param name="useInternalSafety">true to enable database triggers and check constraints during the bulk insert operation; otherwise, false to
        /// bypass them for improved performance.</param>
        /// <param name="validator">An optional function that validates each DataRow before insertion. Should return null if the row is valid,
        /// or an error message string if invalid. If provided and any row fails validation, the operation is aborted
        /// before any database changes are made.</param>
        /// <param name="batchSize">The number of rows to process in each batch during the bulk insert. Must be a positive integer. The default
        /// is 1000.</param>
        /// <exception cref="Exception">Thrown if validation fails for any row, if the connection string or table name is invalid, or if a database
        /// error occurs during the insert operation.</exception>
        public static async Task SqlBulkInsertAsync(
            this DataTable dataTable,
            string _connectionString,
            string destinationTableName,
            IEnumerable<string>? columnsToSkip = null,
            bool useInternalSafety = false,
            Func<DataRow, string>? validator = null,
            int batchSize = 1000)
        {
            // 1. Optional Pre-Check
            if (validator != null)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    string errorMessage = validator(dataTable.Rows[i]);
                    if (errorMessage != null)
                    {
                        // Stop before even touching the database
                        throw new Exception($"Validation failed at row {i}: {errorMessage}");
                    }
                }
            }

            SqlBulkCopyOptions options = useInternalSafety
                ? (SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints)
                : SqlBulkCopyOptions.Default;

            // All or nothing transactions
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var bulkCopy = new SqlBulkCopy(connection, options, transaction))
                        {
                            bulkCopy.DestinationTableName = destinationTableName;
                            bulkCopy.BatchSize = batchSize;
                            bulkCopy.BulkCopyTimeout = 60;

                            // Automatically map columns by name
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                bool shouldSkip = columnsToSkip?.Any(s => s.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase)) ?? false;
                                if (shouldSkip)
                                {
                                    continue; // Skip this column
                                }
                                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                            }
                            // ... setup bulkCopy ...
                            await bulkCopy.WriteToServerAsync(dataTable);
                        }
                        await transaction.CommitAsync();
                    }
                    catch (SqlException sqlEx)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception(GetSqlErrorMessage(sqlEx, connection), sqlEx); // Rethrow to let the UI know it failed
                    }
                    catch (Exception)
                    {
                        // If anything went wrong (SQL error, network drop, etc.), 
                        // undo everything so the DB stays clean.
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}
