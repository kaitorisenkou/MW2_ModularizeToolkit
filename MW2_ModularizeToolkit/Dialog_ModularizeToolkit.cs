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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static ModularWeapons2.MWCameraRenderer;


namespace MW2_ModularizeToolkit {
    public class Dialog_ModularizeToolkit : Window {
        public override Vector2 InitialSize {
            get {
                return new Vector2(Mathf.Min(960f, UI.screenWidth), Mathf.Min(640f, UI.screenHeight));
            }
        }
        public Dialog_ModularizeToolkit(CompEquippable weapon) {
            targetWeaponDef = weapon.parent.def;
            drawSize = targetWeaponDef.graphic.drawSize.x * 2;
            adapters = new List<MountAdapterClass>();
            oneDot = (1f / targetWeaponDef.graphic.MatSingle.mainTexture.width);
        }

        ThingDef targetWeaponDef;
        float drawSize = 1f;
        List<MountAdapterClass> adapters = new List<MountAdapterClass>();
        int selectIndex = -1;
        float oneDot = 0.0078125f;
        RenderTexture renderTexture = null;

        int timeSpan = 0;

        public override void PostOpen() {
            base.PostOpen();
            forcePause = true;
            closeOnClickedOutside = false;
        }
        public override void DoWindowContents(Rect inRect) {
            var fontSize = Text.Font;
            var fontAnchor = Text.Anchor;

            CalcRectSize(inRect);
            DoContentsTopPart(headRect);
            DoContentsLeftPart(leftRect);
            DoContentsRightPart(rightRect);
            DoContentsBottomPart(lowerRect);

            if (timeSpan++ > 1200) {
                timeSpan = 0;
                renderTexture = null;
            }
            if (timeSpan == 600) {
                renderTexture = null;
            }

            Text.Font = fontSize;
            Text.Anchor = fontAnchor;
        }

        void DoContentsTopPart(Rect rect) {
            var closeButtonRect = new Rect(0, 0, 32, 32) {
                x = rect.xMax - 32,
                y = rect.y
            };
            if (Widgets.ButtonImage(closeButtonRect, DevGUI.Close)) {
                //SoundDefOf.Click.PlayOneShotOnCamera();
                Close();
            }
        }

