using System.Collections.Immutable;
using System.Threading.Tasks;
using UnityEngine.Pool;
using nadena.dev.ndmf.preview;

namespace Aoyon.MaterialEditor.Processor;

internal class MaterialEditorPreview : IRenderFilter
{
    ImmutableList<RenderGroup> IRenderFilter.GetTargetGroups(ComputeContext context)
    {
        var groups = new List<RenderGroup>();

        try
        {
            var observeContext = new NDMFObserveContext(context);
            foreach (var root in context.GetAvatarRoots().Distinct())
            {
                if (!context.ActiveInHierarchy(root)) continue;

                var renderers = MaterialEditorProcessor.GetTargetRenderers(root, observeContext);
                // sharedmaterialsはNDMF側で監視されていない
                var allAssignments = new DefaultMaterialTargeting().GetAssignments(renderers, observeContext).ToHashSet(); 

                // 負荷軽減のため、IsEffectiveはGetTargetGroupsで監視せず、Instantiateで監視する。
                var allComponents = context.GetComponentsInChildren<MaterialEditorComponent>(root, true);

                // IsEffectiveは監視しないが、一切のオーバライドが存在しない初期状態のコンポーネントは除外するようにする
                // MenuItemから生成される初期のコンポーネント群を除外したい
                var editedComponents = allComponents
                    .Where(c => context.Observe(c, c => c.OverrideSettings.OverrideCount > 0, (a, b) => a == b));
                
                var componentTargets = new Dictionary<MaterialEditorComponent, HashSet<MaterialAssignment>>();
                foreach (var component in editedComponents)
                {
                    // OriginalRendererなのでObjectRegistryは見なくていい
                    var targetAssignments = MaterialEditorProcessor.SelectTargetAssignments(allAssignments, component, 
                        null, null, observeContext);
                    if (targetAssignments.Count == 0) continue;
                    componentTargets[component] = targetAssignments;
                }
                if (componentTargets.Count == 0) continue;

                groups.AddRange(BuildRenderGroups(componentTargets, root));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        return groups.ToImmutableList();
    }

    private static List<RenderGroup> BuildRenderGroups(Dictionary<MaterialEditorComponent, HashSet<MaterialAssignment>> componentTargets, GameObject root)
    {
        var rendererGroups = new List<(HashSet<Renderer> renderers, HashSet<MaterialEditorComponent> components)>();
        foreach (var (component, assignments) in componentTargets)
        {
            var renderers = assignments.Select(a => a.SlotId.Renderer).ToHashSet();
            var overlappingIndices = rendererGroups
                .Select((r, i) => (r.renderers.Overlaps(renderers), i))
                .Where(t => t.Item1)
                .Select(t => t.i)
                .OrderByDescending(i => i)
                .ToList();

            if (overlappingIndices.Count == 0)
            {
                rendererGroups.Add((renderers, new HashSet<MaterialEditorComponent> { component }));
            }
            else
            {
                var (mergeIntoRenderers, mergeIntoComponents) = rendererGroups[overlappingIndices[^1]];
                mergeIntoRenderers.UnionWith(renderers);
                mergeIntoComponents.Add(component);
                foreach (var idx in overlappingIndices.SkipLast(1))
                {
                    var (otherRenderers, otherComponents) = rendererGroups[idx];
                    mergeIntoRenderers.UnionWith(otherRenderers);
                    mergeIntoComponents.UnionWith(otherComponents);
                    rendererGroups.RemoveAt(idx);
                }
            }
        }
        return rendererGroups.Select(r => RenderGroup.For(r.renderers).WithData(new PassingData(root, r.components))).ToList();
    }

    // GetTargetGroupsは上流のInstantiateの影響を受けないので、詳細な適用対象はPassingDataではなく、Instantiate内で決定する
    Task<IRenderFilterNode> IRenderFilter.Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
    {
        try
        {
            var data = group.GetData<PassingData>();
            var root = data.Root;
            var components = data.Components;

            using var _1 = DictionaryPool<Renderer, Renderer>.Get(out var proxyToOriginal);
            foreach (var (original, proxy) in proxyPairs) proxyToOriginal[proxy] = original;

            var observeContext = new NDMFObserveContext(context);

            var materialTargeting = new DefaultMaterialTargeting();
            using var _3 = HashSetPool<MaterialAssignment>.Get(out var proxyMaterialAssignments);
            foreach (var (original, proxy) in proxyPairs)
            {
                proxyMaterialAssignments.UnionWith(materialTargeting.GetAssignments(proxy));
            }

            // 代わりにここでIsEffectiveを監視する
            var effectiveComponents = components.Where(c => MaterialEditorProcessor.IsEffective(c, observeContext));

            var proxyOverridePlans = MaterialEditorProcessor.BuildOverridePlans(effectiveComponents, proxyMaterialAssignments, 
                Utils.OriginalReferenceEquals, RendererCompare, observeContext);

            bool RendererCompare(Renderer a, Renderer b) =>
                a == b || (proxyToOriginal.TryGetValue(a, out var o) && o == b)
                    || (proxyToOriginal.TryGetValue(b, out var o2) && o2 == a);

            // proxyのmaterial参照は重要ではなく、sharedMaterialsの置き換えはSlotIDで十分
            // proxyの参照は不確実なので、original rendererのSlotIDを用いる
            var replacements = new Dictionary<MaterialSlotId, Material>();
            var proxyReplacements = MaterialEditorProcessor.CloneAndApplyOverrides(proxyOverridePlans, m => Utils.CloneAndRegister(m));
            foreach (var (proxyAssignment, material) in proxyReplacements)
            {
                var originalRenderer = proxyToOriginal[proxyAssignment.SlotId.Renderer];
                var originalSlotId = new MaterialSlotId(originalRenderer, proxyAssignment.SlotId.MaterialIndex);

                replacements[originalSlotId] = material;
            }

            return Task.FromResult<IRenderFilterNode>(new MaterialEditorNode(replacements));
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return Task.FromResult<IRenderFilterNode>(new EmptyNode());
        }
    }

    record PassingData(GameObject Root, HashSet<MaterialEditorComponent> Components);

    class MaterialEditorNode : IRenderFilterNode
    {
        RenderAspects IRenderFilterNode.WhatChanged => RenderAspects.Material;
        private readonly Dictionary<MaterialSlotId, Material> _replacements;

        public MaterialEditorNode(Dictionary<MaterialSlotId, Material> replacements)
        {
            _replacements = replacements;
        }

        void IRenderFilterNode.OnFrame(Renderer original, Renderer proxy)
        {
            var materials = proxy.sharedMaterials;
            var modified = false;
            for (int i = 0; i < materials.Length; i++)
            {
                // original rendererのSlotIDを用いる
                var slotId = new MaterialSlotId(original, i);
                if (_replacements.TryGetValue(slotId, out var replacement))
                {
                    materials[i] = replacement;
                    modified = true;
                }
            }
            if (modified)
            {
                proxy.sharedMaterials = materials;
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var replacement in _replacements.Values)
            {
                Object.DestroyImmediate(replacement);
            }
            _replacements.Clear();
        }
    }

    class EmptyNode : IRenderFilterNode
    {
        RenderAspects IRenderFilterNode.WhatChanged => 0;
        void IRenderFilterNode.OnFrame(Renderer original, Renderer proxy) { }
    }
}