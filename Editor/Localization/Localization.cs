using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.ui;
using UnityEngine.UIElements;

namespace Aoyon.MaterialEditor;

internal static class Localization
{
    private const string LocalizationFolderGUID = "4658d62f77f5742458abe601082a1418";
    private const string DefaultLanguage = "en-US";
    private static readonly string[] SupportedLanguages = new string[] { "en-US", "ja-JP" };
    private static readonly Dictionary<string, string> LocaleNativeNameCache = new();

    private static Localizer? _ndmfLocalizer;
    public static Localizer NdmfLocalizer => _ndmfLocalizer ??= InitializeLocalizer();

    public static event Action? OnLanguageChanged;

    [InitializeOnLoadMethod]
    static void Init()
    {
        LanguagePrefs.RegisterLanguageChangeCallback(typeof(Localization), _ => OnLanguageChanged?.Invoke());
    }

    private static Localizer InitializeLocalizer()
    {
        return new Localizer(DefaultLanguage, () =>
        {
            var localizationFolderPath = AssetDatabase.GUIDToAssetPath(LocalizationFolderGUID);
            var assets = new List<LocalizationAsset>();
            foreach (var language in SupportedLanguages)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolderPath + "/" + language + ".po");
                if (asset == null)
                {
                    Debug.LogError($"Localization asset not found for language: {language}");
                    continue;
                }
                assets.Add(asset);
            }
            return assets;
        });
    }

    private const string TooltipSuffix = ".tooltip";
    public static string S(string key) => NdmfLocalizer.GetLocalizedString(key);
    public static bool TryGetLocalizedString(string key, out string value) => NdmfLocalizer.TryGetLocalizedString(key, out value);
    public static GUIContent G(string key)
    {
        var localized = NdmfLocalizer.GetLocalizedString(key);
        if (NdmfLocalizer.TryGetLocalizedString(key + TooltipSuffix, out var tooltip))
        {
            return new GUIContent(localized, tooltip);
        }
        return new GUIContent(localized);
    }

    public static void LocalizeUIElements(VisualElement element) => NdmfLocalizer.LocalizeUIElements(element);

    public static void DrawLanguageSwitcher() => LanguageSwitcher.DrawImmediate();
    public static void DrawLanguagePopupWithoutLabel(params GUILayoutOption[] options)
    {
        _ = NdmfLocalizer;
        var languages = LanguagePrefs.RegisteredLanguages
            .Where(lang => lang.Contains("-") ||
                           LanguagePrefs.RegisteredLanguages.All(l2 => !l2.StartsWith(lang + "-")))
            .ToArray();
        if (languages.Length == 0) return;

        var currentIndex = Array.IndexOf(languages, LanguagePrefs.Language);
        if (currentIndex < 0) currentIndex = 0;

        var displayNames = languages.Select(GetLocaleNativeName).ToArray();
        var newIndex = EditorGUILayout.Popup(currentIndex, displayNames, options);
        if (newIndex != currentIndex)
        {
            LanguagePrefs.Language = languages[newIndex];
        }
    }

    public static VisualElement CreateLanguageSwitcher() => new LanguageSwitcher();

    private static string GetLocaleNativeName(string locale)
    {
        locale = locale.ToLowerInvariant();
        if (LocaleNativeNameCache.TryGetValue(locale, out var cachedName))
        {
            return cachedName;
        }

        try
        {
            cachedName = System.Globalization.CultureInfo.CreateSpecificCulture(locale).NativeName;
        }
        catch (Exception)
        {
            cachedName = locale;
        }

        LocaleNativeNameCache[locale] = cachedName;
        return cachedName;
    }
}

internal static class LocalizationExtensions
{
    public static string LS(this string key) => Localization.S(key);
    public static GUIContent LG(this string key) => Localization.G(key);
}
