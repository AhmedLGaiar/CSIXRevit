using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToEtabs.JsonHandler
{
    public static class Logger
    {
        public static void Error(string message) => Console.WriteLine($"ERROR: {message}");
        public static void Warning(string message) => Console.WriteLine($"WARNING: {message}");
        public static void Info(string message) => Console.WriteLine($"INFO: {message}");
        public static void Debug(string message) => Console.WriteLine($"DEBUG: {message}");
    }
}
