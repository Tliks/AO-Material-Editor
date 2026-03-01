using UnityEngine.Animations;
using nadena.dev.ndmf;

namespace Aoyon.MaterialEditor
{
    [AddComponentMenu("AO Material Editor/AO Material Editor")]
    internal class MaterialEditorComponent : MonoBehaviour, INDMFEditorOnly
    {
        [NotKeyable]
        public MaterialEntrySettings EntrySettings = new();

        [NotKeyable]
        public MaterialOverrideSettings OverrideSettings = new();

        public void ResolveReferences()
        {
            EntrySettings.ResolveReferences(this);
        }

        // to inspector enabled checkbox
        void Start()
        {
        }
    }
}