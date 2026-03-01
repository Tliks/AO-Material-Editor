using nadena.dev.ndmf;
using Aoyon.MaterialEditor.Processor;

[assembly: ExportsPlugin(typeof(Aoyon.MaterialEditor.PluginDefinition))]

namespace Aoyon.MaterialEditor;

[RunsOnAllPlatforms]
internal sealed class PluginDefinition : Plugin<PluginDefinition>
{
    public override string QualifiedName => "aoyon.material-editor";
    public override string DisplayName => "AO Material Editor";

    protected override void Configure()
    {
        var sequence = InPhase(BuildPhase.Resolving);
        sequence.Run(ResolveReferencesPass.Instance);

        sequence = InPhase(BuildPhase.Transforming)
            .AfterPlugin("nadena.dev.modular-avatar");
        sequence.Run(MaterialEditorBuild.Instance)
            .PreviewingWith(new MaterialEditorPreview());
    }
}

internal sealed class ResolveReferencesPass : Pass<ResolveReferencesPass>
{
    public override string QualifiedName => "aoyon.material-editor.resolve-references";
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
