using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;

namespace Aoyon.MaterialEditor.Processor;

[DependsOnContext(typeof(AnimatorServicesContext))]
internal class MaterialEditorBuild : Pass<MaterialEditorBuild>
{
    protected override void Execute(BuildContext context)
    {
        var root = context.AvatarRootObject;

        var components = root.GetComponentsInChildren<MaterialEditorComponent>(true)
            .Where(c => MaterialEditorProcessor.IsEffective(c))
            .ToList();
        if (components.Count == 0) return;

        var animationIndex = context.Extension<AnimatorServicesContext>().AnimationIndex;
        var materialTargeting = new MaterialTargeting(
            new DefaultMaterialTargeting(), 
            new AnimatorMaterialTargeting(root, animationIndex)
        );

        var renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r is SkinnedMeshRenderer or MeshRenderer);
        var allAssignments = materialTargeting.GetAssignments(renderers).ToHashSet();

        var overridePlans = MaterialEditorProcessor.BuildOverridePlans(components, 
            allAssignments, Utils.OriginalReferenceEquals);

        var replacements = MaterialEditorProcessor.CloneAndApplyOverrides(overridePlans, 
            m => Utils.CloneAndRegister(m));

        materialTargeting.ApplyReplacements(replacements);

        foreach (var component in components)
        {
            Object.DestroyImmediate(component);
        }
    }
}
