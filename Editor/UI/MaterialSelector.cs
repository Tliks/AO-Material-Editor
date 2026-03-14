using UnityEditor.IMGUI.Controls;

namespace Aoyon.MaterialEditor.UI;

internal class MaterialSelector
{
    private readonly static GUIStyle _style = StyleHelper.CenteredPopupStyle;
    
    public static void Draw(Func<List<Material>> getMaterials, 
        Action<Material, int> onSelected, 
        Func<Material, int, string>? labelSelector = null)
    {
        var label = GetContent();
        var rect = GUILayoutUtility.GetRect(label, _style, GUILayout.Width(GetSize(label).x));
        if (GUI.Button(rect, label, _style))
        {
            var dropdown = new MaterialAdvancedDropdown(
                getMaterials(),
                onSelected,
                new(),
                labelSelector);
            dropdown.Show(rect);
        }
    }

    public static void Draw(Rect position, Func<List<Material>> getMaterials, 
        Action<Material, int> onSelected, 
        Func<Material, int, string>? labelSelector = null)
    {
        if (GUI.Button(position, GetContent(), _style))
        {
            var dropdown = new MaterialAdvancedDropdown(
                getMaterials(),
                onSelected,
                new(),
                labelSelector);
            dropdown.Show(position);
        }
    }

    public static Vector2 GetSize()
    {
        return GetSize(GetContent());
    }

    public static Vector2 GetSize(GUIContent content)
    {
        return new Vector2(
            _style.CalcSize(content).x + 8f,
            GUIHelper.propertyHeight
        );
    }
 
    private static GUIContent GetContent()
    {
        return "Label:Select".LG();
    }
}

internal class MaterialAdvancedDropdown : AdvancedDropdown
{
    private readonly List<Material> _materials;
    private readonly Action<Material, int> _onSelected;
    private readonly Func<Material, int, string>? _labelSelector;

    const float minWidth = 260f;
    const float minHeight = 280f;

    public MaterialAdvancedDropdown(
        List<Material> materials,
        Action<Material, int> onSelected,
        AdvancedDropdownState state,
        Func<Material, int, string>? labelSelector = null
    ) : base(state)
    {
        _materials = materials ?? new List<Material>();
        _onSelected = onSelected;
        _labelSelector = labelSelector;
        minimumSize = new Vector2(minWidth, minHeight);
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Label:SelectMaterial".LS());
        int itemId = 0;

        for (int i = 0; i < _materials.Count; i++)
        {
            var mat = _materials[i];
            var name = _labelSelector != null
                ? _labelSelector(mat, i)
                : mat.name;

            var materialItem = new MaterialAdvancedDropdownItem(mat, i, name)
            {
                id = itemId++,
                icon = GetIcon(mat)
            };
            root.AddChild(materialItem);
        }

        return root;
    }

    private Texture2D? GetIcon(Material material)
    {
        var assetPreview = AssetPreview.GetAssetPreview(material);
        if (assetPreview != null) return assetPreview;

        var miniThumbnail = AssetPreview.GetMiniThumbnail(material);
        if (miniThumbnail != null) return miniThumbnail;

        return null;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        base.ItemSelected(item);
        if (item is MaterialAdvancedDropdownItem materialItem)
        {
            _onSelected?.Invoke(materialItem.Material, materialItem.Index);
        }
    }

    class MaterialAdvancedDropdownItem : AdvancedDropdownItem
    {
        public Material Material { get; }
        public int Index { get; }

        public MaterialAdvancedDropdownItem(Material material, int index, string name) : base(name)
        {
            Material = material;
            Index = index;
        }
    }
}
