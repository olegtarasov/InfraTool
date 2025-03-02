using System.Diagnostics.CodeAnalysis;
using System.Text;
using Serilog.Parsing;

namespace InfraTool.Helpers;

/// <summary>
/// Helper methods for working with strings.
/// </summary>
public static class StringHelpers
{
    /// <summary>
    /// Indicates whether the specified string is null or empty.
    /// </summary>
    /// <param name="value">String to check.</param>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// Converts a string to default value if it is null.
    /// </summary>
    /// <param name="input">String to convert.</param>
    /// <param name="defaultValue">Default value to replace.</param>
    public static string ToDefaultIfNull(this string? input, string defaultValue = "null") => input ?? defaultValue;

    /// <summary>
    /// Converts a string to null if it is equal to the passed value.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="value">Value to compare with.</param>
    /// <returns></returns>
    public static string? ToNullIfEquals(this string input, string value) => input.Equals(value) ? null : input;

    /// <summary>
    /// Returns an input string with all but first characters in lower case.
    /// </summary>
    /// <param name="input">A string to convert.</param>
    public static string ToLowerWithCapitalFirst(this string input)
    {
        var builder = new StringBuilder(input.ToLower());
        builder[0] = char.ToUpper(builder[0]);

        return builder.ToString();
    }

    /// <summary>
    /// Renders a structured message template and its arguments into a final string.
    /// </summary>
    /// <param name="messageTemplate">Message template in SeriLog format.</param>
    /// <param name="args">Template args.</param>
    /// <returns>Rendered string.</returns>
    public static string FormatStructuredMessage(this string messageTemplate, object[] args)
    {
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        var format = new StringBuilder();
        var index = 0;
        foreach (var tok in template.Tokens)
        {
            if (tok is TextToken)
                format.Append(tok);
            else
                format.Append("{" + index++ + "}");
        }

        return string.Format(format.ToString(), args);
    }

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    /// <remarks>
    /// Borrowed from https://www.michaelrose.dev/posts/exploring-system-text-json/.
    /// </remarks>
    /// <param name="str">String to convert.</param>
    public static string ToSnakeCase(this string? str)
    {
        if (str == null)
        {
            return string.Empty;
        }

        var upperCaseLength = str.Count(t => t >= 'A' && t <= 'Z' && t != str[0]);
        var bufferSize = str.Length + upperCaseLength;
        Span<char> buffer = new char[bufferSize];
        var bufferPosition = 0;
        var namePosition = 0;
        while (bufferPosition < buffer.Length)
        {
            if (namePosition > 0 && str[namePosition] >= 'A' && str[namePosition] <= 'Z')
            {
                buffer[bufferPosition] = '_';
                buffer[bufferPosition + 1] = str[namePosition];
                bufferPosition += 2;
                namePosition++;
                continue;
            }

            buffer[bufferPosition] = str[namePosition];
            bufferPosition++;
            namePosition++;
        }

        return new string(buffer).ToLower();
    }

    public static string ToSnakeCaseString<T>(this T input) => input == null ? string.Empty : input.ToString().ToSnakeCase();
}