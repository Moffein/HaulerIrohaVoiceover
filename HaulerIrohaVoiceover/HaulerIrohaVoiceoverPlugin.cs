using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Audio;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using System.Collections.Generic;
using HaulerIrohaVoiceover.Modules;
using HaulerIrohaVoiceover.Components;

namespace HaulerIrohaVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.Potmobile")]
    [BepInDependency("com.Schale.HaulerIrohaSkin")]
    [BepInPlugin("com.Schale.HaulerIrohaVoiceover", "HaulerIrohaVoiceover", "1.0.1")]
    public class HaulerIrohaVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;
        public static SurvivorDef haulerSurvivorDef;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            BaseVoiceoverComponent.Init();
            RoR2.RoR2Application.onLoad += OnLoad;
            new Content().Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HaulerIrohaVoiceover.haulerirohavoiceoverbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the Hauler Iroha Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }

        }
        private void EnableVoicelines_SettingChanged(object sender, EventArgs e)
        {
            RefreshNSE();
        }

        private void Start()
        {
            SoundBanks.Init();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("Iroha"));
        }

        private void OnLoad()
        {
            bool foundSkin = false;
            BodyIndex haulerIndex = BodyCatalog.FindBodyIndex("MoffeinHaulerBody");
            SurvivorIndex si = SurvivorCatalog.GetSurvivorIndexFromBodyIndex(haulerIndex);
            haulerSurvivorDef = SurvivorCatalog.GetSurvivorDef(si);

            SkinDef[] skins = SkinCatalog.FindSkinsForBody(haulerIndex);
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "HaulerIrohaSkinDef")
                {
                    foundSkin = true;
                    HaulerIrohaVoiceoverComponent.requiredSkinDefs.Add(skinDef);
                    break;
                }
            }

            if (!foundSkin)
            {
                Debug.LogError("HaulerIrohaVoiceover: Hauler Iroha SkinDef not found. Voicelines will not work!");
            }
            else if (haulerSurvivorDef)
            {
                On.RoR2.CharacterBody.Start += AttachVoiceoverComponent;
                On.RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance += (orig, self) =>
                {
                    orig(self);
                    if (self.currentSurvivorDef == haulerSurvivorDef)
                    {
                        //Loadout isn't loaded first time this is called, so we need to manually get it.
                        //Probably not the most elegant way to resolve this.
                        if (self.loadoutDirty)
                        {
                            if (self.networkUser)
                            {
                                self.networkUser.networkLoadout.CopyLoadout(self.currentLoadout);
                            }
                        }

                        //Check SkinDef
                        BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(self.currentSurvivorDef.survivorIndex);
                        int skinIndex = (int)self.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
                        SkinDef safe = HG.ArrayUtils.GetSafe<SkinDef>(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
                        if (true && enableVoicelines.Value && HaulerIrohaVoiceoverComponent.requiredSkinDefs.Contains(safe))
                        {
                            bool played = false;
                            if (!playedSeasonalVoiceline)
                            {
                                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                                {
                                    Util.PlaySound("Play_HaulerIroha_Lobby_Newyear", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 11 && System.DateTime.Today.Day == 16)
                                {
                                    Util.PlaySound("Play_HaulerIroha_Lobby_bday", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                                {
                                    Util.PlaySound("Play_HaulerIroha_Lobby_Halloween", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                                {
                                    Util.PlaySound("Play_HaulerIroha_Lobby_xmas", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }

                                if (played)
                                {
                                    playedSeasonalVoiceline = true;
                                }
                            }
                            if (!played)
                            {
                                if (Util.CheckRoll(5f))
                                {
                                    Util.PlaySound("Play_HaulerIroha_TitleDrop", self.mannequinInstanceTransform.gameObject);
                                }
                                else
                                {
                                    Util.PlaySound("Play_HaulerIroha_Lobby", self.mannequinInstanceTransform.gameObject);
                                }
                            }
                        }
                    }
                };
            }
            HaulerIrohaVoiceoverComponent.ScepterIndex = ItemCatalog.FindItemIndex("ITEM_ANCIENT_SCEPTER");

            //Add NSE here
            nseList.Add(new NSEInfo(HaulerIrohaVoiceoverComponent.nseTank));
            nseList.Add(new NSEInfo(HaulerIrohaVoiceoverComponent.nseCommon));
            RefreshNSE();
        }

        private void AttachVoiceoverComponent(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self)
            {
                if (self.bodyIndex == BodyCatalog.FindBodyIndex("MoffeinHaulerBody") && (HaulerIrohaVoiceoverComponent.requiredSkinDefs.Contains(SkinCatalog.GetBodySkinDef(self.bodyIndex, (int)self.skinIndex))))
                {
                    BaseVoiceoverComponent existingVoiceoverComponent = self.GetComponent<BaseVoiceoverComponent>();
                    if (!existingVoiceoverComponent) self.gameObject.AddComponent<HaulerIrohaVoiceoverComponent>();
                }
            }
        }

        private void InitNSE()
        {
            HaulerIrohaVoiceoverComponent.nseTank = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            HaulerIrohaVoiceoverComponent.nseTank.eventName = "Play_HaulerIroha_TankCannon";
            Content.networkSoundEventDefs.Add(HaulerIrohaVoiceoverComponent.nseTank);

            HaulerIrohaVoiceoverComponent.nseCommon = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            HaulerIrohaVoiceoverComponent.nseCommon.eventName = "Play_HaulerIroha_CommonSkill";
            Content.networkSoundEventDefs.Add(HaulerIrohaVoiceoverComponent.nseCommon);
        }

        public void RefreshNSE()
        {
            foreach (NSEInfo nse in nseList)
            {
                nse.ValidateParams();
            }
        }

        public static List<NSEInfo> nseList = new List<NSEInfo>();
        public class NSEInfo
        {
            public NetworkSoundEventDef nse;
            public uint akId = 0u;
            public string eventName = string.Empty;

            public NSEInfo(NetworkSoundEventDef source)
            {
                this.nse = source;
                this.akId = source.akId;
                this.eventName = source.eventName;
            }

            private void DisableSound()
            {
                nse.akId = 0u;
                nse.eventName = string.Empty;
            }

            private void EnableSound()
            {
                nse.akId = this.akId;
                nse.eventName = this.eventName;
            }

            public void ValidateParams()
            {
                if (this.akId == 0u) this.akId = nse.akId;
                if (this.eventName == string.Empty) this.eventName = nse.eventName;

                if (!enableVoicelines.Value)
                {
                    DisableSound();
                }
                else
                {
                    EnableSound();
                }
            }
        }
    }
}
