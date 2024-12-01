using BepInEx;
using Dummiesman;
using HarmonyLib;

using SeaPower;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using UnityEngine;
using static SeapowerUI.ViewModels.MissionDebriefViewModel;
using Application = UnityEngine.Application;
using Color = UnityEngine.Color;
using Graphics = UnityEngine.Graphics;
using Random = System.Random;
using Texture = UnityEngine.Texture;

namespace Loader
{

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

            foreach (var dir in FileManager.Instance.Directories.ToList().ConvertAll(dir => dir.DirectoryInfo))
            {
                string possiblepath = Path.Combine(dir.FullName, _rawpath);
                //Log($"Testing from: {dir.FullName} {dir.Name}");
                if (!File.Exists(possiblepath))//TODO log collisions
                    continue;
                Log($"Loading from: {dir.Name}");
                _fullpath = possiblepath;

                return;
            }
            _fullpath = Path.Combine(_defaultpath, _rawpath);

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
        public bool InValidPath(string ext)
        {
            if (Exists)
                return true;
            if (!FullPath.Contains(ext))
                return true;
            EnsureCreated();
            this.Log($" Creating {Extension}");
            return false;
        }
        public StreamWriter CreateStreamWriter(string extension)
        {
            if (this.InValidPath(extension))
                return null;
            this.Log($"Creating SW");
            return new StreamWriter(FullPath);
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
                if (CheckFileType(".png"))//Logged
                {
                    this.CreateTextureTemplate();//Logged
                    return this.LoadPng() as T;//Logged
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
            else
                this.Log($"Trying to load as {typeof(T).Name} from internal resources failed");
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

    [BepInPlugin("SL", "SeaLifter", "1.0")]
    public class Plugin : BaseUnityPlugin
    {

        private void Awake()
        {

            IniConfig.Load();

            // Plugin startup logic
            int ver = 4;
            Logger.LogInfo($"Plugin is loaded! Ver 0.0.0.{ver}");
            Debug.LogWarning($"Plugin is loaded! Ver 0.0.0.{ver}");
            var harmony = new Harmony("sp.sl.lib.harmony.product");
            harmony.PatchAll();




        }

    }


    public static class IniConfig 
    {
        public static string ConfigFolder = "sealifter";

        public static string DefaultFolder = "user";
        public static string DumpFolder = "texturedump";
        public static bool DumpTextures = true;
        public static bool DumpTexturesOpaque = true;
        public static bool LogInternalResources = false;
        public static bool MeshDebugMode    = true;
        public static bool CodeDebugMode = false;



        public static IniHandler IniHandler;
        public static void Load()
        {
            ResourcePath setpath = new ResourcePath("sealifter.ini", ConfigFolder) ;
            setpath.EnsureCreated();

            IniHandler = Utils.openIniFile(setpath.FullPath, true, false);

            
            DefaultFolder        = LoadSaveString("General", "DefaultFolder", DefaultFolder);
            LogInternalResources = LoadSaveBool("General", "LogInternalResourceLoading", LogInternalResources);
            DumpFolder           = LoadSaveString("TextureExtractor", "Folder", DumpFolder);
            DumpTextures         = LoadSaveBool("TextureExtractor", "Active" , DumpTextures);
            DumpTexturesOpaque   = LoadSaveBool("TextureExtractor", "Opaque", DumpTexturesOpaque);
            MeshDebugMode        = LoadSaveBool("Meshes", "DebugMode", MeshDebugMode);
            CodeDebugMode        = LoadSaveBool("Scripts", "DebugMode", CodeDebugMode);


            IniHandler.saveFile(true);

        }
        public static bool LoadSaveBool(string sectionName, string key, bool defaultValue)
        {
            bool configval = IniHandler.readValue(sectionName, key, defaultValue);
            IniHandler.writeValue(sectionName, key, configval);
            Common.Log($"Config {sectionName} {key} {configval}");
            return configval;
        }
        public static string LoadSaveString(string sectionName, string key, string defaultValue)
        {
            string configval = IniHandler.readValue(sectionName, key, defaultValue);
            IniHandler.writeValue(sectionName, key, configval);
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
                resourcePath = new ResourcePath( resourcePath + ".png", IniConfig.DumpFolder, parent: resourcePath);


            if (resourcePath.Exists || tex == null)
                return;
            //Common.Log("Texture Already Loaded" + texturename);
            byte[] pngData;
            if (tex is Texture2D texture2D)
            {
                //Common.Log("Copying Texture" + texturename + texture2D.format);
                resourcePath.Log($"creating texture from {tex.name}");

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
                pngData = newTexture.EncodeToPNG();

                resourcePath.EnsureDirectoryExists();
                // Save the PNG byte array to the file
                File.WriteAllBytes(resourcePath.FullPath, pngData);
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
            __result = resourcePath.LoadResource<GameObject>(__result);//Logged

        }
    }

    [HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.getMaterialResource))]
    public class ResourceLoadergetMaterialResource : MonoBehaviour
    {
        public static void Postfix(ref Material __result, string resourceName)
        {
            ResourcePath resourcePath = new ResourcePath(resourceName, internalresouce: __result != null);
            resourcePath.DumpMaterial(__result);//Logged
            __result = resourcePath.LoadResource<Material>(__result);//Logged
        }

    }
    [HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.getTextureResource))]
    public class ResourceLoadergetTextureResource : MonoBehaviour
    {
        public static void Postfix(ref Texture __result, string resourceName)
        {
            ResourcePath resourcePath = new ResourcePath(resourceName, internalresouce: __result != null);
            resourcePath.SaveProtextedTexture(__result);//Logged
            __result = resourcePath.LoadResource<Texture>(__result);//Logged;
        }

    }

    public static class Templates
    {
        public static GameObject LoadObj(this ResourcePath resourcePath)
        {
            resourcePath.Log($"Loading as obj");

            GameObject obj = new OBJLoader().Load(resourcePath.FullPath);
            if (IniConfig.MeshDebugMode == false)
                obj.transform.position = Vector3.up * -10000;
            Common.LogGameObject(obj, resourcePath.FullPath + ".log");

            resourcePath.Log($"{obj.name} go name");
            resourcePath.Log($"{obj.gameObject.GetComponentInChildren<MeshRenderer>().gameObject.name} rend name");
            return obj;
        }
        public static Texture2D LoadPng(this ResourcePath resourcePath)
        {
            resourcePath.Log($"loading as png");
            Texture2D tex = new Texture2D(2, 2);

            if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(resourcePath.FullPath)))
            {
                Common.Error($"{resourcePath} bad/Missing png File");
                return null;
            }
            

            tex.Apply();
            return tex;
        }


        public static Material LoadMatIni(this ResourcePath resourcePath)
        {
            resourcePath.Log($"Loading as ini");

            IniHandler iniHandler = Utils.openIniFile(resourcePath.FullPath, true, false);
            //Common.Log($"{path} Has BaseMaterial");
            string basefolder = iniHandler.readValue("BaseMaterial", "ResourcesFolder", "weapons/usn_mk-82/");
            string basemat = iniHandler.readValue("BaseMaterial", "ResourcesMaterial", "usn_mk-82_mat");
            Material mat = Singleton<ResourceLoader>.Instance.getMaterialResource(basefolder + basemat);
            if (mat != null)

                if (!iniHandler.doesSectionExist("Textures"))
                {
                    Common.Error("Material has no Textures");
                    return null;
                }


            string folder = iniHandler.readValue("Textures", "ResourcesFolder", "textures/");
            resourcePath.Log($"Loading mat textures from: {folder}");

            void SetTextureFromIni(IniHandler handler, Material mat, string settingname, string propertyname)
            {
                string texturepath = handler.readValue("Textures", settingname, "");

                if (texturepath == "")
                {
                    resourcePath.Log($"Propery {propertyname} using default texture");
                    return;
                }
                else
                    resourcePath.Log($"Loading {settingname} from: {texturepath}");

                texturepath = folder + texturepath;
                Texture tex = Singleton<ResourceLoader>.Instance.getTextureResource(texturepath);
                if (tex != null)
                    mat.SetTexture(propertyname, tex);
                else
                    resourcePath.Error($"null texture at {texturepath}");

            }


            SetTextureFromIni(iniHandler, mat, "DetailAlbedoMap", "_MainTex");
            SetTextureFromIni(iniHandler, mat, "DetailMask", "_SpecTex");
            SetTextureFromIni(iniHandler, mat, "DetailNormalMap", "_BumpMap");

            string DetailNormalMap = ""; // iniHandler.readValue("Textures", "DetailNormalMap", "");
            if (DetailNormalMap.Contains(".png"))
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
            writer.WriteLine("DetailAlbedoMap=DetailAlbedoMap.png");
            writer.WriteLine("DetailMask=DetailMask.png");
            writer.WriteLine("DetailNormalMap=DetailNormalMap.png");

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

        public static void CreateTextureTemplate(this ResourcePath resourcePath)
        {
            if (resourcePath.InValidPath(".png"))//Logged
                return;
            Texture basetex = new ResourcePath(resourcePath.Raw.Replace(".png", "")).LoadResource<Texture>();//LOGGED
            resourcePath.SaveProtextedTexture(basetex, dump: false);//Logged


            resourcePath.Log($"Creating red template texture");

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
                        bmp.SetPixel(x, y, GetRandomColor());//System.Drawing.Color.Red
                    }
                }
                bmp.Save(resourcePath.FullPath, ImageFormat.Png);
            }
        }
    }
}

