using UnityEngine.Animations;
using nadena.dev.ndmf;

namespace Aoyon.MaterialEditor
{
    internal class MaterialEditorComponentBase : MonoBehaviour, INDMFEditorOnly
    {
        [NotKeyable, HideInInspector]
        public int DataVersion = 0;

        void Reset()
        {
            DataVersion = Constants.CurrentDataVersion;
        }

        public bool IsLatestDataVersion()
        {
            return DataVersion == Constants.CurrentDataVersion;
        }
    }
}
