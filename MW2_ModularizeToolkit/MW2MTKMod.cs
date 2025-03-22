using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using Verse;

namespace MW2_ModularizeToolkit {
    public class MW2MTKMod : Mod {
        static ModContentPack contentPack = null;
        public static ModContentPack ContentPack => contentPack;

        static Lazy<string> path_templateText = new Lazy<string>(() => Path.Combine(ContentPack.RootDir, "xml_template.txt"));
        public static string Path_templateText => path_templateText.Value;

        static Lazy<string> path_export = new Lazy<string>(() => Path.Combine(ContentPack.RootDir, "MTK_Output", "Patches"));
        public static string Path_export(ThingDef weaponDef) => Path.Combine(path_export.Value, weaponDef.defName + ".xml");

        public MW2MTKMod(ModContentPack content) : base(content) {
            contentPack = content;
        }
    }
}
