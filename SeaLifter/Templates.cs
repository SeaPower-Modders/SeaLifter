using Dummiesman;
using Loader;
using SeaPower;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Application = UnityEngine.Application;
using Color = UnityEngine.Color;
using Graphics = UnityEngine.Graphics;
using Random = System.Random;

namespace Loader
{
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
            foreach (var mesh in obj.gameObject.GetComponentsInChildren<MeshRenderer>())
                resourcePath.Log($"mesh is named {mesh.gameObject.name}");
            return obj;
        }
        public static Dictionary<string, LoadedTexture> textureCache = new Dictionary<string, LoadedTexture>();

        public static GameObject? hotloader;


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
            Material mat = new(Singleton<ResourceLoader>.Instance.getMaterialResource(basefolder + basemat));
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

            foreach (string property in mat.GetTexturePropertyNames())
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
            if (basetex != null)
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

                        return System.Drawing.Color.FromArgb(255, r, g, b); // Create and return the Color
                    }

                    for (int y = 0; y < bmp.Height; y++)
                    {
                        bmp.SetPixel(x, y, System.Drawing.Color.Red);//System.Drawing.Color.Red
                    }
                }
                if (extension == ".png")
                {
                    resourcePath.Log($"saving png");

                    bmp.Save(resourcePath.FullPath, ImageFormat.Png);

                }
            }
        }
    }
}
