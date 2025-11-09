using System.Globalization;
using Lilly.Engine.Core.Attributes.Scripts;
using Serilog;

namespace Lilly.Engine.Modules;

/// <summary>
/// Console module providing logging functions for Lua scripts.
/// Exposes console API similar to browser console (log, warn, error, etc.)
/// All output is routed through Serilog for centralized logging.
/// </summary>
[ScriptModule("console", "Console API for logging and debugging")]

/// <summary>
/// Provides console logging functions for scripts, routing output through Serilog.
/// </summary>
public class ConsoleModule
{
    private readonly ILogger _logger = Serilog.Log.ForContext<ConsoleModule>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleModule" /> class.
    /// </summary>
    public ConsoleModule() { }

    /// <summary>
    /// Asserts a condition and logs an error if the assertion fails.
    /// Useful for runtime validation in scripts.
    /// </summary>
    /// <param name="condition">The condition to assert (true = pass, false = fail).</param>
    /// <param name="args">The error message arguments if assertion fails.</param>
    /// <example>
    ///     <code>
    /// console.assert(player.health > 0, "Player is dead!")
    /// console.assert(vec.X >= 0, "X must be positive")
    /// </code>
    /// </example>
    [ScriptFunction("assert")]
    public void Assert(bool condition, params object[] args)
    {
        if (!condition)
        {
            var message = args.Length > 0 ? string.Join(" ", args.Select(FormatArg)) : "Assertion failed";
            _logger.Error("[Console] Assertion failed: {Message}", message);
        }
    }

    /// <summary>
    /// Clears the console (logs a clear message).
    /// Note: Does not actually clear the log file, only signals the clear action.
    /// </summary>
    /// <example>
    ///     <code>
    /// console.clear()  -- Logs "[Console] Console cleared"
    /// </code>
    /// </example>
    [ScriptFunction("clear")]
    public void Clear()
    {
        _logger.Information("[Console] Console cleared");
    }

    /// <summary>
    /// Logs a debug message to the console.
    /// Only visible when debug logging is enabled.
    /// </summary>
    /// <param name="args">The arguments to log.</param>
    [ScriptFunction("debug")]
    public void Debug(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Debug("[Console] {Message}", message);
    }

    /// <summary>
    /// Logs an error message to the console.
    /// Used for critical issues and runtime errors.
    /// </summary>
    /// <param name="args">The arguments to log.</param>
    /// <example>
    ///     <code>
    /// console.error("Failed to load asset:", assetName)
    /// </code>
    /// </example>
    [ScriptFunction("error")]
    public void Error(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Error("[Console] {Message}", message);
    }

    /// <summary>
    /// Logs an informational message (alias for log).
    /// </summary>
    /// <param name="args">The arguments to log.</param>
    [ScriptFunction("info")]
    public void Info(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[Console] {Message}", message);
    }

    /// <summary>
    /// Logs an informational message to the console.
    /// Concatenates all arguments with spaces and logs as Information level.
    /// </summary>
    /// <param name="args">The arguments to log (supports any type).</param>
    /// <example>
    ///     <code>
    /// console.log("Hello", "World", 123)  -- Output: "Hello World 123"
    /// </code>
    /// </example>
    [ScriptFunction("log")]
    public void Log(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[Console] {Message}", message);
    }

    /// <summary>
    /// Logs a trace message with stack trace information.
    /// Useful for debugging execution flow and performance issues.
    /// </summary>
    /// <param name="args">The arguments to log.</param>
    /// <example>
    ///     <code>
    /// console.trace("Function called from:")  -- Includes stack trace
    /// </code>
    /// </example>
    [ScriptFunction("trace")]
    public void Trace(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        var stackTrace = Environment.StackTrace;
        _logger.Debug("[Console] {Message}\n{StackTrace}", message, stackTrace);
    }

    /// <summary>
    /// Logs a warning message to the console.
    /// Used for non-critical issues that should be reviewed.
    /// </summary>
    /// <param name="args">The arguments to log.</param>
    /// <example>
    ///     <code>
    /// console.warn("Performance warning: Frame rate dropped")
    /// </code>
    /// </example>
    [ScriptFunction("warn")]
    public void Warn(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Warning("[Console] {Message}", message);
    }

    /// <summary>
    /// Formats an argument for console output.
    /// Converts various types to their string representation.
    /// </summary>
    /// <param name="arg">The argument to format.</param>
    /// <returns>Formatted string representation of the argument.</returns>
    /// <remarks>
    /// Handles special cases:
    /// - null → "null"
    /// - bool → "true"/"false" (lowercase)
    /// - string → as-is
    /// - others → ToString() or "undefined"
    /// </remarks>
    private static string FormatArg(object? arg)
    {
        if (arg == null)
        {
            return "null";
        }

        if (arg is string str)
        {
            return str;
        }

        if (arg is bool b)
        {
            return b.ToString().ToLower(CultureInfo.InvariantCulture);
        }

        return arg.ToString() ?? "undefined";
    }
}
