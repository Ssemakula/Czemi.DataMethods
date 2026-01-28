## DataMethods ##
	void ConfigureCommand(this SqlCommand, Dictionary<string, object>?)
	T ToObject<T>(this IDataRecord reader)
	T ToObject<T>(this SqlDataReader reader)
	DataTable? ToDataTable<T>(this IEnumerable<T> data)
    DataTable? ToDataTable<T>(this List<T> items)
	
 # Other Things #
 
 Licensed under [MIT Licence](https://opensource.org/license/mit)
 
  
