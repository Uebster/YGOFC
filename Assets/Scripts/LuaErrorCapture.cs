using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// FASE 26: LuaErrorCapture
/// Captures, categorizes, and logs Lua script errors
/// </summary>
public class LuaErrorCapture
{
    public class ErrorLog
    {
        public DateTime Timestamp { get; set; }
        public int CardId { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Context { get; set; }
    }

    private List<ErrorLog> errorLogs = new List<ErrorLog>();
    private Dictionary<string, int> errorCategoryCounts = new Dictionary<string, int>
    {
        { "SyntaxError", 0 },
        { "UndefinedConstant", 0 },
        { "MissingFunction", 0 },
        { "MemoryLeakWarning", 0 },
        { "LogicError", 0 },
        { "RuntimeError", 0 },
        { "Other", 0 }
    };

    /// <summary>
    /// Capture a runtime error
    /// </summary>
    public void CaptureRuntimeError(Exception error)
    {
        string category = GetErrorCategory(error);
        
        var log = new ErrorLog
        {
            Timestamp = DateTime.UtcNow,
            Category = category,
            Message = error.Message,
            StackTrace = error.StackTrace
        };

        errorLogs.Add(log);
        IncrementCategory(category);

        Debug.LogError($"[{category}] {error.Message}");
    }

    /// <summary>
    /// Categorize an error by type
    /// </summary>
    public string GetErrorCategory(Exception error)
    {
        if (error == null) return "Other";

        string message = error.Message.ToLower();
        string type = error.GetType().Name.ToLower();

        if (message.Contains("syntax") || type.Contains("syntax"))
            return "SyntaxError";

        if (message.Contains("undefined") || message.Contains("not defined") || message.Contains("nil"))
            return "UndefinedConstant";

        if (message.Contains("function") || message.Contains("method"))
            return "MissingFunction";

        if (message.Contains("memory") || message.Contains("heap") || message.Contains("out of memory"))
            return "MemoryLeakWarning";

        if (message.Contains("logic") || type.Contains("logic"))
            return "LogicError";

        if (type.Contains("runtime"))
            return "RuntimeError";

        return "Other";
    }

    /// <summary>
    /// Log detailed error information
    /// </summary>
    public void LogErrorDetails(Exception error, string context = "")
    {
        var log = new ErrorLog
        {
            Timestamp = DateTime.UtcNow,
            Category = GetErrorCategory(error),
            Message = error.Message,
            StackTrace = error.StackTrace,
            Context = context
        };

        errorLogs.Add(log);
        IncrementCategory(log.Category);

        Debug.LogError($"[{log.Category}] Context: {context}\nError: {error.Message}\n{error.StackTrace}");
    }

    /// <summary>
    /// Generate error summary report
    /// </summary>
    public Dictionary<string, int> GetErrorSummary()
    {
        return new Dictionary<string, int>(errorCategoryCounts);
    }

    /// <summary>
    /// Get all errors of a specific category
    /// </summary>
    public List<ErrorLog> GetErrorsByCategory(string category)
    {
        return errorLogs.Where(e => e.Category == category).ToList();
    }

    /// <summary>
    /// Get the most common errors
    /// </summary>
    public List<(string Message, int Count)> GetTopErrors(int topCount = 10)
    {
        return errorLogs
            .GroupBy(e => e.Message)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending<(string, int), int>(x => x.Item2)
            .Take(topCount)
            .ToList();
    }

    /// <summary>
    /// Check if there are any critical errors
    /// </summary>
    public bool HasCriticalErrors()
    {
        return errorCategoryCounts["SyntaxError"] > 0 || 
               errorCategoryCounts["MemoryLeakWarning"] > 0;
    }

    /// <summary>
    /// Clear all error logs
    /// </summary>
    public void ClearLogs()
    {
        errorLogs.Clear();
        foreach (var key in errorCategoryCounts.Keys.ToList())
        {
            errorCategoryCounts[key] = 0;
        }
    }

    /// <summary>
    /// Export error logs as formatted string
    /// </summary>
    public string ExportAsString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine("ERROR SUMMARY REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine();

        // Summary by category
        sb.AppendLine("ERRORS BY CATEGORY:");
        foreach (var kvp in errorCategoryCounts.OrderByDescending(x => x.Value))
        {
            if (kvp.Value > 0)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("TOP ERRORS:");
        var topErrors = GetTopErrors(10);
        int errorNum = 1;
        foreach (var (message, count) in topErrors)
        {
            sb.AppendLine($"  {errorNum}. [{count}x] {message}");
            errorNum++;
        }

        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════");

        return sb.ToString();
    }

    // ==================== PRIVATE METHODS ====================

    private void IncrementCategory(string category)
    {
        if (errorCategoryCounts.ContainsKey(category))
        {
            errorCategoryCounts[category]++;
        }
        else
        {
            errorCategoryCounts[category] = 1;
        }
    }
}
