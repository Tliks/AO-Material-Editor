using UnityEngine.Pool;
using Aoyon.MaterialEditor.Processor;
using UnityEditorInternal;
using Aoyon.MaterialEditor.Migration;

namespace Aoyon.MaterialEditor.UI;

[CustomEditor(typeof(MaterialEditorComponent))]
internal class MaterialEditorEditor : Editor
{
    private MaterialEditorComponent _target = null!;
    private GameObject? _avatarRoot;

    private SerializedProperty _targetSettings = null!;
    private SerializedProperty _overrideSettings = null!;

    private IMaterialTargeting _materialTargeting = null!;
    private HashSet<MaterialAssignment> _allAssignments = null!;
    private HashSet<Material> _targetMaterials = null!;

    // recoding UI fields
    private Material? _recordingSourceMaterial;
    private Material? _unlockedRecordingSourceMaterial;
    private Material _recordingMaterial = null!;
    private UnityEditor.MaterialEditor _materialEditor = null!;
    private MaterialTargetSettings? _cachedTargetSettings;

    private MaterialOverrideSettings _beforeOverrides = MaterialOverrideSettings.Empty;
    private MaterialOverrideSettings _afterOverrides = MaterialOverrideSettings.Empty;
    private MaterialOverrideSettings? _pendingSelfCommittedOverrides;
    private bool _pendingSelfCommittedRecordingMaterialChange;
    private bool _showEditorDisplaySettings = false;

    private const string RecordingMaterialName = "Recording… (Cloned)";
    private const float EditorContentLeftPadding = 4f;
    private static readonly GUIHelper.FoldoutOptions RectStrictFoldoutOptions = new(RectStrict: true);

