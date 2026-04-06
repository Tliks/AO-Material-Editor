using System.Collections.Immutable;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using UnityEngine.Pool;

namespace Aoyon.MaterialEditor.Processor;

internal class MaterialEditorPreview : IRenderFilter
{
    private readonly PropCache<GameObject, ComponentTargets> _originalComponentTargetsCache = new(
        "MaterialEditorPreview.OriginalComponentTargets", AnalyzeOriginalComponentTargets, (a, b) => a.Equals(b));

    ImmutableList<RenderGroup> IRenderFilter.GetTargetGroups(ComputeContext context)
    {
        try
        {
            var groups = ImmutableList.CreateBuilder<RenderGroup>();
            foreach (var root in context.GetAvatarRoots().Distinct())
            {
                var componentTargets = _originalComponentTargetsCache.Get(context, root);
                if (componentTargets.Values.Length == 0) continue;

                groups.AddRange(BuildRenderGroups(componentTargets));
            }
            return groups.ToImmutable();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return ImmutableList<RenderGroup>.Empty;
        }
    }

    private static ComponentTargets AnalyzeOriginalComponentTargets(ComputeContext context, GameObject root)
    {
        var componentTargets = ImmutableArray.CreateBuilder<(MaterialEditorComponent, ImmutableHashSet<MaterialAssignment>)>();

        var renderers = MaterialEditorProcessor.GetTargetRenderers(root, context);
        var allAssignments = new DefaultMaterialTargeting(context).GetAssignments(renderers).ToHashSet();
        var components = context.GetComponentsInChildren<MaterialEditorComponent>(root, true);

        foreach (var component in components)
        {
            // RenderGroupを小さくするために初期状態を対象から除外する
            var hasOverrides = context.Observe(component, c => c.OverrideSettings.OverrideCount > 0, (a, b) => a == b);
            if (!hasOverrides) continue;

            // OriginalRendererなのでObjectRegistryは見なくていい
            var targetAssignments = MaterialEditorProcessor.SelectTargetAssignments(allAssignments, component, null, null, context);
            if (targetAssignments.Count == 0) continue;

            componentTargets.Add((component, targetAssignments.ToImmutableHashSet()));
        }

        return new ComponentTargets(componentTargets.ToImmutable());
    }

    record ComponentTargets(ImmutableArray<(MaterialEditorComponent Component, ImmutableHashSet<MaterialAssignment> Assignments)> Values)
    {
        public virtual bool Equals(ComponentTargets other)
        {
            if (Values.Length != other.Values.Length) return false;
            for (var i = 0; i < Values.Length; i++)
            {
                if (Values[i].Component != other.Values[i].Component) return false;
                if (!CollectionEquality.SetEquals(Values[i].Assignments, other.Values[i].Assignments)) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hash = 0;
            foreach (var (component, assignments) in Values)
            {
                hash = HashCode.Combine(hash, component, CollectionEquality.GetSetHashCode(assignments));
            }
            return HashCode.Combine(Values.Length, hash);
        }
    }

    private static RenderGroup[] BuildRenderGroups(ComponentTargets componentTargets)
    {
        var componentOrder = componentTargets.Values
            .Select((entry, index) => (entry.Component, index))
            .ToDictionary(x => x.Component, x => x.index);

        var rendererGroups = new List<(HashSet<Renderer> renderers, HashSet<MaterialEditorComponent> components)>();
        foreach (var (component, assignments) in componentTargets.Values)
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
        return rendererGroups
            .Select(r => RenderGroup.For(r.renderers).WithData(new PassingData(
                r.components
                    .OrderBy(component => componentOrder[component])
                    .ToImmutableArray()
            )))
            .ToArray();
    }

    record PassingData(ImmutableArray<MaterialEditorComponent> Components)
    {
        public virtual bool Equals(PassingData other) => CollectionEquality.SequenceEquals(Components, other.Components);
        public override int GetHashCode() => CollectionEquality.GetSequenceHashCode(Components);
    }

    Task<IRenderFilterNode> IRenderFilter.Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
    {
        try
        {
            var components = group.GetData<PassingData>().Components;
            return Node.Create(proxyPairs, context, components);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return Task.FromResult<IRenderFilterNode>(new EmptyNode(0));
        }
    }

