using Aoyon.MaterialEditor.Extension;

namespace Aoyon.MaterialEditor.UI;

internal class MaterialEditorEditorUtility
{
    private readonly Action<MaterialOverrideSettings> _applyOverrides;

    public MaterialEditorEditorUtility(Action<MaterialOverrideSettings> applyOverrides)
    {
        _applyOverrides = applyOverrides;
    }
    
    private bool _showOverrideUtility = false;
    public void DrawOverrideUtility(Material recordingMaterial, Material recordingSourceMaterial)
    {
        _showOverrideUtility = EditorGUILayout.Foldout(_showOverrideUtility, "overrideUtility.title".LS(), true);
        if (!_showOverrideUtility) return;

        using var indent = new EditorGUI.IndentLevelScope();

        DrawReplaceTexture(recordingMaterial);
        DrawMaterialDiff(recordingSourceMaterial);
        DrawMaterialVariantDiff();
    }

    private bool _showReplaceTexture = false;
    private Texture? _sourceTexture = null;
    private Texture? _destinationTexture = null;
    private void DrawReplaceTexture(Material recordingMaterial)
    {
        _showReplaceTexture = EditorGUILayout.Foldout(_showReplaceTexture, "overrideUtility.replaceTexture.title".LS(), true);
        if (_showReplaceTexture)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _sourceTexture = EditorGUILayout.ObjectField("overrideUtility.replaceTexture.source".LS(), _sourceTexture, typeof(Texture), false, GUILayout.Height(18f)) as Texture;
                TextureSelector.Draw(() => MaterialUtility.EnumerateTextures(recordingMaterial).Distinct().ToArray(), (t, _) => { _sourceTexture = t; });
            }
            _destinationTexture = EditorGUILayout.ObjectField("overrideUtility.replaceTexture.destination".LS(), _destinationTexture, typeof(Texture), false, GUILayout.Height(18f)) as Texture;
            using (new EditorGUI.DisabledGroupScope(_sourceTexture == null || _destinationTexture == null))
            {
                if (GUILayout.Button("overrideUtility.replaceTexture.title".LS()))
                {
                    ProcessReplaceTexture();
                }
            }
        }

        void ProcessReplaceTexture()
        {
            if (_sourceTexture == null || _destinationTexture == null) return;

            var overrides = MaterialUtility.GetTextureReplacementOverrides(recordingMaterial, _sourceTexture, _destinationTexture);
            _applyOverrides(overrides);

            _sourceTexture = null;
            _destinationTexture = null;
        }
    }

    private bool _showMaterialDiff = false;
    private Material? _originalMaterial = null;
    private Material? _overrideMaterial = null;
    private void DrawMaterialDiff(Material recordingSourceMaterial)
    {
        _showMaterialDiff = EditorGUILayout.Foldout(_showMaterialDiff, "overrideUtility.materialDiff.title".LS(), true);
        if (_showMaterialDiff)
        {
            _originalMaterial ??= recordingSourceMaterial;
            _originalMaterial = EditorGUILayout.ObjectField("overrideUtility.materialDiff.original".LS(), _originalMaterial, typeof(Material), false) as Material;
            _overrideMaterial = EditorGUILayout.ObjectField("overrideUtility.materialDiff.modified".LS(), _overrideMaterial, typeof(Material), false) as Material;
        
            using (new EditorGUI.DisabledGroupScope(_originalMaterial == null || _overrideMaterial == null))
            {
                if (GUILayout.Button("overrideUtility.addChanges".LS()))
                {
                    ProcessMaterialDiff(true);
                }
                if (GUILayout.Button("overrideUtility.addChangesExcludeTexture".LS()))
                {
                    ProcessMaterialDiff(false);
                }
            }
        }

        void ProcessMaterialDiff(bool includeTexture)
        {
            if (_originalMaterial == null || _overrideMaterial == null) return;

            var overrides = MaterialUtility.GetOverrides(_originalMaterial, _overrideMaterial, false, true, includeTexture);
            _applyOverrides(overrides);

            _originalMaterial = null;
            _overrideMaterial = null;
        }
    }

    private bool _showMaterialVariantDiff = false;
    private Material? _variantMaterial = null;
    private void DrawMaterialVariantDiff()
    {
        _showMaterialVariantDiff = EditorGUILayout.Foldout(_showMaterialVariantDiff, "overrideUtility.variantDiff.title".LS(), true);
        if (_showMaterialVariantDiff)
        {
            _variantMaterial = EditorGUILayout.ObjectField("overrideUtility.variantDiff.material".LS(), _variantMaterial, typeof(Material), false) as Material;
            if (_variantMaterial != null && !_variantMaterial.isVariant)
            {
                EditorGUILayout.HelpBox("overrideUtility.variantDiff.notVariant".LS(), MessageType.Info);
            }
        
            using (new EditorGUI.DisabledGroupScope(_variantMaterial == null || !_variantMaterial.isVariant))
            {
                if (GUILayout.Button("overrideUtility.addChanges".LS()))
                {
                    ProcessMaterialVariantDiff(true);
                }
                if (GUILayout.Button("overrideUtility.addChangesExcludeTexture".LS()))
                {
                    ProcessMaterialVariantDiff(false);
                }
            }
        }

        void ProcessMaterialVariantDiff(bool includeTexture)
        {
            if (_variantMaterial == null || !_variantMaterial.isVariant) return;

            var overrides = MaterialUtility.GetVariantOverrides(_variantMaterial, includeTexture);
            _applyOverrides(overrides);

            _variantMaterial = null;
        
        }
    }

}
