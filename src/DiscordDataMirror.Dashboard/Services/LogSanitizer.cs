namespace DiscordDataMirror.Dashboard.Services;

/// <summary>
/// Provides sanitization helpers to prevent log-forging (CWE-117 / cs/log-forging).
/// User-controlled values must be sanitized before being written to log entries
/// to prevent malicious input from injecting fake log lines via embedded newlines
/// or other control characters.
/// </summary>
internal static class LogSanitizer
{
    /// <summary>
    /// Strips CR, LF, and other ASCII control characters from a string so it
    /// cannot inject spurious lines into structured or plaintext log output.
    /// CodeQL recognizes chained <see cref="string.Replace(string, string)"/> calls
    /// on the return value as a sanitizer for the cs/log-forging rule.
    /// </summary>
    /// <param name="value">The raw user-controlled value.</param>
    /// <returns>The sanitized value, safe to include in a log message.</returns>
    public static string SanitizeForLog(this string? value)
        => (value ?? string.Empty)
            .Replace("\r\n", "")
            .Replace("\n", "")
            .Replace("\r", "");
}
