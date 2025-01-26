using HarmonyLib;
using SeaPower;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Loader
{

    //[HarmonyPatch(typeof(SeaPower.MusicManager), "LoadMusicData")]
    public class DisableLoadMusicDataPatch
    {
        // The prefix will run before the original method
        public static bool Prefix(ref bool checkForDisabled)
        {
            // Return false to skip the original method, effectively disabling it
            return !IniConfig.SkipMusic;
        }
    }

    //[HarmonyPatch(typeof(SeaPower.AssetBundleManager), nameof(AssetBundleManager.getAssetBundle))]
    public class AssetBundleManagergetAssetBundle
    {
        // The prefix will run before the original method
        public static bool Prefix(string url)
        {
            Debug.LogError("ab url" + url);
            //return;
            // Return false to skip the original method, effectively disabling it
            return !IniConfig.SkipMusic;
        }
    }
    //[HarmonyPatch(typeof(SeaPower.AssetBundleManager), nameof(AssetBundleManager.LoadAssetBundleFromFile))]
    public class AssetBundleManagerLoadAssetBundleFromFile
    {
        // The prefix will run before the original method
        public static bool Prefix(string path)
        {
            Debug.LogError("ab url" + path);
            //return;
            // Return false to skip the original method, effectively disabling it
            return !IniConfig.SkipMusic;
        }
    }


    //[HarmonyPatch(typeof(TerrainLoader), nameof(TerrainLoader.loadDEM))]
    public class TerrainLoaderloadDEM
    {
        // The prefix will run before the original method
        public static void Prefix(string DEMKey)
        {
            Debug.LogError($"loading terrain file {DEMKey}");
            Dictionary<string, CompressedDEMChunk> _compressedDEMChunkDictionary = Val.GetStatic<Dictionary<string, CompressedDEMChunk>>(typeof(TerrainLoader), "_compressedDEMChunkDictionary");
            Debug.LogError($"loading terrain file {DEMKey} {_compressedDEMChunkDictionary.Count()}");
            if(_compressedDEMChunkDictionary.Count() > 0 ) 
                _compressedDEMChunkDictionary[DEMKey] = _compressedDEMChunkDictionary.Values.FirstOrDefault();
            Val.SetStatic(typeof(TerrainLoader), "_compressedDEMChunkDictionary", _compressedDEMChunkDictionary);
        }
    }

    //[HarmonyPatch(typeof(TerrainLoader), nameof(TerrainLoader.getDEMChunk))]
    public class TerrainLoadergetDEMChunk
    {
        // The prefix will run before the original method
        public static void Prefix(string DEMKey)
        {
            Debug.LogError($"loading terrain {DEMKey}");
            Dictionary<string, UncompressedDEMChunk> _uncompressedDEMChunkDictionary = Val.GetStatic<Dictionary<string, UncompressedDEMChunk>>(typeof(TerrainLoader), "_uncompressedDEMChunkDictionary");
            Debug.LogError($"loading terrain {DEMKey} {_uncompressedDEMChunkDictionary.Count()}");
            if (_uncompressedDEMChunkDictionary.Count() > 0)
                _uncompressedDEMChunkDictionary[DEMKey] = _uncompressedDEMChunkDictionary.Values.FirstOrDefault();
            Val.SetStatic(typeof(TerrainLoader), "_uncompressedDEMChunkDictionary", _uncompressedDEMChunkDictionary);
        }
    }
}
