using UnityEditor.IMGUI.Controls;

namespace Aoyon.MaterialEditor.UI;

internal class MaterialAdvancedDropdown : AdvancedDropdown
{
    private readonly List<Material> _materials;
    private readonly Action<Material> _onSelected;

    const float minWidth = 260f;
    const float minHeight = 280f;

    public MaterialAdvancedDropdown(List<Material> materials, Action<Material> onSelected, AdvancedDropdownState state) : base(state)
    {
        _materials = materials ?? new List<Material>();
        _onSelected = onSelected;
        minimumSize = new Vector2(minWidth, minHeight);
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Editor:SelectMaterial".LS());
        int itemId = 0;

        foreach (var mat in _materials)
        {
            var materialItem = new MaterialAdvancedDropdownItem(mat, mat.name)
            {
                id = itemId++,
                icon = AssetPreview.GetAssetPreview(mat)
            };
            root.AddChild(materialItem);
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        base.ItemSelected(item);
        if (item is MaterialAdvancedDropdownItem materialItem)
        {
            _onSelected?.Invoke(materialItem.Material);
        }
    }

    class MaterialAdvancedDropdownItem : AdvancedDropdownItem
    {
        public Material Material { get; }

        public MaterialAdvancedDropdownItem(Material material, string name) : base(name)
        {
            Material = material;
        }
    }
}
