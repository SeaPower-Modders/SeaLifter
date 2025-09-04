using AnchorChain;
using Dummiesman;
using HarmonyLib;

using SeaPower;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

using Texture = UnityEngine.Texture;
#nullable enable
namespace Loader
{
    [HarmonyPatch(typeof(ObjectBaseLoader))] // Replace with the actual class that contains the method
    [HarmonyPatch("createObjectPartObject")]
    public class CreateObjectPartObjectPatch
    {
        public static void Postfix(ObjectBaseParameters obp, string objectIniFolder, string subModelName, bool isVisible, bool castShadows, bool hideWithActiveDamageModel,
                                   string alternativeMeshResourcesFolder, string alternativeMaterialResourcesFolder, string rootMeshName, string meshName,
                                   string matName, string materialTextureDiffuse, string materialTextureSpecular, string materialTextureNormal,
                                   string parentName, Vector3 pos, Vector3 rot, GameObject __result)
        {

            if (!IniConfig.DetailedSubPartLogging)
                return;
            // Print out the arguments
            Common.Log($"createObjectPartObject called with arguments: ");
            Common.Log($"obp: {obp}");
            Common.Log($"objectIniFolder: {objectIniFolder}");
            Common.Log($"subModelName: {subModelName}");
            Common.Log($"isVisible: {isVisible}");
            Common.Log($"castShadows: {castShadows}");
            Common.Log($"hideWithActiveDamageModel: {hideWithActiveDamageModel}");
            Common.Log($"alternativeMeshResourcesFolder: {alternativeMeshResourcesFolder}");
            Common.Log($"alternativeMaterialResourcesFolder: {alternativeMaterialResourcesFolder}");
            Common.Log($"rootMeshName: {rootMeshName}");
            Common.Log($"meshName: {meshName}");
            Common.Log($"matName: {matName}");
            Common.Log($"materialTextureDiffuse: {materialTextureDiffuse}");
            Common.Log($"materialTextureSpecular: {materialTextureSpecular}");
            Common.Log($"materialTextureNormal: {materialTextureNormal}");
            Common.Log($"parentName: {parentName}");
            Common.Log($"pos: {pos}");
            Common.Log($"rot: {rot}");

            // Print the name of the returned GameObject (if it's not null)
            if (__result != null)
            {
                Common.Log($"Returned GameObject: {__result.name}");
                Common.LogTransform(__result.transform);
                Common.Log($"obp root");
                Common.LogTransform(obp._rootGameObject.transform);
                
            }
            else
            {
                Common.Log("Returned GameObject is null.");
            }
        }
    }






    [ACPlugin("sp.sl.lib.chainloader.product", "Chainloader", "0.0.0.9")]
    public class Plugin2 : MonoBehaviour, IAnchorChainMod
    {
        public static string HarmonyName => "sp.sl.lib.harmony.product";

        void IAnchorChainMod.TriggerEntryPoint()
        {
            IniConfig.Load();

            // Plugin startup logic
            int ver = 11;

            Debug.LogWarning($"Plugin is loaded! Ver 0.0.0.{ver}");
            var harmony = new Harmony(HarmonyName);
            harmony.PatchAll();
        }
    }





    [HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.RequestGameObjectResource))]
    public class ResourceLoaderRequestGameObjectResource : MonoBehaviour
    {
        static void Prefix(ResourceLoader __instance, string resourceName, Action<GameObject> result, bool async = true, bool loggingEnabled = true)
        {
            ResourcePath resourcePath = new ResourcePath(resourceName);
        }
    }

    [HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.getGameObjectResource))]
    public class ResourceLoadergetGameObjectResource : MonoBehaviour
    { 

        public static void Postfix(ref GameObject __result, string resourceName)
        {

            ResourcePath resourcePath = new ResourcePath(resourceName, internalresouce: __result != null);
            __result = resourcePath.LoadResource(__result);//Logged

        }
    }

    [HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.getMaterialResource))]
    public class ResourceLoadergetMaterialResource : MonoBehaviour
    {
        public static void Postfix(ref Material __result, string resourceName)
        {
            ResourcePath resourcePath = new ResourcePath(resourceName, internalresouce: __result != null);
            resourcePath.DumpMaterial(__result);//Logged
            __result = resourcePath.LoadResource(__result);//Logged
        }

    }
    [HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.getTextureResource))]
    public class ResourceLoadergetTextureResource : MonoBehaviour
    {
        public static void Postfix(ref Texture __result, string resourceName)
        {
            ResourcePath resourcePath = new ResourcePath(resourceName, internalresouce: __result != null);
            resourcePath.SaveProtextedTexture(__result);//Logged
            __result = resourcePath.LoadResource(__result);//Logged;
        }

    }

}

