using Dapper;
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
        /// Gets a record from a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="qry"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DataReadException"></exception>
        public static async Task<T?> GetRecordAsync<T>(
            string connectionString,
            string qry,
            object? parameters = null) where T : new()
        {
            using var conn = new SqlConnection(connectionString);
            try
            {
                // QueryFirstOrDefaultAsync returns the first row mapped to T, 
                // or the default value (null for classes) if no rows are found.
                return await conn.QueryFirstOrDefaultAsync<T>(qry, parameters);
            }
            catch (SqlException ex)
            {
                throw new Exception(GetSqlErrorMessage(ex, conn), ex);
            }
            catch (Exception ex)
            {
                throw new DataReadException($"Unable to read data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a record from a table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="qry"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<T?> GetRecordAsync<T>(
            string connectionString,
            string qry,
            Dictionary<string, object>? parameters = null) where T : new()
        {
            // Simply forwards the call to the object-based overload
            return await GetRecordAsync<T>(connectionString, qry, (object?)parameters);
        }

        /// <summary>
        /// Asynchronously executes the specified SQL query and returns a list of records mapped to the specified type.
        /// </summary>
        /// <remarks>This method uses Dapper for object mapping and query execution. The caller is
        /// responsible for ensuring that the query and parameters are valid for the target database. The method opens
        /// and closes the database connection automatically.</remarks>
        /// <typeparam name="T">The type to which each record in the result set will be mapped. Must have a parameterless constructor.</typeparam>
        /// <param name="connectionString">The connection string used to establish a connection to the SQL database.</param>
        /// <param name="qry">The SQL query to execute against the database. The query should return rows compatible with the type
        /// parameter <typeparamref name="T"/>.</param>
        /// <param name="parameters">An object containing the parameters to be passed to the SQL query, or <see langword="null"/> if the query
        /// does not require parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of records of type
        /// <typeparamref name="T"/> returned by the query. If no records are found, the list will be empty.</returns>
        public static async Task<List<T>> GetRecordsAsync<T>(
            string connectionString,
            string qry,
            object? parameters = null) where T : new()
        {
            using var conn = new SqlConnection(connectionString);
            return (await conn.QueryAsync<T>(qry, parameters)).ToList();
        }

        /// <summary>
        /// Asynchronously retrieves a list of records from the database and maps each result to an instance of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to which each database record will be mapped. Must have a parameterless constructor.</typeparam>
        /// <param name="connectionString">The connection string used to establish a connection to the database.</param>
        /// <param name="qry">The SQL query to execute for retrieving records.</param>
        /// <param name="parameters">An optional dictionary of parameter names and values to be applied to the SQL query. If null, the query is
        /// executed without parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of mapped records of type
        /// <typeparamref name="T"/>. If no records are found, the list will be empty.</returns>
        public static async Task<List<T>> GetRecordsAsync<T>(
        string connectionString,
        string qry,
        Dictionary<string, object>? parameters = null) where T : new()
        {
            // We simply cast or pass the dictionary to the object version
            return await GetRecordsAsync<T>(connectionString, qry, (object?)parameters);
        }

        /// <summary>
        /// Executes a SQL command asynchronously and returns the number of affected rows.
        /// </summary>
        /// <param name="connectionString">The connection string used to establish a connection to the database.</param>
        /// <param name="qry">The SQL query to execute.</param>
        /// <param name="parameters">An optional object containing the parameters to be passed to the SQL query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected by the query.</returns>
        /// <exception cref="Exception"></exception>
        public static async Task<int> ExecuteSqlAsync(
            string connectionString,
            string qry,
            object? parameters = null)
        {
            using var conn = new SqlConnection(connectionString);
            try
            {
                return await conn.ExecuteAsync(qry, parameters);
            }
            catch (SqlException ex)
            {
                throw new Exception(GetSqlErrorMessage(ex, conn), ex);
            }
            catch (Exception) // Return -1 if error is not Sql related
            {
                return -1;
            }
        }

        /// <summary>
        /// Executes a SQL command asynchronously and returns the number of affected rows.
        /// </summary>
        /// <param name="connectionString">The connection string used to establish a connection to the database.</param>
        /// <param name="qry">The SQL query to execute.</param>
        /// <param name="parameters">An optional object containing the parameters to be passed to the SQL query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected by the query.</returns>
        public static async Task<int> ExecuteSqlAsync(
            string connectionString,
            string qry,
            Dictionary<string, object>? parameters = null)
        {
            return await ExecuteSqlAsync(connectionString, qry, (object?)parameters);
        }
    }
}

