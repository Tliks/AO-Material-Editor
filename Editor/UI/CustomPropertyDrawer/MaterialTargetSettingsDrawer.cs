namespace Aoyon.MaterialEditor.UI;

[CustomPropertyDrawer(typeof(MaterialTargetSettings))]
internal class MaterialTargetSettingsDrawer : PropertyDrawer
{
    private const float SettingsBackgroundTopSpacing = 2f;

    private static readonly string[] ModeOptionKeys =
    {
        "targetSettings.mode.singleMaterial",
        "targetSettings.mode.bulkEdit",
        "targetSettings.mode.slotTargets",
    };

    private static readonly string[] BulkModeOptionKeys =
    {
        "targetSettings.mode.bulkMaterials",
        "targetSettings.mode.allMaterials",
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var propertyScope = new EditorGUI.PropertyScope(position, label, property);
        position.SetSingleHeight();

        EditorGUI.LabelField(position, "targetSettings.mode.label".LS(), EditorStyles.boldLabel);
        position.NewLine();

        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        DrawModeToolbar(position, mode);
        position.NewLine();

        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        if (ShouldShowMainModeHelpBox())
        {
            position = GUIHelper.HelpBox(position, GetMainModeHelpKey(selectionMode).LS(), MessageType.Info);
        }

        position.Space();

        EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
        position.NewLine();
        position.y += SettingsBackgroundTopSpacing;

        var settingsBackgroundRect = position;
        settingsBackgroundRect.height = GetTargetSettingsHeaderHeight(property) + GetTargetSettingsHeight(property);
        settingsBackgroundRect = new RectOffset(4, 3, 3, 3).Add(settingsBackgroundRect);
        EditorGUI.LabelField(settingsBackgroundRect, GUIContent.none, EditorStyles.helpBox);

        if (IsBulkEditMode(selectionMode))
        {
            DrawBulkModeToolbar(position, mode);
            position.NewLine();
            selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        }

        if (ShouldShowBulkModeHelpBox(selectionMode))
        {
            position = GUIHelper.HelpBox(position, GetBulkModeHelpKey(selectionMode).LS(), MessageType.Info);
        }

        var settingsProperty = GetModeSettingsProperty(property, selectionMode);
        if (settingsProperty == null) return;

        position.height = EditorGUI.GetPropertyHeight(settingsProperty, includeChildren: true);
        EditorGUI.PropertyField(position, settingsProperty, GUIContent.none, includeChildren: true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = GUIHelper.propertyHeight; // モードラベル
        height += GUIHelper.GUI_SPACE + GUIHelper.propertyHeight; // モードツールバー
        height += GetMainModeHelpHeight(property);
        height += GUIHelper.GUI_SPACE + GUIHelper.LayoutSpace; // セクション間の余白
        height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE; // 編集対象ラベル
        height += SettingsBackgroundTopSpacing;
        height += GetTargetSettingsHeaderHeight(property);
        height += GetTargetSettingsHeight(property);
        height += 3; // 背景のオフセット分
        return height;
    }

    private static float GetTargetSettingsHeaderHeight(SerializedProperty property)
    {
        var height = 0f;
        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        if (IsBulkEditMode(selectionMode))
        {
            height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        }
        if (ShouldShowBulkModeHelpBox(selectionMode))
        {
            height += GUIHelper.GetHelpBoxHeight(GetBulkModeHelpKey(selectionMode).LS(), MessageType.Info);
            height += GUIHelper.GUI_SPACE;
        }
        return height;
    }

    private static float GetMainModeHelpHeight(SerializedProperty property)
    {
        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        if (!ShouldShowMainModeHelpBox()) return 0f;

        return GUIHelper.GetHelpBoxHeight(GetMainModeHelpKey(selectionMode).LS(), MessageType.Info)
            + GUIHelper.GUI_SPACE;
    }

    private static float GetTargetSettingsHeight(SerializedProperty property)
    {
        var mode = property.FindPropertyRelative(nameof(MaterialTargetSettings.Mode));
        var selectionMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        var settingsProperty = GetModeSettingsProperty(property, selectionMode);
        return settingsProperty != null ? EditorGUI.GetPropertyHeight(settingsProperty, includeChildren: true) : 0f;
    }

    private static void DrawModeToolbar(Rect position, SerializedProperty mode)
    {
        using var scope = new EditorGUI.PropertyScope(position, "targetSettings.mode.label".LG(), mode);

        var currentMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        var currentIndex = GetModeToolbarIndex(currentMode);
        var newIndex = LocalizedToolbar.Draw(position, currentIndex, ModeOptionKeys);
        if (newIndex == currentIndex) return;

        mode.enumValueIndex = (int)(newIndex switch
        {
            0 => MaterialTargetSettings.SelectionMode.SingleMaterial,
            1 => IsBulkEditMode(currentMode) ? currentMode : MaterialTargetSettings.SelectionMode.BulkMaterials,
            2 => MaterialTargetSettings.SelectionMode.SlotTargets,
            _ => currentMode,
        });
    }

    private static void DrawBulkModeToolbar(Rect position, SerializedProperty mode)
    {
        using var scope = new EditorGUI.PropertyScope(position, "targetSettings.mode.label".LG(), mode);

        var currentMode = (MaterialTargetSettings.SelectionMode)mode.enumValueIndex;
        var currentIndex = currentMode == MaterialTargetSettings.SelectionMode.AllMaterials ? 1 : 0;
        var newIndex = LocalizedToolbar.Draw(position, currentIndex, BulkModeOptionKeys, "targetSettings.mode.label");
        if (newIndex == currentIndex) return;

        mode.enumValueIndex = (int)(newIndex == 1
            ? MaterialTargetSettings.SelectionMode.AllMaterials
            : MaterialTargetSettings.SelectionMode.BulkMaterials);
    }

    private static int GetModeToolbarIndex(MaterialTargetSettings.SelectionMode mode)
    {
        return mode switch
        {
            MaterialTargetSettings.SelectionMode.SingleMaterial => 0,
            MaterialTargetSettings.SelectionMode.BulkMaterials => 1,
            MaterialTargetSettings.SelectionMode.AllMaterials => 1,
            MaterialTargetSettings.SelectionMode.SlotTargets => 2,
            _ => 0,
        };
    }

    private static bool IsBulkEditMode(MaterialTargetSettings.SelectionMode mode)
    {
        return mode == MaterialTargetSettings.SelectionMode.BulkMaterials
            || mode == MaterialTargetSettings.SelectionMode.AllMaterials;
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

    private static bool ShouldShowMainModeHelpBox()
    {
        return MaterialEditorSettings.ShowInspectorDescription;
    }

    private static bool ShouldShowBulkModeHelpBox(MaterialTargetSettings.SelectionMode mode)
    {
        return MaterialEditorSettings.ShowInspectorDescription
            && mode == MaterialTargetSettings.SelectionMode.AllMaterials;
    }

    private static string GetMainModeHelpKey(MaterialTargetSettings.SelectionMode mode)
    {
        return mode switch
        {
            MaterialTargetSettings.SelectionMode.SingleMaterial => "targetSettings.mode.singleMaterial.help",
            MaterialTargetSettings.SelectionMode.BulkMaterials => "targetSettings.mode.bulkEdit.help",
            MaterialTargetSettings.SelectionMode.SlotTargets => "targetSettings.mode.slotTargets.help",
            MaterialTargetSettings.SelectionMode.AllMaterials => "targetSettings.mode.bulkEdit.help",
            _ => string.Empty,
        };
    }

    private static string GetBulkModeHelpKey(MaterialTargetSettings.SelectionMode mode)
    {
        return mode switch
        {
            MaterialTargetSettings.SelectionMode.AllMaterials => "targetSettings.mode.allMaterials.help",
            _ => string.Empty,
        };
    }
}

[CustomPropertyDrawer(typeof(SingleMaterialTargetSettings))]
internal class SingleMaterialTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var material = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.TargetMaterial));
        var excludedSlots = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.ExcludedSlots));
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(
                Draw: true,
                RectStrict: true),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight,
            tailContent: new GUIHelper.ListContentOptions(
                rect => MaterialSlotReferenceCollectionUI.DrawAddSlotsSelector(rect, excludedSlots, () => GetUsageSlots(material, excludedSlots), "targetSettings.exclusions.selectUsageSlots".LS()),
                () => GUIHelper.propertyHeight));

        position.SetSingleHeight();
        LocalizedUI.PropertyField(position, material, "targetSettings.material.label");
        position.NewLine();
        var isExpanded = GUIHelper.Foldout(position, property, "targetSettings.exclusions.options".LG(), new(RectStrict: true));

        if (!isExpanded) return;
        position.Indent();

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            position.NewLine();
            position.height = GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            GUIHelper.HelpBox(position, "targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
        }

        position.NewLine();
        position.SetSingleHeight();
        position = MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), excludedSlotsListOptions, null);
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
        var excludedSlots = property.FindPropertyRelative(nameof(SingleMaterialTargetSettings.ExcludedSlots));
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(
                Draw: true,
                RectStrict: true),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight,
            tailContent: new GUIHelper.ListContentOptions(_ => { }, () => GUIHelper.propertyHeight));

        var height = EditorGUI.GetPropertyHeight(material);
        height += GUIHelper.GUI_SPACE + GUIHelper.propertyHeight;
        if (property.isExpanded)
        {
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }
            height += GUIHelper.GUI_SPACE + MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions, null);
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
        var excludedSlots = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.ExcludedSlots));
        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var materialsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: maxVisibleListHeight);
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: true, RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight,
            tailContent: new GUIHelper.ListContentOptions(
                rect => MaterialSlotReferenceCollectionUI.DrawAddSlotsSelector(rect, excludedSlots, () => GetUsageSlots(materials, excludedSlots), "targetSettings.exclusions.selectUsageSlots".LS()),
                () => GUIHelper.propertyHeight));

        position = MaterialCollectionUI.Draw(position, materials, "targetSettings.materials.label".LG(), materialsListOptions, MaterialCollectionUI.MaterialOrRendererDropHandler);
        position.SetSingleHeight();
        var isExpanded = GUIHelper.Foldout(position, property, "targetSettings.exclusions.options".LG(), new(RectStrict: true));

        if (!isExpanded) return;
        position.Indent();

        if (MaterialEditorSettings.ShowInspectorDescription)
        {
            position.NewLine();
            position.height = GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            GUIHelper.HelpBox(position, "targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
        }

        position.NewLine();
        position.SetSingleHeight();
        position = MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), excludedSlotsListOptions, null);
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
        var excludedSlots = property.FindPropertyRelative(nameof(BulkMaterialTargetSettings.ExcludedSlots));
        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var materialsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: maxVisibleListHeight);
        var excludedSlotsListOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: true, RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight,
            tailContent: new GUIHelper.ListContentOptions(_ => { }, () => GUIHelper.propertyHeight));

        var height = MaterialCollectionUI.GetHeight(materials, GUIContent.none, materialsListOptions, MaterialCollectionUI.MaterialOrRendererDropHandler);
        height += GUIHelper.GUI_SPACE + GUIHelper.propertyHeight;
        if (property.isExpanded)
        {
            if (MaterialEditorSettings.ShowInspectorDescription)
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("targetSettings.exclusions.useSlots.help".LS(), MessageType.Info);
            }
            height += GUIHelper.GUI_SPACE + MaterialSlotReferenceCollectionUI.GetHeight(excludedSlots, GUIContent.none, excludedSlotsListOptions, null);
        }
        return height;
    }
}

