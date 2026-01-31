## Namespace ##
	Czemi.DataMethods

## DataMethods ##
	void ConfigureCommand(this SqlCommand, Dictionary<string, object>?)
	T ToObject<T>(this IDataRecord reader)
	T ToObject<T>(this SqlDataReader reader)
	BindingList<T> ToBindingList<T>(this List<T> list)
	DataTable? ToDataTable<T>(this List<T> items)
	DataTable? ToDataTable<T>(this IEnumerable<T> data)
	public static async Task<DataTable?> LoadDataTableAsync<T>(
			string connectionString,
			string query,
			Dictionary<string, object>? parameters = null)
	Task<int> ExecuteSqlAsync(
            string connectionString,
            string qry,
            Dictionary<string, object>? parameters = null)
	public static async Task<List<T>> GetRecordsAsync<T>(
			string connectionString,
			string qry,
			Dictionary<string, object>? parameters = null)
	public static async Task<T?> GetRecordAsync<T>(
			string connectionString,
			string qry,
			Dictionary<string, object>? parameters = null)
	Task SqlBulkInsertAsync(
			this DataTable dataTable,
			string _connectionString,
			string destinationTableName,
			IEnumerable<string>? columnsToSkip = null,
			bool useInternalSafety = false,
			Func<DataRow, string>? validator = null,
			int batchSize = 1000)
	
	## public class DbAnsiString ##
	
	public string Value { get; set; }
	public int Size { get; set; } = -1;
	public DbAnsiString(string value, int size = -1) { Value = value; Size = size; }
	
	DbAnsiString ToDbAnsi(this string value, int size = -1)
	
## Configuration ##	
	void ConfigureCommand(this SqlCommand command, Dictionary<string, object>? parameters)
	string GetSqlErrorMessage(SqlException sqlEx, SqlConnection connection)
	
# Other Things #
 
 Licensed under [MIT Licence](https://opensource.org/license/mit)
 
  
