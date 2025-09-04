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
using Application = UnityEngine.Application;
using Color = UnityEngine.Color;
using Graphics = UnityEngine.Graphics;
using Random = System.Random;
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



    public class ResourcePath
    {
        private string _defaultpath = "";
        private string _fullpath = "";
        private string _prefab = "";
        private readonly string _rawpath = "";
        private bool _internalresouce = false;

        public bool InternalResource => _internalresouce;
        public bool Exists => File.Exists(FullPath);
        public string FullPath => _fullpath;
        public string PrefabName => _prefab;
        public override string ToString() => _rawpath;
        public string Raw => _rawpath;
        public string RawPath => Path.GetDirectoryName(Raw);
        public string RawName => Path.GetFileNameWithoutExtension(FullPath);

        public string Extension => Path.GetExtension(FullPath);
        private string FullDirectory => Path.GetDirectoryName(FullPath);
        
        public string FileName => Path.GetFileNameWithoutExtension(FullPath);



        public ResourcePath(string rawpath, string folder = "user", bool internalresouce = false, ResourcePath parent = null)
        {
            //Error($"{rawpath} {folder} {internalresouce}");
            if(parent != null)
                internalresouce = parent.InternalResource;

            _internalresouce = internalresouce;
            _defaultpath = Application.streamingAssetsPath + $"/{folder}/";
            rawpath.Replace(".obj_d", "_d.obj");
            _rawpath = rawpath;
            if (rawpath.Contains('|'))
            {
                Log("Split Path");
                _prefab = rawpath.Split('|')[1];
                _rawpath = rawpath.Split('|')[0];
            }
            else
            {
                _prefab = Path.GetFileNameWithoutExtension(rawpath);
            }


            foreach (var dir in TempUtils.GetLocalDirs())
            {

                string? possiblepath = CheckDirectory(dir);
                if (possiblepath is null)
                    continue;
                _fullpath = possiblepath;

                return;
            }

            foreach (var dir in TempUtils.GetWorkshopDirs())
            {
                string? possiblepath = CheckDirectory(dir);
                if (possiblepath is null)
                    continue;
                _fullpath = possiblepath;

                return;
            }
            _fullpath = Path.Combine(_defaultpath, _rawpath);

        }

        public string? CheckDirectory(SearchDirectory directory)
        {
            DirectoryInfo info = directory.DirectoryInfo;
            string possiblepath = Path.Combine(info.FullName, _rawpath);
            //Log($"Testing from: {dir.FullName} {dir.Name}");
            if (!File.Exists(possiblepath))//TODO log collisions
                return null;
            if (!directory.IsEnabled)
                Error("Loading from disabled directory");
            Log($"Loading from {(directory.IsSteam ? "Workshop" : "Local")}: {info.Name}");
            return possiblepath;
        }
        

        //public static implicit operator string(ResourcePath customString) => customString.ToString();
        public void EnsureCreated()
        {
            if (Exists)
                return;

            EnsureDirectoryExists();
            this.Log($"Creating File");
            File.Create(FullPath).Close();
        }

        public void EnsureDirectoryExists()
        {
            if (Directory.Exists(FullDirectory))
                return;
            this.Log($"Creating Directory");
            Directory.CreateDirectory(FullDirectory);
        }
        public bool ValidNewPath(string ext = "")
        {
            if (Exists)
                return false;
            if (!FullPath.Contains(ext))
                return false;
            EnsureCreated();
            this.Log($"Safe to write to {Extension}");
            return true;
        }
        public StreamWriter CreateStreamWriter(string extension)
        {
            if (!ValidNewPath(extension))
                return null;
            this.Log($"Creating SW");
            return new StreamWriter(FullPath);
        }
        public bool CheckFileType(List<string> extension, bool log = true)
        {
            foreach (string ext in extension)
                if (CheckFileType(ext, log))
                    return true;
            return false;
        }

        public bool CheckFileType(string extension, bool log = true)
        {
            if (Raw.Contains(extension))
            {
                if (log)
                    this.Log("path contains " + extension);

                return true;
            }
            return false;
        }



        public T LoadResource<T>(T example = null) where T : UnityEngine.Object
        {
            if (example != null)
            {
                return example;
            }
            

            if (typeof(T) == typeof(Texture2D) || typeof(T) == typeof(Texture))
            {
                if (CheckFileType([".png", ".jpg", ".exr"]))//Logged
                {
       
                    this.CreateTextureTemplate(this.Extension);//Logged
                    return this.LoadImage() as T;//Logged
                }
     
 
            }
            else if (typeof(T) == typeof(Material) && CheckFileType(".ini"))//Logged
            {
                this.CreateMatTemplate();//Logged
                return this.LoadMatIni() as T;//Logged
            }
            else if (typeof(T) == typeof(GameObject) && this.CheckFileType(".obj"))//Logged
            {
                this.CreateMeshTemplate();//Logged
                return this.LoadObj() as T;//Logged
            }

            if (Exists)
            {
                this.Log($"Loading {PrefabName} from asset bundle");
                var assetBundle = AssetBundle.LoadFromFile(FullPath);
                return assetBundle?.LoadAsset<T>(PrefabName);
            }

            T resource = Resources.Load<T>(Raw);
            if(resource)
                this.Log($"Missing File, Loading From Internal Resources");
            else if(!Raw.Contains("_d"))
            {
                this.Log($"Trying to load as {typeof(T).Name} from internal resources failed");
            }
            return Resources.Load<T>(Raw);
        }

        public void Log(object obj)
        {
            if (InternalResource)
            {
                if(IniConfig.LogInternalResources)
                    Debug.Log($"{this}: {obj}");
                return;
            }
                
            Debug.LogWarning($"{this}: {obj}");
        }
        public void Error(object obj)
        {
            Debug.LogError($"{this}: {obj}");
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





    public static class IniConfig 
    {
        public static string ConfigFolder = "sealifter";

        public static string DefaultFolder = "user";
        public static string DumpFolder = "texturedump";
        public static string DumpFormat = "png";
        public static bool DumpTextures = true;
        public static bool DumpTexturesOpaque = true;
        public static bool LogInternalResources = false;
        public static bool MeshDebugMode    = true;
        public static bool CodeDebugMode = false;
        public static bool DetailedSubPartLogging = false;
        public static bool SkipMusic = true;


        public static IniHandler? Instance ;
        public static void Load()
        {
            /*
            ResourcePath setpath = new ResourcePath("sealifter.ini", ConfigFolder) ;
            setpath.EnsureCreated();

            Instance = ;


            MeshDebugMode = false;
            DefaultFolder        = LoadSaveString("General", "DefaultFolder", DefaultFolder);
            LogInternalResources = LoadSaveBool("General", "LogInternalResourceLoading", LogInternalResources);
            SkipMusic            = LoadSaveBool("General", "SkipMusic", SkipMusic);
            DetailedSubPartLogging = LoadSaveBool("General", "DetailedSubPartLogging", DetailedSubPartLogging);
            DumpFolder           = LoadSaveString("TextureExtractor", "Folder", DumpFolder);
            DumpTextures         = LoadSaveBool("TextureExtractor", "Active" , DumpTextures);
            DumpTexturesOpaque   = LoadSaveBool("TextureExtractor", "Opaque", DumpTexturesOpaque);
            MeshDebugMode        = LoadSaveBool("Meshes", "DebugMode", MeshDebugMode);
            CodeDebugMode        = LoadSaveBool("Scripts", "DebugMode", CodeDebugMode);
            
            //stop preloadObjects
            //stop LoadMusicData

            Instance.saveFile(true);
            */
            if(true)
                ConfigFolder = "sealifter";
                DefaultFolder = "user";
                DumpFolder = "texturedump";
                DumpTextures = false;
                DumpTexturesOpaque = false;
                LogInternalResources = false;
                MeshDebugMode = false;
                CodeDebugMode = false;
                DetailedSubPartLogging = false;
                SkipMusic = false;



        //LogInternalResources = true;
        //DetailedSubPartLogging = true;
    }
    public static bool LoadSaveBool(string sectionName, string key, bool defaultValue)
        {
            bool configval = Instance.readValue(sectionName, key, defaultValue);
            Instance.writeValue(sectionName, key, configval);
            Common.Log($"Config {sectionName} {key} {configval}");
            return configval;
        }
        public static string LoadSaveString(string sectionName, string key, string defaultValue)
        {
            string configval = Instance.readValue(sectionName, key, defaultValue);
            Instance.writeValue(sectionName, key, configval);
            Common.Log($"Config {sectionName} {key} {configval}");
            return configval;
        }


    }

    public static class Common
    {

        public static void LogGameObject(GameObject go, string filepath)
        {
            File.Create(filepath).Close();
            using (StreamWriter writer = new StreamWriter(filepath))
            {
                Common.PrintTransform(writer, go.transform);
            }
        }

        public static void PrintTransform(StreamWriter writer, Transform transform, int depth = 0)
        {
            string indent = new string(' ', depth * 4);
            writer.WriteLine($"{indent}{transform.name}");

            for (int i = 0; i < transform.childCount; i++)
            {
                PrintTransform(writer, transform.GetChild(i), depth + 1);
            }
        }

        public static void LogTransform(Transform transform, int depth = 0)
        {
            string indent = new string(' ', depth * 4);
            Common.Log($"{indent}{transform.name}");

            for (int i = 0; i < transform.childCount; i++)
            {
                LogTransform(transform.GetChild(i), depth + 1);
            }
        }


        public static void Log(object obj)
        {
            Debug.LogWarning(obj);
        }
        public static void Error(object obj)
        {
            Debug.LogError(obj);
        }

        public static void DumpMaterial(this ResourcePath resourcePath, Material mat)
        {
            if (mat == null || resourcePath.Raw.Contains("terrain") || resourcePath.Raw.Contains("damage_decals") || resourcePath.Raw.Contains("environment"))
                return;

            foreach (string prop in mat.GetPropertyNames(MaterialPropertyType.Texture))
            {
                //Common.Log("prop name: " + prop);
                SaveProtextedTexture(new ResourcePath(resourcePath + prop, parent:resourcePath), mat.GetTexture(prop));//Logged
            }

        }



        public static void SaveProtextedTexture(this ResourcePath resourcePath, Texture tex, bool dump = true)
        {
            if(tex == null) 
                return;
            if (dump && !IniConfig.DumpTextures)
                return;
            if(dump)
                resourcePath = new ResourcePath( resourcePath + "." + IniConfig.DumpFormat, IniConfig.DumpFolder, parent: resourcePath);


            if (resourcePath.Exists || tex == null)
                return;
            //Common.Log("Texture Already Loaded" + texturename);
            byte[] imageBytes;
            if (tex is Texture2D texture2D)
            {
                //Common.Log("Copying Texture" + texturename + texture2D.format);
                resourcePath.Log($"creating texture from {tex.name} as {IniConfig.DumpFormat}");

                RenderTexture renderTexture = new RenderTexture(texture2D.width, texture2D.height, 24);
                Graphics.Blit(texture2D, renderTexture);
                Texture2D newTexture = new Texture2D(texture2D.width, texture2D.height, TextureFormat.RGBA32, false);
                RenderTexture.active = renderTexture;
                newTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                if(IniConfig.DumpTexturesOpaque)
                {
                    for (int y = 0; y < newTexture.height; y++)
                    {
                        for (int x = 0; x < newTexture.width; x++)
                        {
                            Color pixelColor = newTexture.GetPixel(x, y);
                            pixelColor.a = 1f;
                            newTexture.SetPixel(x, y, pixelColor);
                        }
                    }
                }

                newTexture.Apply();
                RenderTexture.active = null;
                switch (IniConfig.DumpFormat)
                {
                    case "png":
                        // Handle PNG format
                        imageBytes = newTexture.EncodeToPNG();
                        break;
                    case "jpg":
                    case "jpeg":
                        imageBytes = newTexture.EncodeToJPG();
                        break;
                    case "exr":
                        imageBytes = newTexture.EncodeToEXR();
                        break;
                    case "tga":
                        imageBytes = newTexture.EncodeToTGA();
                        break;

                    default:
                        // Handle unsupported formats
                        
                        resourcePath.Log("Unsupported image format");
                        return ;
                }

                resourcePath.EnsureDirectoryExists();
                // Save the byte array to the file
                File.WriteAllBytes(resourcePath.FullPath, imageBytes);
            }


        }

        public static Texture2D NormalMap(this Texture2D source)
        {
            Texture2D normalTexture = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true);
            Color theColour = new Color();
            for (int x = 0; x < source.width; x++)
            {
                for (int y = 0; y < source.height; y++)
                {
                    theColour.r = 0;
                    theColour.g = source.GetPixel(x, y).g;
                    theColour.b = 0;
                    theColour.a = source.GetPixel(x, y).r;
                    normalTexture.SetPixel(x, y, theColour);
                }
            }
            normalTexture.Apply();
            return normalTexture;
        }


        public static Texture2D ConvertToTexture2D(this Texture texture)
        {
            if (texture is Texture2D)
            {
                return texture as Texture2D;
            }
            return null;
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

    public class LoadedTexture
    {
        public ResourcePath ResourcePath;
        public string hash = "";
        public Texture2D texture = new Texture2D(2, 2);
        private DateTime lastCheckedTime = DateTime.MinValue;

        byte[] FileBytes;

        public LoadedTexture(ResourcePath resourcePath)
        {
            ResourcePath = resourcePath;
        }

        public Texture2D LoadImage(bool deep = true)
        {

            if (!File.Exists(ResourcePath.FullPath))
            {
                ResourcePath.Log($"Failed to find image");
                return null;
            }
            DateTime currentWriteTime = File.GetLastWriteTime(ResourcePath.FullPath);

            if (currentWriteTime == lastCheckedTime && deep == false)
            {
                return texture;


            }

            FileBytes = File.ReadAllBytes(ResourcePath.FullPath);

            string fileHash = GetFileHash();
            if (hash == fileHash)
            {
                ResourcePath.Log($"Texture found in cache with hash {fileHash}");
                return texture; // Return the cached texture
            }
            hash = fileHash;
            lastCheckedTime = currentWriteTime;

            if (ResourcePath.Extension == ".tga")
            {
                ResourcePath.Log($"loading tga image");

                texture = TGALoader.Load(ResourcePath.FullPath);

            }
            else if (!texture.LoadImage(FileBytes))
            {
                ResourcePath.Log($"bad/Missing image File");
                return null;
            }
            texture.Apply();
            ResourcePath.Log($"TextureFormat: {texture.format} Texture Size: {texture.texelSize} Filter Mode: {texture.filterMode}");
            return texture;
        }

        private string GetFileHash()
        {
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(FileBytes);
                return Convert.ToBase64String(hashBytes).ToLowerInvariant();
            }
        }
    }

    public class HotLoader : MonoBehaviour
    {
        private static int currentIndex = 0;
        Queue<string> strings = new Queue<string>();

        public void FixedUpdate()
        {
            if (strings.Count == 0)
            {
                strings = new Queue<string>(Templates.textureCache.Keys);

            }
            // Only load one image per FixedUpdate
            if (strings.Count > 0)
            {
                Templates.textureCache[strings.Dequeue()].LoadImage(false);
            }
            currentIndex++;

            if (currentIndex >= Templates.textureCache.Count)
            {
                currentIndex = 0;
            }
        }


    }

    public static class Templates
    {
        public static GameObject LoadObj(this ResourcePath resourcePath)
        {
            resourcePath.Log($"Loading as obj");

            GameObject obj = new OBJLoader().Load(resourcePath.FullPath);
            if (IniConfig.MeshDebugMode == false)
            {
                obj.transform.position = Vector3.up * -10000;
                obj.transform.localScale = Vector3.one / 10000;
            }
            Common.LogGameObject(obj, resourcePath.FullPath + ".log");

            resourcePath.Log($"root gameobject {obj.name}");
            //TODO hide this behind a flag
            foreach(var mesh in obj.gameObject.GetComponentsInChildren<MeshRenderer>())
                resourcePath.Log($"mesh is named {mesh.gameObject.name}");
            return obj;
        }
        public static Dictionary<string, LoadedTexture> textureCache = new Dictionary<string, LoadedTexture>();

        public static GameObject hotloader;

            
        public static Texture2D LoadImage(this ResourcePath resourcePath)
        {
            if (hotloader == null)
            {
                hotloader = new GameObject("hotloader");
                hotloader.AddComponent<HotLoader>();

            }

            if (!textureCache.TryGetValue(resourcePath.FullPath, out LoadedTexture loadedTexture))
            {
                textureCache[resourcePath.FullPath] = new(resourcePath);
                return textureCache[resourcePath.FullPath].LoadImage();
            }


            return loadedTexture.LoadImage();
        }


        public static Material LoadMatIni(this ResourcePath resourcePath)
        {
            resourcePath.Log($"Loading as ini");

            IniHandler iniHandler = SeaPower.Utils.openIniFile(resourcePath.FullPath, true, false);
            //Common.Log($"{path} Has BaseMaterial");
            string basefolder = iniHandler.readValue("BaseMaterial", "ResourcesFolder", "weapons/usn_mk-82/");
            string basemat = iniHandler.readValue("BaseMaterial", "ResourcesMaterial", "usn_mk-82_mat");
            string shader = iniHandler.readValue("BaseMaterial", "Shader", "");
            Material mat = new(Singleton<ResourceLoader>.Instance.getMaterialResource(basefolder + basemat)) ;
            if (mat == null)
            {
                mat = new Material(Shader.Find(shader));
                resourcePath.Log($"new material using shader: {shader}");
            }
            else
            {
                resourcePath.Log($"copying data from material {mat.name}");
                if (shader != "")
                {
                    resourcePath.Log($"replacing shader with: {shader}");
                    mat.shader = Shader.Find(shader);

                }

            }


            string folder = iniHandler.readValue("Textures", "ResourcesFolder", "textures/");
            resourcePath.Log($"loading textures from: {folder}");

            void SetTextureFromIni(IniHandler handler, Material mat, string propertyname, string settingname = "")
            {
                if (settingname == "")
                    settingname = propertyname;
                if (!handler.doesKeyExist("Textures", settingname)) return;
                string texturepath = handler.readValue("Textures", settingname, "");
                
                resourcePath.Log($"loading {settingname} from: {texturepath}");
                
                texturepath = folder + texturepath;
                Texture tex = Singleton<ResourceLoader>.Instance.getTextureResource(texturepath);
                if (tex != null)
                    mat.SetTexture(propertyname, tex);
                else
                    resourcePath.Error($"null texture at {texturepath}");

            }
            void SetIntFromIni(IniHandler handler, Material mat, string propertyname)
            {
                if (!handler.doesKeyExist("Integers", propertyname)) return;
                int value = handler.readValue("Integers", propertyname, int.MaxValue);
                resourcePath.Log($"loading {propertyname} value: {value}");
                mat.SetInteger(propertyname, value);

            }
            void SetFloatFromIni(IniHandler handler, Material mat, string propertyname)
            {
                if (!handler.doesKeyExist("Floats", propertyname)) return;
                float value = handler.readValue("Floats", propertyname, float.NaN);
                resourcePath.Log($"loading {propertyname} value: {value}");
                mat.SetFloat(propertyname, value);

            }
            void SetColorFromIni(IniHandler handler, Material mat, string propertyname)
            {
                if (!handler.doesKeyExist("Colors", propertyname)) return;
                Vector4 value = handler.readValue("Colors", propertyname, Vector4.positiveInfinity);
                resourcePath.Log($"loading {propertyname} value: {value}");
                mat.SetColor(propertyname, value);


            }
            foreach (string property in new[] { "_MainTex", "_SpecTex", "_BumpMap" })
            {
                SetTextureFromIni(iniHandler, mat, property);
            }
            SetTextureFromIni(iniHandler, mat, "_MainTex", "DetailAlbedoMap");
            SetTextureFromIni(iniHandler, mat, "_SpecTex", "DetailMask");
            SetTextureFromIni(iniHandler, mat, "_BumpMap", "DetailNormalMap");

            foreach (string property in  mat.GetTexturePropertyNames())
            {

                SetTextureFromIni(iniHandler, mat, property);
            }
            foreach (string property in mat.GetPropertyNames(MaterialPropertyType.Float))
            {
                SetFloatFromIni(iniHandler, mat, property);
            }
            foreach (string property in mat.GetPropertyNames(MaterialPropertyType.Int))
            {
                SetIntFromIni(iniHandler, mat, property);
            }
            foreach (string property in mat.GetPropertyNames(MaterialPropertyType.Vector))
            {
                SetColorFromIni(iniHandler, mat, property);
                //resourcePath.Log($"Possible vector Property {property} value: {mat.GetVector(property)}");
            }
            foreach (string property in new[] { "_Color", "_EmissionColor", "_GlowColor", "_SpecColor" })
            {
                SetColorFromIni(iniHandler, mat, property);
                
            }


            string DetailNormalMap = ""; // iniHandler.readValue("Textures", "DetailNormalMap", "");
            if (DetailNormalMap != "")//has filepath
            {
                Texture NormalMap = Singleton<ResourceLoader>.Instance.getTextureResource(folder + DetailNormalMap);
                NormalMap = NormalMap.ConvertToTexture2D().NormalMap();
                if (NormalMap != null)
                    mat.SetTexture("_BumpMap", NormalMap);
            }

            return mat;

        }

        public static void CreateMatTemplate(this ResourcePath resourcePath)
        {

            if (resourcePath.CreateStreamWriter(".ini") is not StreamWriter writer)//Logged
                return;

            // Write to the file
            //writer.WriteLine("[BaseMaterial]");
            //writer.WriteLine("ResourcesFolder=weapons/usn_mk-82/");
            //writer.WriteLine("ResourcesMaterial=usn_mk-82_mat");
            string texturefolder = $"{resourcePath.RawPath}/{resourcePath.RawName}textures/";
            resourcePath.Log($"Creating in folder {texturefolder}");
            writer.WriteLine("[Textures]");
            writer.WriteLine($"ResourcesFolder={texturefolder}\n");
            writer.WriteLine("_MainTex=DetailAlbedoMap.png");
            writer.WriteLine("_SpecTex=DetailMask.png");
            writer.WriteLine("_BumpMap=DetailNormalMap.png");
            writer.Close();
        }

        public static void CreateMeshTemplate(this ResourcePath resourcePath)
        {

            if (resourcePath.CreateStreamWriter(".obj") is not StreamWriter writer)//Logged
                return;

            writer.WriteLine("mtllib untitled.mtl");
            writer.WriteLine($"o {resourcePath.FileName}");
            writer.WriteLine("v 1.000000 1.000000 -1.000000");
            writer.WriteLine("v 1.000000 -1.000000 -1.000000");
            writer.WriteLine("v 1.000000 1.000000 1.000000");
            writer.WriteLine("v 1.000000 -1.000000 1.000000");
            writer.WriteLine("v -1.000000 1.000000 -1.000000");
            writer.WriteLine("v -1.000000 -1.000000 -1.000000");
            writer.WriteLine("v -1.000000 1.000000 1.000000");
            writer.WriteLine("v -1.000000 -1.000000 1.000000");
            writer.WriteLine("vt 0.625000 0.500000");
            writer.WriteLine("vt 0.875000 0.500000");
            writer.WriteLine("vt 0.875000 0.750000");
            writer.WriteLine("vt 0.625000 0.750000");
            writer.WriteLine("vt 0.375000 0.750000");
            writer.WriteLine("vt 0.625000 1.000000");
            writer.WriteLine("vt 0.375000 1.000000");
            writer.WriteLine("vt 0.375000 0.000000");
            writer.WriteLine("vt 0.625000 0.000000");
            writer.WriteLine("vt 0.625000 0.250000");
            writer.WriteLine("vt 0.375000 0.250000");
            writer.WriteLine("vt 0.125000 0.500000");
            writer.WriteLine("vt 0.375000 0.500000");
            writer.WriteLine("vt 0.125000 0.750000");
            writer.WriteLine("s 0");
            writer.WriteLine("usemtl Material");
            writer.WriteLine("f 1/1 5/2 7/3 3/4");
            writer.WriteLine("f 4/5 3/4 7/6 8/7");
            writer.WriteLine("f 8/8 7/9 5/10 6/11");
            writer.WriteLine("f 6/12 2/13 4/5 8/14");
            writer.WriteLine("f 2/13 1/1 3/4 4/5");
            writer.WriteLine("f 6/11 5/10 1/1 2/13");
            writer.Close();
        }

        public static void CreateTextureTemplate(this ResourcePath resourcePath, string extension)
        {
            if (!resourcePath.ValidNewPath(extension))//Logged
                return;
            Texture basetex = new ResourcePath(resourcePath.Raw.Replace(extension, "")).LoadResource<Texture>();//LOGGED
            if(basetex != null)
            {
                resourcePath.Log($"creating template using existing textures");
                resourcePath.SaveProtextedTexture(basetex, dump: false);//Logged

            }


            resourcePath.Log($"creating red template texture");

            using (Bitmap bmp = new Bitmap(32, 32))
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    System.Drawing.Color GetRandomColor()
                    {
                        Random random = new Random();
                        int r = random.Next(0, 256); // Random value for Red (0-255)
                        int g = random.Next(0, 256); // Random value for Green (0-255)
                        int b = random.Next(0, 256); // Random value for Blue (0-255)

                        return  System.Drawing.Color.FromArgb(255, r, g, b); // Create and return the Color
                    }

                    for (int y = 0; y < bmp.Height; y++)
                    {
                        bmp.SetPixel(x, y, System.Drawing.Color.Red);//System.Drawing.Color.Red
                    }
                }
                if(extension == ".png")
                {
                    resourcePath.Log($"saving png");

                    bmp.Save(resourcePath.FullPath, ImageFormat.Png);

                }
            }
        }
    }
}

