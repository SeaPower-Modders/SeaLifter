using SeaPower;
using System.Reflection;
using UnityEngine;


namespace Loader
{

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
                SaveProtextedTexture(new ResourcePath(resourcePath + prop, parent: resourcePath), mat.GetTexture(prop));//Logged
            }

        }



        public static void SaveProtextedTexture(this ResourcePath resourcePath, Texture tex, bool dump = true)
        {
            if (tex == null)
                return;
            if (dump && !IniConfig.DumpTextures)
                return;
            if (dump)
                resourcePath = new ResourcePath(resourcePath + "." + IniConfig.DumpFormat, IniConfig.DumpFolder, parent: resourcePath);


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
                if (IniConfig.DumpTexturesOpaque)
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
                        return;
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

        public enum SaveTextureFileFormat
        {
            exr,
            jpg,
            png,
            tga
        }
        public static string Cat = "≽^•⩊•^≼";

        public static BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public static BindingFlags ValFlags = Flags | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.GetProperty;
        public static BindingFlags FunFlags = Flags;
        public static T Get<T>(object instance, string fieldName, Type type = null)
        {
            if (instance == null)
                return default;
            if (type == null)
                type = instance.GetType();
            FieldInfo field = type.GetField(fieldName, ValFlags);
            if (field != null)
                return (T)field.GetValue(instance);
            else if (type.BaseType != null)
                return Get<T>(instance, fieldName, type.BaseType);
            return default;
        }
        public static T Set<T>(object instance, string fieldName, T value) => (T)Set(instance, fieldName, value, null);
        public static object Set(object instance, string fieldName, object value = null, Type type = null)
        {
            if (type == null)
                type = instance.GetType();
            FieldInfo field = type.GetField(fieldName, ValFlags);
            if (field != null)
                field.SetValue(instance, value);
            else if (type.BaseType != null)
                Set(instance, fieldName, value, type.BaseType);
            return value;
        }

        public static T GetStatic<T>(Type type, string fieldName)
        {
            if (type == null)
                return default;

            FieldInfo field = type.GetField(fieldName, ValFlags | BindingFlags.Static);
            if (field != null)
                return (T)field.GetValue(null); // No instance, so pass null for static field.
            return default;
        }

        public static void SetStatic<T>(Type type, string fieldName, T value)
        {
            if (type == null)
                return;

            FieldInfo field = type.GetField(fieldName, ValFlags | BindingFlags.Static);
            if (field != null)
                field.SetValue(null, value); // No instance, so pass null for static field.
        }

        public static void RunFunc(object instance, string functionname, object[] paramters = null, Type type = null)
        {
            MethodInfo protectedMethod = instance.GetType().GetMethod(functionname, FunFlags);

            protectedMethod.Invoke(instance, paramters);

        }

        public static IEnumerable<SearchDirectory> GetEnabledDirs()
        {
            return FileManager.Instance.Directories.Where(dir => dir.IsEnabled);
        }

        public static IEnumerable<SearchDirectory> GetLocalDirs()
        {
            return GetEnabledDirs().Where(dir => !dir.IsSteam);
        }

        public static IEnumerable<SearchDirectory> GetWorkshopDirs()
        {
            return GetEnabledDirs().Where(dir => dir.IsSteam);
        }

        public static IEnumerable<DirectoryInfo> GetAllDirs()
        {
            return GetEnabledDirs().Select(dir => dir.DirectoryInfo);
        }

        public static void Resize<T>(this List<T> list, int size, T element = default(T))
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity)   // Optimization
                    list.Capacity = size;

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }

        public static void Desize<T>(this List<T> list, int size)
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }

        }

        public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource> source)
        {
            return source.Where(item => item != null);
        }

    }

}



