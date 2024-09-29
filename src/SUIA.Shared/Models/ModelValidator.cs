using System.Buffers;
using System.Numerics;

namespace SUIA.Shared.Models;
public abstract class ModelValidator
{
    private readonly Dictionary<string, string> _errors = [];
    public abstract bool IsValid();
    public void ErrorFor(string name, string message) => _errors.TryAdd(name, message);
    public bool HasErrors() => _errors.Count > 0;
    public Dictionary<string, string> GetErrors() => _errors;
    public void ClearErrors() => _errors.Clear();
}

public sealed class ValidationProblem
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? Details { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public static class InputValidators
{
    private static readonly SearchValues<char> digits = SearchValues.Create("1234567890");
    private static readonly SearchValues<char> alphabets = SearchValues.Create("abcdefghijklmnopqrstuvwxyz");
    private static readonly string[] gstStateCodes = ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "97", "99"];
    private static readonly SearchValues<char> svGst13thDigit = SearchValues.Create("123456789abcdefghijklmnopqrstuvwxyz");
    private static readonly SearchValues<char> svGst15thDigit = SearchValues.Create("1234567890abcdefghijklmnopqrstuvwxyz");


    public static bool IsMobile(this string? input)
        => !string.IsNullOrWhiteSpace(input) &&
            input.Trim().Length == 10 &&
            !input.AsSpan().ContainsAnyExcept(digits) &&
            int.Parse(input[0..1]) > 5;

    public static bool IsPAN(this string? input)
        => !string.IsNullOrWhiteSpace(input) &&
            input.Trim().Length == 10 &&
            !input[0..4].ToLowerInvariant().AsSpan().ContainsAnyExcept(alphabets) &&
            !input[5..8].ToLowerInvariant().AsSpan().ContainsAnyExcept(digits) &&
            !input[9].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(alphabets);

    public static bool IsGST(this string? input)
        => !string.IsNullOrWhiteSpace(input) &&
            input.Trim().Length == 15 &&
            gstStateCodes.Contains(input[..2]) &&
            input[2..12].IsPAN() &&
            !input[12].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(svGst13thDigit) &&
            !input[13].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept('z') &&
            !input[14].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(svGst15thDigit);

    public static bool IsAadhaar(this string? input, char? separator = null)
        => !string.IsNullOrWhiteSpace(input) &&
            (separator is not null && input.Trim().Length == 14 &&
                !input[4].ToString().AsSpan().ContainsAnyExcept(separator.Value) &&
                !input[9].ToString().AsSpan().ContainsAnyExcept(separator.Value) &&
                !input[0..3].AsSpan().ContainsAnyExcept(digits) &&
                !input[5..8].AsSpan().ContainsAnyExcept(digits) &&
                !input[10..13].AsSpan().ContainsAnyExcept(digits) ||
            (separator is null && input?.Trim().Length == 12 &&
                !input.AsSpan().ContainsAnyExcept(digits))) &&
            int.Parse(input[0].ToString()) > 2;

    public static bool IsEmail(this string? input)
        => !string.IsNullOrWhiteSpace(input) &&
            input.Count(a => a == '@') == 1 &&
            input.Split('@')[1].IsDomain() &&
            !input.Contains(".@", StringComparison.OrdinalIgnoreCase) &&
            !input.Contains("@.", StringComparison.OrdinalIgnoreCase) &&
            !input.Contains("..", StringComparison.OrdinalIgnoreCase) &&
            input.Split('@')[1].Contains('.', StringComparison.OrdinalIgnoreCase) &&
            (!input[0].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(alphabets) ||
            !input[0].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(digits));

    public static bool IsDomain(this string? input)
        => !string.IsNullOrWhiteSpace(input) &&
            !input.Contains('_') &&
            input.Count(a => a == '.') == 1 &&
            input.Length < 64 &&
            input.Split('.')[^1].Length > 1 && input.Split('.')[^1].Length < 7 &&
            (!input[0].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(alphabets) ||
            !input[0].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(digits)) &&
            !input[^1].ToString().ToLowerInvariant().AsSpan().ContainsAnyExcept(alphabets);

    public static bool IsInRange<T>(this T input, T from, T to) where T : INumber<T>
        => input >= from && input <= to;

    public static bool IsInRange(this DateTime input, DateTime from, DateTime to)
        => input.Date >= from.Date && input.Date <= to.Date;

    public static bool IsInRange(this DateOnly input, DateOnly from, DateOnly to)
        => input >= from && input <= to;
}