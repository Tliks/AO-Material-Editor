using UnityEngine.Animations;

namespace Aoyon.MaterialEditor
{
    [AddComponentMenu("AO Material Editor/AO Material Editor")]
    internal class MaterialEditorComponent : MaterialEditorComponentBase
    {
        [NotKeyable]
        public MaterialTargetSettings TargetSettings = new();

        [NotKeyable]
        public MaterialOverrideSettings OverrideSettings = new();

        [NotKeyable, Obsolete]
        public MaterialEntrySettings EntrySettings = new();

        public void ResolveReferences()
        {
            TargetSettings.ResolveReferences(this);
        }

        // to inspector enabled checkbox
        void Start()
        {
        }
    }
}