using Aoyon.MaterialEditor.Processor;
using nadena.dev.modular_avatar.core;

namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialTargetSettings))]
internal class MaterialTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.SetSingleHeight();

        using (new EditorGUI.PropertyScope(position, label, property))
        {
            EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
            position.NewLine();
        }

        position.y += GUIHelper.GUI_SPACE;
        
        var helpBoxRect = position;
        helpBoxRect.height = GetInnerHeight(property);
        helpBoxRect = new RectOffset(5, 3, 5, 5).Add(helpBoxRect);
        EditorGUI.LabelField(helpBoxRect, GUIContent.none, EditorStyles.helpBox);

        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        LocalizedPopup.Field(position, mode, "targetSettings.mode.label", LocalizedUI.GetEnumOptionKeys("targetSettings.mode", typeof(MaterialTargetSettings.SelectionMode)));
        position.NewLine();

        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        if (ShouldShowHelpBox(selectionMode))
        {
            position = GUIHelper.HelpBox(position, GetHelpKey(selectionMode).LS(), MessageType.Info);
        }

        var settingsProperty = GetModeSettingsProperty(property, selectionMode);
        if (settingsProperty == null) return;

        position.height = EditorGUI.GetPropertyHeight(settingsProperty, includeChildren: true);
        EditorGUI.PropertyField(position, settingsProperty, GUIContent.none, includeChildren: true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = GUIHelper.propertyHeight;
        height += GUIHelper.GUI_SPACE;
        height += GetInnerHeight(property);
        return height;
    }

    private static float GetInnerHeight(SerializedProperty property)
    {
        var height = 0f;
        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        height += EditorGUI.GetPropertyHeight(mode);
        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        if (ShouldShowHelpBox(selectionMode))
        {
            height += GUIHelper.GUI_SPACE;
            height += GUIHelper.GetHelpBoxHeight(GetHelpKey(selectionMode).LS(), MessageType.Info);
        }

        var settingsProperty = GetModeSettingsProperty(property, selectionMode);
        if (settingsProperty != null)
        {
            height += GUIHelper.GUI_SPACE;
            height += EditorGUI.GetPropertyHeight(settingsProperty, includeChildren: true);
        }
        return height;
    }

    private static SerializedProperty? GetModeSettingsProperty(SerializedProperty property, MaterialTargetSettings.SelectionMode mode)
    {
        return mode switch
        {
            MaterialTargetSettings.SelectionMode.SingleMaterial => property.FindPropertyRelative(nameof(MaterialTargetSettings.SingleMaterial)),
            MaterialTargetSettings.SelectionMode.BulkMaterials => property.FindPropertyRelative(nameof(MaterialTargetSettings.BulkMaterials)),
            MaterialTargetSettings.SelectionMode.SlotTargets => property.FindPropertyRelative(nameof(MaterialTargetSettings.SlotTargets)),
            MaterialTargetSettings.SelectionMode.AllMaterials => property.FindPropertyRelative(nameof(MaterialTargetSettings.AllMaterials)),
            _ => null,
        };
    }

    private static bool ShouldShowHelpBox(MaterialTargetSettings.SelectionMode mode)
    {
        return MaterialEditorSettings.ShowInspectorDescription && (
            mode == MaterialTargetSettings.SelectionMode.BulkMaterials
            || mode == MaterialTargetSettings.SelectionMode.SlotTargets
            || mode == MaterialTargetSettings.SelectionMode.AllMaterials);
    }

    private static string GetHelpKey(MaterialTargetSettings.SelectionMode mode)
    {
        return mode switch
        {
            MaterialTargetSettings.SelectionMode.SingleMaterial => "targetSettings.mode.singleMaterial.help",
            MaterialTargetSettings.SelectionMode.BulkMaterials => "targetSettings.mode.bulkMaterials.help",
            MaterialTargetSettings.SelectionMode.SlotTargets => "targetSettings.mode.slotTargets.help",
            MaterialTargetSettings.SelectionMode.AllMaterials => "targetSettings.mode.allMaterials.help",
            _ => "targetSettings.mode.singleMaterial.help",
        };
    }
}