    private void OnEnable()
    {
        _target = (MaterialEditorComponent)target;
        _avatarRoot = Utils.FindAvatarInParents(_target.gameObject);

        _targetSettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.TargetSettings));
        _overrideSettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.OverrideSettings));

        _materialTargeting = new DefaultMaterialTargeting();
        var renderers = _avatarRoot != null ? MaterialEditorProcessor.GetTargetRenderers(_avatarRoot) : new List<Renderer>();
        _allAssignments = _materialTargeting.GetAssignments(renderers).ToHashSet();
        _targetMaterials = UpdateTargetMaterials();
        _cachedTargetSettings = _target.TargetSettings.Clone();

        _recordingSourceMaterial = AutoSelectRecordingSourceMaterial();
        if (_recordingSourceMaterial != null) { 
            _unlockedRecordingSourceMaterial = CreateUnlockedRecordingSourceMaterial(_recordingSourceMaterial);
            _recordingMaterial = new Material(_unlockedRecordingSourceMaterial) { name = RecordingMaterialName };
            UpdateOtherOverrides();
            SyncRecordingMaterialFromComponent();
        }
        else {
            _recordingMaterial = new Material(Shader.Find("Standard")) { name = RecordingMaterialName };
        }
        _materialEditor = (UnityEditor.MaterialEditor)CreateEditor(_recordingMaterial, typeof(UnityEditor.MaterialEditor));
        InternalEditorUtility.SetIsInspectorExpanded(_recordingMaterial, true); // 初期状態でEditorを展開しておく

        ObjectChangeEvents.changesPublished += OnObjectChanged;
        MaterialEditoEditorContext.StartRecording(
            this,
            _target,
            _recordingMaterial,
            GetCurrentOverridePropertyNames(),
            GetCurrentOverrideSerializedProperties(),
            GetLockedPropertyNames(),
            GetLockedSerializedProperties(),
            IsShaderLocked(),
            IsRenderQueueLocked());
        MaterialEditoEditorContext.OnRecordingEntryStateChanged += OnRecordingEntryStateChanged;
        MaterialEditoEditorContext.OnRecordingOverrideStateChanged += OnRecordingOverrideStateChanged;
    }

    private void OnDisable()
    {
        MaterialEditoEditorContext.StopRecording(_recordingMaterial, _target);

        if (_recordingMaterial != null) { DestroyImmediate(_recordingMaterial); }
        if (_materialEditor != null) { DestroyImmediate(_materialEditor); }

        ObjectChangeEvents.changesPublished -= OnObjectChanged;
        MaterialEditoEditorContext.OnRecordingEntryStateChanged -= OnRecordingEntryStateChanged;
        MaterialEditoEditorContext.OnRecordingOverrideStateChanged -= OnRecordingOverrideStateChanged;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeaderSettings();
        if (!Migrator.CheckAndDrawMigrationButton(_target)) {
            return;
        }
        DrawInformationGUI();
        EditorGUILayout.Space();
        DrawEntrySettings();
        EditorGUILayout.Space();
        DrawEditor();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeaderSettings()
    {
        using var scope = new EditorGUILayout.HorizontalScope();
        DrawLanguageSelector();
        DrawDescriptionToggle();
    }

    private void DrawLanguageSelector()
    {
        var label = new GUIContent("Language");
        var labelWidth = EditorStyles.label.CalcSize(label).x + 2f;
        GUILayout.Label(label, GUILayout.Width(labelWidth));
        Localization.DrawLanguagePopupWithoutLabel(GUILayout.ExpandWidth(true));
    }

    private void DrawDescriptionToggle()
    {
        var label = "editor.showDescription".LG();
        var options = new[]
        {
            "common.on".LG(),
            "common.off".LG()
        };
        var labelWidth = EditorStyles.label.CalcSize(label).x + 2f;
        var itemWidth = options.Max(option => EditorStyles.toolbarButton.CalcSize(option).x) + 6f;
        var toolbarWidth = itemWidth * options.Length;
        var selected = MaterialEditorSettings.ShowInspectorDescription ? 0 : 1;

        GUILayout.Label(label, GUILayout.Width(labelWidth));
        MaterialEditorSettings.ShowInspectorDescription = GUILayout.Toolbar(
            selected,
            options,
            GUILayout.Width(toolbarWidth)) == 0;
    }

    private void DrawInformationGUI()
    {
        if (_avatarRoot == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("editor.noAvatarRoot.help".LS(), MessageType.Warning);
        }

        var effective = MaterialEditorProcessor.IsEffective(_target);
        if (!effective)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("editor.notEffective.help".LS(), MessageType.Warning);
        }
    }

    private void DrawEntrySettings()
    {
        var label = "targetSettings.title".LS();
        EditorGUILayout.PropertyField(_targetSettings, new GUIContent(label));
    }

    private void DrawEditor()
    {
        EditorGUILayout.LabelField("editor.title".LS(), EditorStyles.boldLabel);

        if (_recordingSourceMaterial != null && _materialEditor != null)
        {
            _materialEditor.DrawHeader();
            if (_materialEditor.isVisible) {
                var position = GetEditorPosition(GetEditorContentHeight());
                position = DrawOverridesGUI(position, "overrideSettings.help.visibleEditor");
                position = DrawOverrideUtility(position);
                position = DrawEditorDisplaySettings(position);
                position = DrawEditorHelp(position);
                GUIHelper.DrawFullWidthHorizontalLine(new Color(0.35f, 0.35f, 0.35f));
                // EditorGUILayout.Space();
                var stringTagsBefore = GetRecordingStringTags();
                _materialEditor.OnInspectorGUI();
                if (!StringTagsEqual(stringTagsBefore, GetRecordingStringTags()))
                {
                    CommitRecordingMaterialChange();
                    _pendingSelfCommittedRecordingMaterialChange = true;
                }
            }
            else {
                var editorPosition = GetEditorPosition(EditorGUI.GetPropertyHeight(_overrideSettings, true));
                DrawOverridesGUI(editorPosition, "overrideSettings.help.hiddenEditor");
            }
        }
        else 
        {
            EditorGUILayout.HelpBox("editor.noMaterialSelected.help".LS(), MessageType.Warning, true);
            var editorPosition = GetEditorPosition(EditorGUI.GetPropertyHeight(_overrideSettings, true));
            DrawOverridesGUI(editorPosition, "overrideSettings.help.noMaterial");
        }
    }

    private static Rect GetEditorPosition(float height)
    {
        return GUIHelper.AlignToMarginX(
            EditorGUILayout.GetControlRect(false, height),
            EditorContentLeftPadding);
    }

    private float GetEditorContentHeight()
    {
        return EditorGUI.GetPropertyHeight(_overrideSettings, true)
               + GUIHelper.GUI_SPACE
               + GetOverrideUtilityHeight()
               + GUIHelper.GUI_SPACE
               + GetEditorDisplaySettingsHeight()
               + GetEditorHelpHeight();
    }

    private Rect DrawOverridesGUI(Rect position, string helpKey)
    {
        position.height = EditorGUI.GetPropertyHeight(_overrideSettings, true);
        using (MaterialOverrideSettingsDrawer.HelpKeyScope(helpKey))
        {
            EditorGUI.PropertyField(position, _overrideSettings);
        }
        position.NewLine();
        return position;
    }

    private float GetEditorHelpHeight()
    {
        if (!MaterialEditorSettings.ShowInspectorDescription)
        {
            return 0f;
        }

        return GUIHelper.GetHelpBoxHeight("editor.help".LS(), MessageType.Info)
               + GUIHelper.GUI_SPACE;
    }

    private Rect DrawEditorHelp(Rect position)
    {
        if (!MaterialEditorSettings.ShowInspectorDescription)
        {
            return position;
        }

        var text = "editor.help".LS();
        position.height = GUIHelper.GetHelpBoxHeight(text, MessageType.Info);
        return GUIHelper.HelpBox(position, text, MessageType.Info);
    }
    
    private HashSet<Material> UpdateTargetMaterials()
    {
        _targetMaterials = MaterialEditorProcessor.SelectTargetAssignments(_allAssignments, _target)
            .Select(a => a.Material)
            .ToHashSet();
        return _targetMaterials;
    }

    private Rect DrawEditorDisplaySettings(Rect position)
    {
        if (_targetMaterials.Count < 2) return position;

        position.height = GUIHelper.propertyHeight;
        _showEditorDisplaySettings = GUIHelper.Foldout(
            position,
            _showEditorDisplaySettings,
            "editor.displaySettings".LG(),
            RectStrictFoldoutOptions);
        position.NewLine();
        if (!_showEditorDisplaySettings) return position;

        position.Indent();
        position = DrawRecordingSourceMaterial(position);
        position.Back();
        return position;
    }

    private Rect DrawRecordingSourceMaterial(Rect position)
    {
        position.height = GUIHelper.propertyHeight;
        GUIHelper.SplitRectHorizontallyForLeft(position, EditorGUIUtility.labelWidth, out var labelRect, out var fieldPosition);
        EditorGUI.LabelField(labelRect, "editor.recordingSourceMaterial".LG());
        var selectorWidth = MaterialSelector.GetSize().x;
        GUIHelper.SplitRectHorizontallyForLeft(fieldPosition, selectorWidth, out var selectorRect, out var objectFieldRect);

        MaterialSelector.Draw(selectorRect, () => _targetMaterials.ToArray(), (m, i) => { _recordingSourceMaterial = m; OnRecordingSourceMaterialChanged(); });
        using (new EditorGUI.DisabledGroupScope(true))
        {
            EditorGUI.ObjectField(objectFieldRect, GUIContent.none, _recordingSourceMaterial, typeof(Material), false);
        }

        position.NewLine();
        return position;
    }

    private float GetEditorDisplaySettingsHeight()
    {
        if (_targetMaterials.Count < 2) return 0f;

        var height = GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (_showEditorDisplaySettings)
        {
            height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        }

        return height;
    }

    private Material? AutoSelectRecordingSourceMaterial()
    {
        Material? newTarget;

        var current = _recordingSourceMaterial;
        if (current != null && _targetMaterials.Contains(current)) {
            newTarget = current;
        }
        else {
            newTarget = _targetMaterials.FirstOrDefault();
        }

        _recordingSourceMaterial = newTarget;
        return _recordingSourceMaterial;
    }

    private Material CreateUnlockedRecordingSourceMaterial(Material source)
    {
        var unlocked = new Material(source);
        MaterialUtility.Unlock(unlocked, source);
        return unlocked;
    }

    // PrefabuTility.PrefabInstanceUpdatedはPrefab Revertなどのイベントを拾わずRecording Materialの更新を行えない
    // これを回避するため、コンポーネントの変更とMaterialEditorを介したマテリアルの編集、両方のイベント取得をObjectChangeEventStream経由で行う
    private void OnObjectChanged(ref ObjectChangeEventStream stream)
    {
        DebugLog("OnObjectChanged, frame: " + Time.frameCount);

        var componentId = _target.GetInstanceID();
        var recordingMaterialId = _recordingMaterial.GetInstanceID();
        
        for (int i = 0; i < stream.length; i++)
        {
            var eventType = stream.GetEventType(i);
            
            if (eventType == ObjectChangeKind.ChangeGameObjectOrComponentProperties)
            {
                stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var data);
                if (data.instanceId == componentId)
                {
                    // AdvancedDropdown などは changed を立てないため、ここで検知する。
                    if (!_target.TargetSettings.Equals(_cachedTargetSettings))
                    {
                        _cachedTargetSettings = _target.TargetSettings.Clone();
                        OnEntrySettingsChanged();
                    }
                    else // その他(overrides)の変更
                    {
                        if (TryConsumeSelfCommittedOverrideEcho())
                        {
                            continue;
                        }

                        SyncRecordingMaterialFromComponent();
                        UpdateRecordingOverrideState();
                    }
                    return;
                }
            }
            else if (eventType == ObjectChangeKind.ChangeAssetObjectProperties)
            {
                stream.GetChangeAssetObjectPropertiesEvent(i, out var data);
                if (data.instanceId == recordingMaterialId)
                {
                    if (TryConsumeSelfCommittedRecordingMaterialChange())
                    {
                        continue;
                    }

                    CommitRecordingMaterialChange();
                    return;
                }
            }
        }
    }

    private void CommitRecordingMaterialChange()
    {
        if (this == null || _target == null || _recordingMaterial == null) return;

        if (!SanitizeRecordingMaterialAgainstAfter()) return; // サニタイズに失敗した状態でコンポーネントに書き込むべきではない

        var maintainEqualOverrides = _target.TargetSettings.Mode != MaterialTargetSettings.SelectionMode.SingleMaterial;
        var nextOverrides = BuildOverrideSettingsFromRecordingMaterial(maintainEqualOverrides);
        if (nextOverrides == null) return;

        CommitOverridesFromRecording(nextOverrides);
    }

    private Dictionary<string, string> GetRecordingStringTags()
    {
        using var so = new SerializedObject(_recordingMaterial);
        return MaterialUtility.GetStringTags(so);
    }

    private static bool StringTagsEqual(
        Dictionary<string, string> lhs,
        Dictionary<string, string> rhs)
    {
        if (lhs.Count != rhs.Count) return false;
        foreach (var (key, value) in lhs)
        {
            if (!rhs.TryGetValue(key, out var otherValue) || value != otherValue)
            {
                return false;
            }
        }

        return true;
    }

    private void OnEntrySettingsChanged()
    {
        UpdateTargetMaterials();
        AutoSelectRecordingSourceMaterial();
        OnRecordingSourceMaterialChanged();
        NotifyRecordingEntryStateChanged();
    }

    private void OnRecordingSourceMaterialChanged()
    {
        if (_recordingSourceMaterial != null) {
            _unlockedRecordingSourceMaterial = CreateUnlockedRecordingSourceMaterial(_recordingSourceMaterial);
        }
        else {
            _unlockedRecordingSourceMaterial = null;
        }
        UpdateOtherOverrides();
        UpdateRecordingLockedState();
        SyncRecordingMaterialFromComponent();
    }

    private void OnRecordingEntryStateChanged(MaterialEditorComponent component)
    {
        if (component == _target) return;
        OnOtherComponentChanged();
    }

    private void OnRecordingOverrideStateChanged(MaterialEditorComponent component)
    {
        if (component == _target) return;
        OnOtherComponentChanged();
    }

    // hierarchy上でenabledやeditoronlyが変化したり、削除、移動された場合にも呼ばれるべきではある
    // ObjectChangeEventStreamの拡張で対応可能だが、複雑なので、ここでは同時に開いている場合のみ追従するように
    // Todo
    private void OnOtherComponentChanged()
    {
        UpdateOtherOverrides();
        UpdateRecordingLockedState();
        SyncRecordingMaterialFromComponent();
    }

    private void UpdateOtherOverrides()
    {
        _beforeOverrides = MaterialOverrideSettings.Empty;
        _afterOverrides = MaterialOverrideSettings.Empty;

        if (_avatarRoot == null || _recordingSourceMaterial == null) return;

        var allComponents = _avatarRoot.GetComponentsInChildren<MaterialEditorComponent>(true);
        var myIndex = Array.IndexOf(allComponents, _target);
        if (myIndex == -1) return;

        for (int i = 0; i < allComponents.Length; i++)
        {
            if (i == myIndex) continue;
            var component = allComponents[i];
            if (!MaterialEditorProcessor.IsEffective(component)) continue;

            var targetAssignments = MaterialEditorProcessor.SelectTargetAssignments(_allAssignments, component);
            if (targetAssignments.Any(a => a.Material == _recordingSourceMaterial))
            {
                if (i < myIndex)
                {
                    _beforeOverrides.Merge(component.OverrideSettings);
                }
                else
                {
                    _afterOverrides.Merge(component.OverrideSettings);
                }
            }
        }
    }

    private HashSet<string> GetCurrentOverridePropertyNames()
    {
        return _target.OverrideSettings.PropertyOverrides
            .Select(p => p.PropertyName)
            .ToHashSet();
    }

    private HashSet<RecordingMaterialSerializedProperty> GetCurrentOverrideSerializedProperties()
    {
        var result = new HashSet<RecordingMaterialSerializedProperty>();
        if (_target.OverrideSettings.OverrideRenderQueue) result.Add(RecordingMaterialSerializedProperty.CustomRenderQueue);
        if (_target.OverrideSettings.OverrideLightmapFlags) result.Add(RecordingMaterialSerializedProperty.LightmapFlags);
        if (_target.OverrideSettings.OverrideEnableInstancing) result.Add(RecordingMaterialSerializedProperty.EnableInstancingVariants);
        if (_target.OverrideSettings.OverrideDoubleSidedGI) result.Add(RecordingMaterialSerializedProperty.DoubleSidedGI);
        return result;
    }

    private bool IsShaderLocked() => _afterOverrides.OverrideShader;
    private bool IsRenderQueueLocked() => _afterOverrides.OverrideRenderQueue;
    private HashSet<string> GetLockedPropertyNames() => _afterOverrides.PropertyOverrides
        .Select(p => p.PropertyName)
        .ToHashSet();

    private HashSet<RecordingMaterialSerializedProperty> GetLockedSerializedProperties()
    {
        var result = new HashSet<RecordingMaterialSerializedProperty>();
        if (_afterOverrides.OverrideRenderQueue) result.Add(RecordingMaterialSerializedProperty.CustomRenderQueue);
        if (_afterOverrides.OverrideLightmapFlags) result.Add(RecordingMaterialSerializedProperty.LightmapFlags);
        if (_afterOverrides.OverrideEnableInstancing) result.Add(RecordingMaterialSerializedProperty.EnableInstancingVariants);
        if (_afterOverrides.OverrideDoubleSidedGI) result.Add(RecordingMaterialSerializedProperty.DoubleSidedGI);
        return result;
    }

    private void NotifyRecordingEntryStateChanged()
    {
        MaterialEditoEditorContext.NotifyRecordingEntryStateChanged(_target);
    }

    private void UpdateRecordingOverrideState()
    {
        MaterialEditoEditorContext.UpdateRecordingOverrideState(
            _target,
            GetCurrentOverridePropertyNames(),
            GetCurrentOverrideSerializedProperties());
    }

    private void UpdateRecordingLockedState()
    {
        MaterialEditoEditorContext.UpdateLockedState(
            _target,
            IsShaderLocked(),
            IsRenderQueueLocked(),
            GetLockedPropertyNames(),
            GetLockedSerializedProperties());
    }

    private void SyncRecordingMaterialFromComponent()
    {        
        DebugLog("SyncRecordingMaterialFromComponent, frame: " + Time.frameCount);

        if (_unlockedRecordingSourceMaterial == null) return;

        serializedObject.ApplyModifiedProperties();

        // sourceの状態に初期化
        MaterialUtility.CopyAllSettings(_unlockedRecordingSourceMaterial, _recordingMaterial);
        // 1. 自分より上のオーバーライドを反映
        MaterialUtility.ApplyOverrideSettings(_recordingMaterial, _beforeOverrides);
        // 2. 自分自身のオーバーライドを反映
        MaterialUtility.ApplyOverrideSettings(_recordingMaterial, _target.OverrideSettings);
        // 3. 自分より下のオーバーライドを反映 (上書き)
        MaterialUtility.ApplyOverrideSettings(_recordingMaterial, _afterOverrides);
    }

    private bool SanitizeRecordingMaterialAgainstAfter()
    {
        if (_unlockedRecordingSourceMaterial == null) return true;

        if (!_afterOverrides.OverrideShader && !_afterOverrides.OverrideRenderQueue && _afterOverrides.PropertyOverrides.Count == 0) return true;

        var authoritative = new Material(_unlockedRecordingSourceMaterial);
        try
        {
            var sanitized = false;

            serializedObject.ApplyModifiedProperties();

            MaterialUtility.ApplyOverrideSettings(authoritative, _beforeOverrides);
            MaterialUtility.ApplyOverrideSettings(authoritative, _target.OverrideSettings);
            MaterialUtility.ApplyOverrideSettings(authoritative, _afterOverrides);

            var currentDiff = MaterialUtility.GetOverrides(authoritative, _recordingMaterial, false, true);
            var conflicts = GetAfterOverrideConflicts(currentDiff);

            // 固定されたシェーダーが変更された場合は、プロパティの空間が大規模に変わるので、同時に発生した変更を全て巻き戻す。
            if (conflicts.ShaderLocked)
            {
                MaterialUtility.CopyAllSettings(authoritative, _recordingMaterial);
                LocalizedLog.Warning("lock.shader.log", authoritative.shader.name);
                sanitized = true;
            }
            else
            {
                if (conflicts.RenderQueueLocked)
                {
                    using var authoritativeSo = new SerializedObject(authoritative);
                    using var recordingSo = new SerializedObject(_recordingMaterial);
                    var renderQueue = MaterialUtility.GetCustomRenderQueue(authoritativeSo);
                    MaterialUtility.SetCustomRenderQueue(recordingSo, renderQueue);
                    recordingSo.ApplyModifiedPropertiesWithoutUndo();
                    LocalizedLog.Warning("lock.renderQueue.log", renderQueue);
                    sanitized = true;
                }

                if (conflicts.LockedPropertyNames.Count > 0)
                {
                    using var _ = DictionaryPool<string, MaterialProperty>.Get(out var authoritativeProperties);
                    foreach (var property in MaterialUtility.GetProperties(authoritative)) authoritativeProperties[property.PropertyName] = property;

                    foreach (var propertyName in conflicts.LockedPropertyNames)
                    {
                        if (!authoritativeProperties.TryGetValue(propertyName, out var authoritativeProperty)) continue;

                        authoritativeProperty.TrySet(_recordingMaterial);
                        LocalizedLog.Warning("lock.property.log", propertyName, authoritativeProperty.PropertyValue);
                        sanitized = true;
                    }
                }
            }

            if (sanitized && _materialEditor != null)
            {
                _materialEditor.Repaint();
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }
        finally
        {
            DestroyImmediate(authoritative);
        }
    }

    private AfterOverrideConflicts GetAfterOverrideConflicts(MaterialOverrideSettings candidateOverrides)
    {
        using var _ = DictionaryPool<string, string>.Get(out var afterOverridesPropertyValues);
        foreach (var property in _afterOverrides.PropertyOverrides) afterOverridesPropertyValues[property.PropertyName] = property.PropertyValue;

        var lockedPropertyNames = candidateOverrides.PropertyOverrides
            .Where(property => afterOverridesPropertyValues.ContainsKey(property.PropertyName))
            .Select(property => property.PropertyName)
            .ToHashSet();

        return new AfterOverrideConflicts(
            _afterOverrides.OverrideShader && candidateOverrides.OverrideShader,
            _afterOverrides.OverrideRenderQueue && candidateOverrides.OverrideRenderQueue,
            lockedPropertyNames,
            afterOverridesPropertyValues.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private readonly record struct AfterOverrideConflicts(
        bool ShaderLocked,
        bool RenderQueueLocked,
        HashSet<string> LockedPropertyNames,
        Dictionary<string, string> LockedPropertyValues);

    private MaterialOverrideSettings? BuildOverrideSettingsFromRecordingMaterial(bool maintainEquals)
    {
        DebugLog("BuildOverrideSettingsFromRecordingMaterial, frame: " + Time.frameCount);

        if (_unlockedRecordingSourceMaterial == null) return null;

        var baseMaterial = CreateDiffBaseMaterial();
        try
        {
            var current = MaterialUtility.GetOverrides(baseMaterial, _recordingMaterial, false, true);
            if (current.OverrideShader && current.TargetShader != null)
            {
                current.PropertyOverrides = RemoveNewShaderDefaultPropertyOverrides(
                    current.PropertyOverrides,
                    current.TargetShader,
                    baseMaterial.shader);
            }

            MaterialOverrideSettings result;

            if (maintainEquals)
            {
                // 前段階として、編集によって元の値に戻った設定(新しい差分に存在しないが、これまで存在していた差分)に対して
                // これを維持するために、元の値を書き込む
                serializedObject.ApplyModifiedProperties();
                var previous = _target.OverrideSettings;
                var cloned = previous.Clone();
                MaintainEqualOverrides(previous, current, cloned, baseMaterial);
                // 新しい差分をマージ(上書き, 追加)する
                cloned.Merge(current);
                result = cloned;
            }
            else
            {
                result = current;
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
        finally
        {
            DestroyImmediate(baseMaterial);
        }
    }

    private Material CreateDiffBaseMaterial()
    {
        var baseMaterial = new Material(_unlockedRecordingSourceMaterial!);
        MaterialUtility.ApplyOverrideSettings(baseMaterial, _beforeOverrides);
        MaterialUtility.ApplyOverrideSettings(baseMaterial, _afterOverrides);
        return baseMaterial;
    }

    internal void RevertRecordingProperty(string propertyName)
    {
        ApplyManualRecordingMaterialChange(
            baseMaterial =>
            {
                if (TryGetBaseProperty(baseMaterial, out var baseProperty))
                {
                    baseProperty.TrySet(_recordingMaterial);
                }
            },
            settings => settings.PropertyOverrides.RemoveAll(property => property.PropertyName == propertyName));

        bool TryGetBaseProperty(Material baseMaterial, out MaterialProperty result)
        {
            foreach (var materialProperty in MaterialUtility.GetProperties(baseMaterial))
            {
                if (materialProperty.PropertyName != propertyName) continue;

                result = materialProperty;
                return true;
            }

            result = default;
            return false;
        }
    }

    internal void RevertRecordingSerializedProperty(RecordingMaterialSerializedProperty property)
    {
        ApplyManualRecordingMaterialChange(
            baseMaterial =>
            {
                using var baseSo = new SerializedObject(baseMaterial);
                using var recordingSo = new SerializedObject(_recordingMaterial);

                switch (property)
                {
                    case RecordingMaterialSerializedProperty.CustomRenderQueue:
                        MaterialUtility.SetCustomRenderQueue(recordingSo, MaterialUtility.GetCustomRenderQueue(baseSo));
                        break;
                    case RecordingMaterialSerializedProperty.LightmapFlags:
                        MaterialUtility.SetLightmapFlags(recordingSo, MaterialUtility.GetLightmapFlags(baseSo));
                        break;
                    case RecordingMaterialSerializedProperty.EnableInstancingVariants:
                        MaterialUtility.SetEnableInstancing(recordingSo, MaterialUtility.GetEnableInstancing(baseSo));
                        break;
                    case RecordingMaterialSerializedProperty.DoubleSidedGI:
                        MaterialUtility.SetDoubleSidedGI(recordingSo, MaterialUtility.GetDoubleSidedGI(baseSo));
                        break;
                }

                recordingSo.ApplyModifiedPropertiesWithoutUndo();
            },
            settings => ClearSerializedPropertyOverride(settings, property));
    }

    private static void ClearSerializedPropertyOverride(
        MaterialOverrideSettings settings,
        RecordingMaterialSerializedProperty property)
    {
        switch (property)
        {
            case RecordingMaterialSerializedProperty.CustomRenderQueue:
                settings.OverrideRenderQueue = false;
                break;
            case RecordingMaterialSerializedProperty.LightmapFlags:
                settings.OverrideLightmapFlags = false;
                break;
            case RecordingMaterialSerializedProperty.EnableInstancingVariants:
                settings.OverrideEnableInstancing = false;
                break;
            case RecordingMaterialSerializedProperty.DoubleSidedGI:
                settings.OverrideDoubleSidedGI = false;
                break;
        }
    }

    private void ApplyManualRecordingMaterialChange(
        Action<Material> mutateRecordingMaterial,
        Action<MaterialOverrideSettings> applyExplicitOverrideEdit)
    {
        if (_unlockedRecordingSourceMaterial == null) return;

        var baseMaterial = CreateDiffBaseMaterial();
        try
        {
            mutateRecordingMaterial(baseMaterial);
            MaterialUtility.Normalize(_recordingMaterial);

            if (!SanitizeRecordingMaterialAgainstAfter()) return;

            var maintainEqualOverrides = _target.TargetSettings.Mode != MaterialTargetSettings.SelectionMode.SingleMaterial;
            var nextOverrides = BuildOverrideSettingsFromRecordingMaterial(maintainEqualOverrides);
            if (nextOverrides == null) return;

            applyExplicitOverrideEdit(nextOverrides);
            CommitOverridesFromRecording(nextOverrides);
            SyncRecordingMaterialFromComponent();
        }
        finally
        {
            DestroyImmediate(baseMaterial);
        }
    }

    // shader変更と同時に、新しいshaerにのみ存在するプロパティはデフォルト値が書き込まれるのを仕様とする
    // よってEditor上における差分の自動抽出時には、新しいshaerにのみ存在しデフォルト値のものを除外する
    // これはシェーダー変更時に、新しいshaerにのみ存在するプロパティが、大量のoverrideになる問題への対処仕様
    private List<MaterialProperty> RemoveNewShaderDefaultPropertyOverrides(List<MaterialProperty> target, Shader targetShader, Shader originalShader)
    {
        var originalNames = MaterialUtility.EnumeratePropertyNames(originalShader).ToHashSet();
        var targetDefaults = MaterialUtility.GetShaderDefaultProperties(targetShader).ToDictionary(p => p.PropertyName);
        return target
            .Where(p =>
            {
                if (originalNames.Contains(p.PropertyName)) return true;
                if (!targetDefaults.TryGetValue(p.PropertyName, out var defaultValue)) return true;
                return !p.EqualsImpl(defaultValue, strict: false);
            })
            .ToList();
    }

    private void MaintainEqualOverrides(MaterialOverrideSettings previous, MaterialOverrideSettings current, MaterialOverrideSettings target, Material baseMaterial)
    {
        using var baseSo = new SerializedObject(baseMaterial);

        if (previous.OverrideShader && !current.OverrideShader)
        {
            target.OverrideShader = true;
            target.TargetShader = baseMaterial.shader;
        }

        if (previous.OverrideRenderQueue && !current.OverrideRenderQueue)
        {
            target.OverrideRenderQueue = true;
            target.RenderQueueValue = MaterialUtility.GetCustomRenderQueue(baseSo);
        }

        if (previous.OverrideLightmapFlags && !current.OverrideLightmapFlags)
        {
            target.OverrideLightmapFlags = true;
            target.LightmapFlagsValue = MaterialUtility.GetLightmapFlags(baseSo);
        }

        if (previous.OverrideEnableInstancing && !current.OverrideEnableInstancing)
        {
            target.OverrideEnableInstancing = true;
            target.EnableInstancingValue = MaterialUtility.GetEnableInstancing(baseSo);
        }

        if (previous.OverrideDoubleSidedGI && !current.OverrideDoubleSidedGI)
        {
            target.OverrideDoubleSidedGI = true;
            target.DoubleSidedGIValue = MaterialUtility.GetDoubleSidedGI(baseSo);
        }

        var baseProperties = MaterialUtility.GetProperties(baseMaterial).ToDictionary(item => item.PropertyName);
        target.PropertyOverrides = MaintainEqualOverrideItems(
            previous.PropertyOverrides,
            current.PropertyOverrides,
            item => item.PropertyName,
            item => baseProperties.TryGetValue(item.PropertyName, out var baseItem) ? baseItem : item);

        var baseKeywords = MaterialUtility.GetValidKeywords(baseSo).ToHashSet();
        target.KeywordStateOverrides = MaintainEqualOverrideItems(
            previous.KeywordStateOverrides,
            current.KeywordStateOverrides,
            item => item.Keyword,
            item => new MaterialKeywordStateOverride { Keyword = item.Keyword, Enabled = baseKeywords.Contains(item.Keyword) });

        var baseTags = MaterialUtility.GetStringTags(baseSo);
        target.StringTagOverrides = MaintainEqualOverrideItems(
            previous.StringTagOverrides,
            current.StringTagOverrides,
            item => item.TagName,
            item =>
            {
                return baseTags.TryGetValue(item.TagName, out var value)
                    ? new MaterialStringTagOverride { TagName = item.TagName, Value = value, Remove = false }
                    : new MaterialStringTagOverride { TagName = item.TagName, Value = string.Empty, Remove = true };
            });

        var baseDisabledPasses = MaterialUtility.GetDisabledShaderPasses(baseSo).ToHashSet();
        target.ShaderPassStateOverrides = MaintainEqualOverrideItems(
            previous.ShaderPassStateOverrides,
            current.ShaderPassStateOverrides,
            item => item.PassName,
            item => new MaterialShaderPassStateOverride { PassName = item.PassName, Enabled = !baseDisabledPasses.Contains(item.PassName) });
    }

    private static List<T> MaintainEqualOverrideItems<T>(
        List<T> previous,
        List<T> current,
        Func<T, string> keySelector,
        Func<T, T> getBaseItem)
    {
        var currentKeys = current.Select(keySelector).ToHashSet();
        var result = new List<T>(previous.Count);
        foreach (var item in previous)
        {
            var key = keySelector(item);
            if (currentKeys.Contains(key))
            {
                result.Add(item);
            }
            else
            {
                result.Add(getBaseItem(item));
            }
        }

        return result;
    }

    private void CommitOverridesFromRecording(MaterialOverrideSettings value)
    {
        serializedObject.Update();
        _pendingSelfCommittedOverrides = value.Clone();
        _overrideSettings.CopyFrom(value);
        serializedObject.ApplyModifiedProperties();
        UpdateRecordingOverrideState();
    }

    private bool TryConsumeSelfCommittedOverrideEcho()
    {
        if (_pendingSelfCommittedOverrides == null) return false;

        var expected = _pendingSelfCommittedOverrides;
        _pendingSelfCommittedOverrides = null;

        if (!_target.OverrideSettings.Equals(expected))
        {
            return false;
        }

        DebugLog("ConsumeSelfCommittedOverrideEcho, frame: " + Time.frameCount);
        return true;
    }

    private bool TryConsumeSelfCommittedRecordingMaterialChange()
    {
        if (!_pendingSelfCommittedRecordingMaterialChange) return false;

        _pendingSelfCommittedRecordingMaterialChange = false;
        DebugLog("ConsumeSelfCommittedRecordingMaterialChange, frame: " + Time.frameCount);
        return true;
    }

    // OverrideUtilityGUI
    private bool _showOverrideUtility = false;
    private bool _showMaterialDiff = false;
    private bool _showMaterialVariantDiff = false;
    private Material? _originalMaterial = null;
    private Material? _overrideMaterial = null;
    private Material? _variantMaterial = null;
    private Rect DrawOverrideUtility(Rect position)
    {
        position.height = GUIHelper.propertyHeight;
        _showOverrideUtility = GUIHelper.Foldout(
            position,
            _showOverrideUtility,
            "overrideUtility.title".LG(),
            RectStrictFoldoutOptions);
        position.NewLine();
        if (!_showOverrideUtility) return position;

        // Material Diff Foldout
        position.Indent();
        position.height = GUIHelper.propertyHeight;
        _showMaterialDiff = GUIHelper.Foldout(
            position,
            _showMaterialDiff,
            "overrideUtility.materialDiff.title".LG(),
            RectStrictFoldoutOptions);
        position.NewLine();
        if (_showMaterialDiff)
        {
            _originalMaterial ??= _recordingSourceMaterial;

            position.Indent();
            position.height = GUIHelper.propertyHeight;
            _originalMaterial = EditorGUI.ObjectField(position, "overrideUtility.materialDiff.original".LS(), _originalMaterial, typeof(Material), false) as Material;
            position.NewLine();
            position.height = GUIHelper.propertyHeight;
            _overrideMaterial = EditorGUI.ObjectField(position, "overrideUtility.materialDiff.modified".LS(), _overrideMaterial, typeof(Material), false) as Material;
            position.NewLine();
        
            using (new EditorGUI.DisabledGroupScope(_originalMaterial == null || _overrideMaterial == null))
            {
                position.height = GUIHelper.propertyHeight;
                if (GUI.Button(position, "overrideUtility.addChanges".LS()))
                {
                    ProcessMaterialDiff(true);
                }
                position.NewLine();
                position.height = GUIHelper.propertyHeight;
                if (GUI.Button(position, "overrideUtility.addChangesExcludeTexture".LS()))
                {
                    ProcessMaterialDiff(false);
                }
                position.NewLine();
            }
            position.Back();
        }

        // Material Variant Diff Foldout
        position.height = GUIHelper.propertyHeight;
        _showMaterialVariantDiff = GUIHelper.Foldout(
            position,
            _showMaterialVariantDiff,
            "overrideUtility.variantDiff.title".LG(),
            RectStrictFoldoutOptions);
        position.NewLine();
        if (_showMaterialVariantDiff)
        {
            position.Indent();
            position.height = GUIHelper.propertyHeight;
            _variantMaterial = EditorGUI.ObjectField(position, "overrideUtility.variantDiff.material".LS(), _variantMaterial, typeof(Material), false) as Material;
            position.NewLine();
            if (_variantMaterial != null && !_variantMaterial.isVariant)
            {
                var helpText = "overrideUtility.variantDiff.notVariant".LS();
                position.height = GUIHelper.GetHelpBoxHeight(helpText, MessageType.Info);
                position = GUIHelper.HelpBox(position, helpText, MessageType.Info);
            }
        
            using (new EditorGUI.DisabledGroupScope(_variantMaterial == null || !_variantMaterial.isVariant))
            {
                position.height = GUIHelper.propertyHeight;
                if (GUI.Button(position, "overrideUtility.addChanges".LS()))
                {
                    ProcessMaterialVariantDiff(true);
                }
                position.NewLine();
                position.height = GUIHelper.propertyHeight;
                if (GUI.Button(position, "overrideUtility.addChangesExcludeTexture".LS()))
                {
                    ProcessMaterialVariantDiff(false);
                }
                position.NewLine();
            }
            position.Back();
        }

        position.Back();
        return position;

        void ProcessMaterialDiff(bool includeTexture)
        {
            if (_originalMaterial == null || _overrideMaterial == null) return;

            var overrides = MaterialUtility.GetOverrides(_originalMaterial, _overrideMaterial, false, true, includeTexture);
            ApplyExtractedOverridesToComponent(overrides);

            _originalMaterial = null;
            _overrideMaterial = null;
        }

        void ProcessMaterialVariantDiff(bool includeTexture)
        {
            if (_variantMaterial == null || !_variantMaterial.isVariant) return;

            var overrides = MaterialUtility.GetVariantOverrides(_variantMaterial, includeTexture);
            ApplyExtractedOverridesToComponent(overrides);

            _variantMaterial = null;
        
        }
    }

    private float GetOverrideUtilityHeight()
    {
        var height = GUIHelper.propertyHeight;
        if (!_showOverrideUtility) return height;

        height += GUIHelper.GUI_SPACE + GUIHelper.propertyHeight;
        if (_showMaterialDiff)
        {
            height += (GUIHelper.GUI_SPACE + GUIHelper.propertyHeight) * 4;
        }

        height += GUIHelper.GUI_SPACE + GUIHelper.propertyHeight;
        if (_showMaterialVariantDiff)
        {
            height += GUIHelper.GUI_SPACE + GUIHelper.propertyHeight;
            if (_variantMaterial != null && !_variantMaterial.isVariant)
            {
                height += GUIHelper.GUI_SPACE + GUIHelper.GetHelpBoxHeight("overrideUtility.variantDiff.notVariant".LS(), MessageType.Info);
            }
            height += (GUIHelper.GUI_SPACE + GUIHelper.propertyHeight) * 2;
        }

        return height;
    }

    private void ApplyExtractedOverridesToComponent(MaterialOverrideSettings extractedOverrides)
    {
        if (!SanitizeExtractedOverridesAgainstAfter(extractedOverrides)) return;
        if (extractedOverrides.OverrideCount == 0) return;

        serializedObject.ApplyModifiedProperties();

        var merged = _target.OverrideSettings.Clone();
        merged.Merge(extractedOverrides);

        _overrideSettings.CopyFrom(merged);
        serializedObject.ApplyModifiedProperties();

        // ObjectChnageにより、Recording Materialの変更等は行われる
    }

    private bool SanitizeExtractedOverridesAgainstAfter(MaterialOverrideSettings extractedOverrides)
    {
        var conflicts = GetAfterOverrideConflicts(extractedOverrides);

        if (conflicts.ShaderLocked)
        {
            if (_afterOverrides.TargetShader != null)
            {
                LocalizedLog.Warning("lock.shader.log", _afterOverrides.TargetShader.name);
            }
            return false;
        }

        if (conflicts.RenderQueueLocked)
        {
            extractedOverrides.OverrideRenderQueue = false;
            LocalizedLog.Warning("lock.renderQueue.log", _afterOverrides.RenderQueueValue);
        }

        if (conflicts.LockedPropertyNames.Count > 0)
        {
            extractedOverrides.PropertyOverrides = extractedOverrides.PropertyOverrides
                .Where(property =>
                {
                    if (!conflicts.LockedPropertyValues.TryGetValue(property.PropertyName, out var lockedValue)) return true;

                    LocalizedLog.Warning("lock.property.log", property.PropertyName, lockedValue);
                    return false;
                })
                .ToList();
        }

        return true;
    }

    static void DebugLog(string message)
    {
#if MATERIAL_EDITOR_DEBUG_EDITOR
        Debug.Log("MaterialEditorEditor: " + message);
#endif
    }
}

internal enum RecordingMaterialSerializedProperty
{
    CustomRenderQueue,
    LightmapFlags,
    EnableInstancingVariants,
    DoubleSidedGI,
}

internal static class MaterialEditoEditorContext
{
    public static readonly Dictionary<Material, MaterialEditorComponent> RecordingToComponent = new();
    private static readonly Dictionary<Material, MaterialEditorEditor> RecordingToEditor = new();
    private static readonly Dictionary<MaterialEditorComponent, Material> ComponentToRecording = new();

    public static readonly Dictionary<MaterialEditorComponent, HashSet<string>> ComponentToOverrideProperties = new();
    public static readonly Dictionary<MaterialEditorComponent, HashSet<RecordingMaterialSerializedProperty>> ComponentToOverrideSerializedProperties = new();

    public static readonly Dictionary<MaterialEditorComponent, bool> ComponentToShaderLocked = new();
    public static readonly Dictionary<MaterialEditorComponent, bool> ComponentToRenderQueueLocked = new();
    public static readonly Dictionary<MaterialEditorComponent, HashSet<string>> ComponentToLockedProperties = new();
    public static readonly Dictionary<MaterialEditorComponent, HashSet<RecordingMaterialSerializedProperty>> ComponentToLockedSerializedProperties = new();

    public static bool IsRecording => RecordingToComponent.Count > 0;

    public static event Action<MaterialEditorComponent>? OnStartRecording;
    public static event Action<MaterialEditorComponent>? OnRecordingEntryStateChanged;
    public static event Action<MaterialEditorComponent>? OnRecordingOverrideStateChanged;
    public static event Action<MaterialEditorComponent>? OnStopRecording;

    public static void StartRecording(
        MaterialEditorEditor editor,
        MaterialEditorComponent component,
        Material recordingMaterial,
        HashSet<string> overrideProperties,
        HashSet<RecordingMaterialSerializedProperty> overrideSerializedProperties,
        HashSet<string> lockedProperties,
        HashSet<RecordingMaterialSerializedProperty> lockedSerializedProperties,
        bool shaderLocked,
        bool renderQueueLocked)
    {
        RecordingToComponent[recordingMaterial] = component;
        RecordingToEditor[recordingMaterial] = editor;
        ComponentToRecording[component] = recordingMaterial;
        ComponentToOverrideProperties[component] = overrideProperties;
        ComponentToOverrideSerializedProperties[component] = overrideSerializedProperties;
        ComponentToLockedProperties[component] = lockedProperties;
        ComponentToLockedSerializedProperties[component] = lockedSerializedProperties;
        ComponentToShaderLocked[component] = shaderLocked;
        ComponentToRenderQueueLocked[component] = renderQueueLocked;
        OnStartRecording?.Invoke(component);
    }

    public static void NotifyRecordingEntryStateChanged(
        MaterialEditorComponent component)
    {
        OnRecordingEntryStateChanged?.Invoke(component);
    }

    public static void UpdateRecordingOverrideState(
        MaterialEditorComponent component,
        HashSet<string> overrideProperties,
        HashSet<RecordingMaterialSerializedProperty> overrideSerializedProperties)
    {
        ComponentToOverrideProperties[component] = overrideProperties;
        ComponentToOverrideSerializedProperties[component] = overrideSerializedProperties;
        OnRecordingOverrideStateChanged?.Invoke(component);
    }

    public static void UpdateLockedState(
        MaterialEditorComponent component,
        bool shaderLocked,
        bool renderQueueLocked,
        HashSet<string> lockedProperties,
        HashSet<RecordingMaterialSerializedProperty> lockedSerializedProperties)
    {
        ComponentToShaderLocked[component] = shaderLocked;
        ComponentToRenderQueueLocked[component] = renderQueueLocked;
        ComponentToLockedProperties[component] = lockedProperties;
        ComponentToLockedSerializedProperties[component] = lockedSerializedProperties;
    }

    public static bool TryGetRecordingMaterial(MaterialEditorComponent component, [NotNullWhen(true)] out Material? recordingMaterial)
    {
        return ComponentToRecording.TryGetValue(component, out recordingMaterial);
    }

    public static bool TryGetEditor(Material recordingMaterial, [NotNullWhen(true)] out MaterialEditorEditor? editor)
    {
        return RecordingToEditor.TryGetValue(recordingMaterial, out editor);
    }

    public static void StopRecording(Material recordingMaterial, MaterialEditorComponent component)
    {
        RecordingToComponent.Remove(recordingMaterial);
        RecordingToEditor.Remove(recordingMaterial);
        ComponentToRecording.Remove(component);
        ComponentToOverrideProperties.Remove(component);
        ComponentToOverrideSerializedProperties.Remove(component);
        ComponentToLockedProperties.Remove(component);
        ComponentToLockedSerializedProperties.Remove(component);
        ComponentToShaderLocked.Remove(component);
        ComponentToRenderQueueLocked.Remove(component);
        OnStopRecording?.Invoke(component);
    }
}
