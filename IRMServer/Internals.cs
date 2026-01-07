using System;

namespace IRMServer
{
    internal static class Internals
    {
        internal static void Log(string msg)
        {
            Console.WriteLine($"[>>IRMServer<<] LOG: {msg}");
        }

        internal static void LogError(string msg)
        {
            Console.Error.WriteLine($"[>>IRMServer<<] ERR: {msg}");
        }
    }
}