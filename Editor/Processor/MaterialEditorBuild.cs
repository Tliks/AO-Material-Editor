using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;

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
        var materialTargeting = new MaterialTargeting(
            new DefaultMaterialTargeting(), 
            new AnimatorMaterialTargeting(root, animationIndex)
        );

        var renderers = MaterialEditorProcessor.GetTargetRenderers(root);
        var allAssignments = materialTargeting.GetAssignments(renderers).ToHashSet();

        var overridePlans = MaterialEditorProcessor.BuildOverridePlans(effectiveComponents, 
            allAssignments, Utils.OriginalReferenceEquals, Utils.OriginalReferenceEquals);

        var replacements = MaterialEditorProcessor.CloneAndApplyOverrides(overridePlans, 
            m => Utils.CloneAndRegister(m));

        materialTargeting.ApplyReplacements(replacements);

        foreach (var component in allComponents)
        {
            Object.DestroyImmediate(component);
        }
    }
}
