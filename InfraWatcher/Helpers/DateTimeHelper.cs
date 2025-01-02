using System.Text;

namespace InfraWatcher.Helpers;

/// <summary>
/// Helpers for working with dates and time.
/// </summary>
public static class DateTimeHelper
{
    private static readonly HashSet<char> AllowedShortTsChars = new(new[] { 'd', 'h', 'm', 's' });

    /// <summary>
    /// Rounds a DateTime to the left boundary of a specified interval.
    /// </summary>
    /// <param name="time">DateTime to round.</param>
    /// <param name="interval">Interval to round to.</param>
    public static DateTime RoundLeft(this DateTime time, TimeSpan interval) => RoundLeft(time, interval, out _);

    /// <summary>
    /// Rounds a DateTime to the left boundary of a specified interval.
    /// </summary>
    /// <param name="time">DateTime to round.</param>
    /// <param name="interval">Interval to round to.</param>
    /// <param name="mod">The remainder in ticks.</param>
    public static DateTime RoundLeft(this DateTime time, TimeSpan interval, out long mod)
    {
        mod = time.Ticks % interval.Ticks;
        return time - TimeSpan.FromTicks(mod);
    }

    /// <summary>
    /// Formats specified TimeSpan to the most compact representation.
    /// </summary>
    /// <param name="span">TimeSpan to format.</param>
    public static string ToShortString(this TimeSpan span)
    {
        if (span == TimeSpan.Zero)
            return "0";

        var sb = new StringBuilder();
        if (span.Days != 0)
            sb.Append(span.Days).Append("d");
        if (span.Hours != 0)
            sb.Append(span.Hours).Append("h");
        if (span.Minutes != 0)
            sb.Append(span.Minutes).Append("m");
        if (span.Seconds != 0)
            sb.Append(span.Seconds).Append("s");
        if (span.Milliseconds != 0)
            sb.Append(span.Milliseconds).Append("ms");

        return sb.ToString();
    }

    /// <summary>
    /// Formats DateTime to a string with format 'dd-MM-yyyy HH:mm:ss'.
    /// </summary>
    /// <param name="dateTime">DateTime to format.</param>
    public static string ToShortDateTimeString(this DateTime dateTime) => dateTime.ToString("dd-MM-yyyy HH:mm:ss");

    /// <summary>
    /// Parses a TimeSpan object from compact representation generated with <see cref="ToShortString"/>. 
    /// </summary>
    /// <param name="input">String representation.</param>
    /// <exception cref="FormatException">String was malformed.</exception>
    public static TimeSpan FromShortTimeSpanString(string input)
    {
        var numBuilder = new StringBuilder();
        var specBuilder = new StringBuilder();
        bool isLastNum = true;

        int days = 0, hours = 0, minutes = 0, seconds = 0, ms = 0;

        for (int i = 0; i < input.Length; i++)
        {
            bool isNum;
            if (!AllowedShortTsChars.Contains(input[i]))
            {
                if (!char.IsNumber(input[i]))
                    throw new FormatException($"Invalid character at position {i}: {input}");
                isNum = true;
            }
            else
            {
                isNum = false;
            }

            if (!isLastNum && isNum)
            {
                Process();
            }

            (isNum ? numBuilder : specBuilder).Append(input[i]);
            isLastNum = isNum;
        }

        Process();

        return new TimeSpan(days, hours, minutes, seconds, ms);

        void Process()
        {
            int val = int.Parse(numBuilder.ToString());
            switch (specBuilder.ToString().ToLower())
            {
                case "d":
                    days = val;
                    break;
                case "h":
                    hours = val;
                    break;
                case "m":
                    minutes = val;
                    break;
                case "s":
                    seconds = val;
                    break;
                case "ms":
                    ms = val;
                    break;
                default:
                    throw new FormatException($"Unsupported specifier: {specBuilder.ToString()}");
            }

            numBuilder.Clear();
            specBuilder.Clear();
        }
    }

    /// <summary>
    /// Converts DateTime to Python timestamp (number of microseconds from Unix epoch).
    /// </summary>
    /// <param name="time">DateTime to convert.</param>
    public static long ToPythonTimestamp(this DateTime time) => (long)((time - DateTime.UnixEpoch).Ticks / 10);

    /// <summary>
    /// Converts Python timestamp (number of microseconds from Unix epoch) to DateTime.
    /// </summary>
    /// <param name="timestamp">Timestamp.</param>
    public static DateTime FromPythonTimestamp(long timestamp) =>
        DateTime.UnixEpoch + TimeSpan.FromTicks(timestamp * 10);

    /// <summary>
    /// Converts TimeSpan to microseconds.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static long ToMicroseconds(this TimeSpan span) => span.Ticks / 10;

    /// <summary>
    /// Converts microseconds to a TimeSpan.
    /// </summary>
    /// <param name="us"></param>
    /// <returns></returns>
    public static TimeSpan FromMicroseconds(long us) => TimeSpan.FromTicks(us * 10);
}