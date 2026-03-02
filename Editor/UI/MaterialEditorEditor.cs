using Aoyon.MaterialEditor.Processor;
using NUnit.Framework.Interfaces;
using UnityEngine.Pool;

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

    private const string RecordingMaterialName = "Recording…";

    private void OnEnable()
    {
        _target = (MaterialEditorComponent)target;
        _avatarRoot = Utils.FindAvatarInParents(_target.gameObject);

        _entrySettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.EntrySettings));
        _overrideSettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.OverrideSettings));

        _materialTargeting = new DefaultMaterialTargeting();
        var renderers = _avatarRoot != null ? _avatarRoot.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
        _allAssignments = _materialTargeting.GetAssignments(renderers).ToHashSet();
        _targetMaterials = UpdateTargetMaterials();
        _cachedEntrySettings = _target.EntrySettings.Clone();

        _recordingSourceMaterial = AutoSelectRecordingSourceMaterial();
        if (_recordingSourceMaterial != null) { 
            _recordingMaterial = new Material(_recordingSourceMaterial) { name = RecordingMaterialName };
        }
        else {
            _recordingMaterial = new Material(Shader.Find("Standard")) { name = RecordingMaterialName };
        }
        _materialEditor = (UnityEditor.MaterialEditor)CreateEditor(_recordingMaterial, typeof(UnityEditor.MaterialEditor));

        ObjectChangeEvents.changesPublished += OnObjectChanged;
        MaterialEditoEditorContext.StartRecording(_recordingMaterial, new(_target.OverrideSettings.PropertyOverrides.Select(p => p.PropertyName)));
    }

    private void OnDisable()
    {
        MaterialEditoEditorContext.StopRecording(_recordingMaterial);

        if (_recordingMaterial != null) { DestroyImmediate(_recordingMaterial); }
        if (_materialEditor != null) { DestroyImmediate(_materialEditor); }

        ObjectChangeEvents.changesPublished -= OnObjectChanged;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Localization.DrawLanguageSwitcher();
        EditorGUILayout.Space();
        DrawInformationGUI();
        EntrySettingsGUI();
        EditorGUILayout.Space(); 
        EditorGUI();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawInformationGUI()
    {
        if (_avatarRoot == null)
        {
            EditorGUILayout.HelpBox("HelpBox:NoAvatarRoot".LS(), MessageType.Error);
        }

        var effective = MaterialEditorProcessor.IsEffective(_target);
        if (!effective)
        {
            EditorGUILayout.HelpBox("HelpBox:NotEffective".LS(), MessageType.Warning);
        }
    }

    private void EntrySettingsGUI()
    {
        EditorGUILayout.LabelField("# " + "Label:TargetSettings".LS(), EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_entrySettings);
    }

    private void EditorGUI()
    {
        EditorGUILayout.LabelField("# " + "Label:EditorGUI".LS(), EditorStyles.boldLabel);
        if (_recordingSourceMaterial != null && _materialEditor != null)
        {
            _materialEditor.DrawHeader();
            if (_materialEditor.isVisible) {
                EditorGUILayout.HelpBox("HelpBox:EditorInfo".LS(), MessageType.Info);
                OverridesGUI();
                OverrideUtilityGUI();
                _materialEditor.OnInspectorGUI();
            }
        }
        else 
        {
            EditorGUILayout.HelpBox("HelpBox:NoMaterialSelected".LS(), MessageType.Warning, true);
            OverridesGUI();
        }
    }

    private bool _showOverrides = false;
    private void OverridesGUI()
    {
        var count = _target.OverrideSettings.CountOverrides();
        _showOverrides = EditorGUILayout.Foldout(_showOverrides, string.Format("Label:CurrentOverridesCount".LS(), count), true);
        if (_showOverrides)
        {
            using var indent = new EditorGUI.IndentLevelScope();
            EditorGUILayout.PropertyField(_overrideSettings);
        }
    }
    
    private HashSet<Material> UpdateTargetMaterials()
    {
        _targetMaterials = MaterialEditorProcessor.SelectTargetAssignments(_allAssignments, _target)
            .Select(a => a.Material)
            .ToHashSet();
        return _targetMaterials;
    }

    // 手動選択の設定(シリアライズはしない)を追加するべき…？
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
                        MaterialEditoEditorContext.UpdateRecording(_recordingMaterial, new(_target.OverrideSettings.PropertyOverrides.Select(p => p.PropertyName)));
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
                    SyncComponentFromRecordingMaterial();
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
        SyncRecordingMaterialFromComponent();
    }

    private void SyncRecordingMaterialFromComponent()
    {
        if (_recordingSourceMaterial == null) return;

        serializedObject.ApplyModifiedProperties();

        // sourceの状態に初期化
        // ここではシェーダー未参照のプロパティは削除されないけど…多分大丈夫でしょう…
        MaterialUtility.CopyAllProperties(_recordingSourceMaterial, _recordingMaterial);
        // overrideを反映
        MaterialUtility.ApplyOverrideSettings(_recordingMaterial, _target.OverrideSettings);
    }

    private void SyncComponentFromRecordingMaterial()
    {
        if (_recordingSourceMaterial == null) return;

        serializedObject.ApplyModifiedProperties();

        var cloned = _target.OverrideSettings.Clone();

        var newOvrs = MaterialUtility.GetOverrides(_recordingSourceMaterial, _recordingMaterial, false, true);

        // 前段階として、編集によって元の値に戻ったプロパティ(新しい差分に存在しないが、これまで存在していた差分)に対して
        // これを維持するために、元の値を書き込む
        {
            using var _1 = DictionaryPool<string, MaterialProperty>.Get(out var newDict);
            foreach (var p in newOvrs.PropertyOverrides) newDict[p.PropertyName] = p;
            using var _2 = DictionaryPool<string, MaterialProperty>.Get(out var origDict);
            foreach (var p in MaterialUtility.GetProperties(_recordingSourceMaterial)) origDict[p.PropertyName] = p;

            var modified = new List<MaterialProperty>();
            foreach (var p in cloned.PropertyOverrides)
            {
                var name = p.PropertyName;
                if (!newDict.ContainsKey(name) && origDict.TryGetValue(name, out var o))
                    modified.Add(o);
                else
                    modified.Add(p);
            }
            cloned.PropertyOverrides = modified;
        }

        // 新しい差分をマージ(上書き, 追加, 重複削除)する
        MaterialOverrideSettings.MergeInto(newOvrs, cloned);

        Undo.RecordObject(_target, "Sync AO Material Editor from Recording Material");
        _target.OverrideSettings = cloned;
    }

    // OverrideUtilityGUI
    private bool _showOverrideUtility = false;
    // private Texture? _sourceTexture = null;
    // private Texture? _destinationTexture = null;
    // private Material? _originalMaterial = null;
    // private Material? _overrideMaterial = null;
    // private Material? _variantMaterial = null;
    private void OverrideUtilityGUI()
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
    public static readonly Dictionary<Material, HashSet<string>> OverrideProperties = new();

    public static void StartRecording(Material recordingMaterial, HashSet<string> overrideProperties)
    {
        OverrideProperties[recordingMaterial] = overrideProperties;
    }

    public static void UpdateRecording(Material recordingMaterial, HashSet<string> overrideProperties)
    {
        OverrideProperties[recordingMaterial] = overrideProperties;
    }

    public static void StopRecording(Material recordingMaterial)
    {
        OverrideProperties.Remove(recordingMaterial);
    }
}