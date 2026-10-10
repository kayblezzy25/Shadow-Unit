using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowUnit.EditorTools
{
    /// <summary>Menu commands that create the prototype scene and the assets it loads at runtime.</summary>
    public static class ShadowUnitSetup
    {
        const string Root = "Assets/ShadowUnit";
        const string Res = "Assets/ShadowUnit/Resources/ShadowUnit";
        const string ScenePath = "Assets/ShadowUnit/Scenes/Frontline.unity";

        [MenuItem("Shadow Unit/Create Prototype Scene", priority = 0)]
        public static void CreatePrototypeScene()
        {
            CreateBaseAssets();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("ShadowUnit");
            var boot = go.AddComponent<GameBootstrap>();
            boot.primaryWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>(Res + "/Weapons/VK7_Carbine.asset");

            EnsureFolder(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.productName = "Shadow Unit Frontline";

            EditorUtility.DisplayDialog("Shadow Unit",
                "Prototype scene created at " + ScenePath + " and added to Build Settings.\n\n" +
                "Press Play to test. In the Editor, click the right half of the Game view to use mouse and keyboard (needs Active Input Handling set to 'Both').",
                "OK");
        }

        [MenuItem("Shadow Unit/Create Base Assets", priority = 1)]
        public static void CreateBaseAssets()
        {
            EnsureFolder(Res);
            EnsureFolder(Res + "/Weapons");
            bool urp = GraphicsSettings.currentRenderPipeline != null;

            Shader litShader = urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            CreateOrUpdateMaterial(Res + "/SU_Lit.mat", litShader);
            // a saved material with emission on, so builds keep the emissive shader variant
            Material emissive = CreateOrUpdateMaterial(Res + "/SU_Emissive.mat", litShader);
            if (emissive != null)
            {
                emissive.EnableKeyword("_EMISSION");
                emissive.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (emissive.HasProperty("_EmissionColor")) emissive.SetColor("_EmissionColor", Color.white);
                EditorUtility.SetDirty(emissive);
            }
            CreateOrUpdateMaterial(Res + "/SU_Unlit.mat", Shader.Find("Sprites/Default"));

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = CreateOrUpdateMaterial(Res + "/SU_Sky.mat", skyShader);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.035f);
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.15f);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.55f, 0.6f, 0.68f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.36f, 0.34f, 0.3f));
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.15f);
                EditorUtility.SetDirty(sky);
            }

            string weaponPath = Res + "/Weapons/VK7_Carbine.asset";
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath) == null)
            {
                var w = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(w, weaponPath);
            }
            CreateWeapons();
            CreateOperators();
            CreateLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Creates the operators' starting guns if missing (the VK-7 Carbine is made above). Existing assets keep your tuning.</summary>
        static void CreateWeapons()
        {
            Weapon("Hornet9_SMG", w =>
            {
                w.weaponName = "Hornet-9 SMG";
                w.category = WeaponCategory.SMG;
                w.model = WeaponModelType.SMG;
                w.price = 1200;
                w.damage = 18f; w.headshotMultiplier = 1.6f;
                w.effectiveRange = 18f; w.minDamageFalloff = 0.5f; w.maxRange = 120f;
                w.fireRate = 900f; w.magazineSize = 33; w.reserveAmmo = 165; w.reloadTime = 1.8f;
                w.hipSpread = 1.8f; w.adsSpread = 0.6f; w.spreadPerShot = 0.25f; w.maxExtraSpread = 3f; w.spreadRecovery = 8f; w.moveSpreadMultiplier = 1.2f;
                w.recoilVertical = 0.55f; w.recoilHorizontal = 0.4f; w.recoilRecovery = 9f; w.adsRecoilMultiplier = 0.7f;
                w.hipFov = 70f; w.adsFov = 58f; w.adsTime = 0.13f;
            });
            Weapon("Longreach_DMR", w =>
            {
                w.weaponName = "Longreach DMR";
                w.category = WeaponCategory.DMR;
                w.fireMode = FireMode.SemiAutomatic;
                w.model = WeaponModelType.DMR;
                w.price = 2400;
                w.suppressed = true;
                w.damage = 46f; w.headshotMultiplier = 2f;
                w.effectiveRange = 70f; w.minDamageFalloff = 0.75f; w.maxRange = 260f;
                w.fireRate = 300f; w.magazineSize = 20; w.reserveAmmo = 80; w.reloadTime = 2.6f;
                w.hipSpread = 3.5f; w.adsSpread = 0.08f; w.spreadPerShot = 0.6f; w.maxExtraSpread = 2.5f; w.spreadRecovery = 5f; w.moveSpreadMultiplier = 2f;
                w.recoilVertical = 1.9f; w.recoilHorizontal = 0.3f; w.recoilRecovery = 6f; w.adsRecoilMultiplier = 0.6f;
                w.hipFov = 70f; w.adsFov = 38f; w.adsTime = 0.26f;
            });
        }

        static void Weapon(string file, System.Action<WeaponData> setup)
        {
            string path = Res + "/Weapons/" + file + ".asset";
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(path) != null) return;
            var w = ScriptableObject.CreateInstance<WeaponData>();
            setup(w);
            AssetDatabase.CreateAsset(w, path);
        }

        /// <summary>Creates the five campaign level assets if missing (generated from docs/WORLD.md). Existing assets keep your edits.</summary>
        static void CreateLevels()
        {
            EnsureFolder(Res + "/Levels");
            Level(1, "Lantern Row", "Market District", 1, 6f,
                "The Ward's water stalls doubled their prices overnight. An informant in the Market District says the Maw Syndicate keeps a ledger of who is buying the stolen river water, and who is selling it.",
                new[] { Obj(ObjectiveType.Reach, "Meet the informant at the noodle stall halfway up Lantern Row."), Obj(ObjectiveType.StealIntel, "Steal the Syndicate ledger from the counting house under the shark sign.") },
                "Outer Road gate (east side)", 12, "Syndicate enforcers (rifles), lookouts on balconies (SMGs).",
                "", "",
                "A firefight cuts the ropes on the giant shark-head sign and it crashes into the street, opening a new path and crushing a Syndicate barricade.",
                "####################\n#..E....#O#....E...#\n#.##.##.#.#.##.##..#\n#.......#.#........#\n#.#..E..###...E.#..#\n#.#.............#..#\n#...##.##..##.##...X\n#..................#\n#.##..E.##..##.E.#.#\n#.#.......O......#.#\n#.##.##.....##.##..#\n#.....E.......E....#\n#.##.##.##..##.##.##\n#...........E......#\n#.#.#.#.#..#.#.#.#.#\n#..................#\n#.##.##..##..##.##.#\n#.........S........#\n#.........S........#\n####################");
            Level(2, "Below the Ward", "Undercity Tunnels", 2, 7f,
                "The ledger points underground: the Syndicate has tapped the old waterworks beneath the Ward and is pumping the river out through the Undercity Tunnels. A settler engineer who found the pumps has gone missing down there.",
                new[] { Obj(ObjectiveType.Destroy, "Wreck the three pump valves feeding the Syndicate's pipeline."), Obj(ObjectiveType.Rescue, "Find and free the captured settler engineer.") },
                "Maintenance lift to the surface (south-east)", 14, "Syndicate enforcers, shotgunners in tight corridors, a sniper over the flooded cistern.",
                "", "",
                "Blowing the last valve bursts the main pipe: the cistern floods, the lights fail and the squad fights out by torchlight.",
                "####################\n#S.....#....E....O.#\n#S.###.#.#####.###.#\n#..#...#.....#...#.#\n#..#.E.###.#.#.E.#.#\n#..#.......#.#...#.#\n#.####.###.#.###.#.#\n#......#~~~~~~~#...#\n#.E.##.#~~~~~~~#.E.#\n#...#..#~~O~~~~#...#\n#.###..#~~~~~~~#.###\n#......####.####...#\n#.####......E....#.#\n#....#.####.####.#.#\n#.E..#.#O......#.#.#\n#.##.#.#.####..#...#\n#.#..E.#....#.E###.#\n#.#.####.##.#.....O#\n#..........E#.....X#\n####################");
            Level(3, "The Wreck", "The Hub", 3, 8f,
                "With the pumps down, the Syndicate pulls back to its fortress: the Hub, a settlement built inside a crashed sky-freighter. The engineer's notes show the pipeline carries on past the Hub, towards the Orbital City.",
                new[] { Obj(ObjectiveType.Destroy, "Cut the Hub's power at the generator yard."), Obj(ObjectiveType.Reach, "Breach the freighter hull and reach the bridge deck."), Obj(ObjectiveType.Destroy, "Defeat Mother Maw.") },
                "Armoured truck on the Outer Road", 16, "Syndicate enforcers, shotgunners, heavy gunners on the hull, Mother Maw's guards.",
                "Mother Maw", "Syndicate leader in scrap plate armour with a belt-fed gun. Armour phases at 66% and 33% health; at each phase she calls 2 guards and drops a smoke screen. Weak spot: the fuel canister on her back.",
                "Cutting the power drops the freighter's tail section: the hull slides a few metres down the cliff mid-fight and the floor tilts.",
                "####################\n#.....##########...#\n#....#....B.....#..#\n#...#..E.....E...#.#\n#..#.###.##.###...##\n#..#....O.....E...##\n#...#.##.###.##..#.#\n#....#..E......##..#\n#.....##########...#\n#..........#.......#\n#..##..E...#...##..#\n#..##......#.E.##..#\n#......##.....##...#\n#.E....##..O..##.E.#\n#..##.........##...#\n#..##..##..##......#\n#......##..##..E...#\n#.S..............X.#\n#.S................#\n####################");
            Level(4, "Ship Graveyard", "The Shattered Coast", 4, 8f,
                "The pipeline ends at the Shattered Coast, where Spire Directorate troops load water tankers bound for the Orbital City. The settlers want their people off the coast before the Directorate seals it.",
                new[] { Obj(ObjectiveType.Reach, "Secure the foot of the Main Bridge."), Obj(ObjectiveType.Defend, "Hold the docks for 90 seconds while the settlers' boats leave."), Obj(ObjectiveType.Destroy, "Bring down the Directorate radio mast on the beached tanker.") },
                "Cliff Road switchback (east)", 18, "Directorate riflemen with armour, marksmen on wrecks, a landing squad mid-level.",
                "", "",
                "Directorate charges blow a span out of the Main Bridge while the squad is crossing; they finish the crossing over the wrecks instead.",
                "####################\n#~~~~##~~~~~~##~~~~#\n#~~~####~~~~####~~~#\n#~~~~##~~~O~~##~~~~#\n#~~~~~~~~~~~~~~~~~~#\n#~~##~~~~~~~~~~##~~#\n#=================.#\n#~~~~E~~~~~E~~~~~~.#\n#~~~~~~~~~~~~~~~~~.#\n#....##....##......#\n#.E..##.O..##..E...#\n#......E.......##..#\n#.##..####..##.##..#\n#.##..#..#..##.....#\n#.....#O.#...E..##.#\n#..E..####.......X.#\n#.##.......##..##..#\n#.S...##...##......#\n#.S................#\n####################");
            Level(5, "The Last Watch", "Cliff Road and the Watchtower", 5, 10f,
                "The Directorate's commander is up at the old Watchtower, the only relay that can reach every settlement in the canyon. Send the ledger and the pipeline records from there and the whole Ward learns who has been stealing its river.",
                new[] { Obj(ObjectiveType.Escort, "Get the settler engineer up the Cliff Road to the Watchtower."), Obj(ObjectiveType.StealIntel, "Transmit the ledger from the Watchtower relay."), Obj(ObjectiveType.Destroy, "Defeat Warden Voss.") },
                "The pool below the Waterfall Cave", 20, "Directorate riflemen, marksmen, armoured heavies, a recon drone.",
                "Warden Voss", "Spire Directorate commander in powered armour with a shield drone. Armour phases at 66% and 33%: first the drone shields him, then he calls a landing squad, then he charges. Shoot the drone down to open the shield.",
                "As the transmission finishes, the Watchtower's legs give way; the squad jumps from the collapsing tower into the river and swims for the Waterfall Cave.",
                "####################\n#....#####B#####...#\n#....#...O.....#...#\n#....#..E...E..#...#\n#....###.....###...#\n#......#.....#.....#\n#.##...........E.###\n#....E....##.......#\n###..........##....#\n#....##..E.......E.#\n#.........###......#\n#.E..###.......##..#\n#......#...E.......#\n#.##...........##..#\n#....##..##........#\n#~~~.............E.#\n#~X~...##....##....#\n#~~~..........S....#\n#~~~..........S....#\n####################");
        }

        static LevelObjective Obj(ObjectiveType type, string description) => new LevelObjective { type = type, description = description };

        static void Level(int number, string levelName, string location, int difficulty, float minutes, string story,
            LevelObjective[] objectives, string extraction, int enemyCount, string enemyTypes, string boss, string bossNotes,
            string wow, string ascii)
        {
            string path = Res + "/Levels/Level" + number.ToString("00") + ".asset";
            if (AssetDatabase.LoadAssetAtPath<LevelData>(path) != null) return;
            var l = ScriptableObject.CreateInstance<LevelData>();
            l.number = number;
            l.levelName = levelName;
            l.location = location;
            l.difficulty = difficulty;
            l.estimatedMinutes = minutes;
            l.storyReason = story;
            l.objectives = objectives;
            l.extractionPoint = extraction;
            l.enemyCount = enemyCount;
            l.enemyTypes = enemyTypes;
            l.boss = boss;
            l.bossNotes = bossNotes;
            l.wowMoment = wow;
            l.asciiLayout = ascii;
            AssetDatabase.CreateAsset(l, path);
        }

        /// <summary>Creates the three launch operators if missing. Existing assets are left alone so your tuning is kept.</summary>
        static void CreateOperators()
        {
            EnsureFolder(Res + "/Operators");

            Operator("ZOX", OperatorClass.Assault, new Color(0.78f, 0.2f, 0.18f),
                "Grew up running salvage through flooded border towns. Joined Shadow Unit after her crew was wiped out by the people she now hunts. Fights up close, fast, and never in a straight line.",
                100f, 25f, 1.12f, 3.5f, 1f, "Hornet-9 SMG",
                "Red Rush", "For 5 s: +30% move speed, 40% faster reload and 20% less damage taken. She can fire while sprinting.", 30f, 5f,
                "Second Wind", "Health starts regenerating after 3.5 s without damage instead of 5 s.",
                "Wins close fights and retakes capture points fastest.",
                "Light armour (25): loses long-range duels and can't soak damage.");

            Operator("Scott", OperatorClass.Support, new Color(0.55f, 0.62f, 0.32f),
                "Former signals sergeant and the squad's voice in every earpiece. Keeps everyone stocked and alive. Talks calm, shoots steady.",
                100f, 75f, 0.92f, 5f, 1.1f, "VK-7 Carbine",
                "Resupply Crate", "Drops a crate for 8 s: allies within 5 m get one magazine per second and heal 8 HP per second.", 45f, 8f,
                "Comms Link", "Enemies spotted by any teammate stay on the minimap 2 s longer, and allies within 15 m reload 10% faster.",
                "Keeps the squad fighting longer; heaviest armour (75).",
                "Slowest mover and easiest to spot; longest ability cooldown.");

            Operator("Daed", OperatorClass.Recon, new Color(0.3f, 0.75f, 0.85f),
                "Nobody in the unit has seen his face in daylight. A night-raid specialist who reads the battlefield through four green tubes, he goes in first and quietly. When he speaks, it's a position.",
                90f, 50f, 1f, 5f, 0.75f, "Longreach DMR",
                "Night Sweep", "Pulses his quad-tube night vision: enemies within 40 m show on the minimap and through walls for 4 s.", 35f, 4f,
                "Quiet Feet", "Footsteps are silent while crouched, and enemies spot him from 25% shorter range.",
                "Finds enemies first and wins long-range fights.",
                "Lowest health (90) and weak up close.");
        }

        static void Operator(string codeName, OperatorClass cls, Color accent, string backstory,
            float health, float armour, float speed, float regenDelay, float spotRange, string weapon,
            string ability, string abilityDesc, float cooldown, float duration,
            string passive, string passiveDesc, string strength, string weakness)
        {
            string path = Res + "/Operators/" + codeName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<OperatorData>(path) != null) return;
            var o = ScriptableObject.CreateInstance<OperatorData>();
            o.codeName = codeName;
            o.operatorClass = cls;
            o.accent = accent;
            o.backstory = backstory;
            o.maxHealth = health;
            o.maxArmour = armour;
            o.moveSpeedMultiplier = speed;
            o.regenDelay = regenDelay;
            o.spotRangeMultiplier = spotRange;
            o.startingWeapon = weapon;
            o.abilityName = ability;
            o.abilityDescription = abilityDesc;
            o.abilityCooldown = cooldown;
            o.abilityDuration = duration;
            o.passiveName = passive;
            o.passiveDescription = passiveDesc;
            o.strength = strength;
            o.weakness = weakness;
            AssetDatabase.CreateAsset(o, path);
        }

        [MenuItem("Shadow Unit/Set Input Handling To Both (restarts needed)", priority = 20)]
        public static void SetInputHandlingBoth()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0) { ShowManualInputHelp(); return; }
            var so = new SerializedObject(settings[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            if (p == null) { ShowManualInputHelp(); return; }
            p.intValue = 2; // 0 = Input Manager (old), 1 = Input System, 2 = Both
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Shadow Unit", "Active Input Handling set to 'Both'. Restart Unity for it to take effect.", "OK");
        }

        static void ShowManualInputHelp()
        {
            EditorUtility.DisplayDialog("Shadow Unit", "Set it manually: Edit > Project Settings > Player > Other Settings > Active Input Handling = Both, then restart Unity.", "OK");
        }

        static Material CreateOrUpdateMaterial(string path, Shader shader)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (shader == null) { Debug.LogError("Shadow Unit: shader not found for " + path); return m; }
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader; // render pipeline changed since the asset was made
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
