using Dummiesman;
using Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Loader
{
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
}
