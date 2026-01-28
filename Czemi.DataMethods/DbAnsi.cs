using System;
using System.Collections.Generic;
using System.Text;

namespace Czemi.DataMethods
{
    public class DbAnsiString
    {
        public string Value { get; set; }
        public int Size { get; set; } = -1;
        public DbAnsiString(string value, int size = -1) { Value = value; Size = size; }
    }

    public static class AnsiExtensions
    {
        public static DbAnsiString ToDbAnsi(this string value, int size = -1)
        {
            return new DbAnsiString(value, size);
        }
    }
}
