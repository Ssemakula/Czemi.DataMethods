using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Czemi.DataMethods
{
    /// <summary>
    /// Provides user-friendly error messages for SQL Server exceptions
    /// </summary>
    public static partial class SqlDataAccess
    {
        /// <summary>
        /// Translates SQL Server error numbers into user-friendly messages
        /// </summary>
        public static string GetSqlErrorMessage(SqlException sqlEx, SqlConnection connection)
        {
            // Check each error in the collection (there can be multiple)
            foreach (SqlError error in sqlEx.Errors)
            {
                switch (error.Number)
                {
                    case 229: // Permission denied on object
                        return $"Database Permission Error:\n\n" +
                               $"The user '{GetCurrentUsername(connection)}' does not have SELECT permission on the '{ExtractObjectName(error.Message)}' table.\n\n" +
                               $"Please contact your database administrator to grant the necessary permissions.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 230: // Permission denied on column
                        return $"Database Permission Error:\n\n" +
                               $"The user '{GetCurrentUsername(connection)}' does not have permission to access specific columns.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 262: // Permission denied in database
                        return $"Database Permission Error:\n\n" +
                               $"The user '{GetCurrentUsername(connection)}' does not have permission to access the database.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 297: // User does not have permission
                        return $"Database Permission Error:\n\n" +
                               $"The user '{GetCurrentUsername(connection)}' does not have permission to perform this action.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 208: // Invalid object name (table doesn't exist)
                        return $"Database Schema Error:\n\n" +
                               $"The table '{ExtractObjectName(error.Message)}' does not exist in the database.\n\n" +
                               $"Please ensure the database schema has been created.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 4060: // Cannot open database
                        return $"Database Connection Error:\n\n" +
                               $"Cannot open the database '{GetDatabaseName(connection)}'.\n\n" +
                               $"Please verify the database name and that you have access.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 18456: // Login failed
                        return $"Database Authentication Error:\n\n" +
                               $"Login failed for user '{GetCurrentUsername(connection)}'.\n\n" +
                               $"Please verify the username and password in the connection string.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case -1: // Connection timeout
                    case -2: // Connection broken
                        return $"Database Connection Error:\n\n" +
                               $"Unable to connect to the database server.\n\n" +
                               $"Please check:\n" +
                               $"  • SQL Server is running\n" +
                               $"  • Server name is correct: {GetServerName(connection)}\n" +
                               $"  • Network connectivity\n" +
                               $"  • Firewall settings\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 2627: // Unique constraint violation
                    case 2601: // Duplicate key
                        return $"Database Constraint Error:\n\n" +
                               $"A record with this value already exists.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 547: // Foreign key constraint violation
                        return $"Database Constraint Error:\n\n" +
                               $"Cannot delete or modify this record because it is referenced by other records.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 207: // Invalid column name
                        return $"Database Schema Error:\n\n" +
                               $"Invalid column name '{ExtractObjectName(error.Message)}'.\n\n" +
                               $"Please verify the column exists in the table.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 213: // Column count mismatch
                        return $"Database Schema Error:\n\n" +
                               $"Column name or number of supplied values doesn't match table definition.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 515: // Cannot insert NULL
                        return $"Database Constraint Error:\n\n" +
                               $"Cannot insert NULL value into a required field.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 1205: // Deadlock
                        return $"Database Deadlock Error:\n\n" +
                               $"The operation was chosen as a deadlock victim.\n\n" +
                               $"Please try the operation again.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 1222: // Lock timeout
                        return $"Database Timeout Error:\n\n" +
                               $"Lock request timeout exceeded.\n\n" +
                               $"The database is currently busy. Please try again.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 8152: // String truncation
                        return $"Database Data Error:\n\n" +
                               $"String or binary data would be truncated.\n\n" +
                               $"One or more values are too long for the database field.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 245: // Conversion error
                    case 8114: // Conversion error
                        return $"Database Data Type Error:\n\n" +
                               $"Error converting data to the required type.\n\n" +
                               $"Please check that all values are in the correct format.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 2812: // Stored procedure not found
                        return $"Database Schema Error:\n\n" +
                               $"Could not find stored procedure '{ExtractObjectName(error.Message)}'.\n\n" +
                               $"Technical Details:\n{error.Message}";

                    case 53: // Network error
                    case 233: // Connection initialization
                        return $"Database Network Error:\n\n" +
                               $"Unable to connect to SQL Server.\n\n" +
                               $"Please check:\n" +
                               $"  • SQL Server is running\n" +
                               $"  • Server name: {GetServerName(connection)}\n" +
                               $"  • Network connectivity\n" +
                               $"  • SQL Server Browser service is running\n" +
                               $"  • TCP/IP is enabled in SQL Server Configuration\n\n" +
                               $"Technical Details:\n{error.Message}";
                }
            }

            // Generic SQL error
            return $"Database Error:\n\n" +
                   $"An unexpected database error occurred.\n\n" +
                   $"Error Number: {sqlEx.Number}\n" +
                   $"Error Message: {sqlEx.Message}\n\n" +
                   $"Server: {GetServerName(connection)}\n" +
                   $"Database: {GetDatabaseName(connection)}\n" +
                   $"User: {GetCurrentUsername(connection)}";
        }

        /// <summary>
        /// Gets the current database username from the connection string
        /// </summary>
        public static string GetCurrentUsername(SqlConnection? connection = null)
        {
            // Try to get from active connection first (most accurate)
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    using var command = new SqlCommand("SELECT SUSER_SNAME()", connection);
                    var result = command.ExecuteScalar();
                    return result?.ToString() ?? "Unknown";
                }
                catch { }
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets the server name from the connection string
        /// </summary>
        public static string GetServerName(SqlConnection? connection = null)
        {
            if (connection != null)
            {
                try
                {
                    return connection.DataSource ?? "Unknown";
                }
                catch { }
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets the database name from the connection string
        /// </summary>
        public static string GetDatabaseName(SqlConnection? connection = null)
        {
            if (connection != null)
            {
                try
                {
                    return connection.Database ?? "Unknown";
                }
                catch { }
            }

            return "Unknown";
        }

        /// <summary>
        /// Extracts the object name (table, view, etc.) from SQL error messages
        /// </summary>
        private static string ExtractObjectName(string errorMessage)
        {
            try
            {
                // Error messages often contain object names in single quotes
                var parts = errorMessage.Split('\'');
                if (parts.Length >= 2)
                {
                    return parts[1];
                }
            }
            catch { }

            return "Unknown";
        }
    }
}
