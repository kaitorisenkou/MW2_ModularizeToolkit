using LudeonTK;
using ModularWeapons2;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MW2_ModularizeToolkit {
    public class MW2_ModularizeToolkit {

        public const string AdapterTexturesPath = "ModularParts/Adapters";
        static IEnumerable<Texture2D> adapterTextures = null;
        public static IEnumerable<Texture2D> AdapterTextures {
            get {
                if (adapterTextures == null)
                    adapterTextures = GetAdapterTextures();
                return adapterTextures;
            }
        }
        static IEnumerable<Texture2D> GetAdapterTextures() {
            List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
            foreach(var modList in runningModsListForReading) {
                foreach(var cont in modList.GetContentHolder<Texture2D>().contentList.Where(t => t.Key.Contains(AdapterTexturesPath))) {
                    yield return cont.Value;
                }

            }
        }

        static Lazy<SoundDef> completeSound= new Lazy<SoundDef>(() => DefDatabase<SoundDef>.GetNamed("Message_TaskCompletion"));
        public static SoundDef CompleteSound => completeSound.Value;

        [DebugAction("ModularWeapons2", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ModularizeToolkitWindow() {
            string errorMessage = "MW2MTK_NoCompEquippable";
            foreach (Thing i in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell())) {
                var comp = i.TryGetComp<CompEquippable>();
                if (comp == null) {
                    continue;
                }
                /*if (comp.GetType() != typeof(CompEquippable)) {
                    errorMessage = "MW2MTK_InvalidType";
                    continue;
                }
                if (i.HasComp<CompModularWeapon>()) {
                    errorMessage = "MW2MTK_AlreadyModularized";
                    continue;
                }*/
                Find.WindowStack.Add(new Dialog_ModularizeToolkit(comp));
                return;
            }
            Messages.Message(errorMessage.Translate(), MessageTypeDefOf.RejectInput, false);
        }

    }
}
