using UnityEngine.Pool;
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
        var properties = _target.OverrideSettings.PropertyOverrides.Select(p => p.PropertyName).ToHashSet();
        MaterialEditoEditorContext.StartRecording(_target, _recordingMaterial, properties, _materialEditor);
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
        EditorGUILayout.LabelField("# " + "Label:TargetSettings".LS(), EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_entrySettings);
    }

    private void DrawEditor()
    {
        EditorGUILayout.LabelField("# " + "Label:EditorGUI".LS(), EditorStyles.boldLabel);
        if (_recordingSourceMaterial != null && _materialEditor != null)
        {
            _materialEditor.DrawHeader();
            if (_materialEditor.isVisible) {
                EditorGUILayout.HelpBox("HelpBox:EditorInfo".LS(), MessageType.Info);
                // OverrideUtilityGUI();
                DrawRecordingSourceMaterial();
                _materialEditor.OnInspectorGUI();
            }
        }
        else 
        {
            EditorGUILayout.HelpBox("HelpBox:NoMaterialSelected".LS(), MessageType.Warning, true);
        }
        DrawOverrides();
    }

    private bool _showOverrides = false;
    private void DrawOverrides()
    {
        var count = _target.OverrideSettings.OverrideCount;
        _showOverrides = EditorGUILayout.Foldout(_showOverrides, string.Format("Label:CurrentOverridesCount".LS(), count), true);
        if (_showOverrides)
        {
            EditorGUILayout.HelpBox("HelpBox:OverridesInfo".LS(), MessageType.Info);
            // 内部でEditorGUIを多用しているが、ここでIndentScopeを使いEditorGUILayoutで描画すると何故か崩れるのでEditorGUIに統一する
            // Todo: EditorGUIとEditorGUILayoutが共存できないようなまともでない設計を解消する
            var position = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(_overrideSettings));
            position.Indent();
            EditorGUI.PropertyField(position, _overrideSettings);
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
                        var properties = _target.OverrideSettings.PropertyOverrides.Select(p => p.PropertyName).ToHashSet();
                        MaterialEditoEditorContext.UpdateRecording(_target, properties);
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

            // 固定されたシェーダーが変更された場合は、プロパティの空間が大規模に変わるので、同時に発生した変更を全て巻き戻す。
            if (_afterOverrides.OverrideShader && currentDiff.OverrideShader)
            {
                MaterialUtility.CopyAllSettings(authoritative, _recordingMaterial);
                LocalizedLog.Warning("Log:ShaderIsLocked", authoritative.shader.name);
                sanitized = true;
            }
            else
            {
                if (_afterOverrides.OverrideRenderQueue && currentDiff.OverrideRenderQueue)
                {
                    MaterialUtility.ApplyCustomRenderQueue(_recordingMaterial, MaterialUtility.GetCustomRenderQueue(authoritative));
                    LocalizedLog.Warning("Log:RenderQueueIsLocked", MaterialUtility.GetCustomRenderQueue(authoritative));
                    sanitized = true;
                }

                if (_afterOverrides.PropertyOverrides.Count > 0 && currentDiff.PropertyOverrides.Count > 0)
                {
                    using var _1 = HashSetPool<string>.Get(out var lockedProperties);
                    foreach (var property in _afterOverrides.PropertyOverrides) lockedProperties.Add(property.PropertyName);
                    using var _2 = DictionaryPool<string, MaterialProperty>.Get(out var authoritativeProperties);
                    foreach (var property in MaterialUtility.GetProperties(authoritative)) authoritativeProperties[property.PropertyName] = property;

                    foreach (var property in currentDiff.PropertyOverrides)
                    {
                        if (!lockedProperties.Contains(property.PropertyName)) continue;
                        if (!authoritativeProperties.TryGetValue(property.PropertyName, out var authoritativeProperty)) continue;

                        authoritativeProperty.TrySet(_recordingMaterial);
                        LocalizedLog.Warning("Log:PropertyIsLocked", property.PropertyName, authoritativeProperty.PropertyValue);
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
    // private Texture? _sourceTexture = null;
    // private Texture? _destinationTexture = null;
    // private Material? _originalMaterial = null;
    // private Material? _overrideMaterial = null;
    // private Material? _variantMaterial = null;
    private void DrawOverrideUtility()
    {
        _showOverrideUtility = EditorGUILayout.Foldout(_showOverrideUtility, "Label:OverrideUtility".LS(), true);
        if (!_showOverrideUtility) return;

        using var indent = new EditorGUI.IndentLevelScope();

        EditorGUILayout.HelpBox("未実装！", MessageType.Warning);

        // EditorGUILayout.LabelField("Replace Texture", EditorStyles.boldLabel);
        // _sourceTexture = EditorGUILayout.ObjectField("Source Texture", _sourceTexture, typeof(Texture), false, GUILayout.Height(18f)) as Texture;
        // _destinationTexture = EditorGUILayout.ObjectField("Destination Texture", _destinationTexture, typeof(Texture), false, GUILayout.Height(18f)) as Texture;
        // if (GUILayout.Button("Add diff to this component"))
        // {
        //     ProcessReplaceTexture();
        // }

        // EditorGUILayout.Space();

        // EditorGUILayout.LabelField("Get Material Diff", EditorStyles.boldLabel);
        // _originalMaterial ??= _targetMaterial.objectReferenceValue as Material;
        // _originalMaterial = MaterialSelector.DrawLayout(_originalMaterial, new GUIContent("Original Material"), _target.gameObject, m => _originalMaterial = m);
        // _overrideMaterial = MaterialSelector.DrawLayout(_overrideMaterial, new GUIContent("Override Material"), _target.gameObject, m => _overrideMaterial = m);
        
        // using (new EditorGUILayout.HorizontalScope())
        // {
        //     if (GUILayout.Button("Add diff"))
        //     {
        //         ProcessMaterialDiff(true);
        //     }
        //     if (GUILayout.Button("Add diff (Exclude Texture)"))
        //     {
        //         ProcessMaterialDiff(false);
        //     }
        // }

        // EditorGUILayout.Space();

        // EditorGUILayout.LabelField("Get Material Variant Diff", EditorStyles.boldLabel);
        // _variantMaterial = MaterialSelector.DrawLayout(_variantMaterial, new GUIContent("Material Variant"), _target.gameObject, m => _variantMaterial = m);
        
        // using (new EditorGUILayout.HorizontalScope())
        // {
        //     if (GUILayout.Button("Add diff"))
        //     {
        //         ProcessMaterialVariantDiff(true);
        //     }
        //     if (GUILayout.Button("Add diff (Exclude Texture)"))
        //     {
        //         ProcessMaterialVariantDiff(false);
        //     }
        // }

        // return;

        // void ProcessReplaceTexture()
        // {
        //     if (_sourceTexture == null || _destinationTexture == null) { TTLog.Info("MaterialModifier:info:TargetNotSet"); return; }

        //     _recordingMaterial.ReplaceTexture(_destinationTexture, _sourceTexture);
        //     ApplyRecordingMaterialDiffToComponent();

        //     _sourceTexture = null;
        //     _destinationTexture = null;
        // }

        // void ProcessMaterialDiff(bool includeTexture)
        // {
        //     if (_originalMaterial == null || _overrideMaterial == null) { TTLog.Info("MaterialModifier:info:TargetNotSet"); return; }

        //     MaterialModifier.ApplyMaterialDiff(_originalMaterial, _overrideMaterial, _recordingMaterial, includeTexture);
        //     ApplyRecordingMaterialDiffToComponent();

        //     _originalMaterial = null;
        //     _overrideMaterial = null;
        // }

        // void ProcessMaterialVariantDiff(bool includeTexture)
        // {
        //     if (_variantMaterial == null) { TTLog.Info("MaterialModifier:info:TargetNotSet"); return; }

        //     var overrideProperties = GetVariantOverrideProperties(_variantMaterial, includeTexture).ToList();
        //     MaterialModifier.ConfigureMaterial(_recordingMaterial, false, null, false, 0, overrideProperties);
        //     ApplyRecordingMaterialDiffToComponent();

        //     _variantMaterial = null;
        // }
    }
}

internal static class MaterialEditoEditorContext
{
    public static readonly Dictionary<Material, MaterialEditorComponent> RecordingToComponent = new();
    public static readonly Dictionary<MaterialEditorComponent, HashSet<string>> ComponentToOverrideProperties = new();
    public static readonly Dictionary<MaterialEditorComponent, UnityEditor.MaterialEditor?> ComponentToMaterialEditor = new();

    public static bool IsRecording => RecordingToComponent.Count > 0;

    public static event Action<MaterialEditorComponent>? OnStartRecording;
    public static event Action<MaterialEditorComponent>? OnUpdateRecording;
    public static event Action<MaterialEditorComponent>? OnStopRecording;

    public static void StartRecording(
        MaterialEditorComponent component,
        Material recordingMaterial,
        HashSet<string> overrideProperties,
        UnityEditor.MaterialEditor materialEditor)
    {
        RecordingToComponent[recordingMaterial] = component;
        ComponentToOverrideProperties[component] = overrideProperties;
        ComponentToMaterialEditor[component] = materialEditor;
        OnStartRecording?.Invoke(component);
    }

    public static void UpdateRecording(
        MaterialEditorComponent component,
        HashSet<string> overrideProperties)
    {
        ComponentToOverrideProperties[component] = overrideProperties;
        OnUpdateRecording?.Invoke(component);
    }

    public static void StopRecording(Material recordingMaterial, MaterialEditorComponent component)
    {
        RecordingToComponent.Remove(recordingMaterial);
        ComponentToOverrideProperties.Remove(component);
        ComponentToMaterialEditor.Remove(component);
        OnStopRecording?.Invoke(component);
    }
}