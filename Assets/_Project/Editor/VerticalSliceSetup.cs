using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Core.Diagnostics;
using Zmywak.Core.GameState;
using Zmywak.Core.Graphics;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Flow;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Orders;
using Zmywak.Kitchen.Pile;
using Zmywak.Kitchen.Radio;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.Kitchen.Tools;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.LanguageBarrier;
using Zmywak.Narrative.Progress;
using Zmywak.Narrative.SlangDictionary;
using Zmywak.Narrative.Story;
using Zmywak.UI.Accessibility;
using Zmywak.UI.Common;
using Zmywak.UI.Graphics;
using Zmywak.UI.HUD;
using Zmywak.UI.Screens;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// One-click content + scene generation for the vertical slice. Builds everything through
    /// Unity's own APIs (AssetDatabase, SerializedObject, EditorSceneManager) so every GUID and scene
    /// reference is assigned by the Editor itself instead of being hand-authored YAML.
    /// Menu: Zmywak > Setup > Run All (1-8).
    /// </summary>
    public static partial class VerticalSliceSetup
    {
        private const string ContentFolder = "Assets/_Project/ScriptableObjects";
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string RenderingFolder = "Assets/_Project/Settings/Rendering";
        private const string UrpAssetPath = RenderingFolder + "/Zmywak_URP.asset";
        private const string UrpRendererPath = RenderingFolder + "/Zmywak_URP_Renderer.asset";

        [MenuItem("Zmywak/Setup/Run All (1-8)")]
        public static void RunAll()
        {
            try
            {
                ImportTmpEssentialResourcesHeadless();
                EnsureWordBlurFont();
                EnsureInputSystemBackendsEnabled();
                EnsureUrpPipelineAssigned();
                EnsureProductIdentity();
                EnsurePortraitOnly();
                EnsurePlaceholderMaterialTemplate();
                UiArt.EnsureAssets(); // files only; scene builders load the sprites after their NewScene()
                FxArt.EnsureAssets();
                CreateContentAssets();
                RecordedSounds.EnsureBank(); // the WAVs are committed; the bank the game loads them from is rebuilt
                GameSoundsBake.EnsureBank(); // the same for the interface, the music and the room
                CreateMetaContentAssets();
                CreateNarrativeContentAssets();
                BuildBootScene();
                BuildMainMenuScene();
                BuildMetaHubScene();
                BuildKitchenShiftScene();
                RegisterBuildScenes();

                if (!VerifyRunAllOutputs())
                {
                    Debug.LogError("Zmywak: Run All finished but verification found missing scenes/assets.");
                    ExitIfHeadless(1);
                    return;
                }

                EditorSceneManager.OpenScene($"{ScenesFolder}/Boot.unity");
                Debug.Log("Zmywak: vertical slice setup complete. Open Boot.unity and press Play.");
                ExitIfHeadless(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Zmywak: Run All threw an exception: {ex}");
                ExitIfHeadless(1);
                throw;
            }
        }

        /// <summary>Only quits the Editor when actually running headless (-batchmode) - never during
        /// interactive use from the menu, where quitting the user's open Editor would be unwelcome.</summary>
        private static void ExitIfHeadless(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        /// <summary>
        /// Player Settings > Active Input Handling was left at "Input Manager (Old)", which disables the
        /// native backends of the Input System package: in a player build no touch/pointer events ever reach
        /// Pointer.current (DishInputRouter) or InputSystemUIInputModule (every button), so the game is
        /// unplayable on a phone. PlayMode tests can't catch it - InputTestFixture injects events directly.
        /// Nothing in the project uses the old UnityEngine.Input API, so switch to "Input System Package (New)".
        /// Takes effect the next time the Editor starts.
        /// </summary>
        [MenuItem("Zmywak/Setup/0d. Enable Input System Backends")]
        public static void EnsureInputSystemBackendsEnabled()
        {
            SerializedProperty handler = FindActiveInputHandlerProperty();
            if (handler == null)
            {
                throw new System.InvalidOperationException("Could not find 'activeInputHandler' in ProjectSettings/ProjectSettings.asset.");
            }

            if (handler.intValue == ActiveInputHandlerNewInputSystem)
            {
                return;
            }

            handler.intValue = ActiveInputHandlerNewInputSystem;
            handler.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("Zmywak: Active Input Handling set to 'Input System Package (New)' (restart the Editor if it is open).");
        }

        private const int ActiveInputHandlerNewInputSystem = 1; // 0 = old Input Manager, 1 = new, 2 = both

        internal static SerializedProperty FindActiveInputHandlerProperty()
        {
            foreach (Object settings in AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset"))
            {
                if (settings is PlayerSettings)
                {
                    return new SerializedObject(settings).FindProperty("activeInputHandler");
                }
            }

            return null;
        }

        /// <summary>
        /// The project had the URP package installed but no URP pipeline asset assigned anywhere
        /// (Graphics Settings and every Quality level pointed at nothing), so Unity silently rendered with
        /// the Built-in pipeline - under which every "Universal Render Pipeline/Lit" material draws solid
        /// magenta. This creates the pipeline asset + its Universal Renderer data through URP's own public
        /// API and assigns it as the default pipeline; Quality levels left empty fall back to it.
        /// </summary>
        [MenuItem("Zmywak/Setup/0. Assign URP Pipeline Asset")]
        public static void EnsureUrpPipelineAssigned()
        {
            RemoveLitFromAlwaysIncludedShaders();

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (pipeline == null)
            {
                EnsureFolder(RenderingFolder);

                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    UniversalRenderPipelineAsset.packagePath + "/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(rendererData, UrpRendererPath);
                ResourceReloader.ReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, UrpAssetPath);
            }

            // Look settings, applied on every run so older projects pick them up: soft shadows (they show
            // what lies on what in the pile), a shadow distance matching the ~15 m deep kitchen instead of
            // URP's 50 m (far better shadow resolution), HDR for the bloom on glossy highlights.
            var pipelineSo = new SerializedObject(pipeline);
            pipelineSo.FindProperty("m_SoftShadowsSupported").boolValue = true;
            pipelineSo.FindProperty("m_ShadowDistance").floatValue = 30f; // QualityManager sets it per tier at runtime
            pipelineSo.FindProperty("m_ShadowCascadeCount").intValue = 1;
            pipelineSo.FindProperty("m_SupportsHDR").boolValue = true;
            // The kitchen's baked reflection probe is a box the size of the room: steel reflects the wall where it is.
            pipelineSo.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
            pipelineSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureAmbientOcclusion();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log($"Zmywak: default render pipeline set to {UrpAssetPath}.");
        }

        /// <summary>
        /// Screen-space ambient occlusion: the contact darkness where a dish sits on the counter, in the crate's
        /// corners and under the sink's rim - what makes objects look placed rather than floating. It costs a
        /// phone real GPU time, so QualityManager switches it on at High only. Settings are internal to URP but
        /// serialized, so they are written through SerializedObject.
        /// </summary>
        private static void EnsureAmbientOcclusion()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(UrpRendererPath);
            if (rendererData == null)
            {
                return;
            }

            ScreenSpaceAmbientOcclusion ssao = null;
            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is ScreenSpaceAmbientOcclusion existing)
                {
                    ssao = existing;
                }
            }

            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "AmbientOcclusion";
                AssetDatabase.AddObjectToAsset(ssao, rendererData);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long localId);

                var dataSo = new SerializedObject(rendererData);
                SerializedProperty features = dataSo.FindProperty("m_RendererFeatures");
                SerializedProperty map = dataSo.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                dataSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var so = new SerializedObject(ssao);
            so.FindProperty("m_Settings.Intensity").floatValue = 0.9f;
            so.FindProperty("m_Settings.Radius").floatValue = 0.22f;       // scene units: the set is ~3x life size
            so.FindProperty("m_Settings.DirectLightingStrength").floatValue = 0.3f;
            so.FindProperty("m_Settings.Downsample").boolValue = true;     // half resolution: a fraction of the cost
            so.FindProperty("m_Settings.Falloff").floatValue = 60f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
        }

        /// <summary>
        /// An earlier version of this setup put URP/Lit into Graphics Settings' "Always Included Shaders"
        /// (wrongly diagnosing the magenta as a Shader.Find problem). That disables variant stripping for
        /// the shader: an Android build then tries to prepare ~36 billion ForwardLit variants and dies after
        /// ~30 minutes. Undo that on projects that already ran the old step.
        /// </summary>
        private static void RemoveLitFromAlwaysIncludedShaders()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            if (litShader == null || graphicsSettingsObj == null)
            {
                return;
            }

            var so = new SerializedObject(graphicsSettingsObj);
            SerializedProperty alwaysIncluded = so.FindProperty("m_AlwaysIncludedShaders");

            for (int i = alwaysIncluded.arraySize - 1; i >= 0; i--)
            {
                if (alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue == litShader)
                {
                    alwaysIncluded.DeleteArrayElementAtIndex(i);
                }
            }

            if (so.ApplyModifiedPropertiesWithoutUndo())
            {
                AssetDatabase.SaveAssets();
                Debug.Log("Zmywak: removed 'Universal Render Pipeline/Lit' from Always Included Shaders.");
            }
        }

        /// <summary>
        /// PlaceholderMaterial clones this template at runtime instead of relying on Shader.Find, which only
        /// works in a player build if something else happens to reference the shader. Living in a Resources
        /// folder, the template is always packed into builds and pulls URP/Lit in with normal variant
        /// stripping.
        /// </summary>
        [MenuItem("Zmywak/Setup/0c. Create Placeholder Material Template")]
        public static void EnsurePlaceholderMaterialTemplate()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                throw new System.InvalidOperationException("Shader 'Universal Render Pipeline/Lit' not found - is URP installed?");
            }

            EnsureFolder(ResourcesFolder);

            string path = $"{ResourcesFolder}/{PlaceholderMaterial.TemplateResourceName}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                AssetDatabase.CreateAsset(new Material(litShader), path);
                Debug.Log($"Zmywak: created placeholder material template at {path}.");
            }

            // Dish template: smoothness from the base map's alpha channel, so the painted dirt texture
            // can make grime matte and clean glaze glossy. Built as an asset (not toggled at runtime) so
            // the keyword's shader variant survives build-time stripping.
            string dishPath = $"{ResourcesFolder}/{PlaceholderMaterial.DishTemplateResourceName}.mat";
            var dishTemplate = AssetDatabase.LoadAssetAtPath<Material>(dishPath);
            if (dishTemplate == null)
            {
                dishTemplate = new Material(litShader);
                dishTemplate.SetFloat("_SmoothnessTextureChannel", 1f);
                dishTemplate.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                AssetDatabase.CreateAsset(dishTemplate, dishPath);
                Debug.Log($"Zmywak: created dish material template at {dishPath}.");
            }

            // The dirt's relief comes as a normal map painted with it (DishController); with no map assigned the
            // shader's default is flat, so a template without dirt looks as before.
            if (!dishTemplate.IsKeywordEnabled("_NORMALMAP"))
            {
                dishTemplate.EnableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(dishTemplate);
            }

            // Glass template: transparent, the base map's alpha is opacity (clear where clean, cloudy under
            // grime), smoothness a constant. Specular is kept whole over the see-through body, as on real
            // glass. BaseShaderGUI sets the blend state, queue and keywords exactly as the inspector would.
            string glassPath = $"{ResourcesFolder}/{PlaceholderMaterial.GlassTemplateResourceName}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(glassPath) == null)
            {
                var glass = new Material(litShader);
                glass.SetFloat("_Surface", 1f);                  // Transparent
                glass.SetFloat("_Blend", 0f);                    // Alpha
                glass.SetFloat("_BlendModePreserveSpecular", 1f);
                BaseShaderGUI.SetupMaterialBlendMode(glass);
                AssetDatabase.CreateAsset(glass, glassPath);
                Debug.Log($"Zmywak: created glass material template at {glassPath}.");
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The font WordBlurFormatter sets unknown English words in: the dialogue font's face (LiberationSans), but only
        /// the stand-in letters, sampled at 40 pt with a 20-pixel distance field - half an em of room around each glyph,
        /// where the dialogue font has a tenth - so its material can soften the edges all the way into a smudge without
        /// the glyphs' boxes showing. Static (it never needs the font file at run time); in TMP's "Fonts &amp; Materials"
        /// resources, where a &lt;font&gt; tag finds it by name. Made once; delete the asset to make it again.
        /// </summary>
        [MenuItem("Zmywak/Setup/0d. Create Word Blur Font")]
        public static void EnsureWordBlurFont()
        {
            const string folder = "Assets/TextMesh Pro/Resources/Fonts & Materials";
            string path = $"{folder}/{WordBlurFormatter.BlurFont}.asset";
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                font = CreateWordBlurFont(path);
            }

            // How blurred: soft enough that nothing can be read, firm enough that the words keep their outlines
            // (judged on WordBlurPlayModeTests.ArtPreview_BlurStrengths).
            Material material = font.material;
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.8f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
            ShaderUtilities.UpdateShaderRatios(material);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset CreateWordBlurFont(string path)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (source == null)
            {
                throw new System.InvalidOperationException("No LiberationSans.ttf - import the TMP Essential Resources first.");
            }

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 40, 20, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Dynamic, false);
            font.name = WordBlurFormatter.BlurFont;
            if (!font.TryAddCharacters(WordBlurFormatter.StandIns, out string missing))
            {
                throw new System.InvalidOperationException($"The blur font could not take '{missing}'.");
            }

            font.atlasPopulationMode = AtlasPopulationMode.Static;
            Material material = font.material;
            material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            material.name = $"{WordBlurFormatter.BlurFont} Material";

            AssetDatabase.CreateAsset(font, path);
            Texture2D atlas = font.atlasTextures[0];
            atlas.name = $"{WordBlurFormatter.BlurFont} Atlas";
            AssetDatabase.AddObjectToAsset(atlas, font);
            AssetDatabase.AddObjectToAsset(material, font);
            EditorUtility.SetDirty(font);
            return font;
        }

        /// <summary>
        /// TMPro.TMP_PackageUtilities.ImportProjectResourcesMenu (Window > TextMeshPro > Import TMP Essential
        /// Resources) calls AssetDatabase.ImportPackage with interactive=true, which opens an import dialog
        /// that nobody is there to click in batch mode - it never completes headlessly. This calls the same
        /// underlying package import with interactive=false instead, which imports immediately with no dialog.
        /// </summary>
        [MenuItem("Zmywak/Setup/0b. Import TMP Essential Resources (Headless)")]
        public static void ImportTmpEssentialResourcesHeadless()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
            {
                Debug.Log("Zmywak: TMP Essential Resources already present, skipping import.");
                return;
            }

            string packagePath = TMPro.EditorUtilities.TMP_EditorUtility.packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage";
            if (!File.Exists(packagePath))
            {
                Debug.LogError($"Zmywak: TMP Essential Resources package not found at {packagePath}");
                return;
            }

            UnityEditor.AssetPackage.Package.Import(packagePath, false); // Unity 6.6's replacement for AssetDatabase.ImportPackage
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Zmywak: TMP Essential Resources imported.");
        }

        private static bool VerifyRunAllOutputs()
        {
            string[] requiredScenes =
            {
                "Boot.unity", "MainMenu.unity", "MetaHub.unity", "Kitchen_Shift.unity"
            };

            string[] requiredAssets =
            {
                "DirtType_grease.asset", "DirtType_dried_sauce.asset", "DirtType_burnt_food.asset", "DirtType_streaks.asset",
                "DirtType_dried_porridge.asset", "DirtType_coffee.asset", "DirtType_baked_cheese.asset",
                "Tool_soak.asset", "Tool_sponge.asset", "Tool_steel_wool.asset", "Tool_scraper.asset", "Tool_shower.asset",
                "Dish_plate.asset", "Dish_pan.asset", "Dish_glass.asset", "Dish_bowl.asset", "Dish_mug.asset", "Dish_pot.asset", "Dish_roaster.asset",
                "Dish_lasagne_tray.asset",
                "Shift_vertical_slice.asset", "Shift_calm_start.asset", "Shift_normal.asset", "Shift_lunch_rush.asset",
                "Shift_evening_rush.asset", "Shift_hard_night.asset", "Character_kuba.asset",
                "ShiftCompletedEventChannel.asset", "ExchangeRateTable.asset", "MoodVolumeProfile.asset", "LookVolumeProfile.asset",
                "Slang_slang_grand.asset", "Slang_slang_fair_play.asset", "Slang_slang_craic.asset", "Slang_slang_i_will_yeah.asset",
                "Slang_slang_so.asset", "Slang_slang_jacks.asset", "Slang_slang_eejit.asset", "Slang_slang_deadly.asset",
                "Slang_slang_sound.asset", "Slang_slang_giving_out.asset", "Slang_slang_your_man.asset", "Slang_slang_gas.asset",
                "Slang_slang_banjaxed.asset", "Slang_slang_notions.asset", "Slang_slang_gaff.asset", "Slang_slang_go_way.asset",
                "Slang_slang_begrudgers.asset", "Slang_slang_dole.asset", "Slang_slang_fierce.asset", "Slang_slang_brutal.asset",
                "Slang_slang_sure_look.asset",
                Chapter1Content.ChapterAssetName, Chapter2Content.ChapterAssetName, Chapter3Content.ChapterAssetName,
                Chapter4Content.ChapterAssetName, Chapter5Content.ChapterAssetName, Chapter6Content.ChapterAssetName
            };

            bool ok = true;

            // Every kind of shift the schedule can plan has to be in the catalog the kitchen loads it from.
            var shiftCatalog = AssetDatabase.LoadAssetAtPath<ShiftCatalog>($"{ResourcesFolder}/{ShiftCatalog.ResourceName}.asset");
            foreach (string archetype in ShiftSchedule.Archetypes)
            {
                if (shiftCatalog == null || shiftCatalog.Find(archetype) == null)
                {
                    Debug.LogError($"Zmywak: the shift catalog has no '{archetype}' shift");
                    ok = false;
                }
            }

            var soundBank = AssetDatabase.LoadAssetAtPath<KitchenSoundBank>(RecordedSounds.BankPath);
            foreach (ProceduralSounds.Sound sound in System.Enum.GetValues(typeof(ProceduralSounds.Sound)))
            {
                if (soundBank == null || soundBank.TakesOf(sound).Count == 0)
                {
                    Debug.LogError($"Zmywak: no recorded take of {sound} in {RecordedSounds.BankPath}");
                    ok = false;
                }
            }

            foreach (string scene in requiredScenes)
            {
                string path = $"{ScenesFolder}/{scene}";
                if (!File.Exists(path))
                {
                    Debug.LogError($"Zmywak: missing expected scene: {path}");
                    ok = false;
                    continue;
                }

                // Two EventSystems = a warning logged every frame (and two UI input modules fighting).
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length;
                if (eventSystems > 1)
                {
                    Debug.LogError($"Zmywak: {scene} has {eventSystems} EventSystems, expected at most 1.");
                    ok = false;
                }
            }

            foreach (string asset in requiredAssets)
            {
                string path = $"{ContentFolder}/{asset}";
                if (!File.Exists(path))
                {
                    Debug.LogError($"Zmywak: missing expected asset: {path}");
                    ok = false;
                }
            }

            // Particle materials: a missing texture renders every particle as a solid square.
            foreach (string fx in new[] { "Fx_Soft", "Fx_Bubble", "Fx_Glow", "Fx_Sparkle", "Fx_Shard", "Fx_Ring" })
            {
                string path = $"Assets/_Project/Art/Materials/{fx}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.GetTexture("_BaseMap") == null)
                {
                    Debug.LogError($"Zmywak: particle material missing or without texture: {path}");
                    ok = false;
                }
            }

            if (EditorBuildSettings.scenes.Length != requiredScenes.Length)
            {
                Debug.LogError($"Zmywak: expected {requiredScenes.Length} scenes registered in Build Settings, found {EditorBuildSettings.scenes.Length}.");
                ok = false;
            }

            SerializedProperty inputHandler = FindActiveInputHandlerProperty();
            if (inputHandler == null || inputHandler.intValue == 0)
            {
                Debug.LogError("Zmywak: Active Input Handling is 'Input Manager (Old)' - touch input will not work in player builds.");
                ok = false;
            }

            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
            {
                Debug.LogError("Zmywak: no URP asset assigned as the default render pipeline - URP materials will render magenta.");
                ok = false;
            }

            foreach (string template in new[] { PlaceholderMaterial.TemplateResourceName, PlaceholderMaterial.DishTemplateResourceName })
            {
                if (Resources.Load<Material>(template) == null)
                {
                    Debug.LogError($"Zmywak: material template '{template}' not loadable from Resources.");
                    ok = false;
                }
            }

            return ok;
        }

        // ------------------------------------------------------------------
        // 1. Kitchen content assets
        // ------------------------------------------------------------------

        [MenuItem("Zmywak/Setup/1. Create Kitchen Content Assets")]
        public static void CreateContentAssets()
        {
            EnsureFolder(ContentFolder);

            // Alpha = opacity at full thickness; gloss and relief are how the layer catches the light (a shiny, flat
            // grease film; dried sauce with a little body; matte, lumpy burnt crust).
            // Grease is a dark amber film, not butter-yellow (on a dark pan that read as paint), and only half glossy:
            // the pan is metallic, and a glossy amber on metal is gold leaf.
            DirtTypeDefinition grease = CreateDirtType("grease", "Tłuszcz", new Color(0.42f, 0.31f, 0.12f, 0.55f), 1.0f, gloss: 0.55f, relief: 0.1f);
            // Dried tomato sauce goes dark and brick-red; a bright orange read as paint on the white plate.
            DirtTypeDefinition sauce = CreateDirtType("dried_sauce", "Zaschnięty sos", new Color(0.44f, 0.12f, 0.055f, 0.92f), 1.2f, gloss: 0.4f, relief: 0.45f);
            DirtTypeDefinition burnt = CreateDirtType("burnt_food", "Przypalone jedzenie", new Color(0.24f, 0.13f, 0.06f, 0.95f), 1.8f, gloss: 0.1f, relief: 1f); // charred brown, not black
            // Water streaks drying on a glass: thin dull smears that only break up the reflection when the glass is
            // held to the light, and only form once the grease is off.
            DirtTypeDefinition streaks = CreateDirtType("streaks", "Zacieki", new Color(0.3f, 0.34f, 0.37f, 0.9f), 0.8f, gloss: 0.2f, relief: 0f,
                pattern: DirtPattern.Streaks, showsOnlyInReflection: true);
            // Porridge dried on a bowl's sides sets like glue: the hardest dirt in the kitchen to rub off dry, a third
            // of that after a soak in the sink - the one the soak is for.
            DirtTypeDefinition porridge = CreateDirtType("dried_porridge", "Zaschnięta owsianka", new Color(0.8f, 0.72f, 0.52f, 0.95f), 3.2f, gloss: 0.15f, relief: 0.7f);
            // Coffee leaves a thin brown film and rings round a mug's walls where it stood (the wall band draws
            // the splotches there as tide lines).
            DirtTypeDefinition coffee = CreateDirtType("coffee", "Osad z kawy", new Color(0.3f, 0.17f, 0.07f, 0.78f), 1.0f, gloss: 0.5f, relief: 0.05f);
            // Cheese baked onto a roasting dish: a golden-brown, glossy, lumpy crust, hard but not burnt - the
            // scraper's and the steel wool's, and a soak softens it.
            DirtTypeDefinition bakedCheese = CreateDirtType("baked_cheese", "Zapieczony ser", new Color(0.62f, 0.4f, 0.13f, 0.95f), 2.2f, gloss: 0.4f, relief: 0.8f);

            // Dish soap on a brush (id "soak" from when it was called soaking - soaking is now the sink's own thing).
            ToolDefinition soak = CreateTool("soak", "Płyn", new[] { grease, coffee }, 1f, 0.15f);
            ToolDefinition sponge = CreateTool("sponge", "Gąbka", new[] { sauce, streaks, porridge, coffee }, 1f, 0.15f);
            ToolDefinition steelWool = CreateTool("steel_wool", "Druciak", new[] { burnt, porridge, bakedCheese }, 1f, 0.15f);
            // Fast first passes that leave something for the tools above to finish: the scraper takes a crust off
            // quickly but down to a film, the spray's wide jet blasts sauce and grease but not the last of them.
            CreateTool("scraper", "Skrobak", new[] { burnt, porridge, bakedCheese }, 2.6f, 0.1f, brushRadiusScale: 0.9f, residueFloor: 0.3f);
            CreateTool("shower", "Prysznic", new[] { sauce, grease, coffee }, 0.8f, 0.2f, brushRadiusScale: 1.8f, residueFloor: 0.12f);

            // Coverage = share of the dish surface the dirt layer covers when the dish arrives.
            // Rack footprints are in 0.45 m cells: a plate covers 2x2, a glass one cell, a pan 4x2 - two for the
            // body, two for the handle sticking out to its +x side (so the body sits one cell off the centre).
            DishDefinition plate = CreateDish("plate", "Talerz", DishShape.Plate, new Vector3(0.9f, 0.06f, 0.9f),
                new Color(0.92f, 0.91f, 0.87f), 0.85f, 0f, new[] { (sauce, 0.7f) }, new Vector2Int(2, 2), Vector2.zero, massKg: 0.55f); // off-white: pure white blew out under the key light
            DishDefinition pan = CreateDish("pan", "Patelnia", DishShape.Pan, new Vector3(0.9f, 0.14f, 0.9f),
                new Color(0.27f, 0.28f, 0.3f), 0.55f, 0.6f, new[] { (burnt, 0.55f), (grease, 0.6f) }, new Vector2Int(4, 2), new Vector2(-1f, 0f),
                material: DishMaterial.Steel, massKg: 1.3f);
            DishDefinition glass = CreateDish("glass", "Szklanka", DishShape.Glass, new Vector3(0.4f, 0.5f, 0.4f),
                new Color(0.78f, 0.9f, 0.95f), 0.95f, 0f, new[] { (grease, 0.5f), (streaks, 0.35f) }, Vector2Int.one, Vector2.zero,
                cleanOpacity: 0.3f, material: DishMaterial.Glass, massKg: 0.25f); // see-through: clear glass, clouded where the grease sits
            // A soup bowl (2x2 like a plate), a mug (one cell; its ear fits beside it) and a stockpot - 3x3, the
            // biggest thing the rack takes, with a loop handle each side.
            DishDefinition bowl = CreateDish("bowl", "Miska", DishShape.Bowl, new Vector3(0.72f, 0.2f, 0.72f),
                new Color(0.93f, 0.92f, 0.89f), 0.85f, 0f, new[] { (porridge, 0.6f) }, new Vector2Int(2, 2), Vector2.zero, massKg: 0.45f);
            DishDefinition mug = CreateDish("mug", "Kubek", DishShape.Mug, new Vector3(0.36f, 0.38f, 0.36f),
                new Color(0.94f, 0.93f, 0.9f), 0.88f, 0f, new[] { (coffee, 0.7f) }, Vector2Int.one, Vector2.zero, massKg: 0.35f);
            // Brushed steel, only half metallic: fully metallic, its sides mirrored the studio's dark lower half and the
            // pot came out black.
            DishDefinition pot = CreateDish("pot", "Garnek", DishShape.Pot, new Vector3(0.95f, 0.42f, 0.95f),
                new Color(0.66f, 0.68f, 0.71f), 0.62f, 0.45f, new[] { (burnt, 0.4f), (grease, 0.55f) }, new Vector2Int(3, 3), Vector2.zero,
                material: DishMaterial.Steel, massKg: 1.6f);
            // An oval enamelled roasting dish ("brytfanna", granite-ware): the lasagne's - baked-on cheese and grease.
            // 1.2 x 0.8, so 3x2 on the rack, its ears at the two ends.
            DishDefinition roaster = CreateDish("roaster", "Brytfanna", DishShape.Roaster, new Vector3(1.2f, 0.12f, 0.8f),
                new Color(0.1f, 0.15f, 0.3f), 0.8f, 0f, new[] { (bakedCheese, 0.55f), (grease, 0.5f) }, new Vector2Int(3, 2), Vector2.zero,
                material: DishMaterial.Steel, massKg: 1.2f); // enamelled steel: it chips, it does not shatter
            // Sunday's legendary lasagne tray (SpecialShift.LasagneTray): Declan's battered old steel tray, bigger than the
            // roaster, its cheese baked on in layers nobody has ever got off - 4x3 on the rack. It never comes in the
            // shift's queue: ShiftController drops it on the pile a quarter of the way into a Sunday.
            CreateDish("lasagne_tray", "Legendarna Taca Lasagne", DishShape.Roaster, new Vector3(1.5f, 0.15f, 1.05f),
                new Color(0.52f, 0.53f, 0.55f), 0.5f, 0.55f, new[] { (bakedCheese, 0.85f), (burnt, 0.45f), (grease, 0.6f) },
                new Vector2Int(4, 3), Vector2.zero, material: DishMaterial.Steel, massKg: 2.2f, leftoversOf: "roaster");

            // Two dishes waiting, then one every 18 s tightening to 9 s: a newcomer (~30 s a dish) feels the pile
            // climb late in the shift; a practised porter (~15 s) keeps up until the last minute. The first three
            // are the ones the hints teach on; the bowl, the mug and the pot come in after them.
            // The kitchen's own shift, for the scene on its own (the editor, the tests): the same dishes in the same order
            // every time, the second cycle always breaking down. The game itself plays the kinds of shift below, which the
            // evening schedules day by day (ShiftSchedule).
            CreateShift("vertical_slice", "Pierwsza zmiana", 180f, openingDishes: 2, intervalStart: 18f, intervalEnd: 9f,
                new[] { plate, pan, glass, bowl, plate, mug, pot, pan, roaster, glass, bowl, plate, mug },
                // The second dishwasher cycle always breaks down, so every newcomer meets the repair once;
                // after that each cycle has a 1-in-5 chance, at most twice a shift.
                scriptedBreakdownCycle: 2, breakdownChance: 0.2f, maxBreakdowns: 2, quota: 8);

            // The kinds of shift. A newcomer washes a dish in ~30 s, a practised porter in ~15 s (6 and 12 a shift): each
            // quota sits around half of what arrives, so a beginner can meet the calm one and a practised hand beats them all.
            // The mixes are each shift's weights per round, dealt shuffled (ShuffleBag); the waves speed up the arrivals and
            // the chef's orders inside each rush.
            var shifts = new List<ShiftDefinition>
            {
                // Days 1-3. ~10 dishes arrive; the first day opens with the three the hints teach on.
                CreateShift(ShiftSchedule.CalmStart, "Spokojny początek", 180f, openingDishes: 1, intervalStart: 22f, intervalEnd: 15f,
                    new[] { plate, plate, plate, plate, bowl, bowl, mug, mug, glass, glass, pan },
                    breakdownChance: 0.05f, maxBreakdowns: 1, quota: 5, shuffle: true, opening: new[] { plate, pan, glass },
                    intro: "Spokojny wieczór: mało gości i czas, żeby nauczyć się zlewu.",
                    waves: new[] { new ServiceWave(0.45f, 0.7f, 0.25f, "Kilka stolików naraz.") },
                    urgentChance: 0f, minCyclesBetween: 3, energyCost: 18f),
                // ~16 dishes in two even waves.
                CreateShift(ShiftSchedule.Normal, "Zwykła zmiana", 180f, openingDishes: 2, intervalStart: 18f, intervalEnd: 11f,
                    new[] { plate, plate, plate, pan, pan, glass, glass, bowl, bowl, mug, mug, pot, roaster },
                    breakdownChance: 0.1f, maxBreakdowns: 1, quota: 8, shuffle: true,
                    intro: "Zwykły wieczór w kuchni: dwie fale gości, bez szaleństw.",
                    waves: new[] { new ServiceWave(0.2f, 0.45f, 0.35f, "Pierwsza fala zamówień!"), new ServiceWave(0.6f, 0.85f, 0.35f, "Druga fala!") },
                    urgentChance: 0.25f, minCyclesBetween: 3, energyCost: 24f),
                // ~18 dishes, most of them early: plates and soup bowls.
                CreateShift(ShiftSchedule.LunchRush, "Lunch", 180f, openingDishes: 3, intervalStart: 17f, intervalEnd: 12f,
                    new[] { plate, plate, plate, plate, bowl, bowl, bowl, glass, glass, mug, mug, pan, pot, roaster },
                    breakdownChance: 0.1f, maxBreakdowns: 1, quota: 9, shuffle: true,
                    intro: "Pora lunchu: sala pełna od razu, potem chwila oddechu i jeszcze jedna fala.",
                    waves: new[] { new ServiceWave(0.08f, 0.42f, 0.9f, "Lunch: sala pełna!"), new ServiceWave(0.62f, 0.85f, 0.4f, "Spóźnieni goście!") },
                    urgentChance: 0.35f, minCyclesBetween: 3, energyCost: 27f),
                // ~18 dishes, most of them late: the pans and the glasses of a dinner service.
                CreateShift(ShiftSchedule.EveningRush, "Kolacja", 180f, openingDishes: 2, intervalStart: 18f, intervalEnd: 11f,
                    new[] { plate, plate, plate, pan, pan, glass, glass, glass, pot, roaster, bowl, mug },
                    breakdownChance: 0.12f, maxBreakdowns: 1, quota: 9, shuffle: true,
                    intro: "Kolacja: zaczyna się spokojnie, a potem kuchnia wydaje wszystko naraz.",
                    waves: new[] { new ServiceWave(0.25f, 0.45f, 0.3f, "Pierwsze stoliki."), new ServiceWave(0.55f, 0.92f, 0.9f, "Kolacja: wszystko naraz!") },
                    urgentChance: 0.35f, minCyclesBetween: 3, energyCost: 27f),
                // ~23 dishes in three rushes, heavy steel among them; the machine may stop twice.
                CreateShift(ShiftSchedule.HardNight, "Ciężki wieczór", 180f, openingDishes: 3, intervalStart: 15f, intervalEnd: 9f,
                    new[] { plate, plate, plate, pan, pan, pan, pot, pot, roaster, roaster, glass, glass, bowl, mug },
                    breakdownChance: 0.18f, maxBreakdowns: 2, quota: 11, shuffle: true,
                    intro: "Komplet rezerwacji i za mało rąk: trzy naloty, garnki i brytfanny.",
                    waves: new[]
                    {
                        new ServiceWave(0.12f, 0.32f, 0.6f, "Pierwszy nalot!"), new ServiceWave(0.45f, 0.65f, 0.8f, "Drugi nalot!"),
                        new ServiceWave(0.78f, 0.96f, 0.6f, "Ostatnia fala!")
                    },
                    urgentChance: 0.5f, minCyclesBetween: 3, energyCost: 32f)
            };

            var catalog = CreateOrLoad<ShiftCatalog>($"{ResourcesFolder}/{ShiftCatalog.ResourceName}.asset");
            catalog.SetShifts(shifts);
            EditorUtility.SetDirty(catalog);

            CreateCharacter("kuba", "Kuba", "Polska", 2,
                "Filozof - wewnętrzny monolog odblokowuje dodatkowe opcje dialogowe.");

            CreateOrLoad<ShiftCompletedEventChannel>($"{ContentFolder}/ShiftCompletedEventChannel.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Zmywak: content assets created in " + ContentFolder);
        }

        private static DirtTypeDefinition CreateDirtType(string id, string displayName, Color color, float difficulty, float gloss, float relief,
            DirtPattern pattern = DirtPattern.Splotches, bool showsOnlyInReflection = false)
        {
            var asset = CreateOrLoad<DirtTypeDefinition>($"{ContentFolder}/DirtType_{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("placeholderDirtyColor").colorValue = color;
            so.FindProperty("scrubDifficulty").floatValue = difficulty;
            so.FindProperty("pattern").intValue = (int)pattern;
            so.FindProperty("showsOnlyInReflection").boolValue = showsOnlyInReflection;
            so.FindProperty("gloss").floatValue = gloss;
            so.FindProperty("relief").floatValue = relief;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static ToolDefinition CreateTool(string id, string displayName, DirtTypeDefinition[] effectiveAgainst, float cleanPower, float ineffectivePower,
            float brushRadiusScale = 1f, float residueFloor = 0f)
        {
            var asset = CreateOrLoad<ToolDefinition>($"{ContentFolder}/Tool_{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;

            SerializedProperty listProp = so.FindProperty("effectiveAgainst");
            listProp.arraySize = effectiveAgainst.Length;
            for (int i = 0; i < effectiveAgainst.Length; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = effectiveAgainst[i];
            }

            so.FindProperty("cleanPower").floatValue = cleanPower;
            so.FindProperty("ineffectivePower").floatValue = ineffectivePower;
            so.FindProperty("brushRadiusScale").floatValue = brushRadiusScale;
            so.FindProperty("residueFloor").floatValue = residueFloor;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static DishDefinition CreateDish(string id, string displayName, DishShape shape, Vector3 scale, Color cleanColor,
            float smoothness, float metallic, (DirtTypeDefinition type, float coverage)[] layers, Vector2Int rackFootprint, Vector2 rackBodyOffset, float cleanOpacity = 0f,
            DishMaterial material = DishMaterial.Ceramic, float massKg = 0.5f, string leftoversOf = null)
        {
            var asset = CreateOrLoad<DishDefinition>($"{ContentFolder}/Dish_{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("placeholderPrimitive").intValue = (int)PrimitiveType.Cylinder;
            so.FindProperty("shape").intValue = (int)shape;
            so.FindProperty("placeholderScale").vector3Value = scale;
            so.FindProperty("placeholderCleanColor").colorValue = cleanColor;
            so.FindProperty("smoothness").floatValue = smoothness;
            so.FindProperty("metallic").floatValue = metallic;
            so.FindProperty("cleanOpacity").floatValue = cleanOpacity;
            so.FindProperty("rackFootprint").vector2IntValue = rackFootprint;
            so.FindProperty("rackBodyOffset").vector2Value = rackBodyOffset;
            so.FindProperty("material").intValue = (int)material;
            so.FindProperty("massKg").floatValue = massKg;

            SerializedProperty layersProp = so.FindProperty("dirtLayers");
            layersProp.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
            {
                SerializedProperty element = layersProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("dirtType").objectReferenceValue = layers[i].type;
                element.FindPropertyRelative("coverage").floatValue = layers[i].coverage;
            }

            // The food left on it, if Blender has modelled some for this kind of dish (Tools/Blender/leftovers.py).
            BlenderArt.Leftovers(leftoversOf ?? id, out Mesh leftovers, out Texture2D palette);
            so.FindProperty("leftoversMesh").objectReferenceValue = leftovers;
            so.FindProperty("leftoversPalette").objectReferenceValue = palette;

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static ShiftDefinition CreateShift(string id, string displayName, float duration, int openingDishes, float intervalStart,
            float intervalEnd, DishDefinition[] queue, bool repeatQueue = true, int scriptedBreakdownCycle = 0, float breakdownChance = 0f,
            int maxBreakdowns = 1, int quota = 8, bool shuffle = false, DishDefinition[] opening = null, string intro = "",
            ServiceWave[] waves = null, float urgentChance = 0f, int minCyclesBetween = 0, float energyCost = 0f)
        {
            var asset = CreateOrLoad<ShiftDefinition>($"{ContentFolder}/Shift_{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("intro").stringValue = intro;
            so.FindProperty("durationSeconds").floatValue = duration;
            so.FindProperty("quota").intValue = quota;
            so.FindProperty("energyCost").floatValue = energyCost;
            so.FindProperty("openingDishes").intValue = openingDishes;
            so.FindProperty("spawnIntervalStartSeconds").floatValue = intervalStart;
            so.FindProperty("spawnIntervalEndSeconds").floatValue = intervalEnd;
            so.FindProperty("repeatQueue").boolValue = repeatQueue;
            so.FindProperty("shuffleQueue").boolValue = shuffle;
            so.FindProperty("urgentOrderChance").floatValue = urgentChance;
            so.FindProperty("scriptedBreakdownCycle").intValue = scriptedBreakdownCycle;
            so.FindProperty("breakdownChancePerCycle").floatValue = breakdownChance;
            so.FindProperty("maxBreakdowns").intValue = maxBreakdowns;
            so.FindProperty("minCyclesBetweenBreakdowns").intValue = minCyclesBetween;

            SetDishes(so.FindProperty("dishSpawnQueue"), queue);
            SetDishes(so.FindProperty("fixedOpening"), opening ?? new DishDefinition[0]);

            SerializedProperty wavesProp = so.FindProperty("waves");
            waves = waves ?? new ServiceWave[0];
            wavesProp.arraySize = waves.Length;
            for (int i = 0; i < waves.Length; i++)
            {
                SerializedProperty wave = wavesProp.GetArrayElementAtIndex(i);
                wave.FindPropertyRelative("start01").floatValue = waves[i].start01;
                wave.FindPropertyRelative("end01").floatValue = waves[i].end01;
                wave.FindPropertyRelative("intensity").floatValue = waves[i].intensity;
                wave.FindPropertyRelative("callout").stringValue = waves[i].callout ?? string.Empty;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static void SetDishes(SerializedProperty list, DishDefinition[] dishes)
        {
            list.arraySize = dishes.Length;
            for (int i = 0; i < dishes.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = dishes[i];
            }
        }

        private static CharacterDefinition CreateCharacter(string id, string displayName, string origin, int englishLevel, string skill)
        {
            var asset = CreateOrLoad<CharacterDefinition>($"{ContentFolder}/Character_{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("originCountry").stringValue = origin;
            so.FindProperty("englishLevel").intValue = englishLevel;
            so.FindProperty("specialSkillDescription").stringValue = skill;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        // ------------------------------------------------------------------
        // 2. Meta content assets
        // ------------------------------------------------------------------

        /// <summary>
        /// The base look every scene shares (the mood grade from Boot layers on top): filmic-neutral
        /// tonemapping, a light bloom that only catches glossy highlights (HDR values above 1), and a
        /// soft vignette to pull the eye to the middle of a tall phone screen.
        /// </summary>
        private static VolumeProfile CreateLookProfile()
        {
            string path = $"{ContentFolder}/LookVolumeProfile.asset";
            VolumeProfile look = CreateOrLoad<VolumeProfile>(path);

            if (!look.TryGet(out Tonemapping tonemapping))
            {
                tonemapping = look.Add<Tonemapping>(true);
            }

            // ACES: the filmic curve - highlights on steel and water roll off softly instead of clipping, and
            // mid-tones get a photographic contrast. It darkens a little, so exposure is nudged up below.
            tonemapping.mode.Override(TonemappingMode.ACES);

            // Exposure and contrast only: the mood volume owns saturation and hue (MoodColorGradingController).
            if (!look.TryGet(out ColorAdjustments exposure))
            {
                exposure = look.Add<ColorAdjustments>(true);
            }

            exposure.postExposure.Override(0.3f);
            exposure.contrast.Override(8f);

            if (!look.TryGet(out Bloom bloom))
            {
                bloom = look.Add<Bloom>(true);
            }

            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);

            if (!look.TryGet(out Vignette vignette))
            {
                vignette = look.Add<Vignette>(true);
            }

            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.45f);

            PersistProfileComponents(look);
            return look;
        }

        /// <summary>VolumeProfile.Add() only creates the component in memory - it has to be stored as a
        /// sub-asset of the profile file, or it is gone the next time the profile loads.</summary>
        private static void PersistProfileComponents(VolumeProfile profile)
        {
            foreach (VolumeComponent component in profile.components)
            {
                if (!AssetDatabase.Contains(component))
                {
                    AssetDatabase.AddObjectToAsset(component, profile);
                }

                EditorUtility.SetDirty(component);
            }

            EditorUtility.SetDirty(profile);
        }

        private static void CreateLookVolume(Transform parent)
        {
            VolumeProfile look = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{ContentFolder}/LookVolumeProfile.asset");
            var go = new GameObject("LookVolume", typeof(Volume));
            go.transform.SetParent(parent);
            Volume volume = go.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = -1; // under Boot's mood volume
            volume.sharedProfile = look;
        }

        private static void CreateSceneLighting(Transform parent)
        {
            var go = new GameObject("SceneLighting", typeof(SceneLighting));
            go.transform.SetParent(parent);
        }

        [MenuItem("Zmywak/Setup/2. Create Meta Content Assets")]
        public static void CreateMetaContentAssets()
        {
            EnsureFolder(ContentFolder);
            // The National Bank of Poland's yearly averages, written over whatever an older setup left in the asset.
            ExchangeRateTableAsset rates = CreateOrLoad<ExchangeRateTableAsset>($"{ContentFolder}/ExchangeRateTable.asset");
            rates.SetPoints(ExchangeRateTableAsset.NbpYearlyAverages);
            EditorUtility.SetDirty(rates);

            VolumeProfile moodProfile = CreateOrLoad<VolumeProfile>($"{ContentFolder}/MoodVolumeProfile.asset");
            if (!moodProfile.TryGet(out ColorAdjustments _))
            {
                moodProfile.Add<ColorAdjustments>(true);
            }

            PersistProfileComponents(moodProfile);
            CreateLookProfile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Zmywak: meta content assets created.");
        }

        // ------------------------------------------------------------------
        // 3. Narrative content assets
        // ------------------------------------------------------------------

        [MenuItem("Zmywak/Setup/3. Create Narrative Content Assets")]
        public static void CreateNarrativeContentAssets()
        {
            EnsureFolder(ContentFolder);

            CreateSlangEntry("slang_grand", "Grand",
                "\"W porządku\", \"świetnie\" - najbardziej wszechstronne irlandzkie słowo, pasuje niemal wszędzie.");
            CreateSlangEntry("slang_fair_play", "Fair play",
                "Wyraz uznania i szacunku dla czyjegoś wysiłku albo decyzji.");

            CreateSlangEntry("slang_craic", "What's the craic?",
                "\"Co słychać?\", \"co się dzieje?\" - powitanie. Craic to dobra zabawa, plotki i atmosfera naraz.");
            CreateSlangEntry("slang_i_will_yeah", "I will, yeah",
                "Wbrew słowom: \"akurat\", \"po moim trupie\". Ironiczne \"tak\", które znaczy \"nie\".");
            CreateSlangEntry("slang_so", "..., so.",
                "\"So\" na końcu zdania niczego nie dodaje - tylko je zamyka, jak nasze \"no\" albo \"i tyle\".");

            // Chapter 2: the kitchen's and the pub's words.
            CreateSlangEntry("slang_jacks", "The jacks",
                "Toaleta. \"Where's the jacks?\" - nikt w Dublinie nie powie \"toilet\", jeśli może powiedzieć \"jacks\".");
            CreateSlangEntry("slang_eejit", "Eejit",
                "Głupek, gamoń - ale raczej z czułością niż ze złością. Najczęściej o kimś, kto zamknął łazienkę na klucz.");
            CreateSlangEntry("slang_deadly", "Deadly!",
                "\"Super!\", \"ekstra!\". Nikt nie umiera - po dublińsku to najwyższy zachwyt.");
            CreateSlangEntry("slang_sound", "He's sound",
                "\"Porządny z niego człowiek\", \"można na nim polegać\" - jedna z najwyższych ocen, jakie Irlandczyk daje drugiemu.");

            // Chapter 3: the kitchen's pecking order, and what breaks in it.
            CreateSlangEntry("slang_giving_out", "Giving out",
                "Narzekać, robić wyrzuty. \"He's always giving out\" - ciągle się czepia. Nie ma nic wspólnego z rozdawaniem.");
            CreateSlangEntry("slang_your_man", "Your man / your woman",
                "\"Ten gość\", \"ta pani\" - o kimś, kogo obie strony widzą albo kojarzą, nawet bez imienia.");
            CreateSlangEntry("slang_gas", "That's gas!",
                "\"Ale jazda!\", \"przezabawne!\". Gas to śmiech, nie paliwo.");
            CreateSlangEntry("slang_banjaxed", "Banjaxed",
                "Zepsuty, rozwalony, do niczego: zmywarka, telewizor pani Byrne albo poparzona ręka.");

            // Chapter 4: the boom's words.
            CreateSlangEntry("slang_notions", "Having notions",
                "Zadzierać nosa, mieć wygórowane mniemanie o sobie albo zbyt wielkie plany. W czasach Tygrysa - prawie każdy.");
            CreateSlangEntry("slang_gaff", "Gaff",
                "Dom, mieszkanie, chata. \"Come back to my gaff\" - wpadnij do mnie.");
            CreateSlangEntry("slang_go_way", "Go way outta that!",
                "\"Nie gadaj!\", \"niemożliwe!\" - niedowierzanie, zwykle radosne.");
            CreateSlangEntry("slang_begrudgers", "The begrudgers",
                "Zawistnicy: ci, którzy nie znoszą cudzego sukcesu. Po irlandzku prawie instytucja.");

            // Chapter 5: the words of the bust.
            CreateSlangEntry("slang_dole", "On the dole",
                "Na zasiłku dla bezrobotnych. W 2008 roku słowo, które słychać w każdej kolejce.");
            CreateSlangEntry("slang_fierce", "Fierce",
                "Bardzo, strasznie: \"fierce quiet\", \"fierce cold\". Wzmacnia wszystko, nawet ciszę.");
            CreateSlangEntry("slang_brutal", "Brutal",
                "Okropny, beznadziejny. Pogoda, mecz, gospodarka - jesienią 2008 wszystko naraz.");
            CreateSlangEntry("slang_sure_look", "Ah, sure look",
                "\"No cóż, trudno\" - irlandzka zgoda na to, czego nie da się zmienić, i zarazem otucha.");

            Chapter1Content.Create(ContentFolder);
            Chapter2Content.Create(ContentFolder);
            Chapter3Content.Create(ContentFolder);
            Chapter4Content.Create(ContentFolder);
            Chapter5Content.Create(ContentFolder);
            Chapter6Content.Create(ContentFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Zmywak: narrative content assets created.");
        }

        private static SlangEntry CreateSlangEntry(string id, string phrase, string meaning)
        {
            var asset = CreateOrLoad<SlangEntry>($"{ContentFolder}/Slang_{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("phrase").stringValue = phrase;
            so.FindProperty("meaning").stringValue = meaning;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        // ------------------------------------------------------------------
        // 4. Boot scene
        // ------------------------------------------------------------------

        [MenuItem("Zmywak/Setup/4. Build Boot Scene")]
        public static void BuildBootScene()
        {
            EnsureFolder(ScenesFolder);

            // Asset references must be (re)loaded AFTER NewScene(), not before: EditorSceneManager.NewScene
            // silently invalidates the native object behind any UnityEngine.Object reference that was only
            // held by a local variable (not yet referenced from a live scene/component). Loading first and
            // assigning after NewScene() compiles and runs fine with no error, but every such reference
            // serializes as {fileID: 0} once the scene is saved - this bit every Build*Scene method here
            // until it was caught by a PlayMode test loading Kitchen_Shift and finding shiftDefinition null.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ShiftCompletedEventChannel channel = AssetDatabase.LoadAssetAtPath<ShiftCompletedEventChannel>($"{ContentFolder}/ShiftCompletedEventChannel.asset");
            ExchangeRateTableAsset exchangeRateTable = AssetDatabase.LoadAssetAtPath<ExchangeRateTableAsset>($"{ContentFolder}/ExchangeRateTable.asset");
            VolumeProfile moodProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{ContentFolder}/MoodVolumeProfile.asset");

            if (channel == null || exchangeRateTable == null || moodProfile == null)
            {
                Debug.LogError("Zmywak: content assets are missing. Run steps 1 and 2 first.");
                return;
            }

            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<GameStateMachine>();
            bootstrap.AddComponent<NarrativeProgressController>();
            bootstrap.AddComponent<QualityManager>();
            bootstrap.AddComponent<DebugPerformanceOverlay>();

            MetaProgressController progress = bootstrap.AddComponent<MetaProgressController>();
            var progressSo = new SerializedObject(progress);
            progressSo.FindProperty("shiftCompletedChannel").objectReferenceValue = channel;
            progressSo.FindProperty("exchangeRateTable").objectReferenceValue = exchangeRateTable;
            progressSo.ApplyModifiedPropertiesWithoutUndo();

            var moodVolumeGo = new GameObject("MoodVolume", typeof(Volume));
            moodVolumeGo.transform.SetParent(bootstrap.transform);
            Volume moodVolume = moodVolumeGo.GetComponent<Volume>();
            moodVolume.isGlobal = true;
            moodVolume.priority = 0;
            moodVolume.sharedProfile = moodProfile;
            moodVolumeGo.AddComponent<MoodColorGradingController>();

            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Boot.unity");
            Debug.Log("Zmywak: Boot scene built.");
        }

        // ------------------------------------------------------------------
        // 5. Main menu scene
        // ------------------------------------------------------------------

        [MenuItem("Zmywak/Setup/5. Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            EnsureFolder(ScenesFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Behind the title: Kuba's room at night, the camera breathing slowly, looking at the rainy window.
            RoomArt.EnsureAssets();
            var roomRoot = new GameObject("Room");
            Camera roomCamera = RoomArt.Build(roomRoot.transform);
            roomCamera.transform.position = new Vector3(1.3f, 1.45f, -1.75f);
            roomCamera.transform.LookAt(new Vector3(0.15f, 1.3f, 1.6f));
            roomCamera.gameObject.AddComponent<CameraDrift>();
            CreateLookVolume(roomRoot.transform);
            CreateEventSystem();

            Canvas canvas = CreateCanvas("Canvas");
            GameObject scrimGo = CreateUIObject("Scrim", canvas.transform);
            StretchFull(RT(scrimGo));
            Image scrim = scrimGo.AddComponent<Image>();
            scrim.sprite = UiArt.Scrim;
            scrim.raycastTarget = false;
            GameObject safeArea = CreateUIObject("SafeArea", canvas.transform);
            StretchFull(RT(safeArea));
            safeArea.AddComponent<SafeAreaFitter>();

            TextMeshProUGUI title = CreateText("Title", safeArea.transform, "ZMYWAK", 132, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 8f;
            AnchorTopCenter(RT(title.gameObject), new Vector2(1000, 180), new Vector2(0, -170));
            Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            titleShadow.effectDistance = new Vector2(0f, -8f);

            TextMeshProUGUI subtitle = CreateText("Subtitle", safeArea.transform, "Zielona Wyspa", 60, TextAlignmentOptions.Center);
            subtitle.color = new Color(0.45f, 0.85f, 0.55f);
            subtitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
            AnchorTopCenter(RT(subtitle.gameObject), new Vector2(1000, 90), new Vector2(0, -340));

            TextMeshProUGUI tagline = CreateText("Tagline", safeArea.transform, "Brudne talerze. Czyste marzenia.", 38, TextAlignmentOptions.Center);
            tagline.color = new Color(0.9f, 0.88f, 0.82f);
            tagline.fontStyle = FontStyles.Italic;
            // Under the title, away from the buttons: lower down it lay right under where CONTINUE says where the life stands.
            AnchorTopCenter(RT(tagline.gameObject), new Vector2(900, 70), new Vector2(0, -435));

            // CONTINUE first when there is a life to go on with (with where it stands under it); then a new game, the saves and
            // the settings. MainMenuScreen hides CONTINUE and the saves on a first start, when "Rozpocznij grę" is all there is.
            Button continueButton = CreateButton("MenuContinueButton", safeArea.transform, "KONTYNUUJ");
            AnchorCenter(RT(continueButton.gameObject), new Vector2(760, 170), new Vector2(0, -230));
            continueButton.GetComponentInChildren<TMP_Text>().fontSizeMax = 52;
            TextMeshProUGUI continueDetails = CreateText("ContinueDetails", safeArea.transform, string.Empty, 30, TextAlignmentOptions.Center);
            continueDetails.color = new Color(0.9f, 0.88f, 0.82f);
            AnchorCenter(RT(continueDetails.gameObject), new Vector2(940, 56), new Vector2(0, -345));
            Scalable(continueDetails);

            Button newGameButton = CreateButton("NewGameButton", safeArea.transform, "Nowa gra");
            newGameButton.GetComponent<Image>().color = SecondaryButton;
            AnchorCenter(RT(newGameButton.gameObject), new Vector2(560, 140), new Vector2(0, -470));
            Button loadButton = CreateButton("LoadGameButton", safeArea.transform, "Wczytaj zapis");
            loadButton.GetComponent<Image>().color = SecondaryButton;
            AnchorCenter(RT(loadButton.gameObject), new Vector2(560, 140), new Vector2(0, -630));
            Button settingsButton = CreateButton("MenuSettingsButton", safeArea.transform, "Ustawienia");
            settingsButton.GetComponent<Image>().color = SecondaryButton;
            AnchorCenter(RT(settingsButton.gameObject), new Vector2(560, 140), new Vector2(0, -790));

            // The save slots: a card over the menu, a wide button per slot (where that life stands) and a delete beside it.
            GameObject slotsPanel = CreateModal("SaveSlotsPanel", safeArea.transform, new Vector2(980, 1180), "Zapisy");
            TextMeshProUGUI slotsNote = CreateText("SlotsNote", slotsPanel.transform, string.Empty, 28, TextAlignmentOptions.Center);
            slotsNote.color = new Color(0.82f, 0.87f, 0.92f);
            AnchorCenter(RT(slotsNote.gameObject), new Vector2(880, 70), new Vector2(0, 410));
            Scalable(slotsNote);
            var slotButtons = new Button[SaveSystem.SlotCount];
            var deleteButtons = new Button[SaveSystem.SlotCount];
            for (int slot = 0; slot < SaveSystem.SlotCount; slot++)
            {
                float y = 230f - slot * 240f;
                slotButtons[slot] = CreateButton($"SlotButton{slot + 1}", slotsPanel.transform, $"Zapis {slot + 1}");
                AnchorCenter(RT(slotButtons[slot].gameObject), new Vector2(680, 190), new Vector2(-115, y));
                deleteButtons[slot] = CreateButton($"DeleteSlotButton{slot + 1}", slotsPanel.transform, "Usuń");
                deleteButtons[slot].GetComponent<Image>().color = new Color(0.45f, 0.16f, 0.14f);
                AnchorCenter(RT(deleteButtons[slot].gameObject), new Vector2(190, 140), new Vector2(345, y));
            }

            Button closeSlotsButton = CreateButton("CloseSlotsButton", slotsPanel.transform, "Wróć");
            AnchorCenter(RT(closeSlotsButton.gameObject), new Vector2(360, 130), new Vector2(0, -480));
            slotsPanel.SetActive(false);

            GameObject settingsPanel = BuildSettingsPanel(safeArea.transform);
            GameObject quitPanel = BuildConfirm("QuitPanel", safeArea.transform, "Wyjść z gry?",
                "Wszystko jest zapisane. Kuba poczeka na ciebie przy zlewie.", "Wyjdź", "Zostań", out Button quitYes, out Button quitNo, out _);

            GameObject controller = CreateUIObject("MainMenuController", canvas.transform);
            MainMenuScreen menuScreen = controller.AddComponent<MainMenuScreen>();
            var so = new SerializedObject(menuScreen);
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.FindProperty("continueDetails").objectReferenceValue = continueDetails;
            so.FindProperty("newGameButton").objectReferenceValue = newGameButton;
            so.FindProperty("loadButton").objectReferenceValue = loadButton;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("slotsPanel").objectReferenceValue = slotsPanel;
            so.FindProperty("slotsNote").objectReferenceValue = slotsNote;
            so.FindProperty("closeSlotsButton").objectReferenceValue = closeSlotsButton;
            so.FindProperty("quitPanel").objectReferenceValue = quitPanel;
            so.FindProperty("quitYesButton").objectReferenceValue = quitYes;
            so.FindProperty("quitNoButton").objectReferenceValue = quitNo;
            SerializedProperty slotsProp = so.FindProperty("slotButtons");
            SerializedProperty deletesProp = so.FindProperty("deleteButtons");
            slotsProp.arraySize = SaveSystem.SlotCount;
            deletesProp.arraySize = SaveSystem.SlotCount;
            for (int slot = 0; slot < SaveSystem.SlotCount; slot++)
            {
                slotsProp.GetArrayElementAtIndex(slot).objectReferenceValue = slotButtons[slot];
                deletesProp.GetArrayElementAtIndex(slot).objectReferenceValue = deleteButtons[slot];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // The Android back gesture closes the card on top (the question, the settings, the saves); with none open it asks
            // whether to leave the game.
            BackClosesPanels menuBack = canvas.gameObject.AddComponent<BackClosesPanels>();
            var menuBackSo = new SerializedObject(menuBack);
            SerializedProperty menuBackPanels = menuBackSo.FindProperty("panels");
            menuBackPanels.arraySize = 3;
            menuBackPanels.GetArrayElementAtIndex(0).objectReferenceValue = quitPanel;
            menuBackPanels.GetArrayElementAtIndex(1).objectReferenceValue = settingsPanel;
            menuBackPanels.GetArrayElementAtIndex(2).objectReferenceValue = slotsPanel;
            menuBackSo.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(menuBack.OnNothingOpenEvent, menuScreen.AskToQuit);

            BakedLighting.PrepareRoom(scene);
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/MainMenu.unity");
            BakedLighting.Bake(scene);
            Debug.Log("Zmywak: MainMenu scene built.");
        }

        // ------------------------------------------------------------------
        // 6. Meta hub scene
        // ------------------------------------------------------------------

        [MenuItem("Zmywak/Setup/6. Build Meta Hub Scene")]
        public static void BuildMetaHubScene()
        {
            EnsureFolder(ScenesFolder);

            // See the comment in BuildBootScene: assets must be loaded AFTER NewScene(), not before.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            StoryChapter chapter1 = AssetDatabase.LoadAssetAtPath<StoryChapter>($"{ContentFolder}/{Chapter1Content.ChapterAssetName}");
            StoryChapter chapter2 = AssetDatabase.LoadAssetAtPath<StoryChapter>($"{ContentFolder}/{Chapter2Content.ChapterAssetName}");
            StoryChapter chapter3 = AssetDatabase.LoadAssetAtPath<StoryChapter>($"{ContentFolder}/{Chapter3Content.ChapterAssetName}");
            StoryChapter chapter4 = AssetDatabase.LoadAssetAtPath<StoryChapter>($"{ContentFolder}/{Chapter4Content.ChapterAssetName}");
            StoryChapter chapter5 = AssetDatabase.LoadAssetAtPath<StoryChapter>($"{ContentFolder}/{Chapter5Content.ChapterAssetName}");
            StoryChapter epilogue = AssetDatabase.LoadAssetAtPath<StoryChapter>($"{ContentFolder}/{Chapter6Content.ChapterAssetName}");
            var allSlang = new List<SlangEntry>();
            foreach (string guid in AssetDatabase.FindAssets("t:SlangEntry", new[] { ContentFolder }))
            {
                allSlang.Add(AssetDatabase.LoadAssetAtPath<SlangEntry>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            if (chapter1 == null || chapter2 == null || chapter3 == null || chapter4 == null || chapter5 == null || epilogue == null || allSlang.Count == 0)
            {
                Debug.LogError("Zmywak: narrative content assets are missing. Run 'Zmywak/Setup/3. Create Narrative Content Assets' first.");
                return;
            }

            // The backdrop: Kuba's room in 3D, lit, reflected and graded like the kitchen - the cards sit over it.
            RoomArt.EnsureAssets();
            var roomRoot = new GameObject("Room");
            RoomArt.Build(roomRoot.transform);
            CreateLookVolume(roomRoot.transform);
            CreateEventSystem();

            Canvas canvas = CreateCanvas("Canvas");
            GameObject safeArea = CreateUIObject("SafeArea", canvas.transform);
            StretchFull(RT(safeArea));
            safeArea.AddComponent<SafeAreaFitter>();

            // --- Header: day + wallet on a dark card whose top edge sits off-screen. ---
            Image header = CreatePanel("Header", safeArea.transform, PanelDark);
            RectTransform headerRect = RT(header.gameObject);
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(-24f, 170f);
            headerRect.anchoredPosition = new Vector2(0f, 40f);

            TextMeshProUGUI dayText = CreateText("DayText", safeArea.transform, "Dzień 1", 40, TextAlignmentOptions.MidlineLeft);
            dayText.fontStyle = FontStyles.Bold;
            AnchorTopLeft(RT(dayText.gameObject), new Vector2(520, 90), new Vector2(40, -20)); // clear of the wallet beside it (x 580-1040)
            dayText.textWrappingMode = TextWrappingModes.NoWrap; // the day, the date and the weather on one line: it shrinks to fit
            Scalable(dayText);

            TextMeshProUGUI walletText = CreateText("WalletText", safeArea.transform, "Portfel: 0 EUR", 40, TextAlignmentOptions.MidlineRight);
            walletText.color = new Color(1f, 0.85f, 0.35f);
            walletText.fontStyle = FontStyles.Bold;
            AnchorTopRight(RT(walletText.gameObject), new Vector2(460, 90), new Vector2(-40, -20));
            Scalable(walletText);

            // --- Stats card: a coloured bar per stat. ---
            Image statsCard = CreatePanel("StatsCard", safeArea.transform, PanelDark);
            AnchorTopCenter(RT(statsCard.gameObject), new Vector2(1000, 250), new Vector2(0, -160));
            string[] statNames = { "Energia", "Tęsknota", "Integracja" };
            Color[] statColors = { new Color(0.4f, 0.85f, 0.45f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.65f, 0.25f) };
            var statLabels = new TextMeshProUGUI[3];
            var statFills = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float rowY = -38 - i * 70;
                TextMeshProUGUI label = CreateText($"{statNames[i]}Label", statsCard.transform, statNames[i], 32, TextAlignmentOptions.MidlineLeft);
                AnchorTopLeft(RT(label.gameObject), new Vector2(330, 56), new Vector2(34, rowY));
                Scalable(label);

                Image track = CreatePanel($"{statNames[i]}Track", statsCard.transform, new Color(1f, 1f, 1f, 0.08f));
                AnchorTopLeft(RT(track.gameObject), new Vector2(600, 34), new Vector2(370, rowY - 11));
                Image fill = CreatePanel("Fill", track.transform, statColors[i]);
                StretchFull(RT(fill.gameObject));
                statLabels[i] = label;
                statFills[i] = fill;
            }

            // How Kuba is tonight, in a line under the bars (MetaHubScreen: tired, homesick, one of the crew...).
            TextMeshProUGUI moodText = CreateText("MoodText", safeArea.transform, string.Empty, 28, TextAlignmentOptions.Center);
            moodText.fontStyle = FontStyles.Italic;
            moodText.color = new Color(0.9f, 0.88f, 0.82f);
            AnchorTopCenter(RT(moodText.gameObject), new Vector2(1000, 64), new Vector2(0, -414));
            Scalable(moodText);

            // --- Secondary buttons: four across under the stats - the slang dictionary, the map, the shop, the menu. ---
            Color secondary = new Color(0.22f, 0.3f, 0.38f);
            Button slangToggleButton = CreateButton("SlangToggleButton", safeArea.transform, "Słownik");
            slangToggleButton.GetComponent<Image>().color = secondary;
            AnchorTopLeft(RT(slangToggleButton.gameObject), new Vector2(235, 130), new Vector2(40, -486));

            Button mapToggleButton = CreateButton("MapToggleButton", safeArea.transform, "Mapa");
            mapToggleButton.GetComponent<Image>().color = secondary;
            AnchorTopLeft(RT(mapToggleButton.gameObject), new Vector2(235, 130), new Vector2(295, -486));

            Button shopToggleButton = CreateButton("ShopToggleButton", safeArea.transform, "Zakupy");
            shopToggleButton.GetComponent<Image>().color = secondary;
            AnchorTopLeft(RT(shopToggleButton.gameObject), new Vector2(235, 130), new Vector2(550, -486));

            Button menuToggleButton = CreateButton("HubMenuButton", safeArea.transform, "Menu");
            menuToggleButton.GetComponent<Image>().color = secondary;
            AnchorTopRight(RT(menuToggleButton.gameObject), new Vector2(235, 130), new Vector2(-40, -486));

            // The notice sits over the 3D room: on a dark pill of its own, shown only while there is something to say.
            Image noticeBackdrop = CreatePanel("NoticeBackdrop", safeArea.transform, new Color(0.05f, 0.06f, 0.07f, 0.82f));
            AnchorCenter(RT(noticeBackdrop.gameObject), new Vector2(980, 140), new Vector2(0, -230));
            noticeBackdrop.raycastTarget = false;
            noticeBackdrop.gameObject.SetActive(false);
            TextMeshProUGUI noticeText = CreateText("NoticeText", safeArea.transform, string.Empty, 32, TextAlignmentOptions.Center);
            noticeText.color = new Color(1f, 0.85f, 0.3f);
            noticeText.fontStyle = FontStyles.Bold;
            AnchorCenter(RT(noticeText.gameObject), new Vector2(940, 130), new Vector2(0, -230)); // clear of the evening's buttons below
            Scalable(noticeText);

            // --- The evening: one plan for it (rest, a call home, the pub - or the crew's dinner, a letter, when on offer), money
            // home, and going to work - the main one, wide and green, at the bottom where the thumb is. ---
            GameObject actions = CreateUIObject("Actions", safeArea.transform);
            RectTransform actionsRect = RT(actions);
            actionsRect.anchorMin = new Vector2(0.5f, 0f);
            actionsRect.anchorMax = new Vector2(0.5f, 0f);
            actionsRect.pivot = new Vector2(0.5f, 0f);
            actionsRect.sizeDelta = new Vector2(1000, 580);
            actionsRect.anchoredPosition = new Vector2(0, 30);

            TextMeshProUGUI eveningLabel = CreateText("EveningLabel", actions.transform, "Wieczór - wybierz jedno:", 30, TextAlignmentOptions.MidlineLeft);
            eveningLabel.color = new Color(1f, 0.85f, 0.35f);
            eveningLabel.fontStyle = FontStyles.Bold;
            AnchorBottomLeft(RT(eveningLabel.gameObject), new Vector2(1000, 56), new Vector2(0, 520));
            Scalable(eveningLabel);

            Button restButton = CreateButton("RestButton", actions.transform, "Odpocznij");
            restButton.GetComponent<Image>().color = secondary;
            AnchorBottomLeft(RT(restButton.gameObject), new Vector2(320, 150), new Vector2(0, 360));

            Button callMomButton = CreateButton("CallMomButton", actions.transform, "Zadzwoń do domu");
            callMomButton.GetComponent<Image>().color = secondary;
            AnchorBottomLeft(RT(callMomButton.gameObject), new Vector2(320, 150), new Vector2(340, 360));

            // MetaHubScreen relabels these from LifeRules at runtime; the same text here keeps the scene honest.
            Button pubButton = CreateButton("PubButton", actions.transform, $"Pub z ekipą (-{LifeRules.Default.PubCost:0} EUR)");
            pubButton.GetComponent<Image>().color = secondary;
            AnchorBottomLeft(RT(pubButton.gameObject), new Vector2(320, 150), new Vector2(680, 360));

            // The crew's dinner and the letter share a slot: only one is ever on offer.
            Button dinnerButton = CreateButton("CrewDinnerButton", actions.transform, "Kolacja z ekipą");
            dinnerButton.GetComponent<Image>().color = new Color(0.36f, 0.26f, 0.14f);
            AnchorBottomLeft(RT(dinnerButton.gameObject), new Vector2(490, 140), new Vector2(0, 200));
            dinnerButton.gameObject.SetActive(false);
            Button letterButton = CreateButton("LetterButton", actions.transform, "Napisz list do domu");
            letterButton.GetComponent<Image>().color = new Color(0.2f, 0.28f, 0.45f);
            AnchorBottomLeft(RT(letterButton.gameObject), new Vector2(490, 140), new Vector2(0, 200));
            letterButton.gameObject.SetActive(false);

            Button sendMoneyButton = CreateButton("SendMoneyButton", actions.transform, $"Wyślij {LifeRules.Default.SendHomeStep:0} EUR do domu");
            sendMoneyButton.GetComponent<Image>().color = secondary;
            AnchorBottomLeft(RT(sendMoneyButton.gameObject), new Vector2(490, 140), new Vector2(510, 200));

            Button goToWorkButton = CreateButton("GoToWorkButton", actions.transform, "Idź do pracy");
            AnchorBottomLeft(RT(goToWorkButton.gameObject), new Vector2(1000, 180), new Vector2(0, 0));

            // --- Slang collection panel: a card over the dimmed hub. ---
            GameObject slangPanel = CreateModal("SlangPanel", safeArea.transform, new Vector2(960, 1100), "Słownik slangu");
            TextMeshProUGUI slangListText = CreateText("SlangListText", slangPanel.transform, string.Empty, 32, TextAlignmentOptions.TopLeft);
            AnchorCenter(RT(slangListText.gameObject), new Vector2(860, 760), new Vector2(0, 20));

            Button closeSlangButton = CreateButton("CloseSlangButton", slangPanel.transform, "Zamknij");
            AnchorCenter(RT(closeSlangButton.gameObject), new Vector2(360, 130), new Vector2(0, -440));
            // Persistent listener: a lambda added here in the Editor is not saved with the scene, so the
            // button did nothing in the game.
            UnityEventTools.AddBoolPersistentListener(closeSlangButton.onClick, slangPanel.SetActive, false);

            // --- The map of the city: a card with the painted map and the pins (CityMapPanel makes them). ---
            GameObject mapPanel = CreateModal("MapPanel", safeArea.transform, new Vector2(980, 1500), "Dublin");
            GameObject mapFrame = CreateUIObject("MapFrame", mapPanel.transform);
            AnchorCenter(RT(mapFrame), new Vector2(UiArt.MapWidth * 1.5f, UiArt.MapHeight * 1.5f), new Vector2(0, 0));
            Image mapImage = mapFrame.AddComponent<Image>();
            mapImage.sprite = UiArt.MapOf(Place.Dublin);
            mapImage.raycastTarget = false;
            GameObject mapPins = CreateUIObject("Pins", mapFrame.transform);
            StretchFull(RT(mapPins));
            Button closeMapButton = CreateButton("CloseMapButton", mapPanel.transform, "Zamknij");
            AnchorCenter(RT(closeMapButton.gameObject), new Vector2(360, 130), new Vector2(0, -675));
            CityMapPanel cityMap = CreateUIObject("CityMap", safeArea.transform).AddComponent<CityMapPanel>(); // stays active: it opens the card
            var mapSo = new SerializedObject(cityMap);
            mapSo.FindProperty("panel").objectReferenceValue = mapPanel;
            mapSo.FindProperty("openButton").objectReferenceValue = mapToggleButton;
            mapSo.FindProperty("closeButton").objectReferenceValue = closeMapButton;
            mapSo.FindProperty("title").objectReferenceValue = mapPanel.transform.Find("Title").GetComponent<TMP_Text>();
            mapSo.FindProperty("map").objectReferenceValue = mapImage;
            mapSo.FindProperty("pins").objectReferenceValue = RT(mapPins);
            mapSo.FindProperty("dublin").objectReferenceValue = UiArt.MapOf(Place.Dublin);
            mapSo.FindProperty("krakow").objectReferenceValue = UiArt.MapOf(Place.Krakow);
            mapSo.FindProperty("pilbara").objectReferenceValue = UiArt.MapOf(Place.Pilbara);
            mapSo.FindProperty("pinSprite").objectReferenceValue = UiArt.Circle;
            mapSo.ApplyModifiedPropertiesWithoutUndo();
            mapPanel.SetActive(false);

            // --- The shop, the evening's menu and the settings (opened from the menu), stacking in that order. ---
            GameObject shopPanel = BuildShop(safeArea.transform, out Button[] buyButtons, out TMP_Text[] buyNotes, out _);
            UnityEventTools.AddBoolPersistentListener(shopToggleButton.onClick, shopPanel.SetActive, true);
            GameObject menuPanel = BuildHubMenu(safeArea.transform, out Button menuResume, out Button menuSettings, out Button menuMainMenu,
                out TMP_Text menuNote);
            GameObject settingsPanel = BuildSettingsPanel(safeArea.transform);

            // --- Dialogue (Chapter 1 intro plays here on first visit): a visual-novel style box at the
            // bottom of the dimmed hub, speaker name in a pill on its top edge. ---
            GameObject dialoguePanel = CreateUIObject("DialoguePanel", safeArea.transform);
            StretchFull(RT(dialoguePanel));
            Image dialogueBg = dialoguePanel.AddComponent<Image>();
            dialogueBg.color = new Color(0f, 0f, 0f, 0.72f);

            // The speaker's clay bust (Tools/Blender/portraits.py), on the right above the box and turned towards the
            // text; made before the box, so the box hides where the bust is cut off.
            Image portrait = CreateUIObject("Portrait", dialoguePanel.transform).AddComponent<Image>();
            portrait.raycastTarget = false;
            portrait.preserveAspect = true;
            RectTransform portraitRect = RT(portrait.gameObject);
            portraitRect.anchorMin = new Vector2(0.5f, 0f);
            portraitRect.anchorMax = new Vector2(0.5f, 0f);
            portraitRect.pivot = new Vector2(0.5f, 0f);
            portraitRect.sizeDelta = new Vector2(540, 540);
            portraitRect.anchoredPosition = new Vector2(235, 60 + 860 - 110);

            Image dialogueBox = CreatePanel("DialogueBox", dialoguePanel.transform, new Color(0.09f, 0.1f, 0.11f, 0.97f));
            RectTransform boxRect = RT(dialogueBox.gameObject);
            boxRect.anchorMin = new Vector2(0.5f, 0f);
            boxRect.anchorMax = new Vector2(0.5f, 0f);
            boxRect.pivot = new Vector2(0.5f, 0f);
            boxRect.sizeDelta = new Vector2(1010, 860);
            boxRect.anchoredPosition = new Vector2(0, 60);
            Shadow boxShadow = dialogueBox.gameObject.AddComponent<Shadow>();
            boxShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            boxShadow.effectDistance = new Vector2(0f, -10f);
            CanvasGroup boxGroup = dialogueBox.gameObject.AddComponent<CanvasGroup>();

            Image speakerPill = CreatePanel("SpeakerPill", dialogueBox.transform, new Color(1f, 0.82f, 0.25f));
            AnchorTopLeft(RT(speakerPill.gameObject), new Vector2(420, 76), new Vector2(40, 38));
            TextMeshProUGUI speakerText = CreateText("SpeakerText", speakerPill.transform, string.Empty, 34, TextAlignmentOptions.Center);
            speakerText.color = new Color(0.16f, 0.1f, 0.04f);
            speakerText.fontStyle = FontStyles.Bold;
            StretchFull(RT(speakerText.gameObject));

            TextMeshProUGUI lineText = CreateText("LineText", dialogueBox.transform, string.Empty, 40, TextAlignmentOptions.TopLeft);
            AnchorTopCenter(RT(lineText.gameObject), new Vector2(920, 300), new Vector2(0, -74));
            lineText.lineSpacing = 6f;
            Scalable(lineText);

            // The scene so far, for whoever tapped through a line too fast: bottom left, opposite "Dalej".
            Button historyButton = CreateButton("HistoryButton", dialogueBox.transform, "Historia");
            historyButton.GetComponent<Image>().color = secondary;
            RectTransform historyRect = RT(historyButton.gameObject);
            historyRect.anchorMin = historyRect.anchorMax = historyRect.pivot = Vector2.zero;
            historyRect.sizeDelta = new Vector2(300, 130);
            historyRect.anchoredPosition = new Vector2(40, 40);

            // Plain Latin-1 only: "▸" (U+25B8) is not in LiberationSans and rendered as a missing-glyph box.
            Button continueButton = CreateButton("ContinueButton", dialogueBox.transform, "Dalej »");
            RectTransform continueRect = RT(continueButton.gameObject);
            continueRect.anchorMin = new Vector2(1f, 0f);
            continueRect.anchorMax = new Vector2(1f, 0f);
            continueRect.pivot = new Vector2(1f, 0f);
            continueRect.sizeDelta = new Vector2(360, 130);
            continueRect.anchoredPosition = new Vector2(-40, 40);

            GameObject choicesPanel = CreateUIObject("ChoicesPanel", dialogueBox.transform);
            RectTransform choicesRect = RT(choicesPanel);
            choicesRect.anchorMin = new Vector2(0.5f, 0f);
            choicesRect.anchorMax = new Vector2(0.5f, 0f);
            choicesRect.pivot = new Vector2(0.5f, 0f);
            choicesRect.sizeDelta = new Vector2(920, 410);
            choicesRect.anchoredPosition = new Vector2(0, 36);

            const int choiceSlotCount = 3;
            var choiceButtons = new Button[choiceSlotCount];
            var choiceTexts = new TextMeshProUGUI[choiceSlotCount];
            for (int i = 0; i < choiceSlotCount; i++)
            {
                Button choiceButton = CreateButton($"Choice_{i}", choicesPanel.transform, string.Empty);
                choiceButton.GetComponent<Image>().color = secondary;
                RectTransform choiceRect = RT(choiceButton.gameObject);
                choiceRect.anchorMin = new Vector2(0.5f, 1f);
                choiceRect.anchorMax = new Vector2(0.5f, 1f);
                choiceRect.pivot = new Vector2(0.5f, 1f);
                choiceRect.sizeDelta = new Vector2(900, 130); // 48 dp even on small, dense screens
                choiceRect.anchoredPosition = new Vector2(0, -i * 140);
                choiceButtons[i] = choiceButton;
                choiceTexts[i] = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
                choiceTexts[i].fontSizeMax = 32;
            }

            // The history: a card over the scene with the lines said so far, scrolling.
            GameObject historyPanel = CreateModal("HistoryPanel", dialoguePanel.transform, new Vector2(980, 1500), "Historia rozmowy");
            GameObject viewport = CreateUIObject("Viewport", historyPanel.transform);
            AnchorCenter(RT(viewport), new Vector2(880, 1160), new Vector2(0, 20));
            viewport.AddComponent<RectMask2D>();
            Image viewportHit = viewport.AddComponent<Image>();
            viewportHit.color = new Color(1f, 1f, 1f, 0f);
            TextMeshProUGUI historyText = CreateText("HistoryText", viewport.transform, string.Empty, 32, TextAlignmentOptions.TopLeft);
            RectTransform historyTextRect = RT(historyText.gameObject);
            historyTextRect.anchorMin = new Vector2(0f, 1f);
            historyTextRect.anchorMax = new Vector2(1f, 1f);
            historyTextRect.pivot = new Vector2(0.5f, 1f);
            historyTextRect.sizeDelta = new Vector2(0f, 1160f);
            historyText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.content = historyTextRect;
            scroll.horizontal = false;
            scroll.viewport = RT(viewport);
            scroll.movementType = ScrollRect.MovementType.Clamped;
            Button closeHistory = CreateButton("CloseHistoryButton", historyPanel.transform, "Wróć do rozmowy");
            AnchorCenter(RT(closeHistory.gameObject), new Vector2(440, 130), new Vector2(0, -660));
            historyPanel.SetActive(false);

            GameObject runnerGo = CreateUIObject("DialogueRunner", canvas.transform);
            DialogueRunner dialogueRunner = runnerGo.AddComponent<DialogueRunner>();

            DialogueScreen dialogueScreen = dialoguePanel.AddComponent<DialogueScreen>();
            var dialogueScreenSo = new SerializedObject(dialogueScreen);
            dialogueScreenSo.FindProperty("runner").objectReferenceValue = dialogueRunner;
            dialogueScreenSo.FindProperty("speakerText").objectReferenceValue = speakerText;
            dialogueScreenSo.FindProperty("speakerTag").objectReferenceValue = speakerPill.gameObject;
            dialogueScreenSo.FindProperty("lineText").objectReferenceValue = lineText;
            dialogueScreenSo.FindProperty("continueButton").objectReferenceValue = continueButton;
            dialogueScreenSo.FindProperty("choicesPanel").objectReferenceValue = choicesPanel;
            SerializedProperty choiceButtonsProp = dialogueScreenSo.FindProperty("choiceButtons");
            choiceButtonsProp.arraySize = choiceSlotCount;
            SerializedProperty choiceTextsProp = dialogueScreenSo.FindProperty("choiceButtonTexts");
            choiceTextsProp.arraySize = choiceSlotCount;
            for (int i = 0; i < choiceSlotCount; i++)
            {
                choiceButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceButtons[i];
                choiceTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = choiceTexts[i];
            }

            dialogueScreenSo.FindProperty("portrait").objectReferenceValue = portrait;
            dialogueScreenSo.FindProperty("dialogueBox").objectReferenceValue = boxRect;
            dialogueScreenSo.FindProperty("boxGroup").objectReferenceValue = boxGroup;
            dialogueScreenSo.FindProperty("historyButton").objectReferenceValue = historyButton;
            dialogueScreenSo.FindProperty("historyPanel").objectReferenceValue = historyPanel;
            dialogueScreenSo.FindProperty("historyText").objectReferenceValue = historyText;
            dialogueScreenSo.FindProperty("closeHistoryButton").objectReferenceValue = closeHistory;
            SerializedProperty portraitsProp = dialogueScreenSo.FindProperty("portraits");
            var portraitSprites = new List<KeyValuePair<string, Sprite>>(PortraitArt.Load());
            portraitSprites.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key)); // the same scene file every run
            portraitsProp.arraySize = portraitSprites.Count;
            for (int i = 0; i < portraitSprites.Count; i++)
            {
                SerializedProperty entry = portraitsProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("speakerId").stringValue = portraitSprites[i].Key;
                entry.FindPropertyRelative("sprite").objectReferenceValue = portraitSprites[i].Value;
            }

            dialogueScreenSo.ApplyModifiedPropertiesWithoutUndo();

            dialoguePanel.SetActive(false);
            slangPanel.SetActive(false);

            GameObject controller = CreateUIObject("MetaHubController", canvas.transform);
            MetaHubScreen hubScreen = controller.AddComponent<MetaHubScreen>();
            var hubSo = new SerializedObject(hubScreen);
            hubSo.FindProperty("dayText").objectReferenceValue = dayText;
            hubSo.FindProperty("walletText").objectReferenceValue = walletText;
            hubSo.FindProperty("noticeBackdrop").objectReferenceValue = noticeBackdrop.gameObject;
            hubSo.FindProperty("energyLabel").objectReferenceValue = statLabels[0];
            hubSo.FindProperty("energyFill").objectReferenceValue = statFills[0];
            hubSo.FindProperty("homesicknessLabel").objectReferenceValue = statLabels[1];
            hubSo.FindProperty("homesicknessFill").objectReferenceValue = statFills[1];
            hubSo.FindProperty("integrationLabel").objectReferenceValue = statLabels[2];
            hubSo.FindProperty("integrationFill").objectReferenceValue = statFills[2];
            hubSo.FindProperty("noticeText").objectReferenceValue = noticeText;
            hubSo.FindProperty("goToWorkButton").objectReferenceValue = goToWorkButton;
            hubSo.FindProperty("cityMap").objectReferenceValue = cityMap;
            hubSo.FindProperty("callMomButton").objectReferenceValue = callMomButton;
            hubSo.FindProperty("pubButton").objectReferenceValue = pubButton;
            hubSo.FindProperty("sendMoneyButton").objectReferenceValue = sendMoneyButton;
            hubSo.FindProperty("slangToggleButton").objectReferenceValue = slangToggleButton;
            hubSo.FindProperty("slangPanel").objectReferenceValue = slangPanel;
            hubSo.FindProperty("slangListText").objectReferenceValue = slangListText;
            hubSo.FindProperty("moodText").objectReferenceValue = moodText;
            hubSo.FindProperty("eveningLabel").objectReferenceValue = eveningLabel;
            hubSo.FindProperty("restButton").objectReferenceValue = restButton;
            hubSo.FindProperty("dinnerButton").objectReferenceValue = dinnerButton;
            hubSo.FindProperty("letterButton").objectReferenceValue = letterButton;
            hubSo.FindProperty("shopPanel").objectReferenceValue = shopPanel;
            SetArray(hubSo.FindProperty("buyButtons"), buyButtons);
            SetArray(hubSo.FindProperty("buyNotes"), buyNotes);
            hubSo.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            hubSo.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            hubSo.FindProperty("menuResumeButton").objectReferenceValue = menuResume;
            hubSo.FindProperty("menuSettingsButton").objectReferenceValue = menuSettings;
            hubSo.FindProperty("menuMainMenuButton").objectReferenceValue = menuMainMenu;
            hubSo.FindProperty("menuNote").objectReferenceValue = menuNote;
            SerializedProperty allSlangProp = hubSo.FindProperty("allSlangEntries");
            allSlangProp.arraySize = allSlang.Count;
            for (int i = 0; i < allSlang.Count; i++)
            {
                allSlangProp.GetArrayElementAtIndex(i).objectReferenceValue = allSlang[i];
            }
            hubSo.ApplyModifiedPropertiesWithoutUndo();

            // The chapter Kuba is in, under the day in the header; the story scene due today plays over the hub.
            TextMeshProUGUI chapterText = CreateText("ChapterText", safeArea.transform, string.Empty, 26, TextAlignmentOptions.MidlineLeft);
            chapterText.color = new Color(1f, 0.85f, 0.35f, 0.9f);
            AnchorTopLeft(RT(chapterText.gameObject), new Vector2(620, 36), new Vector2(42, -94));
            // Made last, it would be drawn over every modal; next to the day it sits under their backdrop like the rest of the header.
            chapterText.transform.SetSiblingIndex(dayText.transform.GetSiblingIndex() + 1);

            GameObject storyTriggerGo = CreateUIObject("StoryBeatTrigger", canvas.transform);
            StoryBeatTrigger storyTrigger = storyTriggerGo.AddComponent<StoryBeatTrigger>();
            var storyTriggerSo = new SerializedObject(storyTrigger);
            SerializedProperty chapters = storyTriggerSo.FindProperty("chapters");
            chapters.arraySize = 6; // in the order they are told
            chapters.GetArrayElementAtIndex(0).objectReferenceValue = chapter1;
            chapters.GetArrayElementAtIndex(1).objectReferenceValue = chapter2;
            chapters.GetArrayElementAtIndex(2).objectReferenceValue = chapter3;
            chapters.GetArrayElementAtIndex(3).objectReferenceValue = chapter4;
            chapters.GetArrayElementAtIndex(4).objectReferenceValue = chapter5;
            chapters.GetArrayElementAtIndex(5).objectReferenceValue = epilogue;
            storyTriggerSo.FindProperty("dialogueRunner").objectReferenceValue = dialogueRunner;
            storyTriggerSo.FindProperty("hubActionsPanel").objectReferenceValue = actions;
            storyTriggerSo.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
            storyTriggerSo.FindProperty("chapterText").objectReferenceValue = chapterText;
            storyTriggerSo.ApplyModifiedPropertiesWithoutUndo();
            var hubStorySo = new SerializedObject(hubScreen);
            hubStorySo.FindProperty("story").objectReferenceValue = storyTrigger;
            hubStorySo.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddVoidPersistentListener(menuToggleButton.onClick, hubScreen.OpenMenu);

            // The Android back gesture closes whichever panel is on top. They stack in the order they are made above: the
            // slang list, the map card, the shop, the menu, settings, the scene's history - so topmost first is the history,
            // settings, the menu, the shop, the map, the slang list. With none open, back opens the evening's menu (the way to
            // the main menu): never nothing.
            BackClosesPanels back = canvas.gameObject.AddComponent<BackClosesPanels>();
            var backSo = new SerializedObject(back);
            SerializedProperty backPanels = backSo.FindProperty("panels");
            backPanels.arraySize = 6;
            backPanels.GetArrayElementAtIndex(0).objectReferenceValue = historyPanel;
            backPanels.GetArrayElementAtIndex(1).objectReferenceValue = settingsPanel;
            backPanels.GetArrayElementAtIndex(2).objectReferenceValue = menuPanel;
            backPanels.GetArrayElementAtIndex(3).objectReferenceValue = shopPanel;
            backPanels.GetArrayElementAtIndex(4).objectReferenceValue = mapPanel;
            backPanels.GetArrayElementAtIndex(5).objectReferenceValue = slangPanel;
            backSo.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(back.OnNothingOpenEvent, hubScreen.OpenMenu);

            BakedLighting.PrepareRoom(scene);
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/MetaHub.unity");
            BakedLighting.Bake(scene);
            Debug.Log("Zmywak: MetaHub scene built.");
        }

        // ------------------------------------------------------------------
        // 7. Kitchen shift scene
        // ------------------------------------------------------------------

        [MenuItem("Zmywak/Setup/7. Build Kitchen Shift Scene")]
        public static void BuildKitchenShiftScene()
        {
            EnsureFolder(ScenesFolder);

            // See the comment in BuildBootScene: assets must be loaded AFTER NewScene(), not before.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ShiftDefinition shiftDef = AssetDatabase.LoadAssetAtPath<ShiftDefinition>($"{ContentFolder}/Shift_vertical_slice.asset");
            ShiftCompletedEventChannel channel = AssetDatabase.LoadAssetAtPath<ShiftCompletedEventChannel>($"{ContentFolder}/ShiftCompletedEventChannel.asset");
            ToolDefinition soak = AssetDatabase.LoadAssetAtPath<ToolDefinition>($"{ContentFolder}/Tool_soak.asset");
            ToolDefinition sponge = AssetDatabase.LoadAssetAtPath<ToolDefinition>($"{ContentFolder}/Tool_sponge.asset");
            ToolDefinition steelWool = AssetDatabase.LoadAssetAtPath<ToolDefinition>($"{ContentFolder}/Tool_steel_wool.asset");
            ToolDefinition scraper = AssetDatabase.LoadAssetAtPath<ToolDefinition>($"{ContentFolder}/Tool_scraper.asset");
            ToolDefinition shower = AssetDatabase.LoadAssetAtPath<ToolDefinition>($"{ContentFolder}/Tool_shower.asset");
            // The finishing tools first: the hint names, and autoplay and tests pick, the first one that works on a dirt.
            ToolDefinition[] tools = { soak, sponge, steelWool, scraper, shower };

            if (shiftDef == null || channel == null || System.Array.IndexOf(tools, null) >= 0)
            {
                Debug.LogError("Zmywak: content assets are missing. Run 'Zmywak/Setup/1. Create Kitchen Content Assets' first.");
                return;
            }

            KitchenArt.EnsureAssets(); // after NewScene(), like every other asset this scene references

            CreateKitchenCamera();
            CreateDirectionalLight();
            CreateEventSystem();
            var setDressing = new GameObject("SetDressing");
            KitchenArt.BuildRoom(setDressing.transform);
            KitchenArt.BuildWorkLight(setDressing.transform);

            var lookRoot = new GameObject("Look");
            CreateSceneLighting(lookRoot.transform);
            CreateLookVolume(lookRoot.transform);

            var kitchenRoot = new GameObject("KitchenRoot");
            DishInputRouter inputRouter = kitchenRoot.AddComponent<DishInputRouter>();

            // Portrait layout, top of screen to bottom: rack + dishwasher, the pile (middle left), the sink
            // (bottom, where the thumb rests - the most rubbing happens there).
            var spawnPoint = new GameObject("DishSpawnPoint");
            spawnPoint.transform.SetParent(kitchenRoot.transform);
            spawnPoint.transform.position = new Vector3(-1.5f, 0f, -0.6f);
            KitchenArt.BuildPileTub(setDressing.transform, spawnPoint.transform.position);

            WashStation washStation = BuildWashStation(kitchenRoot.transform, out Transform waterOrigin);
            RackGrid rackGrid = BuildRackGrid(kitchenRoot.transform);
            DishwasherController dishwasher = BuildDishwasher(kitchenRoot.transform, rackGrid, out Transform steamOrigin);
            BigDishwasherController bigDishwasher = BuildBigDishwasher(kitchenRoot.transform, washStation, shiftDef);
            BuildToolSelector(kitchenRoot.transform, tools);

            var pileGo = new GameObject("DishPile");
            pileGo.transform.SetParent(kitchenRoot.transform);
            pileGo.transform.position = spawnPoint.transform.position; // where relaxed mode puts fallen dishes back
            DishPileController pileController = pileGo.AddComponent<DishPileController>();

            var flowGo = new GameObject("FlowStreakController");
            flowGo.transform.SetParent(kitchenRoot.transform);
            FlowStreakController flowController = flowGo.AddComponent<FlowStreakController>();

            var orderGo = new GameObject("ChefOrderController");
            orderGo.transform.SetParent(kitchenRoot.transform);
            ChefOrderController orderController = orderGo.AddComponent<ChefOrderController>();
            var orderSo = new SerializedObject(orderController);
            orderSo.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            orderSo.FindProperty("flowController").objectReferenceValue = flowController;
            orderSo.ApplyModifiedPropertiesWithoutUndo();

            ShiftController shiftController = kitchenRoot.AddComponent<ShiftController>();
            var shiftSo = new SerializedObject(shiftController);
            shiftSo.FindProperty("shiftDefinition").objectReferenceValue = shiftDef;
            shiftSo.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            shiftSo.FindProperty("rackGrid").objectReferenceValue = rackGrid;
            shiftSo.FindProperty("washStation").objectReferenceValue = washStation;
            shiftSo.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            shiftSo.FindProperty("bigDishwasher").objectReferenceValue = bigDishwasher;
            shiftSo.FindProperty("pileController").objectReferenceValue = pileController;
            shiftSo.FindProperty("flowController").objectReferenceValue = flowController;
            shiftSo.FindProperty("shiftCompletedChannel").objectReferenceValue = channel;
            shiftSo.FindProperty("legendaryTray").objectReferenceValue = LegendaryTray;
            shiftSo.ApplyModifiedPropertiesWithoutUndo();

            // The local gameplay diary: each finished shift as a line of gameplay_log.jsonl.
            ShiftDiary diary = kitchenRoot.AddComponent<ShiftDiary>();
            var diarySo = new SerializedObject(diary);
            diarySo.FindProperty("shiftController").objectReferenceValue = shiftController;
            diarySo.FindProperty("washStation").objectReferenceValue = washStation;
            diarySo.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            diarySo.FindProperty("pileController").objectReferenceValue = pileController;
            diarySo.FindProperty("chefOrders").objectReferenceValue = orderController;
            diarySo.FindProperty("flow").objectReferenceValue = flowController;
            diarySo.FindProperty("shiftCompletedChannel").objectReferenceValue = channel;
            diarySo.ApplyModifiedPropertiesWithoutUndo();

            BuildKitchenHud(shiftController, channel, orderController, flowController, washStation, pileController, dishwasher, inputRouter,
                tools, bigDishwasher);

            GlovedHand hand = KitchenArt.BuildHand(kitchenRoot.transform, inputRouter, washStation);
            BuildFeedback(kitchenRoot.transform, dishwasher, waterOrigin, steamOrigin, hand, washStation);

            BakedLighting.PrepareKitchen(scene);
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Kitchen_Shift.unity");
            BakedLighting.Bake(scene); // on the PC's GPU; the phone only reads the result
            Debug.Log("Zmywak: Kitchen_Shift scene built.");
        }

        /// <summary>Particles, vibration and sound for the kitchen (all driven by KitchenFeedbackEvents).</summary>
        private static void BuildFeedback(Transform parent, DishwasherController dishwasher, Transform waterOrigin, Transform steamOrigin, GlovedHand hand,
            WashStation washStation)
        {
            var go = new GameObject("Feedback");
            go.transform.SetParent(parent);

            KitchenFx fx = go.AddComponent<KitchenFx>();
            var fxSo = new SerializedObject(fx);
            fxSo.FindProperty("softMaterial").objectReferenceValue = FxArt.Soft;
            fxSo.FindProperty("bubbleMaterial").objectReferenceValue = FxArt.Bubble;
            fxSo.FindProperty("glowMaterial").objectReferenceValue = FxArt.Glow;
            fxSo.FindProperty("sparkleMaterial").objectReferenceValue = FxArt.Sparkle;
            fxSo.FindProperty("shardMaterial").objectReferenceValue = FxArt.Shard;
            fxSo.FindProperty("ringMaterial").objectReferenceValue = FxArt.Ring;
            fxSo.FindProperty("tapSpout").objectReferenceValue = waterOrigin;
            fxSo.FindProperty("steamOrigin").objectReferenceValue = steamOrigin;
            fxSo.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            fxSo.FindProperty("hand").objectReferenceValue = hand;
            fxSo.FindProperty("washStation").objectReferenceValue = washStation;
            fxSo.ApplyModifiedPropertiesWithoutUndo();

            KitchenAudio audio = go.AddComponent<KitchenAudio>();
            var audioSo = new SerializedObject(audio);
            audioSo.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            audioSo.FindProperty("hand").objectReferenceValue = hand;
            audioSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static WashStation BuildWashStation(Transform parent, out Transform waterOrigin)
        {
            var stationGo = new GameObject("WashStation");
            stationGo.transform.SetParent(parent);
            stationGo.transform.position = new Vector3(0.6f, 0f, -2.3f);
            WashStation station = stationGo.AddComponent<WashStation>();
            waterOrigin = KitchenArt.BuildSink(stationGo.transform);

            var so = new SerializedObject(station);
            so.FindProperty("heldHeight").floatValue = 0.8f;
            so.FindProperty("heldScaleMultiplier").floatValue = 2.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }

        private static RackGrid BuildRackGrid(Transform parent)
        {
            // The rack's transform is its centre; RackGrid lays its cells out around it (6 x 4 cells of 0.45 m).
            var rackGridGo = new GameObject("RackGrid");
            rackGridGo.transform.SetParent(parent);
            rackGridGo.transform.position = new Vector3(-1.2f, 0f, 1.85f);
            RackGrid rackGrid = rackGridGo.AddComponent<RackGrid>();

            for (int row = 0; row < rackGrid.Rows; row++)
            {
                for (int col = 0; col < rackGrid.Columns; col++)
                {
                    var cellGo = new GameObject($"Cell_{col}_{row}", typeof(RackCell));
                    cellGo.transform.SetParent(rackGridGo.transform);
                    cellGo.transform.position = rackGrid.CellCenter(col, row);
                    var cellSo = new SerializedObject(cellGo.GetComponent<RackCell>());
                    cellSo.FindProperty("column").intValue = col;
                    cellSo.FindProperty("row").intValue = row;
                    cellSo.ApplyModifiedPropertiesWithoutUndo();

                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    marker.name = "Marker";
                    marker.transform.SetParent(cellGo.transform, false);
                    // Just above the rack tray's top - coplanar quads z-fight into stripes.
                    marker.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                    marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    marker.transform.localScale = Vector3.one * rackGrid.CellSize * 0.84f;
                    Collider markerCollider = marker.GetComponent<Collider>();
                    if (markerCollider != null)
                    {
                        Object.DestroyImmediate(markerCollider);
                    }
                    marker.GetComponent<Renderer>().sharedMaterial = KitchenArt.SlotMaterial;
                }
            }

            KitchenArt.BuildRackFrame(rackGridGo.transform);
            return rackGrid;
        }

        private static DishwasherController BuildDishwasher(Transform parent, RackGrid rackGrid, out Transform steamOrigin)
        {
            var dishwasherGo = new GameObject("Dishwasher");
            dishwasherGo.transform.SetParent(parent);
            dishwasherGo.transform.position = new Vector3(1.7f, 0f, 1.85f);
            DishwasherController dishwasher = dishwasherGo.AddComponent<DishwasherController>();
            KitchenArt.BuildDishwasher(dishwasherGo.transform, dishwasher);
            steamOrigin = new GameObject("SteamOrigin").transform;
            steamOrigin.SetParent(dishwasherGo.transform, false);
            steamOrigin.localPosition = new Vector3(0f, 0.96f, 0f);

            var so = new SerializedObject(dishwasher);
            so.FindProperty("rackGrid").objectReferenceValue = rackGrid;
            so.FindProperty("cycleSeconds").floatValue = 20f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return dishwasher;
        }

        private static void BuildToolSelector(Transform parent, ToolDefinition[] tools)
        {
            var go = new GameObject("ActiveToolSelector");
            go.transform.SetParent(parent);
            ActiveToolSelector selector = go.AddComponent<ActiveToolSelector>();

            var so = new SerializedObject(selector);
            SerializedProperty available = so.FindProperty("availableTools");
            available.arraySize = tools.Length;
            for (int i = 0; i < tools.Length; i++)
            {
                available.GetArrayElementAtIndex(i).objectReferenceValue = tools[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The big dishwasher's controls (BigDishwasherPanel). The load button sits over the toolbar on the left, in
        /// reach of the thumb and clear of the dish held in the sink on every screen shape; the card sits beside the
        /// machine, in the gap between the pile, the rack and the sink (worked out from the camera at 16:9, 19.5:9
        /// and 4:3), so it hides neither the rack's cells nor the dish being washed. Both hide when not needed.
        /// </summary>
        private static void BuildBigDishwasherHud(Transform safeArea, BigDishwasherController machine, ShiftController shiftController)
        {
            Button loadButton = CreateButton("BigDishwasherLoadButton", safeArea, "Do dużej zmywarki (0/40)");
            loadButton.GetComponent<Image>().color = new Color(0.14f, 0.45f, 0.47f);
            RectTransform loadRect = RT(loadButton.gameObject);
            loadRect.anchorMin = loadRect.anchorMax = loadRect.pivot = Vector2.zero;
            loadRect.sizeDelta = new Vector2(400, 128); // 48 dp on a 420 dpi phone
            loadRect.anchoredPosition = new Vector2(34, 396);
            TMP_Text loadLabel = loadButton.GetComponentInChildren<TMP_Text>();
            loadButton.gameObject.SetActive(false);

            Image card = CreatePanel("BigDishwasherCard", safeArea, new Color(0.07f, 0.08f, 0.09f, 0.82f));
            RectTransform cardRect = RT(card.gameObject);
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.54f, 0.493f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            // One line of status over a full-size (48 dp) button: no taller, or it would reach the rack or the dish.
            cardRect.sizeDelta = new Vector2(320, 200);
            cardRect.anchoredPosition = Vector2.zero;
            card.raycastTarget = false;

            TextMeshProUGUI status = CreateText("BigDishwasherStatus", card.transform, "Duża zmywarka 0/40", 26, TextAlignmentOptions.Center);
            RectTransform statusRect = RT(status.gameObject);
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.sizeDelta = new Vector2(-16f, 52f);
            statusRect.anchoredPosition = new Vector2(0f, -4f);
            status.enableAutoSizing = true;
            status.fontSizeMin = 16;
            status.fontSizeMax = 26;
            status.raycastTarget = false;
            Scalable(status);

            Button action = CreateButton("BigDishwasherActionButton", card.transform, "Uruchom (30 min)");
            RectTransform actionRect = RT(action.gameObject);
            actionRect.anchorMin = actionRect.anchorMax = new Vector2(0.5f, 0f);
            actionRect.pivot = new Vector2(0.5f, 0f);
            actionRect.sizeDelta = new Vector2(296, 128);
            actionRect.anchoredPosition = new Vector2(0f, 10f);
            TMP_Text actionLabel = action.GetComponentInChildren<TMP_Text>();
            action.gameObject.SetActive(false);

            BigDishwasherPanel panel = card.gameObject.AddComponent<BigDishwasherPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("machine").objectReferenceValue = machine;
            so.FindProperty("shiftController").objectReferenceValue = shiftController;
            so.FindProperty("loadButton").objectReferenceValue = loadButton;
            so.FindProperty("loadLabel").objectReferenceValue = loadLabel;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("actionButton").objectReferenceValue = action;
            so.FindProperty("actionLabel").objectReferenceValue = actionLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Soaking in the sink: one button stacked over the big dishwasher's load button, the same size and in the same
        /// thumb-reach strip left of the dish being washed - put the dish in to soak, see how far it has got, take it out.
        /// </summary>
        private static void BuildSoakHud(Transform safeArea, WashStation washStation, ShiftController shiftController)
        {
            Button button = CreateButton("SoakButton", safeArea, "Odstaw do moczenia");
            RectTransform rect = RT(button.gameObject);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 128);
            rect.anchoredPosition = new Vector2(34, 396 + 128 + 18); // just over the load button
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            button.gameObject.SetActive(false);

            var holder = new GameObject("SoakPanel");
            holder.transform.SetParent(safeArea, false);
            SoakPanel panel = holder.AddComponent<SoakPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("washStation").objectReferenceValue = washStation;
            so.FindProperty("shiftController").objectReferenceValue = shiftController;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sunday's lasagne tray (made with the content; not in any shift's queue).</summary>
        private static DishDefinition LegendaryTray => AssetDatabase.LoadAssetAtPath<DishDefinition>($"{ContentFolder}/Dish_lasagne_tray.asset");

        private static BigDishwasherController BuildBigDishwasher(Transform parent, WashStation washStation, ShiftDefinition shift)
        {
            // On the counter right of the pile, between the small machine and the sink.
            var go = new GameObject("BigDishwasher");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(1.85f, 0f, -0.2f); // 1.1-2.6: clear of the card on its left and the screen's edge
            BigDishwasherController machine = go.AddComponent<BigDishwasherController>();
            KitchenArt.BuildBigDishwasher(go.transform, machine);

            var known = new List<DishDefinition>();
            foreach (DishDefinition dish in shift.DishSpawnQueue)
            {
                if (dish != null && !known.Contains(dish))
                {
                    known.Add(dish);
                }
            }

            if (LegendaryTray != null)
            {
                known.Add(LegendaryTray); // Sunday's tray can go in too (it just earns no whip-round)
            }

            var so = new SerializedObject(machine);
            so.FindProperty("washStation").objectReferenceValue = washStation;
            so.FindProperty("capacity").intValue = 40;
            so.FindProperty("cycleMinutes").floatValue = 30f;
            SerializedProperty dishes = so.FindProperty("knownDishes");
            dishes.arraySize = known.Count;
            for (int i = 0; i < known.Count; i++)
            {
                dishes.GetArrayElementAtIndex(i).objectReferenceValue = known[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return machine;
        }

        private static void BuildKitchenHud(ShiftController shiftController, ShiftCompletedEventChannel channel, ChefOrderController orderController,
            FlowStreakController flowController, WashStation washStation, DishPileController pileController, DishwasherController dishwasher,
            DishInputRouter inputRouter, ToolDefinition[] tools, BigDishwasherController bigDishwasher)
        {
            // No CreateEventSystem() here: BuildKitchenShiftScene already made one, and a second one logged
            // "There are 2 event systems in the scene" every single frame.
            Canvas canvas = CreateCanvas("Canvas");
            GameObject safeArea = CreateUIObject("SafeArea", canvas.transform);
            StretchFull(RT(safeArea));
            safeArea.AddComponent<SafeAreaFitter>();

            // --- Top bar: dark panel so the readouts stay legible over the tiled wall. Its top edge sits
            // off-screen, so only the rounded bottom corners show. ---
            Image topBar = CreatePanel("TopBar", safeArea.transform, PanelDark);
            RectTransform topBarRect = RT(topBar.gameObject);
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.sizeDelta = new Vector2(-24f, 230f);
            topBarRect.anchoredPosition = new Vector2(0f, 40f);

            TextMeshProUGUI timerText = CreateText("TimerText", safeArea.transform, "03:00", 64, TextAlignmentOptions.TopLeft);
            timerText.fontStyle = FontStyles.Bold;
            AnchorTopLeft(RT(timerText.gameObject), new Vector2(320, 90), new Vector2(40, -22));
            Scalable(timerText);

            Image flowChip = CreatePanel("FlowChip", safeArea.transform, new Color(1f, 0.83f, 0.3f, 0.22f));
            AnchorTopLeft(RT(flowChip.gameObject), new Vector2(250, 58), new Vector2(36, -116));
            flowChip.raycastTarget = false;
            TextMeshProUGUI flowText = CreateText("FlowText", flowChip.transform, "Flow ×0", 32, TextAlignmentOptions.Center);
            flowText.color = new Color(1f, 0.85f, 0.35f);
            flowText.fontStyle = FontStyles.Bold;
            StretchFull(RT(flowText.gameObject));
            Scalable(flowText);
            flowChip.gameObject.SetActive(false);

            TextMeshProUGUI dishesText = CreateText("DishesText", safeArea.transform, "Umyte: 0 / 0", 38, TextAlignmentOptions.TopRight);
            dishesText.fontStyle = FontStyles.Bold;
            AnchorTopRight(RT(dishesText.gameObject), new Vector2(440, 64), new Vector2(-40, -26));
            Scalable(dishesText);

            // The quota as a bar under its count: how far the night has got, at a glance.
            Image quotaTrack = CreatePanel("QuotaBar", safeArea.transform, new Color(1f, 1f, 1f, 0.12f));
            AnchorTopRight(RT(quotaTrack.gameObject), new Vector2(300, 16), new Vector2(-42, -94));
            quotaTrack.raycastTarget = false;
            Image quotaFill = CreatePanel("Fill", quotaTrack.transform, new Color(0.45f, 0.75f, 1f));
            StretchFull(RT(quotaFill.gameObject));
            quotaFill.raycastTarget = false;
            quotaFill.enabled = false;

            Image brokenChip = CreatePanel("BrokenChip", safeArea.transform, new Color(1f, 0.35f, 0.3f, 0.22f));
            AnchorTopRight(RT(brokenChip.gameObject), new Vector2(270, 54), new Vector2(-36, -120));
            brokenChip.raycastTarget = false;
            TextMeshProUGUI brokenText = CreateText("BrokenText", brokenChip.transform, "Stłuczone: 0", 30, TextAlignmentOptions.Center);
            brokenText.color = new Color(1f, 0.62f, 0.56f);
            brokenText.fontStyle = FontStyles.Bold;
            StretchFull(RT(brokenText.gameObject));
            Scalable(brokenText);
            brokenChip.gameObject.SetActive(false);

            // --- Chef's order: a yellow pill under the top bar, only while an order is open. ---
            Image orderBanner = CreatePanel("OrderBanner", safeArea.transform, new Color(1f, 0.82f, 0.25f));
            AnchorTopCenter(RT(orderBanner.gameObject), new Vector2(840, 88), new Vector2(0, -214));
            Shadow orderShadow = orderBanner.gameObject.AddComponent<Shadow>();
            orderShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            orderShadow.effectDistance = new Vector2(0f, -6f);
            TextMeshProUGUI orderText = CreateText("OrderText", orderBanner.transform, string.Empty, 36, TextAlignmentOptions.Center);
            orderText.color = new Color(0.16f, 0.1f, 0.04f);
            orderText.fontStyle = FontStyles.Bold;
            orderText.enableAutoSizing = true;
            orderText.fontSizeMin = 22;
            orderText.fontSizeMax = 36;
            Scalable(orderText);
            StretchFull(RT(orderText.gameObject));
            orderText.margin = new Vector4(24f, 6f, 24f, 6f);
            orderBanner.gameObject.SetActive(false);

            // --- Pile meter: how close the pile is to collapsing, next to the tub on the left edge. ---
            Image meterBack = CreatePanel("PileMeter", safeArea.transform, new Color(0.07f, 0.08f, 0.09f, 0.75f));
            CanvasGroup pileMeterGroup = meterBack.gameObject.AddComponent<CanvasGroup>();
            pileMeterGroup.alpha = 0f;
            pileMeterGroup.blocksRaycasts = false;
            RectTransform meterRect = RT(meterBack.gameObject);
            meterRect.anchorMin = new Vector2(0f, 0f);
            meterRect.anchorMax = new Vector2(0f, 0f);
            meterRect.pivot = new Vector2(0f, 0f);
            meterRect.sizeDelta = new Vector2(46, 330);
            meterRect.anchoredPosition = new Vector2(10, 720);
            Image meterFill = CreatePanel("Fill", meterBack.transform, new Color(0.3f, 0.8f, 0.45f));
            meterFill.enabled = false; // empty pile; ShiftHUD sizes it (sliced, so the rounded caps survive)
            RectTransform fillRect = RT(meterFill.gameObject);
            StretchFull(fillRect);
            fillRect.offsetMin = new Vector2(7f, 7f);
            fillRect.offsetMax = new Vector2(-7f, -7f);
            // The word sits on the meter's own left edge, running right: centred on a bar only 46 wide and 10 from the screen's edge, it
            // began 12 px off the screen (PWS-013).
            TextMeshProUGUI meterLabel = CreateText("Label", meterBack.transform, "Góra", 22, TextAlignmentOptions.BottomLeft);
            meterLabel.fontStyle = FontStyles.Bold;
            RectTransform meterLabelRect = RT(meterLabel.gameObject);
            meterLabelRect.anchorMin = new Vector2(0f, 1f);
            meterLabelRect.anchorMax = new Vector2(0f, 1f);
            meterLabelRect.pivot = new Vector2(0f, 0f);
            meterLabelRect.sizeDelta = new Vector2(90, 34);
            meterLabelRect.anchoredPosition = new Vector2(0f, 4f);
            Scalable(meterLabel);

            // --- Rack fill: how many of the rack's cells the current load covers, just above the rack. ---
            Image rackPill = CreatePanel("RackFill", safeArea.transform, new Color(0.07f, 0.08f, 0.09f, 0.75f));
            AnchorTopLeft(RT(rackPill.gameObject), new Vector2(340, 58), new Vector2(34, -458));
            TextMeshProUGUI rackFillText = CreateText("RackFillText", rackPill.transform, "Kosz: 0/24", 30, TextAlignmentOptions.Center);
            rackFillText.fontStyle = FontStyles.Bold;
            rackFillText.raycastTarget = false;
            StretchFull(RT(rackFillText.gameObject));
            Scalable(rackFillText);

            // --- Bottom panel: tools, the dishwasher button and the stage hint. ---
            Image bottomPanel = CreatePanel("BottomPanel", safeArea.transform, PanelDark);
            RectTransform bottomRect = RT(bottomPanel.gameObject);
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.sizeDelta = new Vector2(-24f, 420f);
            bottomRect.anchoredPosition = new Vector2(0f, -40f);

            GameObject toolbar = CreateUIObject("Toolbar", safeArea.transform);
            RectTransform toolbarRect = RT(toolbar);
            toolbarRect.anchorMin = new Vector2(0.5f, 0f);
            toolbarRect.anchorMax = new Vector2(0.5f, 0f);
            toolbarRect.pivot = new Vector2(0.5f, 0f);
            toolbarRect.sizeDelta = new Vector2(1010, 196);
            toolbarRect.anchoredPosition = new Vector2(0, 26);

            for (int i = 0; i < tools.Length; i++)
            {
                CreateToolButton($"Tool_{tools[i].Id}", toolbar.transform, tools[i].DisplayName, tools[i], i, tools.Length, toolbarRect.sizeDelta);
            }

            CanvasGroup toolbarGroup = toolbar.AddComponent<CanvasGroup>();
            var layoutSo = new SerializedObject(toolbar.AddComponent<ToolbarLayout>());
            layoutSo.FindProperty("slots").intValue = tools.Length;
            layoutSo.ApplyModifiedPropertiesWithoutUndo();

            Button dishwasherButton = CreateButton("StartDishwasherButton", safeArea.transform, "Uruchom zmywarkę");
            dishwasherButton.GetComponent<Image>().color = new Color(0.17f, 0.42f, 0.72f);
            AnchorBottomRight(RT(dishwasherButton.gameObject), new Vector2(360, 130), new Vector2(-40, 240));
            // The bar's own strip along the button's bottom; the fill inside it is sized by FillBar (anchors 0..value).
            GameObject cycleStrip = CreateUIObject("CycleProgress", dishwasherButton.transform);
            RectTransform cycleRect = RT(cycleStrip);
            cycleRect.anchorMin = new Vector2(0f, 0f);
            cycleRect.anchorMax = new Vector2(1f, 0f);
            cycleRect.pivot = new Vector2(0.5f, 0f);
            cycleRect.sizeDelta = new Vector2(-28f, 10f);
            cycleRect.anchoredPosition = new Vector2(0f, 8f);
            Image cycleBar = CreatePanel("Fill", cycleStrip.transform, new Color(1f, 1f, 1f, 0.55f));
            StretchFull(RT(cycleBar.gameObject));
            cycleBar.raycastTarget = false;
            cycleBar.enabled = false;

            Image hintPill = CreatePanel("StageHint", safeArea.transform, new Color(1f, 1f, 1f, 0.07f));
            RectTransform hintPillRect = RT(hintPill.gameObject);
            hintPillRect.anchorMin = new Vector2(0f, 0f);
            hintPillRect.anchorMax = new Vector2(0f, 0f);
            hintPillRect.pivot = new Vector2(0f, 0f);
            hintPillRect.sizeDelta = new Vector2(620, 130);
            hintPillRect.anchoredPosition = new Vector2(40, 240);
            TextMeshProUGUI stageHintText = CreateText("StageHintText", hintPill.transform, string.Empty, 30, TextAlignmentOptions.Left);
            StretchFull(RT(stageHintText.gameObject));
            stageHintText.margin = new Vector4(22f, 8f, 18f, 8f);
            stageHintText.enableAutoSizing = true;
            stageHintText.fontSizeMin = 20;
            stageHintText.fontSizeMax = 30;
            stageHintText.color = new Color(0.88f, 0.95f, 1f);
            Scalable(stageHintText);

            // --- Flourishes: "Flow ×N!" popup and a red flash at the screen edges when something smashes. ---
            TextMeshProUGUI flowPopup = CreateText("FlowPopup", safeArea.transform, "Flow ×2!", 88, TextAlignmentOptions.Center);
            flowPopup.fontStyle = FontStyles.Bold;
            flowPopup.color = new Color(1f, 0.85f, 0.3f);
            flowPopup.raycastTarget = false;
            AnchorCenter(RT(flowPopup.gameObject), new Vector2(900, 140), new Vector2(0, 0));
            RT(flowPopup.gameObject).anchorMin = new Vector2(0.5f, 0.62f);
            RT(flowPopup.gameObject).anchorMax = new Vector2(0.5f, 0.62f);
            flowPopup.gameObject.SetActive(false);

            GameObject flashGo = CreateUIObject("BreakFlash", canvas.transform);
            StretchFull(RT(flashGo));
            Image breakFlash = flashGo.AddComponent<Image>();
            breakFlash.sprite = UiArt.Vignette;
            breakFlash.color = new Color(1f, 0.15f, 0.1f, 0f);
            breakFlash.raycastTarget = false;
            breakFlash.enabled = false;

            // --- The big dishwasher: its load button over the toolbar, its card beside the machine (under every card). ---
            BuildBigDishwasherHud(safeArea.transform, bigDishwasher, shiftController);
            BuildSoakHud(safeArea.transform, washStation, shiftController);

            // --- The radio on the wall: below where the chef's orders show, above the rack's counter. A tap turns the dial. ---
            BuildRadio(safeArea.transform);
            // --- Beside it, a special shift's card: a wedding, an inspection, the lasagne tray, St. Patrick's. ---
            BuildSpecialShiftBadge(safeArea.transform, shiftController);

            // --- The closing stamp: "KONIEC ZMIANY" lands over the kitchen as the bell rings. ---
            TextMeshProUGUI endStamp = CreateText("EndStamp", safeArea.transform, "KONIEC ZMIANY", 112, TextAlignmentOptions.Center);
            endStamp.fontStyle = FontStyles.Bold;
            endStamp.color = new Color(1f, 0.85f, 0.35f);
            endStamp.raycastTarget = false;
            endStamp.outlineWidth = 0.18f;
            endStamp.outlineColor = new Color32(40, 20, 5, 255);
            AnchorCenter(RT(endStamp.gameObject), new Vector2(1000, 180), new Vector2(0, 120));
            endStamp.gameObject.SetActive(false);

            // --- The guided first plate's overlay: over the HUD, under the repair card, the board, the pause and the result. ---
            KitchenTutorial tutorial = BuildTutorialOverlay(safeArea.transform, shiftController, washStation, shiftController.Rack, dishwasher,
                pileController, orderController, toolbarRect, toolbarGroup, RT(dishwasherButton.gameObject));

            // --- Dishwasher repair: a card over the kitchen while the machine is broken (under the result card). ---
            DishwasherRepairPanel repairPanel = BuildRepairPanel(canvas.transform, safeArea.transform, dishwasher, inputRouter);

            // --- The board: prep before the shift, for a Kuba who took the knife (above the repair card, under the pause). ---
            BuildPrepPanel(canvas.transform, safeArea.transform, shiftController, inputRouter);

            // --- Pause: a button in the top bar, and a card over the kitchen (above the repair card). ---
            KitchenPauseMenu pauseMenu = BuildPauseMenu(canvas.transform, safeArea.transform, shiftController, inputRouter, repairPanel);

            // --- End of shift: a screen of its own over the dimmed kitchen. ---
            ShiftResultScreen resultScreen = BuildResultScreen(safeArea.transform);

            GameObject hudGo = CreateUIObject("ShiftHUDController", canvas.transform);
            ShiftHUD hud = hudGo.AddComponent<ShiftHUD>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("shiftController").objectReferenceValue = shiftController;
            hudSo.FindProperty("shiftCompletedChannel").objectReferenceValue = channel;
            hudSo.FindProperty("chefOrderController").objectReferenceValue = orderController;
            hudSo.FindProperty("flowController").objectReferenceValue = flowController;
            hudSo.FindProperty("timerText").objectReferenceValue = timerText;
            hudSo.FindProperty("dishesText").objectReferenceValue = dishesText;
            hudSo.FindProperty("brokenText").objectReferenceValue = brokenText;
            hudSo.FindProperty("flowText").objectReferenceValue = flowText;
            hudSo.FindProperty("orderText").objectReferenceValue = orderText;
            hudSo.FindProperty("orderBanner").objectReferenceValue = orderBanner.gameObject;
            hudSo.FindProperty("startDishwasherButton").objectReferenceValue = dishwasherButton;
            hudSo.FindProperty("resultScreen").objectReferenceValue = resultScreen;
            hudSo.FindProperty("endStamp").objectReferenceValue = endStamp;
            hudSo.FindProperty("tutorial").objectReferenceValue = tutorial;
            hudSo.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            hudSo.FindProperty("quotaFill").objectReferenceValue = quotaFill;
            hudSo.FindProperty("flowChip").objectReferenceValue = flowChip.gameObject;
            hudSo.FindProperty("brokenChip").objectReferenceValue = brokenChip.gameObject;
            hudSo.FindProperty("dishwasherProgress").objectReferenceValue = cycleBar;
            hudSo.FindProperty("hintPill").objectReferenceValue = hintPill.gameObject;
            hudSo.FindProperty("pileMeter").objectReferenceValue = pileMeterGroup;
            hudSo.FindProperty("rackPill").objectReferenceValue = rackPill.gameObject;
            hudSo.FindProperty("toolbar").objectReferenceValue = toolbarRect;
            hudSo.FindProperty("soakPanel").objectReferenceValue = safeArea.transform.Find("SoakPanel")?.gameObject;
            hudSo.FindProperty("bigDishwasherCard").objectReferenceValue = safeArea.transform.Find("BigDishwasherCard")?.gameObject;
            hudSo.FindProperty("washStation").objectReferenceValue = washStation;
            hudSo.FindProperty("stageHintText").objectReferenceValue = stageHintText;
            hudSo.FindProperty("pileController").objectReferenceValue = pileController;
            hudSo.FindProperty("pileMeterFill").objectReferenceValue = meterFill;
            hudSo.FindProperty("flowPopup").objectReferenceValue = flowPopup;
            hudSo.FindProperty("breakFlash").objectReferenceValue = breakFlash;
            hudSo.FindProperty("repairPanel").objectReferenceValue = repairPanel;
            hudSo.FindProperty("rackFillText").objectReferenceValue = rackFillText;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The repair card over a dimmed kitchen: what broke, a round look inside the machine (drain filter
        /// with scraps / spray arm / cut-out reset button - one shows at a time), the instruction, a progress
        /// bar and a way back to the sink. The controller sits on its own always-active object, because the
        /// card itself is switched off while the machine works.
        /// </summary>
        private static DishwasherRepairPanel BuildRepairPanel(Transform canvas, Transform parent, DishwasherController dishwasher,
            DishInputRouter inputRouter)
        {
            GameObject root = CreateUIObject("DishwasherRepair", parent);
            StretchFull(RT(root));
            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f); // also swallows taps meant for the HUD buttons behind

            Image card = CreatePanel("Card", root.transform, new Color(0.09f, 0.1f, 0.11f, 0.97f));
            // Fits between the top bar and the tool panel, so the red dishwasher button is never half covered.
            AnchorCenter(RT(card.gameObject), new Vector2(960, 1200), new Vector2(0, 90));
            Shadow cardShadow = card.gameObject.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            cardShadow.effectDistance = new Vector2(0f, -10f);

            TextMeshProUGUI header = CreateText("Header", card.transform, "Zmywarka stanęła!", 58, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            header.color = new Color(1f, 0.45f, 0.38f);
            AnchorTopCenter(RT(header.gameObject), new Vector2(880, 76), new Vector2(0, -28));
            Scalable(header);

            TextMeshProUGUI title = CreateText("FaultTitle", card.transform, string.Empty, 40, TextAlignmentOptions.Center);
            title.enableAutoSizing = true;
            title.fontSizeMin = 26;
            title.fontSizeMax = 40;
            AnchorTopCenter(RT(title.gameObject), new Vector2(880, 56), new Vector2(0, -104));
            Scalable(title);

            // The inside of the machine. Centre pivot: the panel works in a unit disc around it.
            GameObject area = CreateUIObject("PlayArea", card.transform);
            RectTransform areaRect = RT(area);
            AnchorTopCenter(areaRect, new Vector2(740, 740), Vector2.zero);
            areaRect.pivot = new Vector2(0.5f, 0.5f);
            areaRect.anchoredPosition = new Vector2(0f, -166f - 370f);
            Image tub = area.AddComponent<Image>();
            tub.sprite = UiArt.Basin; // dark inside, its rim lit from above
            tub.color = Color.white;

            // Clogged filter: a perforated steel disc with a drain in the middle; scraps are cloned from the template.
            GameObject filterView = CreateUIObject("FilterView", area.transform);
            StretchFull(RT(filterView));
            Image filterDisc = CreateDisc("FilterDisc", filterView.transform, 600f, Vector2.zero, Color.white);
            filterDisc.sprite = UiArt.FilterSteel;
            DropShadow(filterDisc, 8f, 0.45f);
            for (float y = -260f; y <= 260f; y += 52f)
            {
                for (float x = -260f; x <= 260f; x += 52f)
                {
                    var hole = new Vector2(x + (Mathf.Abs(y) % 104f < 1f ? 0f : 26f), y);
                    if (hole.magnitude < 270f && hole.magnitude > 70f)
                    {
                        CreateDisc("Hole", filterView.transform, 20f, hole, Color.white).sprite = UiArt.Hole;
                    }
                }
            }

            CreateDisc("Drain", filterView.transform, 110f, Vector2.zero, Color.white).sprite = UiArt.Hole;
            Image scrapTemplate = CreateDisc("ScrapTemplate", filterView.transform, 80f, Vector2.zero, new Color(0.45f, 0.27f, 0.12f));
            scrapTemplate.sprite = UiArt.Scrap; // lumpy and wet, tinted per scrap by the panel
            DropShadow(scrapTemplate, 5f, 0.4f);

            // Stuck spray arm: a steel bar with nozzles turning on a hub.
            GameObject armView = CreateUIObject("SprayArmView", area.transform);
            StretchFull(RT(armView));
            // A faint track where the finger should go round.
            CreateDisc("Track", armView.transform, 500f, Vector2.zero, new Color(1f, 1f, 1f, 0.1f));
            CreateDisc("TrackInner", armView.transform, 400f, Vector2.zero, new Color(0.17f, 0.19f, 0.21f));
            Image arm = CreatePanel("SprayArm", armView.transform, new Color(0.82f, 0.84f, 0.87f));
            arm.sprite = UiArt.Bar; // a round steel bar, not a flat strip
            AnchorCenter(RT(arm.gameObject), new Vector2(620, 84), Vector2.zero);
            DropShadow(arm, 8f, 0.45f);
            foreach (float x in new[] { -250f, -170f, -90f, 90f, 170f, 250f })
            {
                // Nozzles alternate sides, the way the water jets push the arm round.
                CreateDisc("Nozzle", arm.transform, 20f, new Vector2(x, x > 0f ? 14f : -14f), Color.white).sprite = UiArt.Hole;
            }

            CreateDisc("Hub", armView.transform, 150f, Vector2.zero, new Color(0.3f, 0.32f, 0.34f)).sprite = UiArt.Dome;
            CreateDisc("HubCap", armView.transform, 70f, Vector2.zero, new Color(0.82f, 0.84f, 0.87f)).sprite = UiArt.Dome;
            // What jammed it: gunk packed round the hub (it turns with the arm).
            CreateDisc("Gunk", arm.transform, 58f, new Vector2(-92f, 30f), new Color(0.45f, 0.3f, 0.14f)).sprite = UiArt.Scrap;
            CreateDisc("Gunk", arm.transform, 46f, new Vector2(96f, -26f), new Color(0.74f, 0.3f, 0.13f)).sprite = UiArt.Scrap;
            CreateDisc("Gunk", arm.transform, 36f, new Vector2(-70f, -38f), new Color(0.93f, 0.82f, 0.45f)).sprite = UiArt.Scrap;

            // Tripped cut-out: a red reset button in an electrical box, ringed by the hold progress.
            GameObject resetView = CreateUIObject("ResetView", area.transform);
            StretchFull(RT(resetView));
            Image box = CreatePanel("Box", resetView.transform, new Color(0.24f, 0.25f, 0.27f));
            AnchorCenter(RT(box.gameObject), new Vector2(520, 560), new Vector2(0, -10));
            DropShadow(box, 10f, 0.45f);
            TextMeshProUGUI boxLabel = CreateText("BoxLabel", box.transform, "BEZPIECZNIK", 28, TextAlignmentOptions.Center);
            boxLabel.color = new Color(1f, 0.85f, 0.3f);
            boxLabel.fontStyle = FontStyles.Bold;
            AnchorTopCenter(RT(boxLabel.gameObject), new Vector2(480, 50), new Vector2(0, -24));
            CreateDisc("RingBack", resetView.transform, 380f, Vector2.zero, new Color(0.08f, 0.08f, 0.09f));
            Image ring = CreateDisc("Ring", resetView.transform, 380f, Vector2.zero, new Color(0.35f, 0.85f, 0.45f));
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
            CreateDisc("RingGap", resetView.transform, 330f, Vector2.zero, new Color(0.24f, 0.25f, 0.27f));
            Image resetButton = CreateDisc("ResetButton", resetView.transform, 300f, Vector2.zero, new Color(0.9f, 0.2f, 0.16f));
            resetButton.sprite = UiArt.Dome; // a domed button that looks pressable
            DropShadow(resetButton, 8f, 0.5f);
            TextMeshProUGUI resetLabel = CreateText("Label", resetButton.transform, "RESET", 56, TextAlignmentOptions.Center);
            resetLabel.fontStyle = FontStyles.Bold;
            StretchFull(RT(resetLabel.gameObject));

            TextMeshProUGUI instruction = CreateText("Instruction", card.transform, string.Empty, 34, TextAlignmentOptions.Center);
            instruction.enableAutoSizing = true;
            instruction.fontSizeMin = 24;
            instruction.fontSizeMax = 34;
            instruction.color = new Color(0.88f, 0.95f, 1f);
            AnchorTopCenter(RT(instruction.gameObject), new Vector2(860, 84), new Vector2(0, -916));
            Scalable(instruction);

            Image barBack = CreatePanel("ProgressBar", card.transform, new Color(0.03f, 0.03f, 0.04f, 0.8f));
            AnchorTopCenter(RT(barBack.gameObject), new Vector2(700, 38), new Vector2(0, -1008));
            Image barFill = CreatePanel("Fill", barBack.transform, new Color(1f, 0.55f, 0.2f));
            StretchFull(RT(barFill.gameObject));
            RT(barFill.gameObject).offsetMin = new Vector2(6f, 6f);
            RT(barFill.gameObject).offsetMax = new Vector2(-6f, -6f);
            barFill.enabled = false; // FillBar sizes it

            Button close = CreateButton("RepairCloseButton", card.transform, "Wróć do zlewu");
            close.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.32f);
            AnchorTopCenter(RT(close.gameObject), new Vector2(460, 130), new Vector2(0, -1058));

            root.SetActive(false);

            GameObject controllerGo = CreateUIObject("DishwasherRepairPanel", canvas);
            DishwasherRepairPanel panel = controllerGo.AddComponent<DishwasherRepairPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            so.FindProperty("inputRouter").objectReferenceValue = inputRouter;
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("instructionText").objectReferenceValue = instruction;
            so.FindProperty("progressFill").objectReferenceValue = barFill;
            so.FindProperty("playArea").objectReferenceValue = areaRect;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("filterView").objectReferenceValue = filterView;
            so.FindProperty("scrapTemplate").objectReferenceValue = scrapTemplate;
            so.FindProperty("sprayArmView").objectReferenceValue = armView;
            so.FindProperty("sprayArm").objectReferenceValue = RT(arm.gameObject);
            so.FindProperty("resetView").objectReferenceValue = resetView;
            so.FindProperty("resetButton").objectReferenceValue = RT(resetButton.gameObject);
            so.FindProperty("resetRing").objectReferenceValue = ring;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        /// <summary>
        /// The kitchen radio (KitchenRadio plays it) and its face on the wall: a little portable - a speaker grille, a lit
        /// dial window with the station and a crawling caption (programme or headline), an on light and an aerial. The
        /// whole radio is the button that turns the dial.
        /// </summary>
        private static void BuildRadio(Transform safeArea)
        {
            var radioGo = new GameObject("KitchenRadio");
            KitchenRadio radio = radioGo.AddComponent<KitchenRadio>();

            Button button = CreateButton("RadioButton", safeArea, string.Empty);
            Object.DestroyImmediate(button.transform.Find("Label").gameObject);
            Image body = button.GetComponent<Image>();
            body.color = new Color(0.17f, 0.14f, 0.12f);
            AnchorTopLeft(RT(button.gameObject), new Vector2(346, 128), new Vector2(34, -312));

            Image aerial = CreatePanel("Aerial", button.transform, new Color(0.72f, 0.74f, 0.77f));
            aerial.sprite = null;
            aerial.raycastTarget = false;
            RectTransform aerialRect = RT(aerial.gameObject);
            aerialRect.anchorMin = aerialRect.anchorMax = new Vector2(1f, 1f);
            aerialRect.pivot = new Vector2(0.5f, 0f);
            aerialRect.sizeDelta = new Vector2(5f, 70f);
            aerialRect.anchoredPosition = new Vector2(-34f, -4f);
            aerialRect.localRotation = Quaternion.Euler(0f, 0f, -24f);
            CreateDisc("Tip", aerial.transform, 12f, new Vector2(0f, 35f), new Color(0.72f, 0.74f, 0.77f));

            Image speaker = CreateDisc("Speaker", button.transform, 104f, new Vector2(-110f, 0f), new Color(0.09f, 0.08f, 0.07f));
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    if (x * x + y * y <= 10)
                    {
                        CreateDisc("Hole", speaker.transform, 7f, new Vector2(x * 12f, y * 12f), new Color(0.27f, 0.24f, 0.21f));
                    }
                }
            }

            Image dial = CreatePanel("Dial", button.transform, new Color(0.98f, 0.82f, 0.46f));
            dial.raycastTarget = false;
            AnchorCenter(RT(dial.gameObject), new Vector2(206, 96), new Vector2(54f, 0f));

            TextMeshProUGUI station = CreateText("Station", dial.transform, string.Empty, 24, TextAlignmentOptions.Center);
            station.fontStyle = FontStyles.Bold;
            station.color = new Color(0.22f, 0.13f, 0.05f);
            station.enableAutoSizing = true;
            station.fontSizeMin = 14;
            station.textWrappingMode = TextWrappingModes.NoWrap; // "FM 88.5 · Wiadomości" on one line
            station.fontSizeMax = 24;
            station.raycastTarget = false;
            AnchorCenter(RT(station.gameObject), new Vector2(196, 40), new Vector2(0f, 22f));

            GameObject viewport = CreateUIObject("CaptionWindow", dial.transform);
            viewport.AddComponent<RectMask2D>();
            AnchorCenter(RT(viewport), new Vector2(192, 40), new Vector2(0f, -22f));
            TextMeshProUGUI caption = CreateText("Caption", viewport.transform, string.Empty, 22, TextAlignmentOptions.Left);
            caption.color = new Color(0.3f, 0.18f, 0.07f);
            caption.textWrappingMode = TextWrappingModes.NoWrap; // one line that crawls, never two
            caption.overflowMode = TextOverflowModes.Overflow;
            caption.raycastTarget = false;
            RectTransform captionRect = RT(caption.gameObject);
            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0f, 0.5f);
            captionRect.pivot = new Vector2(0f, 0.5f);
            captionRect.sizeDelta = new Vector2(2400f, 40f);
            captionRect.anchoredPosition = Vector2.zero;

            Image onLight = CreateDisc("OnLight", button.transform, 14f, new Vector2(150f, 48f), new Color(1f, 0.35f, 0.2f));

            RadioWidget widget = button.gameObject.AddComponent<RadioWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("radio").objectReferenceValue = radio;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("dial").objectReferenceValue = dial;
            so.FindProperty("stationText").objectReferenceValue = station;
            so.FindProperty("captionViewport").objectReferenceValue = RT(viewport);
            so.FindProperty("captionText").objectReferenceValue = caption;
            so.FindProperty("onLight").objectReferenceValue = onLight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A special shift's card (SpecialShiftBadge) to the right of the radio: a title, and a line or two under
        /// it. Its tint is the shift's; it stays hidden on a plain shift.</summary>
        private static void BuildSpecialShiftBadge(Transform safeArea, ShiftController shiftController)
        {
            GameObject root = CreateUIObject("SpecialShift", safeArea);
            StretchFull(RT(root));

            Image card = CreatePanel("SpecialShiftCard", root.transform, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            card.raycastTarget = false; // a label: taps go through to the kitchen
            AnchorTopRight(RT(card.gameObject), new Vector2(650, 128), new Vector2(-34, -312));

            TextMeshProUGUI title = CreateText("Title", card.transform, string.Empty, 30, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.86f, 0.45f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 20;
            title.fontSizeMax = 30;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.raycastTarget = false;
            AnchorTopCenter(RT(title.gameObject), new Vector2(610, 40), new Vector2(0, -8));
            Scalable(title);

            TextMeshProUGUI status = CreateText("Status", card.transform, string.Empty, 22, TextAlignmentOptions.TopLeft);
            status.color = new Color(0.95f, 0.95f, 0.93f);
            status.enableAutoSizing = true;
            status.fontSizeMin = 15;
            status.fontSizeMax = 22;
            status.raycastTarget = false;
            AnchorTopCenter(RT(status.gameObject), new Vector2(610, 72), new Vector2(0, -50));
            Scalable(status);

            SpecialShiftBadge badge = root.AddComponent<SpecialShiftBadge>();
            var so = new SerializedObject(badge);
            so.FindProperty("shiftController").objectReferenceValue = shiftController;
            so.FindProperty("card").objectReferenceValue = card.gameObject;
            so.FindProperty("background").objectReferenceValue = card;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);
        }

        /// <summary>
        /// The board before the shift (PrepBoardPanel): a card over the dimmed kitchen with a side view of a chopping
        /// board - the vegetable lying under the knife, the claw on top of it, a green mark three millimetres past the
        /// blade, the cut cubes heaping up to the left - a line of counts and a time bar; and, once it is done, the
        /// result and "Do zlewu". Play-area units: centre at 0,0; the board's top at y = -20; the knife at x = -160.
        /// </summary>
        private static void BuildPrepPanel(Transform canvas, Transform parent, ShiftController shiftController, DishInputRouter inputRouter)
        {
            const float knifeX = -160f;
            const float boardTop = -20f;
            const float vegetableHeight = 120f;
            const float pixelsPerMm = 8f;

            GameObject root = CreateUIObject("PrepBoard", parent);
            StretchFull(RT(root));
            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f); // also takes the taps: the whole card is the cutting surface

            Image card = CreatePanel("Card", root.transform, new Color(0.09f, 0.1f, 0.11f, 0.97f));
            AnchorCenter(RT(card.gameObject), new Vector2(960, 1200), new Vector2(0, 90));
            Shadow cardShadow = card.gameObject.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            cardShadow.effectDistance = new Vector2(0f, -10f);

            TextMeshProUGUI header = CreateText("Header", card.transform, "Deska przed zmianą", 58, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            header.color = new Color(1f, 0.82f, 0.38f);
            AnchorTopCenter(RT(header.gameObject), new Vector2(880, 76), new Vector2(0, -28));
            Scalable(header);

            TextMeshProUGUI title = CreateText("Vegetable", card.transform, string.Empty, 40, TextAlignmentOptions.Center);
            title.enableAutoSizing = true;
            title.fontSizeMin = 26;
            title.fontSizeMax = 40;
            AnchorTopCenter(RT(title.gameObject), new Vector2(880, 56), new Vector2(0, -104));
            Scalable(title);

            GameObject roundView = CreateUIObject("RoundView", card.transform);
            StretchFull(RT(roundView));

            TextMeshProUGUI instruction = CreateText("Instruction", roundView.transform,
                "Tnij, gdy warzywo dojdzie do zielonej kreski (3 mm). Za wcześnie - palce!", 32, TextAlignmentOptions.Center);
            instruction.enableAutoSizing = true;
            instruction.fontSizeMin = 22;
            instruction.fontSizeMax = 32;
            instruction.color = new Color(0.88f, 0.95f, 1f);
            AnchorTopCenter(RT(instruction.gameObject), new Vector2(860, 96), new Vector2(0, -166));
            Scalable(instruction);

            // The board, side on. Centre pivot: the panel places everything in play-area units around it.
            GameObject area = CreateUIObject("PlayArea", roundView.transform);
            RectTransform areaRect = RT(area);
            AnchorTopCenter(areaRect, new Vector2(880, 560), Vector2.zero);
            areaRect.pivot = new Vector2(0.5f, 0.5f);
            areaRect.anchoredPosition = new Vector2(0f, -270f - 280f);

            // A brushed steel counter, and on it a wooden board: grain on its top, its front edge in shadow (drawn, UiArt).
            Image counter = CreatePanel("Counter", area.transform, Color.white);
            counter.sprite = UiArt.SteelCounter;
            counter.type = Image.Type.Simple;
            AnchorCenter(RT(counter.gameObject), new Vector2(880, 190), new Vector2(0, boardTop - 70f - 95f));
            Image board = CreatePanel("Board", area.transform, Color.white);
            board.sprite = UiArt.WoodBoard;
            board.type = Image.Type.Simple;
            AnchorCenter(RT(board.gameObject), new Vector2(840, 70), new Vector2(0, boardTop - 35f));
            DropShadow(board, 6f, 0.4f);

            GameObject heap = CreateUIObject("Heap", area.transform);
            StretchFull(RT(heap));
            Image cubeTemplate = CreatePanel("Cube", heap.transform, Color.white);
            cubeTemplate.sprite = UiArt.CutPiece; // a lit top face and a shaded side, tinted per vegetable by the panel
            cubeTemplate.raycastTarget = false;
            DropShadow(cubeTemplate, 3f, 0.3f);
            AnchorCenter(RT(cubeTemplate.gameObject), new Vector2(24, 60), Vector2.zero);

            Image vegetable = CreatePanel("Vegetable", area.transform, Color.white);
            vegetable.sprite = UiArt.VegetableBody; // round, lit from above (9-sliced: it shortens as it is cut)
            vegetable.raycastTarget = false;
            DropShadow(vegetable, 6f, 0.35f);
            RectTransform vegetableRect = RT(vegetable.gameObject);
            AnchorCenter(vegetableRect, new Vector2(400, vegetableHeight), Vector2.zero);
            vegetableRect.pivot = new Vector2(0f, 0.5f);
            vegetableRect.anchoredPosition = new Vector2(knifeX, boardTop + vegetableHeight * 0.5f);
            // The fresh cut face at its end: paler and wetter than the skin.
            Image cutFace = CreatePanel("CutFace", vegetable.transform, new Color(1f, 1f, 1f, 0.55f));
            cutFace.raycastTarget = false;
            RectTransform faceRect = RT(cutFace.gameObject);
            faceRect.anchorMin = new Vector2(0f, 0.08f);
            faceRect.anchorMax = new Vector2(0f, 0.92f);
            faceRect.pivot = new Vector2(0f, 0.5f);
            faceRect.sizeDelta = new Vector2(10f, 0f);
            faceRect.anchoredPosition = new Vector2(2f, 0f);

            // Where the cut end should be when the knife comes down: 2-4 mm past the blade, the mark at 3.
            Image zone = CreatePanel("EvenZone", area.transform, new Color(0.4f, 1f, 0.45f, 0.22f));
            zone.raycastTarget = false;
            AnchorCenter(RT(zone.gameObject), new Vector2(2f * pixelsPerMm, vegetableHeight + 40f),
                new Vector2(knifeX - 3f * pixelsPerMm, boardTop + vegetableHeight * 0.5f));
            Image mark = CreatePanel("Mark", area.transform, new Color(0.45f, 1f, 0.5f));
            mark.raycastTarget = false;
            AnchorCenter(RT(mark.gameObject), new Vector2(4f, vegetableHeight + 70f),
                new Vector2(knifeX - 3f * pixelsPerMm, boardTop + vegetableHeight * 0.5f));

            // The claw: fingertips tucked, knuckles against the flat of the blade; a blue plaster once nicked.
            Image claw = CreatePanel("Claw", area.transform, Color.white);
            claw.sprite = UiArt.Claw; // the fingers, their nails and knuckles drawn in one shaded hand
            claw.type = Image.Type.Simple;
            claw.raycastTarget = false;
            AnchorCenter(RT(claw.gameObject), new Vector2(170, 85), new Vector2(knifeX + 100f, boardTop + vegetableHeight + 30f));
            DropShadow(claw, 5f, 0.35f);

            Image plaster = CreatePanel("Plaster", claw.transform, new Color(0.3f, 0.58f, 0.95f));
            plaster.raycastTarget = false;
            AnchorCenter(RT(plaster.gameObject), new Vector2(46, 70), new Vector2(-50f, 0f));
            plaster.gameObject.SetActive(false);

            // The knife: a steel blade over the cut, its handle above; the panel drops it to the board on every cut.
            GameObject knife = CreateUIObject("Knife", area.transform);
            RectTransform knifeRect = RT(knife);
            AnchorCenter(knifeRect, Vector2.zero, new Vector2(knifeX, 0f));
            // Steel with a ground edge, a bolster and a riveted wooden handle - each shaded round (UiArt).
            Image blade = CreatePanel("Blade", knife.transform, Color.white);
            blade.sprite = UiArt.BladeSteel;
            blade.type = Image.Type.Simple;
            blade.raycastTarget = false;
            AnchorCenter(RT(blade.gameObject), new Vector2(18, 100), new Vector2(0f, boardTop + vegetableHeight + 10f + 50f));
            DropShadow(blade, 3f, 0.3f);
            Image bolster = CreatePanel("Bolster", knife.transform, new Color(0.72f, 0.74f, 0.78f));
            bolster.sprite = UiArt.Cylinder;
            bolster.type = Image.Type.Simple;
            AnchorCenter(RT(bolster.gameObject), new Vector2(26, 14), new Vector2(0f, boardTop + vegetableHeight + 10f + 100f + 7f));
            Image handle = CreatePanel("Handle", knife.transform, new Color(0.36f, 0.21f, 0.11f));
            handle.sprite = UiArt.Cylinder;
            handle.type = Image.Type.Simple;
            handle.raycastTarget = false;
            AnchorCenter(RT(handle.gameObject), new Vector2(24, 56), new Vector2(0f, boardTop + vegetableHeight + 10f + 100f + 14f + 28f)); // its end at the area's top
            foreach (float y in new[] { -15f, 0f, 15f })
            {
                CreateDisc("Rivet", handle.transform, 8f, new Vector2(0f, y), new Color(0.85f, 0.86f, 0.88f)).sprite = UiArt.Dome;
            }

            TextMeshProUGUI cutLabel = CreateText("CutLabel", area.transform, string.Empty, 42, TextAlignmentOptions.Center);
            cutLabel.fontStyle = FontStyles.Bold;
            AnchorCenter(RT(cutLabel.gameObject), new Vector2(360, 60), new Vector2(knifeX + 260f, 235f));

            Image flash = CreatePanel("Flash", area.transform, new Color(1f, 0.15f, 0.1f, 0f));
            flash.raycastTarget = false;
            StretchFull(RT(flash.gameObject));
            flash.enabled = false;

            TextMeshProUGUI stats = CreateText("Stats", roundView.transform, string.Empty, 30, TextAlignmentOptions.Center);
            stats.enableAutoSizing = true;
            stats.fontSizeMin = 20;
            stats.fontSizeMax = 30;
            AnchorTopCenter(RT(stats.gameObject), new Vector2(880, 50), new Vector2(0, -846));
            Scalable(stats);

            Image barBack = CreatePanel("TimeBar", roundView.transform, new Color(0.03f, 0.03f, 0.04f, 0.8f));
            AnchorTopCenter(RT(barBack.gameObject), new Vector2(700, 38), new Vector2(0, -912));
            Image barFill = CreatePanel("Fill", barBack.transform, new Color(1f, 0.78f, 0.3f));
            StretchFull(RT(barFill.gameObject));
            RT(barFill.gameObject).offsetMin = new Vector2(6f, 6f);
            RT(barFill.gameObject).offsetMax = new Vector2(-6f, -6f);

            GameObject resultView = CreateUIObject("ResultView", card.transform);
            StretchFull(RT(resultView));
            TextMeshProUGUI result = CreateText("Result", resultView.transform, string.Empty, 42, TextAlignmentOptions.Center);
            result.enableAutoSizing = true;
            result.fontSizeMin = 26;
            result.fontSizeMax = 42;
            AnchorTopCenter(RT(result.gameObject), new Vector2(820, 640), new Vector2(0, -200));
            Scalable(result);
            Button toTheSink = CreateButton("PrepContinueButton", resultView.transform, "Do zlewu");
            AnchorTopCenter(RT(toTheSink.gameObject), new Vector2(460, 130), new Vector2(0, -1040));
            resultView.SetActive(false);

            root.SetActive(false);

            GameObject controllerGo = CreateUIObject("PrepBoardPanel", canvas);
            PrepBoardPanel panel = controllerGo.AddComponent<PrepBoardPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("shiftController").objectReferenceValue = shiftController;
            so.FindProperty("inputRouter").objectReferenceValue = inputRouter;
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("statsText").objectReferenceValue = stats;
            so.FindProperty("cutLabel").objectReferenceValue = cutLabel;
            so.FindProperty("resultText").objectReferenceValue = result;
            so.FindProperty("playArea").objectReferenceValue = areaRect;
            so.FindProperty("vegetable").objectReferenceValue = vegetableRect;
            so.FindProperty("vegetableImage").objectReferenceValue = vegetable;
            so.FindProperty("knife").objectReferenceValue = knifeRect;
            so.FindProperty("claw").objectReferenceValue = RT(claw.gameObject);
            so.FindProperty("plaster").objectReferenceValue = plaster.gameObject;
            so.FindProperty("cubeTemplate").objectReferenceValue = cubeTemplate;
            so.FindProperty("flash").objectReferenceValue = flash;
            so.FindProperty("timeFill").objectReferenceValue = barFill;
            so.FindProperty("roundView").objectReferenceValue = roundView;
            so.FindProperty("resultView").objectReferenceValue = resultView;
            so.FindProperty("continueButton").objectReferenceValue = toTheSink;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The shift's pause: a round button between the timer and the counters (big enough for a thumb, and
        /// drawn with two bars rather than a font glyph), and a card with "back to the sink" and "end the shift".
        /// </summary>
        private static KitchenPauseMenu BuildPauseMenu(Transform canvas, Transform parent, ShiftController shiftController,
            DishInputRouter inputRouter, DishwasherRepairPanel repairPanel)
        {
            Button pauseButton = CreateButton("PauseButton", parent, string.Empty);
            Image pauseBack = pauseButton.GetComponent<Image>();
            pauseBack.sprite = UiArt.Circle;
            pauseBack.type = Image.Type.Simple;
            pauseBack.color = new Color(1f, 1f, 1f, 0.14f);
            AnchorTopCenter(RT(pauseButton.gameObject), new Vector2(130, 130), new Vector2(-20, -26));
            foreach (float x in new[] { -15f, 15f })
            {
                Image bar = CreatePanel("Bar", pauseButton.transform, Color.white);
                AnchorCenter(RT(bar.gameObject), new Vector2(18, 50), new Vector2(x, 0));
                bar.raycastTarget = false;
            }

            GameObject root = CreateUIObject("PauseMenu", parent);
            StretchFull(RT(root));
            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);

            Image card = CreatePanel("Card", root.transform, new Color(0.09f, 0.1f, 0.11f, 0.97f));
            AnchorCenter(RT(card.gameObject), new Vector2(860, 960), new Vector2(0, 60));
            root.AddComponent<PanelTransition>();
            TextMeshProUGUI title = CreateText("Title", card.transform, "Przerwa", 60, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            AnchorTopCenter(RT(title.gameObject), new Vector2(760, 90), new Vector2(0, -40));
            TextMeshProUGUI note = CreateText("Note", card.transform, "Zegar zmiany stoi. Góra też czeka.", 32, TextAlignmentOptions.Center);
            note.color = new Color(0.8f, 0.86f, 0.9f);
            AnchorTopCenter(RT(note.gameObject), new Vector2(760, 60), new Vector2(0, -140));
            Scalable(note);

            Button resume = CreateButton("ResumeButton", card.transform, "Wracam do zlewu");
            resume.GetComponent<Image>().color = ButtonGreen;
            AnchorTopCenter(RT(resume.gameObject), new Vector2(600, 130), new Vector2(0, -240));
            Button settings = CreateButton("PauseSettingsButton", card.transform, "Ustawienia");
            settings.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.32f);
            AnchorTopCenter(RT(settings.gameObject), new Vector2(600, 130), new Vector2(0, -390));
            Button endShift = CreateButton("EndShiftButton", card.transform, "Kończę zmianę");
            endShift.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.32f);
            AnchorTopCenter(RT(endShift.gameObject), new Vector2(600, 130), new Vector2(0, -540));
            Button toMenu = CreateButton("PauseMainMenuButton", card.transform, "Menu główne");
            toMenu.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.32f);
            AnchorTopCenter(RT(toMenu.gameObject), new Vector2(600, 130), new Vector2(0, -690));
            TextMeshProUGUI saved = CreateText("SavedNote", card.transform, "Wypłata za to, co umyte, zostaje - nic nie przepada.", 26,
                TextAlignmentOptions.Center);
            saved.color = new Color(0.75f, 0.8f, 0.85f);
            AnchorTopCenter(RT(saved.gameObject), new Vector2(760, 60), new Vector2(0, -840));
            Scalable(saved);
            root.SetActive(false);

            GameObject settingsPanel = BuildSettingsPanel(parent);
            GameObject confirm = BuildConfirm("MenuConfirmPanel", parent, "Wrócić do menu?",
                "Zmiana skończy się teraz: dostaniesz wypłatę za to, co już umyte, i dzień się zaliczy.", "Do menu", "Zostaję",
                out Button confirmYes, out Button confirmNo, out _);

            GameObject controllerGo = CreateUIObject("KitchenPauseMenu", canvas);
            KitchenPauseMenu menu = controllerGo.AddComponent<KitchenPauseMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("shiftController").objectReferenceValue = shiftController;
            so.FindProperty("inputRouter").objectReferenceValue = inputRouter;
            so.FindProperty("repairPanel").objectReferenceValue = repairPanel;
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("endShiftButton").objectReferenceValue = endShift;
            so.FindProperty("settingsButton").objectReferenceValue = settings;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("menuButton").objectReferenceValue = toMenu;
            so.FindProperty("menuConfirm").objectReferenceValue = confirm;
            so.FindProperty("menuYesButton").objectReferenceValue = confirmYes;
            so.FindProperty("menuNoButton").objectReferenceValue = confirmNo;
            so.ApplyModifiedPropertiesWithoutUndo();
            return menu;
        }

        /// <summary>A soft drop shadow under a minigame prop, down the screen as the kitchen's light falls.</summary>
        private static void DropShadow(Graphic graphic, float distance, float alpha)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(distance * 0.35f, -distance);
        }

        private static Image CreateDisc(string name, Transform parent, float diameter, Vector2 position, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.sprite = UiArt.Circle;
            image.color = color;
            image.raycastTarget = false;
            AnchorCenter(RT(go), new Vector2(diameter, diameter), position);
            return image;
        }

        /// <summary>Tool button: icon on top, name underneath, highlighted while it is the tool in hand.</summary>
        private static void CreateToolButton(string name, Transform parent, string label, ToolDefinition tool, int index, int count, Vector2 rowSize)
        {
            Button button = CreateButton(name, parent, label);
            RectTransform rect = RT(button.gameObject);
            PositionInRow(rect, index, count, rowSize);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rowSize.y);
            Image background = button.GetComponent<Image>();
            background.color = new Color(0.2f, 0.23f, 0.27f);

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            RectTransform labelRect = RT(text.gameObject);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 64f);
            labelRect.anchoredPosition = new Vector2(0f, 10f);
            text.fontSizeMax = 32;

            Sprite iconSprite = tool != null ? UiArt.ToolIcon(tool.Id) : null;
            if (iconSprite != null)
            {
                GameObject iconGo = CreateUIObject("Icon", button.transform);
                Image icon = iconGo.AddComponent<Image>();
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                AnchorTopCenter(RT(iconGo), new Vector2(96, 96), new Vector2(0, -14));
            }

            ToolButton toolButton = button.gameObject.AddComponent<ToolButton>();
            var so = new SerializedObject(toolButton);
            so.FindProperty("tool").objectReferenceValue = tool;
            so.FindProperty("background").objectReferenceValue = background;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AnchorTopRight(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        /// <summary>Anchored and pivoted at its parent's bottom-left corner (the position is of its own bottom-left corner).</summary>
        private static void AnchorBottomLeft(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void AnchorBottomRight(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        // ------------------------------------------------------------------
        // 8. Build settings
        // ------------------------------------------------------------------

        [MenuItem("Zmywak/Setup/8. Register Scenes In Build Settings")]
        public static void RegisterBuildScenes()
        {
            string[] paths =
            {
                $"{ScenesFolder}/Boot.unity",
                $"{ScenesFolder}/MainMenu.unity",
                $"{ScenesFolder}/MetaHub.unity",
                $"{ScenesFolder}/Kitchen_Shift.unity"
            };

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
                else
                {
                    Debug.LogWarning($"Zmywak: scene not found, skipping: {path}");
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Zmywak: build settings updated.");
        }

        // ------------------------------------------------------------------
        // 9. Player settings for Android/iOS (Stage 6)
        // ------------------------------------------------------------------

        /// <summary>
        /// Not part of Run All on purpose - it switches Android to store builds (AAB) and sets the release
        /// version; run it once you're ready for Stage 6. The game's name and identifier are Run All's already
        /// (EnsureProductIdentity): check them before the first upload, after which the identifier can never
        /// change. See README section 8 for the full build walkthrough.
        /// </summary>
        [MenuItem("Zmywak/Setup/9. Configure Player Settings (Mobile)")]
        public static void ConfigurePlayerSettings()
        {
            EnsureProductIdentity();
            PlayerSettings.bundleVersion = "0.1.0";

            EnsurePortraitOnly();

            // ARM64-only Android needs IL2CPP (Mono can't target 64-bit on Android); Google Play has
            // required 64-bit support since 2019, so this isn't optional polish.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.bundleVersionCode = 1;
            EditorUserBuildSettings.buildAppBundle = true;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";

            AssetDatabase.SaveAssets();
            Debug.Log("Zmywak: player settings configured for Android/iOS (portrait, IL2CPP, ARM64, AAB). " +
                      $"Application Identifier: {ApplicationIdentifier} - it becomes permanent with the first store upload.");
        }

        public const string CompanyName = "Zielona Wyspa Games";
        /// <summary>The label under the icon: short, because a launcher cuts a long one off - and without the title's
        /// colon, which cannot be in the Windows folder the Editor saves to. The full title "ZMYWAK: Zielona Wyspa"
        /// is the main menu's (and will be the store listing's).</summary>
        public const string ProductName = "ZMYWAK";
        public const string ApplicationIdentifier = "com.zielonawyspagames.zmywak";

        /// <summary>
        /// The game's own name and identity on the phone, instead of what Unity derived from the project folder -
        /// "DefaultCompany" and "PowerWash Simulator", someone else's game, shown under the icon. Part of Run All:
        /// until the first store upload the identifier can still change freely (after it, never).
        /// </summary>
        public static void EnsureProductIdentity()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationIdentifier);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ApplicationIdentifier);
        }

        /// <summary>
        /// The game is drawn for a phone held upright (the kitchen's camera, every screen's layout): turning the phone
        /// must not turn the game. Unity's default - and this project's until now - is Auto Rotation with every
        /// direction allowed: only the manual "9." step set it right, and nothing ran that step. Run All, that step and
        /// the Android build all call this, so ProjectSettings holds the same however the project was prepared
        /// (ProjectConfigurationTests checks it).
        /// </summary>
        public static void EnsurePortraitOnly()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        // ------------------------------------------------------------------
        // Shared helpers
        // ------------------------------------------------------------------

        internal static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                // Always rewrite on save. If the generator's values equal the field defaults, applying them
                // is "no change" and the file would keep its old layout - renamed fields lingering, new ones
                // missing and only working because they happen to default to the same values.
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static Camera CreateKitchenCamera()
        {
            // Angled (not top-down) on purpose: the Pile grows along Y, and a straight top-down ortho
            // view makes height changes invisible on screen. This reads the stack's height properly.
            // The game is portrait, so the layout runs along Z (far = top of screen): rack + dishwasher
            // at the back, pile in the lower middle, all within |x| <= 2.5. The pitch is steep enough that
            // the counter fills the band between the top HUD and the bottom toolbar.
            const float pitchDegrees = 55f;
            var lookAt = new Vector3(0f, 0f, 0.2f);
            // A long lens rather than an orthographic view: the same framing at the counter (5 units half-height
            // at the look-at point), but seen from further back through a narrow angle. Nearer things are a little
            // bigger than further ones, which gives the scene depth, and the flat steel mirrors a slightly
            // different part of the room at every point instead of one direction all over (it came out one grey).
            const float halfHeight = 5f;
            const float fieldOfView = 24f;
            float distance = halfHeight / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);

            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraWidthFitter));
            go.tag = "MainCamera";
            var camera = go.GetComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.09f, 0.08f); // only ever visible past the wall's edges
            camera.transform.rotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
            camera.transform.position = lookAt - camera.transform.forward * distance;
            // The whole kitchen sits within ~9 units either side of the look-at point along the view. The default
            // 0.3-1000 range wastes almost all depth precision on empty space; with it, dishes resting on the
            // counter and the rack markers 1 cm above it z-fought into speckles along their edges.
            camera.nearClipPlane = distance - 9f;
            camera.farClipPlane = distance + 12f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var fitterSo = new SerializedObject(go.GetComponent<CameraWidthFitter>());
            fitterSo.FindProperty("minHalfWidth").floatValue = 2.8f;
            fitterSo.FindProperty("minOrthographicSize").floatValue = halfHeight;
            fitterSo.FindProperty("focusDistance").floatValue = distance;
            fitterSo.ApplyModifiedPropertiesWithoutUndo();
            return camera;
        }

        private static void CreateDirectionalLight()
        {
            // Warm afternoon light over the left shoulder; soft shadows ground the dishes on the counter
            // and show how the pile is stacked.
            var go = new GameObject("Directional Light", typeof(Light));
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            light.transform.rotation = Quaternion.Euler(58f, -35f, 0f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Expand: the canvas is never smaller than 1080x1920 either way - a tall 20:9 phone gets extra
            // height, a 4:3 tablet extra width. Matching width and height halfway instead shrank the canvas
            // to ~966 units wide on 20:9, and rows laid out for 1080 ran off both edges and into each other.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return canvas;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AnchorCenter(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void AnchorTopCenter(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void AnchorTopLeft(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void PositionInRow(RectTransform itemRect, int index, int count, Vector2 containerSize)
        {
            float itemWidth = containerSize.x / count;
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(0f, 0.5f);
            itemRect.pivot = new Vector2(0f, 0.5f);
            itemRect.sizeDelta = new Vector2(itemWidth - 20, 160);
            itemRect.anchoredPosition = new Vector2(index * itemWidth + 10, 0);
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = CreateUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        /// <summary>
        /// A text that follows the player's text-size setting. It is auto-sized with the size it was designed at
        /// as the ceiling, and TextScaler raises that ceiling: a larger setting grows it as far as its box holds
        /// and no further, so no setting can push it out over its neighbours (a longer line shrinks to fit too).
        /// </summary>
        private static void Scalable(TMP_Text text, float minShare = 0.6f)
        {
            if (!text.enableAutoSizing)
            {
                text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = Mathf.Round(text.fontSize * minShare);
            }

            // Auto-size fits by the letters' advances, but bold glyphs reach a few units past them: at the largest
            // setting "Portfel: 20,00 EUR" came out 3 units wider than its box. A little side margin keeps the ink in.
            Vector4 margin = text.margin;
            text.margin = new Vector4(Mathf.Max(margin.x, SideMargin), margin.y, Mathf.Max(margin.z, SideMargin), margin.w);
            text.gameObject.AddComponent<TextScaler>();
        }

        private const float SideMargin = 4f;

        private static readonly Color ButtonGreen = new Color(0.16f, 0.52f, 0.37f);
        private static readonly Color PanelDark = new Color(0.07f, 0.08f, 0.09f, 0.86f);

        /// <summary>
        /// A dimmed full-screen layer with a titled card in the middle (slang dictionary, settings). The
        /// returned object is the full-screen layer, which is what gets shown/hidden; content goes on top
        /// of the card, positioned relative to the screen centre.
        /// </summary>
        private static GameObject CreateModal(string name, Transform parent, Vector2 cardSize, string title)
        {
            GameObject modal = CreateUIObject(name, parent);
            StretchFull(RT(modal));
            Image dim = modal.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, ModalDimAlpha);

            Image card = CreatePanel("Card", modal.transform, new Color(0.09f, 0.1f, 0.11f, 0.98f));
            AnchorCenter(RT(card.gameObject), cardSize, Vector2.zero);
            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -10f);
            modal.AddComponent<PanelTransition>(); // the card grows in over the dimmed screen

            TextMeshProUGUI titleText = CreateText("Title", modal.transform, title, 46, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.85f, 0.35f);
            AnchorCenter(RT(titleText.gameObject), new Vector2(cardSize.x - 80f, 80f), new Vector2(0f, cardSize.y * 0.5f - 70f));
            return modal;
        }

        /// <summary>A rounded (9-sliced) panel image.</summary>
        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = UiArt.Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            GameObject go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = UiArt.Rounded;
            image.type = Image.Type.Sliced;
            image.color = ButtonGreen;
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -6f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            button.colors = colors;

            GameObject textGo = CreateUIObject("Label", go.transform);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            // Auto-size down (never up) so longer labels ("Uruchom zmywarkę", "Wyślij 20 EUR do domu")
            // shrink to fit their button instead of running into its edges.
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = 40;
            tmp.fontSizeMin = 22;
            tmp.fontSize = 40;
            tmp.margin = new Vector4(16f, 8f, 16f, 8f);
            StretchFull(RT(textGo));
            textGo.AddComponent<TextScaler>(); // already auto-sized: the setting raises its ceiling

            return button;
        }

        private static Toggle CreateToggle(string name, Transform parent, string label, Vector2 anchoredPosition)
        {
            // The whole row - box and label - is the touch target, 130 units tall (48 dp on dense small screens).
            GameObject go = CreateUIObject(name, parent);
            AnchorCenter(RT(go), new Vector2(880, 130), anchoredPosition);
            Image hitArea = go.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            GameObject background = CreateUIObject("Background", go.transform);
            RectTransform bgRect = RT(background);
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.sizeDelta = new Vector2(64, 64);
            bgRect.anchoredPosition = new Vector2(10, 0);
            Image bgImage = background.AddComponent<Image>();
            bgImage.sprite = UiArt.Rounded;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(1f, 1f, 1f, 0.2f);

            GameObject checkmark = CreateUIObject("Checkmark", background.transform);
            RectTransform checkRect = RT(checkmark);
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(8, 8);
            checkRect.offsetMax = new Vector2(-8, -8);
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.7f, 0.5f);

            Toggle toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            TextMeshProUGUI labelText = CreateText("Label", go.transform, label, 30, TextAlignmentOptions.Left);
            RectTransform labelRect = RT(labelText.gameObject);
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(780, 110);
            labelRect.anchoredPosition = new Vector2(96, 0);
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 24;
            labelText.fontSizeMax = 32;
            labelText.gameObject.AddComponent<TextScaler>();

            return toggle;
        }
    }
}
