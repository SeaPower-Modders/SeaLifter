using Loader;
using UnityEngine;

namespace LibLoader
{
    public class Class1 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }
}
