using UnityEditor.IMGUI.Controls;

namespace Aoyon.MaterialEditor.UI;

internal class MaterialSelector : AssetSelector<Material>
{
}

internal class TextureSelector : AssetSelector<Texture>
{
}

internal class AssetSelector<T> : AdvancedSelector<T> where T : Object
{
    public static new void Draw(Func<T?[]> getAssets, Action<T?, int>? onSelected, 
        Func<T?[], string[]>? getItemLabels = null, 
        Func<T?[], Texture2D?[]>? getItemIcons = null,
        GUIContent? label = null,
        GUIStyle? style = null,
        Func<string>? getSelectLabel = null)
    {
        AdvancedSelector<T>.Draw(getAssets, onSelected, 
            getItemLabels ?? GetDefaultItemLabels, getItemIcons ?? GetDefaultItemIcons, 
            label, style, getSelectLabel
        );
    }

    public static new void Draw(Rect position, Func<T?[]> getAssets, Action<T?, int>? onSelected, 
        Func<T?[], string[]>? getItemLabels = null,
        Func<T?[], Texture2D?[]>? getItemIcons = null,
        GUIContent? label = null,
        GUIStyle? style = null,
        Func<string>? getSelectLabel = null)
    {
        AdvancedSelector<T>.Draw(position, getAssets, onSelected, 
            getItemLabels ?? GetDefaultItemLabels, getItemIcons ?? GetDefaultItemIcons, 
            label, style, getSelectLabel
        );
    }

    private static string[] GetDefaultItemLabels(T?[] assets)
    {
        var labels = new string[assets.Length];
        for (int i = 0; i < assets.Length; i++)
        {
            var asset = assets[i];
            labels[i] = GetDefaultItemLabel(asset);
        }
        return labels;
    }

    protected static string GetDefaultItemLabel(T? asset)
    {
        if (asset == null) return "None";
        return asset.name;
    }

    private static Texture2D?[] GetDefaultItemIcons(T?[] assets)
    {
        var icons = new Texture2D?[assets.Length];
        for (int i = 0; i < assets.Length; i++)
        {
            var asset = assets[i];
            icons[i] = GetDefaultItemIcon(asset);
        }
        return icons;
    }

    protected static Texture2D? GetDefaultItemIcon(T? asset)
    {
        if (asset == null) return null;

        var assetPreview = AssetPreview.GetAssetPreview(asset);
        if (assetPreview != null) return assetPreview;

        var miniThumbnail = AssetPreview.GetMiniThumbnail(asset);
        if (miniThumbnail != null) return miniThumbnail;

        return null;
    }
}

internal class AdvancedSelector<T>
{    
    public static void Draw(Func<T?[]> getAssets, 
        Action<T?, int>? onSelected, 
        Func<T?[], string[]> getItemLabels, 
        Func<T?[], Texture2D?[]>? getItemIcons = null,
        GUIContent? label = null,
        GUIStyle? style = null,
        Func<string>? getSelectLabel = null)
    {
        label ??= DefaultContent;
        style ??= DefaultStyle;
        var rect = GUILayoutUtility.GetRect(label, style, GUILayout.Width(GetSize(label, style).x));
        if (GUI.Button(rect, label, style))
        {
            var assets = getAssets();
            Open(rect, assets, onSelected, getItemLabels, getItemIcons, getSelectLabel);
        }
    }

    public static void Draw(Rect position, Func<T?[]> getAssets, 
        Action<T?, int>? onSelected, 
        Func<T?[], string[]> getItemLabels,
        Func<T?[], Texture2D?[]>? getItemIcons = null,
        GUIContent? label = null,
        GUIStyle? style = null,
        Func<string>? getSelectLabel = null)
    {
        label ??= DefaultContent;
        style ??= DefaultStyle;
        if (GUI.Button(position, label, style))
        {
            var assets = getAssets();
            Open(position, assets, onSelected, getItemLabels, getItemIcons, getSelectLabel);
        }
    }

    public static void Open(Rect position,
        Func<T?[]> getAssets,
        Action<T?, int>? onSelected,
        Func<T?[], string[]> getItemLabels,
        Func<T?[], Texture2D?[]>? getItemIcons = null,
        Func<string>? getSelectLabel = null)
    {
        var assets = getAssets();
        Open(position, assets, onSelected, getItemLabels, getItemIcons, getSelectLabel);
    }

    private static void Open(Rect position,
        T?[] assets,
        Action<T?, int>? onSelected,
        Func<T?[], string[]> getItemLabels,
        Func<T?[], Texture2D?[]>? getItemIcons = null,
        Func<string>? getSelectLabel = null)
    {
        var dropdown = new CustomAdvancedDropdown<T>(
            assets,
            getItemLabels(assets),
            getItemIcons != null ? getItemIcons(assets) : new Texture2D?[assets.Length],
            getSelectLabel != null ? getSelectLabel() : GetDefaultSelectLabel(),
            onSelected,
            new()
        );
        dropdown.Show(position);
    }

    private static GUIContent DefaultContent => "common.select".LG();
    private static GUIStyle DefaultStyle => StyleHelper.CenteredPopupStyle;

    private static string GetDefaultSelectLabel()
    {
        var assetLabelKey = GetAssetLabelKey();
        var assetName = assetLabelKey != null && Localization.TryGetLocalizedString(assetLabelKey, out var label)
            ? label
            : typeof(T).Name;
        return string.Format("common.selectWithName".LS(), assetName);
    }

    private static string? GetAssetLabelKey()
    {
        var type = typeof(T);
        if (type == typeof(Material)) return "common.material";
        if (type == typeof(Texture)) return "common.texture";
        if (type == typeof(MaterialSlotReference)) return "common.materialSlot";
        return null;
    }

    public static Vector2 GetSize(GUIContent? label = null, GUIStyle? style = null)
    {
        label ??= DefaultContent;
        style ??= DefaultStyle;
        return new Vector2(
            style.CalcSize(label).x + 8f,
            GUIHelper.propertyHeight
        );
    }
}

internal class CustomAdvancedDropdown<T> : AdvancedDropdown
{
    private readonly T?[] _assets;
    private readonly string[] _labels;
    private readonly Texture2D?[] _icons;
    private readonly Action<T?, int>? _onSelected;
    private readonly string _selectLabel;

    const float minWidth = 260f;
    const float minHeight = 280f;

    public CustomAdvancedDropdown(
        T?[] assets,
        string[] labels,
        Texture2D?[] icons,
        string selectLabel,
        Action<T?, int>? onSelected,
        AdvancedDropdownState state
    ) : base(state)
    {
        _assets = assets;
        _labels = labels;
        _icons = icons;
        _selectLabel = selectLabel;
        _onSelected = onSelected;
        minimumSize = new Vector2(minWidth, minHeight);
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem(_selectLabel);
        int itemId = 0;

        for (int i = 0; i < _assets.Length; i++)
        {
            var asset = _assets[i];
            var name = _labels[i];

            var materialItem = new CustomAdvancedDropdownItem(asset, i, name)
            {
                id = itemId++,
                icon = _icons[i]
            };
            root.AddChild(materialItem);
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        base.ItemSelected(item);
        if (item is CustomAdvancedDropdownItem assetItem)
        {
            _onSelected?.Invoke(assetItem.Asset, assetItem.Index);
        }
    }

    class CustomAdvancedDropdownItem : AdvancedDropdownItem
    {
        public T? Asset { get; }
        public int Index { get; }

        public CustomAdvancedDropdownItem(T? asset, int index, string name) : base(name)
        {
            Asset = asset;
            Index = index;
        }
    }
}
