using nadena.dev.ndmf;

namespace Aoyon.MaterialEditor;

internal static class LocalizedLog
{
    public static void Info(string key, params object[] args)
    {
        ErrorReport.ReportError(Localization.NdmfLocalizer, ErrorSeverity.Information, key, args);
    }

    public static void Warning(string key, params object[] args)
    {
        ErrorReport.ReportError(Localization.NdmfLocalizer, ErrorSeverity.NonFatal, key, args);
    }

    public static void Error(string key, params object[] args)
    {
        ErrorReport.ReportError(Localization.NdmfLocalizer, ErrorSeverity.Error, key, args);
    }
}