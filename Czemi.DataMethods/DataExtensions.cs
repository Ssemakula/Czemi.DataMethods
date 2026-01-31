using Microsoft.Data.SqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Czemi.DataMethods
{
    public static partial class SqlDataHelper
    {
        public static BindingList<T> ToBindingList<T>(this List<T> list)
        {
            return new BindingList<T>(list);
        }

        // Strict version (keeps old behavior)
        public static DataTable? ToDataTable<T>(this List<T> items)
            => ToDataTable((IEnumerable<T>)items);

        // Cache properties per DTO type
        private static readonly ConcurrentDictionary<Type, (string Name, PropertyInfo Prop)[]> _columnCache =
            new ConcurrentDictionary<Type, (string Name, PropertyInfo Prop)[]>();

        public static DataTable? ToDataTable<T>(this IEnumerable<T> data)
        {
            if (data == null) return null;

            var type = typeof(T);

            // 1. Cache both the PropertyInfo AND the DisplayName/Name once
            var columns = _columnCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                 .Where(p => p.CanRead)
                 .Select(p => (
                     Name: p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name,
                     Prop: p
                 )).ToArray()
            );

            var table = new DataTable(type.Name);

            // 2. Define columns using the cached names/types
            foreach (var col in columns)
            {
                var propType = Nullable.GetUnderlyingType(col.Prop.PropertyType) ?? col.Prop.PropertyType;
                table.Columns.Add(col.Name, propType);
            }

            // 3. Fill the table
            foreach (var item in data)
            {
                var values = new object[columns.Length];
                for (int i = 0; i < columns.Length; i++)
                {
                    // Still using Reflection, but minimized overhead
                    values[i] = columns[i].Prop.GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(values);
            }

            return table;
        }

        /// <summary>
        /// Converts the current row of a <see cref="SqlDataReader"/> to an instance of the specified type.
        /// </summary>
        /// <remarks>The method maps column names from the <paramref name="reader"/> to property names of
        /// the type <typeparamref name="T"/>. Property names are matched case-insensitively. Only public instance
        /// properties with a matching name are set.</remarks>
        /// <typeparam name="T">The type of the object to create. Must have a parameterless constructor.</typeparam>
        /// <param name="reader">The <see cref="SqlDataReader"/> from which to read the data. The reader must be positioned on a valid row.</param>
        /// <returns>An instance of type <typeparamref name="T"/> populated with data from the current row of the <paramref
        /// name="reader"/>.</returns>
        public static T ToObject<T>(this SqlDataReader reader) where T : new()
        {
            T obj = new T();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                PropertyInfo? property = typeof(T).GetProperty(columnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null && !reader.IsDBNull(i))
                {
                    object value = reader.GetValue(i);
                    property.SetValue(obj, Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType), null);
                }
            }
            return obj;
        }

        /// <summary>
        /// Converts the current <see cref="IDataRecord"/> to an instance of the specified type.
        /// </summary>
        /// <remarks>This method maps the column names in the <paramref name="record"/> to the property
        /// names of the target type <typeparamref name="T"/>. The mapping is case-insensitive. Properties that do not
        /// match any column name or are not writable are ignored. If a column value is <see cref="DBNull"/>, the
        /// corresponding property is not set.</remarks>
        /// <typeparam name="T">The type of object to create. Must have a parameterless constructor and public writable properties.</typeparam>
        /// <param name="record">The <see cref="IDataRecord"/> containing the data to map to the object.</param>
        /// <returns>An instance of type <typeparamref name="T"/> with its properties populated from the data in the <paramref
        /// name="record"/>.</returns>
        public static T ToObject<T>(this IDataRecord reader) where T : new()
        {
            T obj = new T();
            Type objType = typeof(T);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                PropertyInfo? prop = objType.GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null || !prop.CanWrite || reader.IsDBNull(i))
                    continue;

                object value = reader.GetValue(i);
                Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    object convertedValue;

                    if (targetType.IsEnum)
                    {
                        if (value is string strVal)
                            convertedValue = Enum.Parse(targetType, strVal);
                        else
                            convertedValue = Enum.ToObject(targetType, value);
                    }
                    else if (targetType == typeof(Guid))
                    {
                        var str = value?.ToString();
                        convertedValue = str is not null ? Guid.Parse(str) : Guid.Empty;
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(value, targetType);
                    }

                    prop.SetValue(obj, convertedValue);
                }
                catch
                {
                    // Optional: log or skip silently
                }
            }

            return obj;
        }

        /// <summary>
        /// Configures the specified <see cref="SqlCommand"/> by adding the provided parameters to its <see
        /// cref="SqlCommand.Parameters"/> collection.
        /// </summary>
        /// <remarks>If a parameter value is <see langword="null"/>, it will be replaced with <see
        /// cref="DBNull.Value"/> when added to the command.</remarks>
        /// <param name="command">The <see cref="SqlCommand"/> to configure. Cannot be <see langword="null"/>.</param>
        /// <param name="parameters">A dictionary containing parameter names and their corresponding values to add to the command.  If a
        /// parameter name does not start with '@', it will be prefixed with '@'.  If <paramref name="parameters"/> is
        /// <see langword="null"/>, an empty dictionary is used.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> is <see langword="null"/>.</exception>
        public static void ConfigureCommand(this SqlCommand command, Dictionary<string, object>? parameters)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            parameters = parameters ?? new Dictionary<string, object>();

            foreach (var param in parameters)
            {
                string paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                SqlParameter sqlParam = new SqlParameter(paramName, null);

                if (param.Value is DbAnsiString ansi)
                {
                    sqlParam.SqlDbType = SqlDbType.VarChar;
                    sqlParam.Value = ansi.Value != null ? (object)ansi.Value : DBNull.Value;
                    sqlParam.Size = ansi.Size;
                }
                else
                {
                    sqlParam.Value = param.Value ?? DBNull.Value;
                }

                command.Parameters.Add(sqlParam);
            }
        }
    }
}
