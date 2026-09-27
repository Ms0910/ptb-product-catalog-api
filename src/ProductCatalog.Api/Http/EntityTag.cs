using System.Globalization;

namespace ProductCatalog.Api.Http;

/// <summary>Convierte versiones de producto hacia y desde entity tags HTTP.</summary>
internal static class EntityTag
{
    public static string From(uint version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    /// <summary>
    /// Parsea un valor de <c>If-Match</c>. Ausente o <c>*</c> significa "cualquier versión" (<c>null</c>).
    /// </summary>
    public static bool TryParseVersion(string? header, out uint? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(header) || header.Trim() == "*")
        {
            return true;
        }

        var value = header.Trim();

        if (value.StartsWith("W/", StringComparison.Ordinal))
        {
            value = value[2..];
        }

        value = value.Trim('"');

        if (uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            version = parsed;
            return true;
        }

        return false;
    }
}
