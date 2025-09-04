using Loader;
using SeaPower;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loader
{
    public static class IniConfig
    {
        public static string ConfigFolder = "sealifter";

        public static string DefaultFolder = "user";
        public static string DumpFolder = "texturedump";
        public static string DumpFormat = "png";
        public static bool DumpTextures = true;
        public static bool DumpTexturesOpaque = true;
        public static bool LogInternalResources = false;
        public static bool MeshDebugMode = true;
        public static bool CodeDebugMode = false;
        public static bool DetailedSubPartLogging = false;
        public static bool SkipMusic = true;


        public static IniHandler? Instance;
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
            if (true)
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
}
