namespace AutonomusCRM.Application.Common.Imports;

public static class ImportGuard
{
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxRows = 5000;

    public static (bool Ok, string? ErrorKey, object[]? FormatArgs) ValidateFile(long length, string fileName)
    {
        if (length <= 0)
            return (false, "Import_Error_EmptyFile", null);
        if (length > MaxFileBytes)
            return (false, "Import_Error_FileTooLarge", new object[] { MaxFileBytes / 1024 / 1024 });
        var ext = Path.GetExtension(fileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase))
            return (false, "Import_Error_InvalidExtension", null);
        return (true, null, null);
    }

    public static (bool Ok, string? ErrorKey, object[]? FormatArgs) ValidateRowCount(int count)
    {
        if (count == 0)
            return (false, "Import_Error_NoRows", null);
        if (count > MaxRows)
            return (false, "Import_Error_TooManyRows", new object[] { MaxRows });
        return (true, null, null);
    }
}