    class Node : IRenderFilterNode
    {
        private readonly ImmutableArray<MaterialEditorComponent> _components;
        private OverridePlans _currentPlans;
    
        // proxyの参照に依存せずoriginalのSlotIDを用いる
        private Dictionary<MaterialSlotId, Material> _replacements;

        public RenderAspects WhatChanged { get; private set; } = RenderAspects.Material;

        private Node(ImmutableArray<MaterialEditorComponent> components, OverridePlans plans, 
            Dictionary<MaterialSlotId, Material> replacements)
        {
            _components = components;
            _currentPlans = plans;
            _replacements = replacements;
        }

        public static Task<IRenderFilterNode> Create(IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context,
            ImmutableArray<MaterialEditorComponent> components)
        {
            try
            {
                using var _1 = ListPool<(Renderer Original, Renderer Proxy)>.Get(out var proxyPairsList);
                foreach (var (original, proxy) in proxyPairs) proxyPairsList.Add((original, proxy));

                var plans = ComputeOverridePlans(context, proxyPairsList, components);
                var replacements = BuildReplacements(proxyPairsList, plans);
                return Task.FromResult<IRenderFilterNode>(new Node(components, plans, replacements));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Task.FromResult<IRenderFilterNode>(new EmptyNode(0));
            }
        }

        public Task<IRenderFilterNode> Refresh(IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context, RenderAspects updatedAspects)
        {
            var materialChanged = updatedAspects.HasFlag(RenderAspects.Material);
            var definitelyIrrelevant = updatedAspects != 0 && !materialChanged;

            if (definitelyIrrelevant) 
            {
                WhatChanged = 0;
                return Task.FromResult<IRenderFilterNode>(this);
            }

            try
            {
                using var _1 = ListPool<(Renderer Original, Renderer Proxy)>.Get(out var proxyPairsList);
                foreach (var (original, proxy) in proxyPairs) proxyPairsList.Add((original, proxy));

                var plans = ComputeOverridePlans(context, proxyPairsList, _components);
                var plansChanged = !_currentPlans.Equals(plans);

                if (!plansChanged && !materialChanged)
                {
                    WhatChanged = 0;
                    return Task.FromResult<IRenderFilterNode>(this);
                }

                var replacements = BuildReplacements(proxyPairsList, plans);
                return Task.FromResult<IRenderFilterNode>(new Node(_components, plans, replacements));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                // 前回から変更したのか分からないので安全側に寄せる
                return Task.FromResult<IRenderFilterNode>(new EmptyNode(RenderAspects.Material));
            }
        }

        public void OnFrame(Renderer original, Renderer proxy)
        {
            var materials = proxy.sharedMaterials;
            var modified = false;

            for (var i = 0; i < materials.Length; i++)
            {
                var slotId = new MaterialSlotId(original, i);
                if (!_replacements.TryGetValue(slotId, out var replacement)) continue;

                materials[i] = replacement;
                modified = true;
            }

            if (modified)
            {
                proxy.sharedMaterials = materials;
            }
        }

        public void Dispose()
        {
            foreach (var material in _replacements.Values)
            {
                if (material == null) continue;
                Object.DestroyImmediate(material);
            }
            _replacements.Clear();
        }

        // proxyの参照に依存せずoriginalのSlotIDを用いる
        record OverridePlans(Dictionary<MaterialSlotId, MaterialOverrideSettings> Values)
        {
            public virtual bool Equals(OverridePlans other) => CollectionEquality.DictionaryEquals(Values, other.Values, (a, b) => a.Equals(b));
            public override int GetHashCode() => CollectionEquality.GetDictionaryHashCode(Values, a => a.GetHashCode());
        }

