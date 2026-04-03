using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using Aoyon.MaterialEditor.Processor.Extension;

namespace Aoyon.MaterialEditor.Processor;

[DependsOnContext(typeof(AnimatorServicesContext))]
internal class MaterialEditorBuild : Pass<MaterialEditorBuild>
{
    public override string QualifiedName => "aoyon.material-editor.build";
    public override string DisplayName => "Build";

    protected override void Execute(BuildContext context)
    {
        var root = context.AvatarRootObject;

        var allComponents = root.GetComponentsInChildren<MaterialEditorComponent>(true);
        if (allComponents.Length == 0) return;

        var effectiveComponents = allComponents.Where(c => MaterialEditorProcessor.IsEffective(c));

        var animationIndex = context.Extension<AnimatorServicesContext>().AnimationIndex;
        // TTT/LLCに対する互換性を優先し、他ツールのAnimatorへ追加されるマテリアルはフィールド置き換えで対応する
        // https://github.com/Tliks/AO-Material-Editor/issues/18
        var materialTargeting = new MaterialTargeting(
            new DefaultMaterialTargeting(), 
            new AnimatorMaterialTargeting(root, animationIndex),
#if ME_LI
            new LilycalInventoryMaterialTargeting(root),
#endif
            new ModularAvatarMaterialTargeting(root)
        );

        var renderers = MaterialEditorProcessor.GetTargetRenderers(root);
        var allAssignments = materialTargeting.GetAssignments(renderers).ToHashSet();

        var overridePlans = MaterialEditorProcessor.BuildOverridePlans(effectiveComponents, 
            allAssignments, Utils.OriginalReferenceEquals, Utils.OriginalReferenceEquals);

        var replacements = MaterialEditorProcessor.CloneAndApplyOverrides(overridePlans, 
            Utils.CloneAndRegister);

        materialTargeting.ApplyReplacements(replacements);

        foreach (var component in allComponents)
        {
            Object.DestroyImmediate(component);
        }
    }
}