[CustomPropertyDrawer(typeof(AllMaterialSettings))]
internal class AllMaterialSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var excludedMaterials = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedMaterials));
        var excludedSlots = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedSlots));
        var excludedObjects = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedObjects));

        position.SetSingleHeight();
        var isExpanded = GUIHelper.Foldout(position, property, "targetSettings.exclusions.options".LG(), new(RectStrict: true));
        if (!isExpanded) return;

        position.Indent();

        var maxVisibleListHeight = GUIHelper.DefaultScrollableListHeight;
        var listOptions = new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(RectStrict: true),
            maxVisibleListHeight: maxVisibleListHeight);

        position.NewLine();
        position = MaterialCollectionUI.Draw(position, excludedMaterials, "targetSettings.exclusions.materials".LG(), listOptions);
        position = MaterialSlotReferenceCollectionUI.Draw(position, excludedSlots, "targetSettings.exclusions.slots".LG(), listOptions);
        position = AvatarObjectReferenceCollectionUI.Draw(position, excludedObjects, "targetSettings.exclusions.objects".LG(), listOptions);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var excludedMaterials = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedMaterials));
        var excludedSlots = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedSlots));
        var excludedObjects = property.FindPropertyRelative(nameof(AllMaterialSettings.ExcludedObjects));

        var height = GUIHelper.propertyHeight;
        if (!property.isExpanded) return height;

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

[CustomPropertyDrawer(typeof(SlotTargetSettings))]
internal class SlotTargetSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var slots = property.FindPropertyRelative(nameof(SlotTargetSettings.TargetSlots));
        var listOptions = CreateListOptions(slots);
        position = MaterialSlotReferenceCollectionUI.Draw(position, slots, "targetSettings.slotTargets.label".LG(), listOptions);
    }

    private static GUIHelper.ListOptions CreateListOptions(SerializedProperty slotsProperty)
    {
        return new GUIHelper.ListOptions(
            foldout: new GUIHelper.FoldoutOptions(Draw: false),
            maxVisibleListHeight: GUIHelper.DefaultScrollableListHeight);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var slots = property.FindPropertyRelative(nameof(SlotTargetSettings.TargetSlots));
        var listOptions = CreateListOptions(slots);
        return MaterialSlotReferenceCollectionUI.GetHeight(slots, GUIContent.none, listOptions);
    }
}
