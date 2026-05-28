namespace AutonomusCRM.Application.Common.Imports;

public static class ImportGuard
{
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxRows = 5000;

    public static (bool Ok, string? Error) ValidateFile(long length, string fileName)
    {
        if (length <= 0)
            return (false, "Archivo vacío.");
        if (length > MaxFileBytes)
            return (false, $"Archivo excede el máximo de {MaxFileBytes / 1024 / 1024} MB.");
        var ext = Path.GetExtension(fileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase))
            return (false, "Solo se permiten archivos .csv o .json.");
        return (true, null);
    }

    public static (bool Ok, string? Error) ValidateRowCount(int count)
    {
        if (count == 0)
            return (false, "No hay filas válidas para importar.");
        if (count > MaxRows)
            return (false, $"Máximo {MaxRows} filas por importación.");
        return (true, null);
    }
}
