using UnityEngine.Animations;
using nadena.dev.ndmf;

namespace Aoyon.MaterialEditor
{
    internal class MaterialEditorComponentBase : MonoBehaviour, INDMFEditorOnly
    {
        public const int CurrentDataVersion = 1;

        [NotKeyable, HideInInspector]
        public int DataVersion = 0;

        void Reset()
        {
            DataVersion = CurrentDataVersion;
        }

        public bool IsLatestDataVersion()
        {
            return DataVersion == CurrentDataVersion;
        }
    }
}