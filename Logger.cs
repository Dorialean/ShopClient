class Logger
{
    public static void LogToFile(string message)
    {
        var logFilePath = "api_calls_log.log";
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}\n";
        File.AppendAllText(logFilePath, logEntry);
    }
}