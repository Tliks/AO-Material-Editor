using UnityEngine.Pool;
using UnityEngine.Rendering;
using Aoyon.MaterialEditor.Processor;

namespace Aoyon.MaterialEditor.UI;

[CustomEditor(typeof(MaterialEditorComponent))]
internal class MaterialEditorEditor : Editor
{
    private MaterialEditorComponent _target = null!;
    private GameObject? _avatarRoot;

    private SerializedProperty _entrySettings = null!;
    private SerializedProperty _overrideSettings = null!;

    private IMaterialTargeting _materialTargeting = null!;
    private HashSet<MaterialAssignment> _allAssignments = null!;
    private HashSet<Material> _targetMaterials = null!;

    // recoding UI fields
    private Material? _recordingSourceMaterial;
    private Material _recordingMaterial = null!;
    private UnityEditor.MaterialEditor _materialEditor = null!;
    private MaterialEntrySettings? _cachedEntrySettings;

    private MaterialOverrideSettings _beforeOverrides = MaterialOverrideSettings.Empty;
    private MaterialOverrideSettings _afterOverrides = MaterialOverrideSettings.Empty;

    private const string RecordingMaterialName = "Recording…";

    private void OnEnable()
    {
        _target = (MaterialEditorComponent)target;
        _avatarRoot = Utils.FindAvatarInParents(_target.gameObject);

        _entrySettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.EntrySettings));
        _overrideSettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.OverrideSettings));

        _materialTargeting = new DefaultMaterialTargeting();
        var renderers = _avatarRoot != null ? MaterialEditorProcessor.GetTargetRenderers(_avatarRoot) : new List<Renderer>();
        _allAssignments = _materialTargeting.GetAssignments(renderers).ToHashSet();
        _targetMaterials = UpdateTargetMaterials();
        _cachedEntrySettings = _target.EntrySettings.Clone();

        _recordingSourceMaterial = AutoSelectRecordingSourceMaterial();
        if (_recordingSourceMaterial != null) { 
            _recordingMaterial = new Material(_recordingSourceMaterial) { name = RecordingMaterialName, parent = null };
            UpdateOtherOverrides();
            SyncRecordingMaterialFromComponent();
        }
        else {
            _recordingMaterial = new Material(Shader.Find("Standard")) { name = RecordingMaterialName };
        }
        _materialEditor = (UnityEditor.MaterialEditor)CreateEditor(_recordingMaterial, typeof(UnityEditor.MaterialEditor));

        ObjectChangeEvents.changesPublished += OnObjectChanged;
        MaterialEditoEditorContext.StartRecording(
            _target,
            _recordingMaterial,
            GetCurrentOverridePropertyNames(),
            GetLockedPropertyNames(),
            IsShaderLocked(),
            IsRenderQueueLocked(),
            _materialEditor);
        MaterialEditoEditorContext.OnUpdateRecording += OnUpdateRecording;
    }

    private void OnDisable()
    {
        MaterialEditoEditorContext.StopRecording(_recordingMaterial, _target);

        if (_recordingMaterial != null) { DestroyImmediate(_recordingMaterial); }
        if (_materialEditor != null) { DestroyImmediate(_materialEditor); }

        ObjectChangeEvents.changesPublished -= OnObjectChanged;
        MaterialEditoEditorContext.OnUpdateRecording -= OnUpdateRecording;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Localization.DrawLanguageSwitcher();
        EditorGUILayout.Space();
        DrawInformationGUI();
        EditorGUILayout.Space();
        DrawEntrySettings();
        EditorGUILayout.Space(); 
        DrawEditor();
        DrawOverrides();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawInformationGUI()
    {
        if (_avatarRoot == null)
        {
            EditorGUILayout.HelpBox("HelpBox:NoAvatarRoot".LS(), MessageType.Warning);
        }

        var effective = MaterialEditorProcessor.IsEffective(_target);
        if (!effective)
        {
            EditorGUILayout.HelpBox("HelpBox:NotEffective".LS(), MessageType.Warning);
        }
    }

    private void DrawEntrySettings()
    {
        EditorGUILayout.PropertyField(_entrySettings, "Label:TargetSettings".LG());
    }

    private void DrawEditor()
    {
        EditorGUILayout.LabelField("# " + "Label:EditorGUI".LS(), EditorStyles.boldLabel);
        if (_recordingSourceMaterial != null && _materialEditor != null)
        {
            _materialEditor.DrawHeader();
            if (_materialEditor.isVisible) {
                EditorGUILayout.HelpBox("HelpBox:EditorInfo".LS(), MessageType.Info);
                DrawOverrideUtility();
                DrawRecordingSourceMaterial();
                EditorGUILayout.Space();
                _materialEditor.OnInspectorGUI();
            }
        }
        else 
        {
            EditorGUILayout.HelpBox("HelpBox:NoMaterialSelected".LS(), MessageType.Warning, true);
        }
    }

    private void DrawOverrides()
    {
        var count = _target.OverrideSettings.OverrideCount;
        LocalizedUI.PropertyField(_overrideSettings, string.Format("Label:CurrentOverridesCount".LS(), count));
        if (_overrideSettings.isExpanded)
        {
            if (GUILayout.Button("Label:ResetAll".LS()))
            {
                Undo.RecordObject(_target, "Reset AO Material Editor Overrides");
                _target.OverrideSettings = MaterialOverrideSettings.Empty;
            }
        }
    }
    
    private HashSet<Material> UpdateTargetMaterials()
    {
        _targetMaterials = MaterialEditorProcessor.SelectTargetAssignments(_allAssignments, _target)
            .Select(a => a.Material)
            .ToHashSet();
        return _targetMaterials;
    }

    private void DrawRecordingSourceMaterial()
    {
        if (_targetMaterials.Count < 2) return;

        using var _ = new EditorGUILayout.HorizontalScope();
        using (new EditorGUI.DisabledGroupScope(true)) {
            EditorGUILayout.ObjectField("Label:RecordingSourceMaterial".LS(), _recordingSourceMaterial, typeof(Material), false);
        }
        MaterialSelector.Draw(() => _targetMaterials.ToList(), (m, i) => { _recordingSourceMaterial = m; OnRecordingSourceMaterialChanged(); });
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

    // PrefabuTility.PrefabInstanceUpdatedはPrefab Revertなどのイベントを拾わずRecording Materialの更新を行えない
    // これを回避するため、コンポーネントの変更とMaterialEditorを介したマテリアルの編集、両方のイベント取得をObjectChangeEventStream経由で行う
    private void OnObjectChanged(ref ObjectChangeEventStream stream)
    {
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
                    if (!_target.EntrySettings.Equals(_cachedEntrySettings))
                    {
                        _cachedEntrySettings = _target.EntrySettings.Clone();
                        OnEntrySettingsChanged();
                    }
                    else // その他(overrides)の変更
                    {
                        // マテリアルを直接編集するのでUndoに通知されない
                        // これにより、コンポーネントRecoridng Material間の無限ループは起きない
                        SyncRecordingMaterialFromComponent();
                        UpdateRecordingContext();
                    }
                }
            }
            else if (eventType == ObjectChangeKind.ChangeAssetObjectProperties)
            {
                stream.GetChangeAssetObjectPropertiesEvent(i, out var data);
                if (data.instanceId == recordingMaterialId)
                {
                    // コンポーネントの変更はUndoに通知される
                    // これにより、次のフレームで数行上のSyncRecordingMaterialFromComponentが実行される
                    // 重複してパフォーマンス的にも無駄ではあるけど一応害はない
                    // Todo: もっと良い感じのロジックを考える、あると良いな
                    if (SanitizeRecordingMaterialAgainstAfter()) { // サニタイズに失敗した状態でコンポーネントに書き込むべきではない
                        SyncComponentFromRecordingMaterial();
                    }
                    return;
                }
            }
        }
    }

    private void OnEntrySettingsChanged()
    {
        UpdateTargetMaterials();
        AutoSelectRecordingSourceMaterial();
        OnRecordingSourceMaterialChanged();
    }

    private void OnRecordingSourceMaterialChanged()
    {
        UpdateOtherOverrides();
        UpdateRecordingContext();
        SyncRecordingMaterialFromComponent();
    }

    private void OnUpdateRecording(MaterialEditorComponent component)
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
        UpdateRecordingContext();
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
                    MaterialOverrideSettings.MergeInto(component.OverrideSettings, _beforeOverrides);
                }
                else
                {
                    MaterialOverrideSettings.MergeInto(component.OverrideSettings, _afterOverrides);
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

    private HashSet<string> GetLockedPropertyNames()
    {
        return _afterOverrides.PropertyOverrides
            .Select(p => p.PropertyName)
            .ToHashSet();
    }

    private void UpdateRecordingContext()
    {
        MaterialEditoEditorContext.UpdateRecording(
            _target,
            GetCurrentOverridePropertyNames(),
            GetLockedPropertyNames(),
            IsShaderLocked(),
            IsRenderQueueLocked());
    }

    private bool IsShaderLocked() => _afterOverrides.OverrideShader;
    private bool IsRenderQueueLocked() => _afterOverrides.OverrideRenderQueue;

    private void SyncRecordingMaterialFromComponent()
    {
        if (_recordingSourceMaterial == null) return;

        serializedObject.ApplyModifiedProperties();

        // sourceの状態に初期化
        MaterialUtility.CopyAllSettings(_recordingSourceMaterial, _recordingMaterial);
        // 1. 自分より上のオーバーライドを反映
        MaterialUtility.ApplyOverrideSettings(_recordingMaterial, _beforeOverrides);
        // 2. 自分自身のオーバーライドを反映
        MaterialUtility.ApplyOverrideSettings(_recordingMaterial, _target.OverrideSettings);
        // 3. 自分より下のオーバーライドを反映 (上書き)
        MaterialUtility.ApplyOverrideSettings(_recordingMaterial, _afterOverrides);
    }

    private bool SanitizeRecordingMaterialAgainstAfter()
    {
        if (_recordingSourceMaterial == null) return true;

        if (!_afterOverrides.OverrideShader && !_afterOverrides.OverrideRenderQueue && _afterOverrides.PropertyOverrides.Count == 0) return true;

        var authoritative = new Material(_recordingSourceMaterial) { parent = null };
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
                LocalizedLog.Warning("Log:ShaderIsLocked", authoritative.shader.name);
                sanitized = true;
            }
            else
            {
                if (conflicts.RenderQueueLocked)
                {
                    MaterialUtility.ApplyCustomRenderQueue(_recordingMaterial, MaterialUtility.GetCustomRenderQueue(authoritative));
                    LocalizedLog.Warning("Log:RenderQueueIsLocked", MaterialUtility.GetCustomRenderQueue(authoritative));
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
                        LocalizedLog.Warning("Log:PropertyIsLocked", propertyName, authoritativeProperty.PropertyValue);
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

    private void SyncComponentFromRecordingMaterial()
    {
        if (_recordingSourceMaterial == null) return;

        var baseMaterial = new Material(_recordingSourceMaterial) { parent = null };
        try
        {
            MaterialUtility.ApplyOverrideSettings(baseMaterial, _beforeOverrides);
            MaterialUtility.ApplyOverrideSettings(baseMaterial, _afterOverrides);

            serializedObject.ApplyModifiedProperties();

            var previous = _target.OverrideSettings;
            var cloned = previous.Clone();
            var newOvrs = MaterialUtility.GetOverrides(baseMaterial, _recordingMaterial, false, true);

            // 前段階として、編集によって元の値に戻った設定(新しい差分に存在しないが、これまで存在していた差分)に対して
            // これを維持するために、元の値を書き込む
            {
                if (previous.OverrideShader && !newOvrs.OverrideShader)
                {
                    cloned.OverrideShader = true;
                    cloned.TargetShader = baseMaterial.shader;
                }

                if (previous.OverrideRenderQueue && !newOvrs.OverrideRenderQueue)
                {
                    cloned.OverrideRenderQueue = true;
                    cloned.RenderQueueValue = MaterialUtility.GetCustomRenderQueue(baseMaterial);
                }

                using var _1 = DictionaryPool<string, MaterialProperty>.Get(out var newDict);
                foreach (var p in newOvrs.PropertyOverrides) newDict[p.PropertyName] = p;
                using var _2 = DictionaryPool<string, MaterialProperty>.Get(out var origDict);
                foreach (var p in MaterialUtility.GetProperties(baseMaterial)) origDict[p.PropertyName] = p;

                var modified = new List<MaterialProperty>();
                foreach (var p in previous.PropertyOverrides)
                {
                    var name = p.PropertyName;
                    if (!newDict.ContainsKey(name) && origDict.TryGetValue(name, out var o))
                        modified.Add(o);
                    else
                        modified.Add(p);
                }
                cloned.PropertyOverrides = modified;
            }

            // 新しい差分をマージ(上書き, 追加)する
            MaterialOverrideSettings.MergeInto(newOvrs, cloned);

            Undo.RecordObject(_target, "Sync AO Material Editor from Recording Material");
            _target.OverrideSettings = cloned;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            DestroyImmediate(baseMaterial);
        }
    }

    // OverrideUtilityGUI
    private bool _showOverrideUtility = false;
    private bool _showReplaceTexture = false;
    private bool _showMaterialDiff = false;
    private bool _showMaterialVariantDiff = false;
    private Texture? _sourceTexture = null;
    private Texture? _destinationTexture = null;
    private Material? _originalMaterial = null;
    private Material? _overrideMaterial = null;
    private Material? _variantMaterial = null;
    private void DrawOverrideUtility()
    {
        _showOverrideUtility = EditorGUILayout.Foldout(_showOverrideUtility, "Label:OverrideUtility".LS(), true);
        if (!_showOverrideUtility) return;

        using var indent = new EditorGUI.IndentLevelScope();

        // Replace Texture Foldout
        _showReplaceTexture = EditorGUILayout.Foldout(_showReplaceTexture, "label:ReplaceTexture".LS(), true);
        if (_showReplaceTexture)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _sourceTexture = EditorGUILayout.ObjectField("label:SourceTexture".LS(), _sourceTexture, typeof(Texture), false, GUILayout.Height(18f)) as Texture;
                _destinationTexture = EditorGUILayout.ObjectField("label:DestinationTexture".LS(), _destinationTexture, typeof(Texture), false, GUILayout.Height(18f)) as Texture;
                using (new EditorGUI.DisabledGroupScope(_sourceTexture == null || _destinationTexture == null))
                {
                    if (GUILayout.Button("label:ReplaceTexture".LS()))
                    {
                        ProcessReplaceTexture();
                    }
                }
            }
            EditorGUILayout.Space();
        }

        // Material Diff Foldout
        _showMaterialDiff = EditorGUILayout.Foldout(_showMaterialDiff, "label:GetMaterialDiff".LS(), true);
        if (_showMaterialDiff)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _originalMaterial ??= _recordingSourceMaterial;
                _originalMaterial = EditorGUILayout.ObjectField("label:OriginalMaterial".LS(), _originalMaterial, typeof(Material), false) as Material;
                _overrideMaterial = EditorGUILayout.ObjectField("label:OverrideMaterial".LS(), _overrideMaterial, typeof(Material), false) as Material;
            
                using (new EditorGUI.DisabledGroupScope(_originalMaterial == null || _overrideMaterial == null))
                {
                    if (GUILayout.Button("label:AddDiff".LS()))
                    {
                        ProcessMaterialDiff(true);
                    }
                    if (GUILayout.Button("label:AddDiffExcludeTexture".LS()))
                    {
                        ProcessMaterialDiff(false);
                    }
                }
            }
            EditorGUILayout.Space();
        }

        // Material Variant Diff Foldout
        _showMaterialVariantDiff = EditorGUILayout.Foldout(_showMaterialVariantDiff, "label:GetMaterialVariantDiff".LS(), true);
        if (_showMaterialVariantDiff)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                _variantMaterial = EditorGUILayout.ObjectField("label:MaterialVariant".LS(), _variantMaterial, typeof(Material), false) as Material;
                if (_variantMaterial != null && !_variantMaterial.isVariant)
                {
                    EditorGUILayout.HelpBox("HelpBox:SelectedMaterialIsNotVariant".LS(), MessageType.Info);
                }
            
                using (new EditorGUI.DisabledGroupScope(_variantMaterial == null || !_variantMaterial.isVariant))
                {
                    if (GUILayout.Button("label:AddDiff".LS()))
                    {
                        ProcessMaterialVariantDiff(true);
                    }
                    if (GUILayout.Button("label:AddDiffExcludeTexture".LS()))
                    {
                        ProcessMaterialVariantDiff(false);
                    }
                }
            }
        }

        return;

        void ProcessReplaceTexture()
        {
            if (_sourceTexture == null || _destinationTexture == null) return;

            var overrides = GetTextureReplacementOverrides(_sourceTexture, _destinationTexture);
            ApplyExtractedOverridesToComponent(overrides);

            _sourceTexture = null;
            _destinationTexture = null;
        }

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

    private MaterialOverrideSettings GetTextureReplacementOverrides(Texture sourceTexture, Texture destinationTexture)
    {
        var overrides = new MaterialOverrideSettings();
        foreach (var property in MaterialUtility.GetProperties(_recordingMaterial))
        {
            if (property.PropertyType != ShaderPropertyType.Texture) continue;
            if (property.TextureValue != sourceTexture) continue;

            var updatedProperty = property;
            updatedProperty.TextureValue = destinationTexture;
            overrides.PropertyOverrides.Add(updatedProperty);
        }

        return overrides;
    }

    private void ApplyExtractedOverridesToComponent(MaterialOverrideSettings extractedOverrides)
    {
        if (!SanitizeExtractedOverridesAgainstAfter(extractedOverrides)) return;
        if (extractedOverrides.OverrideCount == 0) return;

        serializedObject.ApplyModifiedProperties();

        var merged = _target.OverrideSettings.Clone();
        MaterialOverrideSettings.MergeInto(extractedOverrides, merged);

        Undo.RecordObject(_target, "Add AO Material Editor Overrides");
        _target.OverrideSettings = merged;

        // ObjectChnageにより、Recording Materialの変更等は行われる
    }

    private bool SanitizeExtractedOverridesAgainstAfter(MaterialOverrideSettings extractedOverrides)
    {
        var conflicts = GetAfterOverrideConflicts(extractedOverrides);

        if (conflicts.ShaderLocked)
        {
            if (_afterOverrides.TargetShader != null)
            {
                LocalizedLog.Warning("Log:ShaderIsLocked", _afterOverrides.TargetShader.name);
            }
            return false;
        }

        if (conflicts.RenderQueueLocked)
        {
            extractedOverrides.OverrideRenderQueue = false;
            LocalizedLog.Warning("Log:RenderQueueIsLocked", _afterOverrides.RenderQueueValue);
        }

        if (conflicts.LockedPropertyNames.Count > 0)
        {
            extractedOverrides.PropertyOverrides = extractedOverrides.PropertyOverrides
                .Where(property =>
                {
                    if (!conflicts.LockedPropertyValues.TryGetValue(property.PropertyName, out var lockedValue)) return true;

                    LocalizedLog.Warning("Log:PropertyIsLocked", property.PropertyName, lockedValue);
                    return false;
                })
                .ToList();
        }

        return true;
    }
}

