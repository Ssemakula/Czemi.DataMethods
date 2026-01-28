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
        /// Executes the specified query against the database and loads the results into a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>This method retrieves data from the database by executing the specified query and
        /// maps the results to objects of type <typeparamref name="T"/>. The resulting objects are then converted into
        /// a <see cref="DataTable"/> for further processing.</remarks>
        /// <typeparam name="T">The type of the objects to map the query results to. Must have a parameterless constructor.</typeparam>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to include in the query.  If null, the query will be
        /// executed without parameters.</param>
        /// <returns>A <see cref="DataTable"/> containing the query results. If the query returns no rows, the <see
        /// cref="DataTable"/> will be empty.</returns>
        public static async Task<DataTable?> LoadDataTableAsync<T>(
        string connectionString,
        string query,
        Dictionary<string, object>? parameters = null) where T : new()
        {
            List<T> records = new List<T>();

            try
            {
                records = await GetRecordsAsync<T>(connectionString, query, parameters);
            }
            catch (Exception)
            {
                // Swallow exception to return empty DataTable
            }
            return records.ToDataTable();
        }

        /// <summary>
        /// Asynchronously retrieves a single record from a database and maps it to an object of type <typeparamref
        /// name="T"/>.
        /// </summary>
        /// <remarks>This method opens a database connection, executes the provided query, and maps the
        /// first result to an object of type <typeparamref name="T"/>. If no record is found, the method returns the
        /// default value of <typeparamref name="T"/>. Exceptions are caught and displayed using <see
        /// cref="System.Windows.Forms.MessageBox.Show(string)"/>, but the method does not rethrow them.</remarks>
        /// <typeparam name="T">The type of the object to map the database record to. Must have a parameterless constructor.</typeparam>
        /// <param name="ID">The unique identifier of the record to retrieve. This parameter is not directly used in the method but can
        /// be included in the query or parameters.</param>
        /// <param name="connectionString">The connection string used to establish a connection to the database. Cannot be null or empty.</param>
        /// <param name="qry">The SQL query to execute for retrieving the record. The query should be parameterized to prevent SQL
        /// injection.</param>
        /// <param name="parameters">A dictionary of parameter names and values to be used with the SQL query. Can be null if the query does not
        /// require parameters.</param>
        /// <returns>An object of type <typeparamref name="T"/> representing the retrieved record, or the default value of
        /// <typeparamref name="T"/> if no record is found or an error occurs.</returns>
        public static async Task<T?> GetRecordAsync<T>(
            string connectionString,
            string qry,
            Dictionary<string, object>? parameters = null) where T : new()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();
                }
                catch (SqlException ex)
                {
                    throw new Exception(GetSqlErrorMessage(ex, conn), ex); // Rethrow to let the caller know it failed
                }
                catch (Exception ex)
                {
                    throw new DataReadException($"Unable to open table. {ex}", ex);
                }

                try
                {
                    using (SqlCommand cmd = new SqlCommand(qry, conn))
                    {
                        cmd.ConfigureCommand(parameters);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return reader.ToObject<T>();
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new Exception(GetSqlErrorMessage(ex, conn), ex); // Rethrow to let the caller know it failed
                }
                catch (Exception ex)
                {
                    throw new DataReadException($"Unable to read data: {ex}", ex);
                }
            }

            return default;
        }

        /// <summary>
        /// Executes the specified SQL query asynchronously and retrieves a list of records mapped to the specified
        /// type.
        /// </summary>
        /// <remarks>This method opens a database connection, executes the provided query, and maps the
        /// results to objects of type <typeparamref name="T"/>. Ensure that the type <typeparamref name="T"/> has a
        /// parameterless constructor and that the query results can be mapped to its properties.</remarks>
        /// <typeparam name="T">The type of objects to map the query results to. Must have a parameterless constructor.</typeparam>
        /// <param name="connectionString">The connection string used to establish a connection to the database.</param>
        /// <param name="qry">The SQL query to execute. The query can include parameter placeholders.</param>
        /// <param name="parameters">A dictionary of parameter names and their corresponding values to be used in the query.  The keys should
        /// match the parameter placeholders in the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of objects of type
        /// <typeparamref name="T"/>  populated with the data retrieved from the query. If no records are found, an
        /// empty list is returned.</returns>
        public static async Task<List<T>> GetRecordsAsync<T>(
            string connectionString,
            string qry,
            Dictionary<string, object>? parameters = null) where T : new()
        {
            List<T> results = new List<T>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();
                }
                catch (SqlException ex)
                {
                    throw new Exception(GetSqlErrorMessage(ex, conn), ex); // Rethrow to let the caller know it failed
                }
                catch (Exception ex)
                {
                    throw new DataReadException($"Unable to open table: {ex}");
                }

                try
                {
                    using (SqlCommand cmd = new SqlCommand(qry, conn))
                    {
                        cmd.ConfigureCommand(parameters);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(reader.ToObject<T>());
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new Exception(GetSqlErrorMessage(ex, conn), ex); // Rethrow to let the caller know it failed
                }
                catch (Exception ex)
                {
                    throw new DataReadException($"Unable to read data: {ex}");
                }
            }

            return results;
        }

        /// <summary>
        /// Asynchronously executes a non-query SQL command (INSERT, UPDATE, DELETE) with parameters.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="qry">The SQL command to execute.</param>
        /// <param name="parameters">A dictionary of parameter names and values (can be null).</param>
        /// <returns>The number of rows affected, or -1 if an error occurs.</returns>
        public static async Task<int> ExecuteSqlAsync(
            string connectionString,
            string qry,
            Dictionary<string, object>? parameters = null)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(qry, conn))
            {
                cmd.ConfigureCommand(parameters);

                try
                {
                    await conn.OpenAsync();
                    return await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    throw new Exception(GetSqlErrorMessage(ex, conn), ex); // Rethrow to let the caller know it failed
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }
    }
}