        private static OverridePlans ComputeOverridePlans(ComputeContext context, IReadOnlyList<(Renderer Original, Renderer Proxy)> proxyPairs,
            ImmutableArray<MaterialEditorComponent> components)
        {
            var effectiveComponents = components.Where(c => MaterialEditorProcessor.IsEffective(c, context));
            using var _1 = DictionaryPool<Renderer, Renderer>.Get(out var originalByProxy);
            FillOriginalByProxyMap(proxyPairs, originalByProxy);

            var materialTargeting = new DefaultMaterialTargeting(null); // proxyに対する監視は不要
            using var _2 = HashSetPool<MaterialAssignment>.Get(out var proxyAssignments);
            foreach (var proxy in originalByProxy.Keys)
            {
                proxyAssignments.UnionWith(materialTargeting.GetAssignments(proxy));
            }
            
            var proxyOverridePlans = MaterialEditorProcessor.BuildOverridePlans(effectiveComponents, proxyAssignments,
                Utils.OriginalReferenceEquals, RendererCompare, context);

            bool RendererCompare(Renderer a, Renderer b) =>
                a == b || (originalByProxy.TryGetValue(a, out var o) && o == b)
                    || (originalByProxy.TryGetValue(b, out var o2) && o2 == a);

            var originalOverridePlans = new Dictionary<MaterialSlotId, MaterialOverrideSettings>(proxyOverridePlans.Count);
            foreach (var (proxyAssignment, settings) in proxyOverridePlans)
            {
                originalOverridePlans[RemapSlot(proxyAssignment.SlotId, originalByProxy)] = settings;
            }

            return new OverridePlans(originalOverridePlans);
        }

        private static Dictionary<MaterialSlotId, Material> BuildReplacements(IReadOnlyList<(Renderer Original, Renderer Proxy)> proxyPairs, 
            OverridePlans plans)
        {
            using var _1 = DictionaryPool<Renderer, Renderer>.Get(out var originalByProxy);
            FillOriginalByProxyMap(proxyPairs, originalByProxy);
            using var _2 = DictionaryPool<Renderer, Renderer>.Get(out var proxyByOriginal);
            FillProxyByOriginalMap(proxyPairs, proxyByOriginal);
            using var _3 = DictionaryPool<Renderer, Material[]>.Get(out var materialsByProxy);

            using var _4 = DictionaryPool<MaterialAssignment, MaterialOverrideSettings>.Get(out var replacementsInput);
            foreach (var (originalSlotId, settings) in plans.Values)
            {
                var originalRenderer = originalSlotId.Renderer;
                var materialIndex = originalSlotId.MaterialIndex;

                if (!proxyByOriginal.TryGetValue(originalRenderer, out var proxy)) continue;

                if (!materialsByProxy.TryGetValue(proxy, out var materials))
                {
                    materials = proxy.sharedMaterials;
                    materialsByProxy[proxy] = materials;
                }

                if (materialIndex < 0 || materialIndex >= materials.Length) continue;

                var material = materials[materialIndex];
                if (material == null) continue;

                var proxySlotId = RemapSlot(originalSlotId, proxyByOriginal);
                replacementsInput[new MaterialAssignment(proxySlotId, material)] = settings;
            }

            var proxyReplacements = MaterialEditorProcessor.CloneAndApplyOverrides(replacementsInput, Utils.CloneAndRegister);

            var replacements = new Dictionary<MaterialSlotId, Material>(proxyReplacements.Count);
            foreach (var (proxyAssignment, material) in proxyReplacements)
            {
                replacements[RemapSlot(proxyAssignment.SlotId, originalByProxy)] = material;
            }

            return replacements;
        }

        private static void FillOriginalByProxyMap(IReadOnlyList<(Renderer Original, Renderer Proxy)> proxyPairs,
            Dictionary<Renderer, Renderer> originalByProxy)
        {
            foreach (var (original, proxy) in proxyPairs) originalByProxy[proxy] = original;
        }

        private static void FillProxyByOriginalMap(IReadOnlyList<(Renderer Original, Renderer Proxy)> proxyPairs,
            Dictionary<Renderer, Renderer> proxyByOriginal)
        {
            foreach (var (original, proxy) in proxyPairs) proxyByOriginal[original] = proxy;
        }

        private static MaterialSlotId RemapSlot(MaterialSlotId slotId, IReadOnlyDictionary<Renderer, Renderer> rendererMap)
            => new(rendererMap[slotId.Renderer], slotId.MaterialIndex);
    }

    class EmptyNode : IRenderFilterNode
    {
        private readonly RenderAspects _whatChanged;

        public EmptyNode(RenderAspects whatChanged)
        {
            _whatChanged = whatChanged;
        }

        RenderAspects IRenderFilterNode.WhatChanged => _whatChanged;
        void IRenderFilterNode.OnFrame(Renderer original, Renderer proxy) { }
    }
}
