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

    private MaterialEditorSession _session = null!;

    private bool _showEditorDisplaySettings = false;
    private const float EditorContentLeftPadding = 4f;
    private static readonly GUIHelper.FoldoutOptions RectStrictFoldoutOptions = new(RectStrict: true);

    private void OnEnable()
    {
        _target = (MaterialEditorComponent)target;
        _avatarRoot = Utils.FindAvatarInParents(_target.gameObject);

        _targetSettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.TargetSettings));
        _overrideSettings = serializedObject.FindProperty(nameof(MaterialEditorComponent.OverrideSettings));

        _session = new MaterialEditorSession(
            _target,
            _avatarRoot,
            serializedObject,
            _overrideSettings);
    }

    private void OnDisable()
    {
        _session.Dispose();
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

        if (_session.RecordingSourceMaterial != null && _session.MaterialEditor != null)
        {
            var editor = _session.MaterialEditor;
            editor.DrawHeader();
            if (editor.isVisible) {
                var position = GetEditorPosition(GetEditorContentHeight());
                position = DrawOverridesGUI(position, "overrideSettings.help.visibleEditor");
                position = DrawOverrideUtility(position);
                position = DrawEditorDisplaySettings(position);
                position = DrawEditorHelp(position);
                GUIHelper.DrawFullWidthHorizontalLine(new Color(0.35f, 0.35f, 0.35f));
                // EditorGUILayout.Space();
                editor.OnInspectorGUI();
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
    
    private Rect DrawEditorDisplaySettings(Rect position)
    {
        if (_session.TargetMaterials.Count < 2) return position;

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

        MaterialSelector.Draw(selectorRect, () => _session.TargetMaterials.ToArray(), (m, i) =>
        {
            if (m != null) {
                _session.SelectRecordingSourceMaterial(m);
            }
        });
        using (new EditorGUI.DisabledGroupScope(true))
        {
            EditorGUI.ObjectField(objectFieldRect, GUIContent.none, _session.RecordingSourceMaterial, typeof(Material), false);
        }

        position.NewLine();
        return position;
    }

    private float GetEditorDisplaySettingsHeight()
    {
        if (_session.TargetMaterials.Count < 2) return 0f;

        var height = GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        if (_showEditorDisplaySettings)
        {
            height += GUIHelper.propertyHeight + GUIHelper.GUI_SPACE;
        }

        return height;
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
            _originalMaterial ??= _session.RecordingSourceMaterial;

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
            _session.ApplyExtractedOverrides(overrides);

            _originalMaterial = null;
            _overrideMaterial = null;
        }

        void ProcessMaterialVariantDiff(bool includeTexture)
        {
            if (_variantMaterial == null || !_variantMaterial.isVariant) return;

            var overrides = MaterialUtility.GetVariantOverrides(_variantMaterial, includeTexture);
            _session.ApplyExtractedOverrides(overrides);

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

}

internal sealed class MaterialEditorSession : IDisposable
{
    private readonly MaterialEditorComponent _target;
    private readonly GameObject? _avatarRoot;
    private readonly SerializedObject _serializedObject;
    private readonly SerializedProperty _overrideSettings;

    private readonly HashSet<MaterialAssignment> _allAssignments;
    private readonly MaterialEditorComponent[] _allComponents;
    private readonly int _myIndex = -1;

    private MaterialOverrideSettings _beforeOverrides = MaterialOverrideSettings.Empty;
    private MaterialOverrideSettings _afterOverrides = MaterialOverrideSettings.Empty;

    public HashSet<Material> TargetMaterials { get; private set; }
    public Material? RecordingSourceMaterial { get; private set; }
    private Material? _unlockedRecordingSourceMaterial;

    private Material? _diffBaseMaterial;
    private bool _diffBaseMaterialDirty = true;

    public Material RecordingMaterial { get; }
    public UnityEditor.MaterialEditor MaterialEditor { get; }

    private readonly ChangeWatcher<int> _targetSettingsChangeWatcher;
    private readonly ChangeWatcher<int> _overrideSettingsChangeWatcher;
    private readonly ChangeWatcher<int> _recordingMaterialChangeWatcher;

    private bool _materialEditorRepaintScheduled;
    private const string RecordingMaterialName = "Recording… (Cloned)";

    private static readonly Dictionary<MaterialEditorComponent, MaterialEditorSession> ComponentToSession = new();
    private static readonly Dictionary<Material, MaterialEditorSession> RecordingMaterialToSession = new();

    public MaterialEditorSession(
        MaterialEditorComponent target,
        GameObject? avatarRoot,
        SerializedObject serializedObject,
        SerializedProperty overrideSettings)
    {
        _target = target;
        _avatarRoot = avatarRoot;
        _serializedObject = serializedObject;
        _overrideSettings = overrideSettings;

        var materialTargeting = new DefaultMaterialTargeting();
        var renderers = _avatarRoot != null ? MaterialEditorProcessor.GetTargetRenderers(_avatarRoot) : new List<Renderer>();
        _allAssignments = materialTargeting.GetAssignments(renderers).ToHashSet();
        _allComponents = _avatarRoot != null ? _avatarRoot.GetComponentsInChildren<MaterialEditorComponent>(true) : Array.Empty<MaterialEditorComponent>();
        _myIndex = Array.IndexOf(_allComponents, _target);

        TargetMaterials = UpdateTargetMaterials();
        RecordingSourceMaterial = AutoSelectRecordingSourceMaterial();
        if (RecordingSourceMaterial != null) {
            _unlockedRecordingSourceMaterial = CreateUnlockedRecordingSourceMaterial(RecordingSourceMaterial);
            RecordingMaterial = new Material(_unlockedRecordingSourceMaterial) { name = RecordingMaterialName };
            UpdateOtherComponentOverrides();
            SyncRecordingMaterialFromComponent();
        }
        else {
            RecordingMaterial = new Material(Shader.Find("Standard")) { name = RecordingMaterialName };
        }

        MaterialEditor = (UnityEditor.MaterialEditor)Editor.CreateEditor(RecordingMaterial, typeof(UnityEditor.MaterialEditor));
        InternalEditorUtility.SetIsInspectorExpanded(RecordingMaterial, true); // 初期状態でEditorを展開しておく

        _targetSettingsChangeWatcher = new ChangeWatcher<int>(
            () => _target.TargetSettings.GetHashCode(),
            (previous, current) => previous == current,
            (_, _) => OnEntrySettingsChanged());
        _overrideSettingsChangeWatcher = new ChangeWatcher<int>(
            () => _target.OverrideSettings.GetHashCode(),
            (previous, current) => previous == current,
            (_, _) => OnOverrideSettingsChanged());
        _recordingMaterialChangeWatcher = new ChangeWatcher<int>(
            () => RecordingMaterial.ComputeCRC(),
            (previous, current) => previous == current,
            (_, _) => OnRecordingMaterialChanged());
        _targetSettingsChangeWatcher.Start();
        _overrideSettingsChangeWatcher.Start();
        _recordingMaterialChangeWatcher.Start();

        ComponentToSession[_target] = this;
        RecordingMaterialToSession[RecordingMaterial] = this;
    }

    public static bool IsRecording => RecordingMaterialToSession.Count > 0;

    public static bool TryGetRecordingSession(
        Material recordingMaterial,
        [NotNullWhen(true)] out MaterialEditorSession? session)
    {
        return RecordingMaterialToSession.TryGetValue(recordingMaterial, out session);
    }

    public static bool TryGetRecordingSession(
        MaterialEditorComponent component,
        [NotNullWhen(true)] out MaterialEditorSession? session)
    {
        return ComponentToSession.TryGetValue(component, out session);
    }

    public void Dispose()
    {
        _targetSettingsChangeWatcher.Dispose();
        _overrideSettingsChangeWatcher.Dispose();
        _recordingMaterialChangeWatcher.Dispose();

        ComponentToSession.Remove(_target);
        RecordingMaterialToSession.Remove(RecordingMaterial);

        if (_unlockedRecordingSourceMaterial != null) { Object.DestroyImmediate(_unlockedRecordingSourceMaterial); }
        if (_diffBaseMaterial != null) { Object.DestroyImmediate(_diffBaseMaterial); }
        if (RecordingMaterial != null) { Object.DestroyImmediate(RecordingMaterial); }
        if (MaterialEditor != null) { Object.DestroyImmediate(MaterialEditor); }
    }

    public void SelectRecordingSourceMaterial(Material material)
    {
        RecordingSourceMaterial = material;
        OnRecordingSourceMaterialChanged();
    }

    public bool IsShaderLocked => _afterOverrides.OverrideShader;
    public bool IsRenderQueueLocked => _afterOverrides.OverrideRenderQueue;

    public bool IsPropertyOverriden(string propertyName)
    {
        return _target.OverrideSettings.PropertyOverrides.Any(property => property.PropertyName == propertyName);
    }

    public bool IsPropertyLocked(string propertyName)
    {
        return _afterOverrides.PropertyOverrides.Any(property => property.PropertyName == propertyName);
    }

    public bool IsSerializedPropertyOverriden(RecordingMaterialSerializedProperty property)
    {
        return property switch
        {
            RecordingMaterialSerializedProperty.CustomRenderQueue => _target.OverrideSettings.OverrideRenderQueue,
            RecordingMaterialSerializedProperty.LightmapFlags => _target.OverrideSettings.OverrideLightmapFlags,
            RecordingMaterialSerializedProperty.EnableInstancingVariants => _target.OverrideSettings.OverrideEnableInstancing,
            RecordingMaterialSerializedProperty.DoubleSidedGI => _target.OverrideSettings.OverrideDoubleSidedGI,
            _ => false,
        };
    }

    public bool IsSerializedPropertyLocked(RecordingMaterialSerializedProperty property)
    {
        return property switch
        {
            RecordingMaterialSerializedProperty.CustomRenderQueue => _afterOverrides.OverrideRenderQueue,
            RecordingMaterialSerializedProperty.LightmapFlags => _afterOverrides.OverrideLightmapFlags,
            RecordingMaterialSerializedProperty.EnableInstancingVariants => _afterOverrides.OverrideEnableInstancing,
            RecordingMaterialSerializedProperty.DoubleSidedGI => _afterOverrides.OverrideDoubleSidedGI,
            _ => false,
        };
    }

    private HashSet<Material> UpdateTargetMaterials()
    {
        TargetMaterials = MaterialEditorProcessor.SelectTargetAssignments(_allAssignments, _target)
            .Select(a => a.Material)
            .ToHashSet();
        return TargetMaterials;
    }

    private Material? AutoSelectRecordingSourceMaterial()
    {
        Material? newTarget;

        var current = RecordingSourceMaterial;
        if (current != null && TargetMaterials.Contains(current)) {
            newTarget = current;
        }
        else {
            newTarget = TargetMaterials.FirstOrDefault();
        }

        RecordingSourceMaterial = newTarget;
        return RecordingSourceMaterial;
    }

    private Material CreateUnlockedRecordingSourceMaterial(Material source)
    {
        var unlocked = new Material(source);
        MaterialUtility.Unlock(unlocked, source);
        return unlocked;
    }

    private void OnEntrySettingsChanged()
    {
        UpdateTargetMaterials();
        AutoSelectRecordingSourceMaterial();
        OnRecordingSourceMaterialChanged();
    }

    private void OnRecordingSourceMaterialChanged()
    {
        RefreshUnlockedRecordingSourceMaterial();
        UpdateOtherComponentOverrides();
        using (_recordingMaterialChangeWatcher.ChangeWithoutNotify())
        {
            SyncRecordingMaterialFromComponent();
        }
        NotifyOtherSessionsChanged();
        ScheduleMaterialEditorRepaint();
    }

    private void OnOverrideSettingsChanged()
    {
        using (_recordingMaterialChangeWatcher.ChangeWithoutNotify())
        {
            SyncRecordingMaterialFromComponent();
        }
        NotifyOtherSessionsChanged();
        ScheduleMaterialEditorRepaint();
    }

    private void OnRecordingMaterialChanged()
    {
        using (_recordingMaterialChangeWatcher.ChangeWithoutNotify())
        using (_overrideSettingsChangeWatcher.ChangeWithoutNotify())
        {
            CommitRecordingMaterialChange();
        }
        NotifyOtherSessionsChanged();
        ScheduleMaterialEditorRepaint();
    }

    // hierarchy上でenabledやeditoronlyが変化したり、削除、移動された場合にも呼ばれるべきではある
    // ここでは同時に開いているMaterialEditorComponentの変更のみ追従する
    // Todo
    private void OnOtherComponentChanged()
    {
        UpdateOtherComponentOverrides();
        using (_recordingMaterialChangeWatcher.ChangeWithoutNotify())
        {
            SyncRecordingMaterialFromComponent();
        }
        ScheduleMaterialEditorRepaint();
    }

    private void RefreshUnlockedRecordingSourceMaterial()
    {
        if (_unlockedRecordingSourceMaterial != null)
        {
            Object.DestroyImmediate(_unlockedRecordingSourceMaterial);
            _unlockedRecordingSourceMaterial = null;
        }

        if (RecordingSourceMaterial != null) {
            _unlockedRecordingSourceMaterial = CreateUnlockedRecordingSourceMaterial(RecordingSourceMaterial);
        }
        else {
            _unlockedRecordingSourceMaterial = null;
        }

        InvalidateDiffBaseMaterial();
    }

    private void CommitRecordingMaterialChange()
    {
        if (_target == null || RecordingMaterial == null) return;

        if (!SanitizeRecordingMaterialAgainstAfter()) return; // サニタイズに失敗した状態でコンポーネントに書き込むべきではない

        var maintainEqualOverrides = _target.TargetSettings.Mode != MaterialTargetSettings.SelectionMode.SingleMaterial;
        var nextOverrides = BuildOverrideSettingsFromRecordingMaterial(maintainEqualOverrides);
        if (nextOverrides == null) return;

        CommitOverrideSettings(nextOverrides);
    }

    private void CommitOverrideSettings(MaterialOverrideSettings value)
    {
        _overrideSettings.CopyFrom(value);
        _serializedObject.ApplyModifiedProperties();
    }

    private void UpdateOtherComponentOverrides()
    {
        _beforeOverrides = MaterialOverrideSettings.Empty;
        _afterOverrides = MaterialOverrideSettings.Empty;

        if (_avatarRoot == null || RecordingSourceMaterial == null) return;

        for (int i = 0; i < _allComponents.Length; i++)
        {
            if (i == _myIndex) continue;
            var component = _allComponents[i];
            if (!MaterialEditorProcessor.IsEffective(component)) continue;

            var targetAssignments = MaterialEditorProcessor.SelectTargetAssignments(_allAssignments, component);
            if (targetAssignments.Any(a => a.Material == RecordingSourceMaterial))
            {
                if (i < _myIndex)
                {
                    _beforeOverrides.Merge(component.OverrideSettings);
                }
                else
                {
                    _afterOverrides.Merge(component.OverrideSettings);
                }
            }
        }

        InvalidateDiffBaseMaterial();
    }

    private void NotifyOtherSessionsChanged()
    {
        foreach (var session in RecordingMaterialToSession.Values.ToArray())
        {
            if (session == this) continue;
            session.OnOtherComponentChanged();
        }
    }

    private void SyncRecordingMaterialFromComponent()
    {
        DebugLog("SyncRecordingMaterialFromComponent, frame: " + Time.frameCount);

        if (_unlockedRecordingSourceMaterial == null) return;

        _serializedObject.ApplyModifiedProperties();

        // sourceの状態に初期化
        MaterialUtility.CopyAllSettings(_unlockedRecordingSourceMaterial, RecordingMaterial);
        // 1. 自分より上のオーバーライドを反映
        MaterialUtility.ApplyOverrideSettings(RecordingMaterial, _beforeOverrides);
        // 2. 自分自身のオーバーライドを反映
        MaterialUtility.ApplyOverrideSettings(RecordingMaterial, _target.OverrideSettings);
        // 3. 自分より下のオーバーライドを反映 (上書き)
        MaterialUtility.ApplyOverrideSettings(RecordingMaterial, _afterOverrides);
    }

    private MaterialOverrideSettings? BuildOverrideSettingsFromRecordingMaterial(bool maintainEquals)
    {
        DebugLog("BuildOverrideSettingsFromRecordingMaterial, frame: " + Time.frameCount);

        var baseMaterial = GetDiffBaseMaterial();
        if (baseMaterial == null) return null;

        try
        {
            var current = MaterialUtility.GetOverrides(baseMaterial, RecordingMaterial, false, true);
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
                _serializedObject.ApplyModifiedProperties();
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
    }

    private Material? GetDiffBaseMaterial()
    {
        if (_unlockedRecordingSourceMaterial == null) return null;
        if (!_diffBaseMaterialDirty && _diffBaseMaterial != null) return _diffBaseMaterial;

        if (_diffBaseMaterial != null)
        {
            Object.DestroyImmediate(_diffBaseMaterial);
        }

        _diffBaseMaterial = new Material(_unlockedRecordingSourceMaterial);
        MaterialUtility.ApplyOverrideSettings(_diffBaseMaterial, _beforeOverrides);
        MaterialUtility.ApplyOverrideSettings(_diffBaseMaterial, _afterOverrides);
        _diffBaseMaterialDirty = false;
        return _diffBaseMaterial;
    }

    private void InvalidateDiffBaseMaterial()
    {
        _diffBaseMaterialDirty = true;
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

    public void RevertRecordingProperty(string propertyName)
    {
        ApplyManualRecordingMaterialChange(
            baseMaterial =>
            {
                if (TryGetBaseProperty(baseMaterial, out var baseProperty))
                {
                    baseProperty.TrySet(RecordingMaterial);
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

    public void RevertRecordingSerializedProperty(RecordingMaterialSerializedProperty property)
    {
        ApplyManualRecordingMaterialChange(
            baseMaterial =>
            {
                using var baseSo = new SerializedObject(baseMaterial);
                using var recordingSo = new SerializedObject(RecordingMaterial);

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
            settings =>
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
            });
    }

    private void ApplyManualRecordingMaterialChange(
        Action<Material> mutateRecordingMaterial,
        Action<MaterialOverrideSettings> applyExplicitOverrideEdit)
    {
        if (_unlockedRecordingSourceMaterial == null) return;

        using (_overrideSettingsChangeWatcher.ChangeWithoutNotify())
        using (_recordingMaterialChangeWatcher.ChangeWithoutNotify())
        {
            var diffBaseMaterial = GetDiffBaseMaterial();
            if (diffBaseMaterial == null) return;

            var baseMaterial = new Material(diffBaseMaterial);
            try
            {
                mutateRecordingMaterial(baseMaterial);
                MaterialUtility.Normalize(RecordingMaterial);

                if (!SanitizeRecordingMaterialAgainstAfter()) return;

                var maintainEqualOverrides = _target.TargetSettings.Mode != MaterialTargetSettings.SelectionMode.SingleMaterial;
                var nextOverrides = BuildOverrideSettingsFromRecordingMaterial(maintainEqualOverrides);
                if (nextOverrides == null) return;

                applyExplicitOverrideEdit(nextOverrides);
                CommitOverrideSettings(nextOverrides);
                OnManualOverrideSettingsCommitted();
            }
            finally
            {
                Object.DestroyImmediate(baseMaterial);
            }
        }
    }

    public void ApplyExtractedOverrides(MaterialOverrideSettings extractedOverrides)
    {
        if (!SanitizeExtractedOverridesAgainstAfter(extractedOverrides)) return;
        if (extractedOverrides.OverrideCount == 0) return;

        _serializedObject.ApplyModifiedProperties();

        var merged = _target.OverrideSettings.Clone();
        merged.Merge(extractedOverrides);

        using (_overrideSettingsChangeWatcher.ChangeWithoutNotify())
        using (_recordingMaterialChangeWatcher.ChangeWithoutNotify())
        {
            CommitOverrideSettings(merged);
            OnManualOverrideSettingsCommitted();
        }
    }

    private void OnManualOverrideSettingsCommitted()
    {
        NotifyOtherSessionsChanged();
        SyncRecordingMaterialFromComponent();
        ScheduleMaterialEditorRepaint();
    }

    private void ScheduleMaterialEditorRepaint()
    {
        if (_materialEditorRepaintScheduled) return;

        _materialEditorRepaintScheduled = true;
        EditorApplication.delayCall += () =>
        {
            _materialEditorRepaintScheduled = false;
            if (MaterialEditor != null) {
                MaterialEditor.Repaint();
            }
        };
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


    private bool SanitizeRecordingMaterialAgainstAfter()
    {
        if (_unlockedRecordingSourceMaterial == null) return true;

        if (!_afterOverrides.OverrideShader && !_afterOverrides.OverrideRenderQueue && _afterOverrides.PropertyOverrides.Count == 0) return true;

        var authoritative = new Material(_unlockedRecordingSourceMaterial);
        try
        {
            var sanitized = false;

            _serializedObject.ApplyModifiedProperties();

            MaterialUtility.ApplyOverrideSettings(authoritative, _beforeOverrides);
            MaterialUtility.ApplyOverrideSettings(authoritative, _target.OverrideSettings);
            MaterialUtility.ApplyOverrideSettings(authoritative, _afterOverrides);

            var currentDiff = MaterialUtility.GetOverrides(authoritative, RecordingMaterial, false, true);
            var conflicts = GetAfterOverrideConflicts(currentDiff);

            // 固定されたシェーダーが変更された場合は、プロパティの空間が大規模に変わるので、同時に発生した変更を全て巻き戻す。
            if (conflicts.ShaderLocked)
            {
                MaterialUtility.CopyAllSettings(authoritative, RecordingMaterial);
                LocalizedLog.Warning("lock.shader.log", authoritative.shader.name);
                sanitized = true;
            }
            else
            {
                if (conflicts.RenderQueueLocked)
                {
                    using var authoritativeSo = new SerializedObject(authoritative);
                    using var recordingSo = new SerializedObject(RecordingMaterial);
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

                        authoritativeProperty.TrySet(RecordingMaterial);
                        LocalizedLog.Warning("lock.property.log", propertyName, authoritativeProperty.PropertyValue);
                        sanitized = true;
                    }
                }
            }

            if (sanitized && MaterialEditor != null)
            {
                MaterialEditor.Repaint();
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
            Object.DestroyImmediate(authoritative);
        }
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

    private static void DebugLog(string message)
    {
#if MATERIAL_EDITOR_DEBUG_EDITOR
        Debug.Log("MaterialEditorSession: " + message);
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

internal sealed class ChangeWatcher<T> : IDisposable
{
    private const double PollInterval = 0.1d;
    private readonly Func<T> _capture;
    private readonly Func<T, T, bool> _equals;
    private readonly Action<T, T> _onChanged;
    private T _snapshot;
    private int _suppressCount;
    private double _nextPollTime;
    private bool _started;
    private bool _disposed;

    public ChangeWatcher(
        Func<T> capture,
        Func<T, T, bool> equals,
        Action<T, T> onChanged)
    {
        _capture = capture;
        _equals = equals;
        _onChanged = onChanged;
        _snapshot = _capture();
    }


    public void Start()
    {
        if (_started || _disposed) return;

        _started = true;
        EditorApplication.update += OnEditorUpdate;
    }

    public IDisposable ChangeWithoutNotify()
    {
        return new ChangeWithoutNotifyScope(this);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        if (_started)
        {
            EditorApplication.update -= OnEditorUpdate;
        }
    }

    private void OnEditorUpdate()
    {
        if (_disposed || !_started || _suppressCount > 0) return;
        if (EditorApplication.timeSinceStartup < _nextPollTime) return;
        _nextPollTime = EditorApplication.timeSinceStartup + PollInterval;

        var current = _capture();
        if (_equals(_snapshot, current)) return;

        var previous = _snapshot;
        _snapshot = current;
        _onChanged(previous, current);
    }

    private sealed class ChangeWithoutNotifyScope : IDisposable
    {
        private readonly ChangeWatcher<T> _owner;
        private bool _disposed;

        public ChangeWithoutNotifyScope(ChangeWatcher<T> owner)
        {
            _owner = owner;
            _owner._suppressCount++;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            if (!_owner._disposed)
            {
                _owner._snapshot = _owner._capture();
            }
            _owner._suppressCount--;
        }
    }
}
