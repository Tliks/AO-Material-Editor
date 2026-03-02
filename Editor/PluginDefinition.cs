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

        // 外部に参照されていたマテリアルを割り当てるプラグインの後
        sequence = InPhase(BuildPhase.Transforming)
            .AfterPlugin("nadena.dev.modular-avatar")
            .AfterPlugin("net.narazaka.vrchat.avatar-menu-creater-for-ma")
            .AfterPlugin("jp.lilxyzw.lilycalinventory");
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
