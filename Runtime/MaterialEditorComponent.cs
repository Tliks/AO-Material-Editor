using UnityEngine.Animations;

namespace Aoyon.MaterialEditor
{
    [AddComponentMenu($"{Constants.DisplayName}/{Constants.DisplayName}")]
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