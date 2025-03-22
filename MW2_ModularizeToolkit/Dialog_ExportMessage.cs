using LudeonTK;
using RimWorld;
using ModularWeapons2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using static RimWorld.ColonistBar;
using static RimWorld.PsychicRitualRoleDef;
using static UnityEngine.Random;


namespace MW2_ModularizeToolkit {
    public class Dialog_ExportMessage : Window {
        public Dialog_ExportMessage() {
        }
        public override void PostOpen() {
            base.PostOpen();
            doCloseButton = true;
        }

        public override void DoWindowContents(Rect inRect) {
            Widgets.Label(inRect, "MW2MTK_PostExportMessage".Translate());
        }
    }
}
