using nadena.dev.ndmf;
using Aoyon.MaterialEditor.Processor;

[assembly: ExportsPlugin(typeof(Aoyon.MaterialEditor.PluginDefinition))]

namespace Aoyon.MaterialEditor;

[RunsOnAllPlatforms]
internal sealed class PluginDefinition : Plugin<PluginDefinition>
{
    public override string QualifiedName => Constants.QualifiedName; // aoyon.material-editor
    public override string DisplayName => Constants.DisplayName;

    protected override void Configure()
    {
        var sequence = InPhase(BuildPhase.Resolving);
        sequence.Run(ResolveReferencesPass.Instance);

        sequence = InPhase(BuildPhase.Transforming);
        sequence.Run(MaterialEditorBuild.Instance)
            .PreviewingWith(new MaterialEditorPreview())
            .BeforePlugin("net.rs64.tex-trans-tool")
#if ME_LLC_2_4_0_OR_NEWER
            .BeforePass("io.github.azukimochi.light-limit-changer.normalize-materials");
#else
            .BeforePlugin("io.github.azukimochi.light-limit-changer");
#endif
    }
}

internal sealed class ResolveReferencesPass : Pass<ResolveReferencesPass>
{
    public override string QualifiedName => Constants.QualifiedName + ".resolve-references";
    public override string DisplayName => "Resolve References";

    protected override void Execute(BuildContext context)
    {
        var components = context.AvatarRootObject.GetComponentsInChildren<MaterialEditorComponent>(true);
        foreach (var component in components)
        {
            component.ResolveReferences();
        }
    }
}
