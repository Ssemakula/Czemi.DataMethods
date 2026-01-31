using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Reflection;

namespace Czemi.DataMethods
{
    public static partial class SqlDataAccess
    {
        /*private static string? _connectionString;

        // Call this once in your Program.cs or Main()
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }*/



        public static List<T> LoadData<T>(string connectionString, string sql, object parameters = null)
        {
            using IDbConnection connection = new SqlConnection(connectionString);
            // Dapper replaces your entire ToObject logic with this:
            return connection.Query<T>(sql, parameters).ToList();
        }
    }
}