        Rect partsButtonRect = new Rect(0, 0, 56, 56);
        void DoContentsLeftPart(Rect rect) {
            Widgets.DrawBox(rect);
            if (renderTexture == null) {
                Texture rawTexture = targetWeaponDef.graphic.MatSingle.mainTexture;
                renderTexture = new RenderTexture(rawTexture.width * 2, rawTexture.height * 2, 32, RenderTextureFormat.ARGB32);
                MW2MTK_CameraRenderer.Render(renderTexture, GetCameraRequests().ToArray());
            }
            Rect weaponRect = new Rect(rect) {
                size = Vector2.one * Mathf.Min(rect.width, rect.height),
                center = rect.center
            };
            GUI.DrawTexture(weaponRect, renderTexture);
            Vector2 buttonPosScale = weaponRect.size * new Vector2(0.40625f, -0.40625f);
            Vector2 linePosScale = weaponRect.size / drawSize * new Vector2(1, -1);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            for (int i = 0; i < adapters.Count; i++) {
                var adapter = adapters[i];
                partsButtonRect.center = rect.center + adapter.NormalizedOffsetForUI * buttonPosScale;
                if (selectIndex == i) {
                    Widgets.DrawLine(rect.center + adapter.offset * linePosScale, partsButtonRect.center, Color.white, 1f);
                    //Widgets.DrawHighlightSelected(partsButtonRect);
                    Widgets.DrawWindowBackground(partsButtonRect.ExpandedBy(4f));
                }
                Widgets.DrawWindowBackground(partsButtonRect);
                Widgets.Label(partsButtonRect, adapter.mountDef.LabelShort.CapitalizeFirst());
                if (Mouse.IsOver(partsButtonRect)) {
                    Widgets.DrawHighlight(partsButtonRect);
                }
                if (Widgets.ButtonInvisible(partsButtonRect, true)) {
                    SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    selectIndex = selectIndex == i ? -1 : i;
                }
            }
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(rect.x+4, rect.yMax-56,120,48), "MW2MTK_AdapterCreateNew".Translate())) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                var newAdapter = new MountAdapterClass();
                newAdapter.mountDef = DefDatabase<ModularPartsMountDef>.GetRandom();
                adapters.Add(newAdapter);
                selectIndex = adapters.Count - 1;
                renderTexture = null;
            }
        }

        IEnumerable<MWCameraRequest> GetCameraRequests() {
            yield return new MWCameraRequest(targetWeaponDef.graphic.MatSingle, Vector2.zero, 0);
            foreach(var i in adapters) {
                var adapter = i;
                var offset = i.offset;
                var scale = i.scale;
                if (i.adapterGraphic != null) {
                    yield return new MWCameraRequest(i.adapterGraphic.Graphic.MatSingle, i.offset, i.layerOrder,scale);
                }
                if (timeSpan>=600 && !i.mountDef.canAdaptAs.NullOrEmpty()) {
                    adapter = adapter.mountDef.canAdaptAs.Last();
                    offset += adapter.offset;
                    scale *= adapter.scale;
                    if (adapter.adapterGraphic != null) {
                        yield return new MWCameraRequest(adapter.adapterGraphic.Graphic.MatSingle, offset, i.layerOrder, scale);
                    }
                }
                ModularPartsDef part = DefDatabase<ModularPartsDef>.AllDefsListForReading.Where(t => t.attachedTo == adapter.mountDef).RandomElement();
                while (part == null && !adapter.mountDef.canAdaptAs.NullOrEmpty()) {
                    adapter = adapter.mountDef.canAdaptAs.First();
                    offset += adapter.offset;
                    scale *= adapter.scale;
                    part = DefDatabase<ModularPartsDef>.AllDefsListForReading.FirstOrFallback(t => t.attachedTo == adapter.mountDef);
                }
                if (part != null) {
                    yield return new MWCameraRequest(part.graphicData.Graphic.MatSingle, offset, i.layerOrder, scale);
                }
            }
        }

        void DoContentsRightPart(Rect rect) {
            Widgets.DrawBox(rect);
            rect = rect.ContractedBy(4f);
            if (selectIndex <0) {
                return;
            }
            var adapter = adapters[selectIndex];
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Listing_Standard listing = new Listing_Standard();
            float lineHeight = Text.LineHeightOf(GameFont.Small) * 2f;
            listing.Begin(rect);
            var lineRect = listing.GetRect(lineHeight);
            var leftPart = lineRect.LeftPart(0.325f);
            var rightPart = lineRect.RightPart(0.625f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftPart, "MW2MTK_MountDef".Translate());
            Widgets.Dropdown(
                rightPart.LeftPartPixels(32),
                adapter,
                t => t.mountDef,
                MenuGenerator_MountDef,
                "..."
                );
            Widgets.Label(new Rect(rightPart) { xMin = rightPart.x + 36 }, adapter.mountDef?.defName ?? "null");
            
            lineRect = listing.GetRect(lineHeight);
            leftPart = lineRect.LeftPart(0.325f);
            rightPart = lineRect.RightPart(0.625f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftPart, "MW2MTK_Offset_X".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rightPart, adapter.offset.x + "\n(" + adapter.offset.x / oneDot + " pixels)");
            var offsetButtonRect = rightPart.LeftPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "-32")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.x -= oneDot * 32;
                adapter.SetParentAdapter(null);    //<- これでオフセットのキャッシュをリセット
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "-8")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.x -= oneDot * 8;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "-1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.x -= oneDot * 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect = rightPart.RightPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "+32")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.x += oneDot * 32;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "+8")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.x += oneDot * 8;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "+1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.x += oneDot * 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            lineRect = listing.GetRect(lineHeight);
            leftPart = lineRect.LeftPart(0.325f);
            rightPart = lineRect.RightPart(0.625f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftPart, "MW2MTK_Offset_Y".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rightPart, adapter.offset.y + "\n(" + adapter.offset.y / oneDot + " pixels)");
            offsetButtonRect = rightPart.LeftPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "-32")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.y -= oneDot * 32;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "-8")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.y -= oneDot * 8;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "-1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.y -= oneDot * 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect = rightPart.RightPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "+32")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.y += oneDot * 32;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "+8")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.y += oneDot * 8;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "+1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.offset.y += oneDot * 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }

            lineRect = listing.GetRect(lineHeight);
            leftPart = lineRect.LeftPart(0.325f);
            rightPart = lineRect.RightPart(0.625f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftPart, "MW2MTK_Scale_X".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rightPart, adapter.scale.x.ToString());
            offsetButtonRect = rightPart.LeftPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "-1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.x -= 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.25")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.x -= 0.25f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.01")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.x -= 0.01f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect = rightPart.RightPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "+1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.x += 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.25")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.x += 0.25f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.01")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.x += 0.01f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }

            lineRect = listing.GetRect(lineHeight);
            leftPart = lineRect.LeftPart(0.325f);
            rightPart = lineRect.RightPart(0.625f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftPart, "MW2MTK_Scale_Y".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rightPart, adapter.scale.y.ToString());
            offsetButtonRect = rightPart.LeftPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "-1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.y -= 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.25")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.y -= 0.25f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x += offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.01")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.y -= 0.01f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect = rightPart.RightPartPixels(32);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "+1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.y += 1;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.25")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.y += 0.25f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }
            offsetButtonRect.x -= offsetButtonRect.width;
            if (Widgets.ButtonText(offsetButtonRect, "0.01")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.scale.y += 0.01f;
                adapter.SetParentAdapter(null);
                renderTexture = null;
            }

            lineRect = listing.GetRect(lineHeight);
            leftPart = lineRect.LeftPart(0.325f);
            rightPart = lineRect.RightPart(0.625f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftPart, "MW2MTK_LayerOrder".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rightPart, adapter.layerOrder.ToString());
            offsetButtonRect = rightPart.LeftPartPixels(lineHeight);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "-1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.layerOrder--;
                renderTexture = null;
            }
            offsetButtonRect = rightPart.RightPartPixels(lineHeight);
            offsetButtonRect.height /= 1.5f;
            offsetButtonRect.y += offsetButtonRect.height / 4f;
            if (Widgets.ButtonText(offsetButtonRect, "+1")) {
                SoundDefOf.Click.PlayOneShotOnCamera();
                adapter.layerOrder++;
                renderTexture = null;
            }


            lineRect = listing.GetRect(lineHeight);
            leftPart = lineRect.LeftPart(0.325f);
            rightPart = lineRect.RightPart(0.625f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(leftPart, "MW2MTK_AdapterTexture".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Dropdown(
                rightPart.LeftPartPixels(32),
                adapter,
                t => t.adapterGraphic.texPath,
                MenuGenerator_AdapterTextures,
                "..."
                );
            Widgets.Label(new Rect(rightPart) { xMin = rightPart.x + 36 }, adapter.adapterGraphic?.texPath ?? "null");

            listing.End();

            var colorPrev = GUI.color;
            GUI.color = Color.red;
            if(Widgets.ButtonText(new Rect(rect.xMax - 128, rect.yMax - 56, 120, 48), "Remove".Translate())) {
                adapters.RemoveAt(selectIndex);
                selectIndex = -1;
            }
            GUI.color = colorPrev;
        }
        IEnumerable<Widgets.DropdownMenuElement<ModularPartsMountDef>> MenuGenerator_MountDef(MountAdapterClass targ) {
            foreach(var i in DefDatabase<ModularPartsMountDef>.AllDefsListForReading) {
                yield return new Widgets.DropdownMenuElement<ModularPartsMountDef> {
                    option = new FloatMenuOption(i.defName, () => { 
                        targ.mountDef = i;
                        renderTexture = null;
                    }),
                    payload = i
                };
            }
        }
        IEnumerable<Widgets.DropdownMenuElement<string>> MenuGenerator_AdapterTextures(MountAdapterClass targ) {
            yield return new Widgets.DropdownMenuElement<string> {
                option = new FloatMenuOption("null", () => {
                    targ.adapterGraphic = null;
                    renderTexture = null;
                }),
                payload = null
            };
            foreach (var i in MW2_ModularizeToolkit.AdapterTextures) {
                yield return new Widgets.DropdownMenuElement<string> {
                    option = new FloatMenuOption(i.name, () => {
                        targ.adapterGraphic = new GraphicData();
                        targ.adapterGraphic.shaderType = ShaderTypeDefOf.Cutout;
                        targ.adapterGraphic.texPath = MW2_ModularizeToolkit.AdapterTexturesPath+"/" + i.name;
                        targ.adapterGraphic.graphicClass = typeof(Graphic_Single);
                        renderTexture = null;
                    }),
                    payload = MW2_ModularizeToolkit.AdapterTexturesPath+"/" + i.name
                };
            }
        }

        void DoContentsBottomPart(Rect rect) {
            if (Widgets.ButtonText(rect.RightPart(0.2f).ContractedBy(4f), "MW2MTK_Export".Translate())) {
                MW2_ModularizeToolkit.CompleteSound.PlayOneShotOnCamera();
                ExportXml(); 
                Find.WindowStack.Add(new Dialog_ExportMessage());
            }
        }

        void ExportXml() {
            //{0}
            var defName = targetWeaponDef.defName;
            //{1}
            var texPath = targetWeaponDef.graphicData.texPath;
            //{2}
            var drawSize = targetWeaponDef.graphicData.drawSize.x * 2;
            //{3}
            var sb_partsMounts = new StringBuilder();
            foreach (var i in adapters) {
                sb_partsMounts.AppendLine("          <li>");
                sb_partsMounts.AppendLine($"            <mountDef>{i.mountDef.defName}</mountDef>");
                sb_partsMounts.AppendLine($"            <offset>({i.offset.x}, {i.offset.y})</offset>");
                sb_partsMounts.AppendLine($"            <scale>({i.scale.x}, {i.scale.y})</scale>");
                sb_partsMounts.AppendLine($"            <layerOrder>{i.layerOrder}</layerOrder>");
                if (i.adapterGraphic != null) {
                    sb_partsMounts.AppendLine("            <adapterGraphic>");
                    sb_partsMounts.AppendLine($"              <texPath>{i.adapterGraphic.texPath}</texPath>");
                    sb_partsMounts.AppendLine($"              <graphicClass>{i.adapterGraphic.graphicClass}</graphicClass>");
                    sb_partsMounts.AppendLine("            </adapterGraphic>");
                }
                sb_partsMounts.AppendLine("          </li>");
            }

            _ = ExportXmlAsync(MW2MTKMod.Path_templateText, MW2MTKMod.Path_export(targetWeaponDef), defName, texPath, drawSize.ToString(), sb_partsMounts.ToString());
        }
        async Task ExportXmlAsync(string templatePath, string writePath, params string[] formatArgs) {
            using (StreamReader sr = new StreamReader(templatePath, Encoding.UTF8)) {
                using(StreamWriter sw = new StreamWriter(writePath, false, Encoding.UTF8)) {
                    var template = await sr.ReadToEndAsync();
                    await sw.WriteAsync(string.Format(template, formatArgs.Cast<object>().ToArray()));
                }
            }

            System.Diagnostics.Process.Start(new DirectoryInfo(writePath).Parent.FullName);
        }

        bool rectSizeCached = false;
        Rect headRect;
        Rect lowerRect;
        Rect leftRect;
        Rect rightRect;
        void CalcRectSize(Rect inRect) {
            if (rectSizeCached) 
                return;
            headRect = inRect.TopPartPixels(Text.LineHeightOf(GameFont.Medium) * 2f);
            lowerRect = inRect.BottomPartPixels(Text.LineHeightOf(GameFont.Medium) * 2f);
            leftRect = new Rect(inRect.x, headRect.yMax, inRect.width / 2, inRect.height - headRect.height - lowerRect.height);
            rightRect = new Rect(leftRect) { x = leftRect.xMax };
            rectSizeCached = true;
        }
    }
}