[CustomPropertyDrawer(typeof(SingleMaterialTargetSettings))]
internal class SingleMaterialTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var material = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.TargetMaterial));
        var useExclusions = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.ExcludedSlots));
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(
                Draw: false,
                RectStrict: true),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(
                rect => MaterialSlotReferenceCollectionUI.DrawAddSlotsSelector(rect, excludedSlots, () => GetUsageSlots(material, excludedSlots), "targetSettings.exclusions.selectUsageSlots".LS()),
                () => GUIHelper.propertyHeight));

        position.SetSingleHeight();
        LocalizedUI.PropertyField(position, material, "targetSettings.material.label");
        position.NewLine();
        (var isExpanded, var isEnabled) = GUIHelper.FoldoutAndToggleLeft(position, useExclusions, "targetSettings.exclusions.useSlots".LG(), true);

        if (!isExpanded) return;
        position.Indent();

        var hasTargetMaterial = material.objectReferenceValue != null;

        if (!hasTargetMaterial)
        {
            position.NewLine();
            position.height = GUIHelper.GetHelpBoxHeight("editor.noMaterialSelected.help".LS(), MessageType.Warning);
            GUIHelper.HelpBox(position, "editor.noMaterialSelected.help".LS(), MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!isEnabled || !hasTargetMaterial))
        {
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                position.NewLine();
                position.height = GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
                GUIHelper.HelpBox(position, "targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }

            position.NewLine();
            position.SetSingleHeight();
            position.height = MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions);
            MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), excludedSlotsListOptions);
        }
    }

    private static MaterialSlotReference[] GetUsageSlots(SerializedProperty materialProperty, SerializedProperty excludedSlotsProperty)
    {
        if (!materialProperty.TryGetGameObject(out var gameObject)) return Array.Empty<MaterialSlotReference>();
        if (materialProperty.objectReferenceValue is not Material material) return Array.Empty<MaterialSlotReference>();
        return MaterialSlotReferenceCollectionUI.EnumerateMaterialUsages(gameObject, material)
            .Where(slot => !MaterialSlotReferenceCollectionUI.ContainsSlot(excludedSlotsProperty, slot))
            .ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var material = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.TargetMaterial));
        var useExclusions = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.ExcludedSlots));
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(
                Draw: false,
                RectStrict: true),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(_ => { }, () => GUIHelper.propertyHeight));

        var height = EditorGUI.GetPropertyHeight(material);
        height += GUIHelper.GUI_SPACE + EditorGUI.GetPropertyHeight(useExclusions);
        if (useExclusions.isExpanded)
        {
            if (material.objectReferenceValue == null)
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("editor.noMaterialSelected.help".LS(), MessageType.Warning);
            }
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }
            height += GUIHelper.GUI_SPACE + MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions);
        }
        return height;
    }
}

