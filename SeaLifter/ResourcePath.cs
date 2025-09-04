using Loader;
using SeaPower;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
            if (parent != null)
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


            foreach (var dir in Common.GetLocalDirs())
            {

                string? possiblepath = CheckDirectory(dir);
                if (possiblepath is null)
                    continue;
                _fullpath = possiblepath;

                return;
            }

            foreach (var dir in Common.GetWorkshopDirs())
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



        public T LoadResource<T>(T? example = null) where T : UnityEngine.Object
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
            if (resource)
                this.Log($"Missing File, Loading From Internal Resources");
            else if (!Raw.Contains("_d"))
            {
                this.Log($"Trying to load as {typeof(T).Name} from internal resources failed");
            }
            return Resources.Load<T>(Raw);
        }

        public void Log(object obj)
        {
            if (InternalResource)
            {
                if (IniConfig.LogInternalResources)
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
}
