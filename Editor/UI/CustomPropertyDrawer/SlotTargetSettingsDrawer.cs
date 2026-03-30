using Aoyon.MaterialEditor.Processor;
using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(SlotTargetSettings))]
internal class SlotTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var slots = property.FindPropertyRelative(nameof(SlotTargetSettings.TargetSlots));
        var listOptions = CreateListOptions(slots);
        position.height = MaterialSlotReferenceCollectionUI.GetHeight(slots, GUIContent.none, listOptions);
        MaterialSlotReferenceCollectionUI.Draw(position, slots, "targetSettings.slotTargets.label".LG(), listOptions);
    }

    private static GUIHelper.ListOptions CreateListOptions(SerializedProperty slotsProperty)
    {
        return new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(
                rect => DrawMiddleContent(rect, slotsProperty),
                () => GetMiddleContentHeight(slotsProperty)));
    }

    private static void DrawMiddleContent(Rect position, SerializedProperty slotsProperty)
    {
        position.SetSingleHeight();
        var isExpanded = GUIHelper.Foldout(position, slotsProperty, "targetSettings.slotTargets.add.title".LG(), new(RectStrict: true));
        if (!isExpanded) return;

        position.NewLine();
        position.Indent();

        position = new RectOffset(0, -4, 0, 0).Add(position); // 少し狭める

        var helpboxHeight = GUIHelper.GetHelpBoxHeight("targetSettings.slotTargets.add.help".LS(), MessageType.Info);

        var backGroundRect = position;
        backGroundRect.height = GUIHelper.propertyHeight * 4 + GUIHelper.GUI_SPACE * 3;
        if (MaterialEditorSettings.ShowInspectorDescription) backGroundRect.height += GUIHelper.GUI_SPACE + helpboxHeight;
        backGroundRect = new RectOffset(5, 4, 3, 3).Add(backGroundRect);
        EditorGUI.LabelField(backGroundRect, GUIContent.none, EditorStyles.helpBox);

        EditorGUI.LabelField(position, "targetSettings.slotTargets.add.byGameObject".LG());
        position.NewLine();
        position.Indent();
        DrawAddSlotsUnderGameObjectField(position, slotsProperty);
        position.NewLine();
        position.Back();
        
        EditorGUI.LabelField(position, "targetSettings.slotTargets.add.byMaterial".LG());
        position.NewLine();
        position.Indent();
        DrawAddMaterialField(position, slotsProperty);
        position.NewLine();
        position.Back();

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            position.height = helpboxHeight;
            GUIHelper.HelpBox(position, "targetSettings.slotTargets.add.help".LS(), MessageType.Info);
        }
    }

    private static void DrawAddSlotsUnderGameObjectField(Rect position, SerializedProperty slotsProperty)
    {
        using var check = new EditorGUI.ChangeCheckScope();
        var gameObject = EditorGUI.ObjectField(position, GUIContent.none, null, typeof(GameObject), true) as GameObject;
        if (!check.changed || gameObject == null) return;

        foreach (var slot in GetSlotsUnderGameObject(slotsProperty, gameObject))
        {
            MaterialSlotReferenceCollectionUI.AppendSlot(slotsProperty, slot);
        }

        slotsProperty.serializedObject.ApplyModifiedProperties();
    }

    private static void DrawAddMaterialField(Rect position, SerializedProperty slotsProperty)
    {
        var selectorWidth = MaterialSelector.GetSize().x;
        GUIHelper.SplitRectHorizontallyForRight(position, selectorWidth, out var fieldRect, out var selectorRect);

        using (var check = new EditorGUI.ChangeCheckScope())
        {
            var selectedMaterial = EditorGUI.ObjectField(fieldRect, GUIContent.none, null, typeof(Material), false) as Material;
            if (check.changed && selectedMaterial != null) AddMaterialUsageSlots(slotsProperty, selectedMaterial);
        }

        MaterialSelector.Draw(selectorRect, () => Utils.GetAllTargetMaterialsInAvatar(slotsProperty),
            (material, _) => { if (material != null) AddMaterialUsageSlots(slotsProperty, material); });
    }


    private static void AddMaterialUsageSlots(SerializedProperty slotsProperty, Material material)
    {
        foreach (var slot in GetMaterialUsageSlots(slotsProperty, material))
        {
            MaterialSlotReferenceCollectionUI.AppendSlot(slotsProperty, slot);
        }

        slotsProperty.serializedObject.ApplyModifiedProperties();
    }

    private static MaterialSlotReference[] GetSlotsUnderGameObject(SerializedProperty slotsProperty, GameObject target)
    {
        if (!slotsProperty.TryGetGameObject(out var gameObject)) return Array.Empty<MaterialSlotReference>();

        var root = Utils.FindAvatarInParents(gameObject);
        if (root == null || !target.transform.IsChildOf(root.transform)) return Array.Empty<MaterialSlotReference>();

        var renderers = MaterialEditorProcessor
            .GetTargetRenderers(root)
            .Where(renderer => renderer != null && renderer.transform.IsChildOf(target.transform))
            .ToList();

        return new DefaultMaterialTargeting()
            .GetAssignments(renderers)
            .Select(assignment => new MaterialSlotReference
            {
                RendererReference = new AvatarObjectReference(assignment.SlotId.Renderer.gameObject),
                MaterialIndex = assignment.SlotId.MaterialIndex,
            })
            .Where(slot => !MaterialSlotReferenceCollectionUI.ContainsSlot(slotsProperty, slot))
            .ToArray();
    }

    private static MaterialSlotReference[] GetMaterialUsageSlots(SerializedProperty slotsProperty, Material material)
    {
        if (!slotsProperty.TryGetGameObject(out var gameObject)) return Array.Empty<MaterialSlotReference>();

        return MaterialSlotReferenceCollectionUI
            .EnumerateMaterialUsages(gameObject, material)
            .Where(slot => !MaterialSlotReferenceCollectionUI.ContainsSlot(slotsProperty, slot))
            .ToArray();
    }

    private static float GetMiddleContentHeight(SerializedProperty slotsProperty)
    {
        if (!slotsProperty.isExpanded)
        {
            return GUIHelper.propertyHeight;
        }

        var height = GUIHelper.propertyHeight * 5 + GUIHelper.GUI_SPACE * 4;
        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("targetSettings.slotTargets.add.help".LS(), MessageType.Info);
        }
        height += GUIHelper.GUI_SPACE * 2; // 背景をHeloBoxで描画する分スペースを多めにとる
        return height;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var slots = property.FindPropertyRelative(nameof(SlotTargetSettings.TargetSlots));
        var listOptions = CreateListOptions(slots);
        return MaterialSlotReferenceCollectionUI.GetHeight(slots, GUIContent.none, listOptions);
    }
}
