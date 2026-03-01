using Aoyon.MaterialEditor.Processor;
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
        MaterialAndOverrideGUI();

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

    private void MaterialAndOverrideGUI()
    {
        EditorGUILayout.LabelField("# " + "Label:Edit".LS(), EditorStyles.boldLabel);
        if (_recordingSourceMaterial != null && _materialEditor != null)
        {
            _materialEditor.DrawHeader();
            if (_materialEditor.isVisible) {
                OverridesGUI();
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
        var lineRect = EditorGUILayout.GetControlRect();
        const float arrowWidth = 12f;
        var arrowRect = new Rect(lineRect.x, lineRect.y, arrowWidth, lineRect.height);
        var labelRect = new Rect(lineRect.x + arrowWidth, lineRect.y, lineRect.width - arrowWidth, lineRect.height);

        _showOverrides = EditorGUI.Foldout(arrowRect, _showOverrides, GUIContent.none, true);

        // Make the label a bit larger and bold, and center it
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = EditorStyles.boldLabel.fontSize
        };

        if (GUI.Button(labelRect, string.Format("Label:CurrentOverridesCount".LS(), count), labelStyle))
            _showOverrides = !_showOverrides;

        GUIHelper.DrawLine(lineRect);

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