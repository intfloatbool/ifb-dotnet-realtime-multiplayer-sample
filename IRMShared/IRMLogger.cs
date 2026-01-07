using System;

namespace IRMShared
{
    public static class IRMLogger
    {
        private static Action<string> _logMsg = Console.WriteLine;
        private static Action<string> _logErr = Console.Error.WriteLine;

        public static bool IsLoggingEnabled { get; set; } =  true;
        
        public static void Setup(Action<string> logMsg, Action<string> logErr)
        {
            _logMsg = logMsg;
            _logErr = logErr;
        }
        
        public static void LogMsg(string msg)
        {
            if (!IsLoggingEnabled)
            {
                return;
            }
            _logMsg.Invoke($"[IRMLogger]LogMsg() -> {msg}");
        }

        public static void LogErr(string msg)
        {
            if (!IsLoggingEnabled)
            {
                return;
            }
            
            _logErr.Invoke($"[IRMLogger]LogErr() -> {msg}");
        }
    }
}