[CustomPropertyDrawer(typeof(BulkMaterialTargetSettings))]
internal class BulkMaterialTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var materials = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.TargetMaterials));
        var useExclusions = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.ExcludedSlots));
        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var materialsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: maxVisibleListHeight);
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false, RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(
                rect => MaterialSlotReferenceCollectionUI.DrawAddSlotsSelector(rect, excludedSlots, () => GetUsageSlots(materials, excludedSlots), "targetSettings.exclusions.selectUsageSlots".LS()),
                () => GUIHelper.propertyHeight));

        position.height = MaterialCollectionUI.GetHeight(materials, GUIContent.none, materialsListOptions);
        MaterialCollectionUI.Draw(position, materials, "targetSettings.materials.label".LG(), materialsListOptions);
        position.NewLine();
        position.SetSingleHeight();
        (var isExpanded, var isEnabled) = GUIHelper.FoldoutAndToggleLeft(position, useExclusions, "targetSettings.exclusions.useSlots".LG(), true);

        if (!isExpanded) return;
        position.Indent();

        var hasTargetMaterials = HasTargetMaterials(materials);

        if (!hasTargetMaterials)
        {
            position.NewLine();
            position.height = GUIHelper.GetHelpBoxHeight("editor.noMaterialSelected.help".LS(), MessageType.Warning);
            GUIHelper.HelpBox(position, "editor.noMaterialSelected.help".LS(), MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!isEnabled || !hasTargetMaterials))
        {
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                position.NewLine();
                position.height = GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
                GUIHelper.HelpBox(position, "targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }

            position.NewLine();
            position.SetSingleHeight();
            position.height = MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions);
            MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), excludedSlotsListOptions);
        }
    }

    private static bool HasTargetMaterials(SerializedProperty materialsProperty)
    {
        for (int i = 0; i < materialsProperty.arraySize; i++)
        {
            var material = materialsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Material;
            if (material == null) continue;
            return true;
        }
        return false;
    }

    private static MaterialSlotReference[] GetUsageSlots(SerializedProperty materialsProperty, SerializedProperty excludedSlotsProperty)
    {
        if (!materialsProperty.TryGetGameObject(out var gameObject)) return Array.Empty<MaterialSlotReference>();
        var result = new List<MaterialSlotReference>();
        for (int i = 0; i < materialsProperty.arraySize; i++)
        {
            var material = materialsProperty.GetArrayElementAtIndex(i).objectReferenceValue as Material;
            if (material == null) continue;
            foreach (var slot in MaterialSlotReferenceCollectionUI.EnumerateMaterialUsages(gameObject, material))
            {
                if (MaterialSlotReferenceCollectionUI.ContainsSlot(excludedSlotsProperty, slot)) continue;
                result.Add(slot);
            }
        }
        return result.ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var materials = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.TargetMaterials));
        var useExclusions = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.UseSlotExclusions));
        var excludedSlots = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.ExcludedSlots));
        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var materialsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: maxVisibleListHeight);
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false, RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight,
            middleContent: new GUIHelper.ListMiddleContentOptions(_ => { }, () => GUIHelper.propertyHeight));

        var height = MaterialCollectionUI.GetHeight(materials, GUIContent.none, materialsListOptions);
        height += GUIHelper.GUI_SPACE + EditorGUI.GetPropertyHeight(useExclusions);
        if (useExclusions.isExpanded)
        {
            if (!HasTargetMaterials(materials))
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("editor.noMaterialSelected.help".LS(), MessageType.Warning);
            }
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }
            height += GUIHelper.GUI_SPACE + MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions);
        }
        return height;
    }
}

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

[CustomPropertyDrawer(typeof(AllMaterialSettings))]
internal class AllMaterialSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var useExclusions = property.FindPropertyRelative(nameof(AllMaterialSettings.UseExclusions));
        var excludedMaterials = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedMaterials));
        var excludedSlots = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedSlots));
        var excludedObjects = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedObjects));

        position.SetSingleHeight();
        (var isExpanded, var isEnabled) = GUIHelper.FoldoutAndToggleLeft(position, useExclusions, "targetSettings.exclusions.use".LG(), true);
        if (!isExpanded) return;

        using (new EditorGUI.DisabledScope(!isEnabled))
        {
            position.Indent();

            var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
            var listOptions = new GUIHelper.ListOptions(
                foldout: new GUIHelper.FoldoutOptions(RectStrict: true),
                maxVisibleListHeight: maxVisibleListHeight);

            position.NewLine();
            position.height = MaterialCollectionUI.GetHeight(excludedMaterials, GUIContent.none, listOptions);
            MaterialCollectionUI.Draw(position, excludedMaterials, "targetSettings.exclusions.materials".LG(), listOptions);
            position.NewLine();
            position.height = MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, listOptions);
            MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), listOptions);
            position.NewLine();
            position.height = AvatarObjectReferenceCollectionUI.GetHeight(excludedObjects, GUIContent.none, listOptions);
            AvatarObjectReferenceCollectionUI.Draw(position, excludedObjects, "targetSettings.exclusions.objects".LG(), listOptions);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var useExclusions = property.FindPropertyRelative(nameof(AllMaterialSettings.UseExclusions));
        var excludedMaterials = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedMaterials));
        var excludedSlots = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedSlots));
        var excludedObjects = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedObjects));

        var height = GUIHelper.propertyHeight;
        if (!useExclusions.isExpanded) return height;

        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var listOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight);
        height += GUIHelper.GUI_SPACE + MaterialCollectionUI.GetHeight(excludedMaterials, GUIContent.none, listOptions);
        height += GUIHelper.GUI_SPACE + MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, listOptions);
        height += GUIHelper.GUI_SPACE + AvatarObjectReferenceCollectionUI.GetHeight(excludedObjects, GUIContent.none, listOptions);
        return height;
    }
}