internal static class MaterialEditoEditorContext
{
    public static readonly Dictionary<Material, MaterialEditorComponent> RecordingToComponent = new();
    public static readonly Dictionary<MaterialEditorComponent, HashSet<string>> ComponentToOverrideProperties = new();
    public static readonly Dictionary<MaterialEditorComponent, HashSet<string>> ComponentToLockedProperties = new();
    public static readonly Dictionary<MaterialEditorComponent, bool> ComponentToShaderLocked = new();
    public static readonly Dictionary<MaterialEditorComponent, bool> ComponentToRenderQueueLocked = new();
    public static readonly Dictionary<MaterialEditorComponent, UnityEditor.MaterialEditor?> ComponentToMaterialEditor = new();

    public static bool IsRecording => RecordingToComponent.Count > 0;

    public static event Action<MaterialEditorComponent>? OnStartRecording;
    public static event Action<MaterialEditorComponent>? OnUpdateRecording;
    public static event Action<MaterialEditorComponent>? OnStopRecording;

    public static void StartRecording(
        MaterialEditorComponent component,
        Material recordingMaterial,
        HashSet<string> overrideProperties,
        HashSet<string> lockedProperties,
        bool shaderLocked,
        bool renderQueueLocked,
        UnityEditor.MaterialEditor materialEditor)
    {
        RecordingToComponent[recordingMaterial] = component;
        ComponentToOverrideProperties[component] = overrideProperties;
        ComponentToLockedProperties[component] = lockedProperties;
        ComponentToShaderLocked[component] = shaderLocked;
        ComponentToRenderQueueLocked[component] = renderQueueLocked;
        ComponentToMaterialEditor[component] = materialEditor;
        OnStartRecording?.Invoke(component);
    }

    public static void UpdateRecording(
        MaterialEditorComponent component,
        HashSet<string> overrideProperties,
        HashSet<string> lockedProperties,
        bool shaderLocked,
        bool renderQueueLocked)
    {
        ComponentToOverrideProperties[component] = overrideProperties;
        ComponentToLockedProperties[component] = lockedProperties;
        ComponentToShaderLocked[component] = shaderLocked;
        ComponentToRenderQueueLocked[component] = renderQueueLocked;
        OnUpdateRecording?.Invoke(component);
    }

    public static void StopRecording(Material recordingMaterial, MaterialEditorComponent component)
    {
        RecordingToComponent.Remove(recordingMaterial);
        ComponentToOverrideProperties.Remove(component);
        ComponentToLockedProperties.Remove(component);
        ComponentToShaderLocked.Remove(component);
        ComponentToRenderQueueLocked.Remove(component);
        ComponentToMaterialEditor.Remove(component);
        OnStopRecording?.Invoke(component);
    }
}