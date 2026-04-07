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

    private MaterialEditorEditorUtility _materialEditorEditorUtility = null!;

    private const string RecordingMaterialName = "Recording…";

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

        _materialEditorEditorUtility = new MaterialEditorEditorUtility(ApplyExtractedOverridesToComponent);

        ObjectChangeEvents.changesPublished += OnObjectChanged;
        MaterialEditoEditorContext.StartRecording(
            _target,
            _recordingMaterial,
            GetCurrentOverridePropertyNames(),
            GetLockedPropertyNames(),
            IsShaderLocked(),
            IsRenderQueueLocked(),
            _materialEditor);
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

        Localization.DrawLanguageSwitcher();
        if (!Migrator.CheckAndDrawMigrationButton(_target)) {
            return;
        }
        DrawInformationGUI();
        EditorGUILayout.Space();
        DrawEntrySettings();
        EditorGUILayout.Space();
        EditorGUILayout.Space(); 
        DrawEditor();

        serializedObject.ApplyModifiedProperties();
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
        var label = $"# {"targetSettings.title".LS()}";
        EditorGUILayout.PropertyField(_targetSettings, new GUIContent(label));
    }

    private void DrawEditor()
    {
        EditorGUILayout.LabelField("# " + "editor.title".LS(), EditorStyles.boldLabel);

        if (_recordingSourceMaterial != null && _materialEditor != null)
        {
            _materialEditor.DrawHeader();
            if (_materialEditor.isVisible) {
                DrawOverridesGUI();
                if (MaterialEditorSettings.ShowInspectorDescription)
                {
                    EditorGUILayout.HelpBox("editor.help".LS(), MessageType.Info);
                }
                DrawRecordingSourceMaterial();
                using (new EditorGUI.IndentLevelScope()) {
                    _materialEditorEditorUtility.DrawOverrideUtility(_recordingMaterial, _recordingSourceMaterial);
                }
                GUIHelper.DrawFullWidthHorizontalLine(new Color(0.35f, 0.35f, 0.35f));
                EditorGUILayout.Space();
                _materialEditor.OnInspectorGUI();
            }
            else {
                DrawOverridesGUI();
            }
        }
        else 
        {
            EditorGUILayout.HelpBox("editor.noMaterialSelected.help".LS(), MessageType.Warning, true);
            DrawOverridesGUI();
        }
    }

    private void DrawOverridesGUI()
    {
        var count = _target.OverrideSettings.OverrideCount;
        EditorGUILayout.PropertyField(_overrideSettings, new GUIContent(string.Format("overrideSettings.count".LS(), count)));
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
            EditorGUILayout.ObjectField("editor.recordingSourceMaterial".LS(), _recordingSourceMaterial, typeof(Material), false);
        }
        MaterialSelector.Draw(() => _targetMaterials.ToArray(), (m, i) => { _recordingSourceMaterial = m; OnRecordingSourceMaterialChanged(); });
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
                }
            }
            else if (eventType == ObjectChangeKind.ChangeAssetObjectProperties)
            {
                stream.GetChangeAssetObjectPropertiesEvent(i, out var data);
                if (data.instanceId == recordingMaterialId)
                {
                    if (SanitizeRecordingMaterialAgainstAfter()) { // サニタイズに失敗した状態でコンポーネントに書き込むべきではない
                        var nextOverrides = BuildOverrideSettingsFromRecordingMaterial();
                        if (nextOverrides != null)
                        {
                            CommitOverridesFromRecording(nextOverrides);
                        }
                    }
                }
            }
        }
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

    private bool IsShaderLocked() => _afterOverrides.OverrideShader;
    private bool IsRenderQueueLocked() => _afterOverrides.OverrideRenderQueue;
    private HashSet<string> GetLockedPropertyNames() => _afterOverrides.PropertyOverrides
        .Select(p => p.PropertyName)
        .ToHashSet();

    private void NotifyRecordingEntryStateChanged()
    {
        MaterialEditoEditorContext.NotifyRecordingEntryStateChanged(_target);
    }

    private void UpdateRecordingOverrideState()
    {
        MaterialEditoEditorContext.UpdateRecordingOverrideState(
            _target,
            GetCurrentOverridePropertyNames());
    }

    private void UpdateRecordingLockedState()
    {
        MaterialEditoEditorContext.UpdateLockedState(
            _target,
            IsShaderLocked(),
            IsRenderQueueLocked(),
            GetLockedPropertyNames());
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
                    MaterialUtility.ApplyCustomRenderQueue(_recordingMaterial, MaterialUtility.GetCustomRenderQueue(authoritative));
                    LocalizedLog.Warning("lock.renderQueue.log", MaterialUtility.GetCustomRenderQueue(authoritative));
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

    private MaterialOverrideSettings? BuildOverrideSettingsFromRecordingMaterial()
    {
        DebugLog("BuildOverrideSettingsFromRecordingMaterial, frame: " + Time.frameCount);

        if (_unlockedRecordingSourceMaterial == null) return null;

        var baseMaterial = new Material(_unlockedRecordingSourceMaterial);
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

            return cloned;
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

    private void ApplyExtractedOverridesToComponent(MaterialOverrideSettings extractedOverrides)
    {
        if (!SanitizeExtractedOverridesAgainstAfter(extractedOverrides)) return;
        if (extractedOverrides.OverrideCount == 0) return;

        serializedObject.ApplyModifiedProperties();

        var merged = _target.OverrideSettings.Clone();
        MaterialOverrideSettings.MergeInto(extractedOverrides, merged);

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

internal static class MaterialEditoEditorContext
{
    public static readonly Dictionary<Material, MaterialEditorComponent> RecordingToComponent = new();
    public static readonly Dictionary<MaterialEditorComponent, UnityEditor.MaterialEditor?> ComponentToMaterialEditor = new();

    public static readonly Dictionary<MaterialEditorComponent, HashSet<string>> ComponentToOverrideProperties = new();

    public static readonly Dictionary<MaterialEditorComponent, bool> ComponentToShaderLocked = new();
    public static readonly Dictionary<MaterialEditorComponent, bool> ComponentToRenderQueueLocked = new();
    public static readonly Dictionary<MaterialEditorComponent, HashSet<string>> ComponentToLockedProperties = new();

    public static bool IsRecording => RecordingToComponent.Count > 0;

    public static event Action<MaterialEditorComponent>? OnStartRecording;
    public static event Action<MaterialEditorComponent>? OnRecordingEntryStateChanged;
    public static event Action<MaterialEditorComponent>? OnRecordingOverrideStateChanged;
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

    public static void NotifyRecordingEntryStateChanged(
        MaterialEditorComponent component)
    {
        OnRecordingEntryStateChanged?.Invoke(component);
    }

    public static void UpdateRecordingOverrideState(
        MaterialEditorComponent component,
        HashSet<string> overrideProperties)
    {
        ComponentToOverrideProperties[component] = overrideProperties;
        OnRecordingOverrideStateChanged?.Invoke(component);
    }

    public static void UpdateLockedState(
        MaterialEditorComponent component,
        bool shaderLocked,
        bool renderQueueLocked,
        HashSet<string> lockedProperties)
    {
        ComponentToShaderLocked[component] = shaderLocked;
        ComponentToRenderQueueLocked[component] = renderQueueLocked;
        ComponentToLockedProperties[component] = lockedProperties;
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
