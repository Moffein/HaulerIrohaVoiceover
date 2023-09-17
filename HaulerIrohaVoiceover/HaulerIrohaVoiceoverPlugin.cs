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
using BaseVoiceoverLib;

namespace HaulerIrohaVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.Potmobile")]
    [BepInDependency("com.Schale.HaulerIrohaSkin")]
    [BepInPlugin("com.Schale.HaulerIrohaVoiceover", "HaulerIrohaVoiceover", "1.1.0")]
    public class HaulerIrohaVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;
        public static SurvivorDef haulerSurvivorDef;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
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
            BodyIndex haulerIndex = BodyCatalog.FindBodyIndex("MoffeinHaulerBody");

            SkinDef irohaSkin = null;
            SkinDef[] skins = SkinCatalog.FindSkinsForBody(haulerIndex);
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "HaulerIrohaSkinDef")
                {
                    irohaSkin = skinDef;
                    break;
                }
            }

            if (!irohaSkin)
            {
                Debug.LogError("HaulerIrohaVoiceover: Hauler Iroha SkinDef not found. Voicelines will not work!");
            }
            else
            {
                VoiceoverInfo vo = new VoiceoverInfo(typeof(HaulerIrohaVoiceoverComponent),irohaSkin, "MoffeinHaulerBody");
                vo.selectActions += IrohaSelect;
            }

            RefreshNSE();
        }

        private void IrohaSelect(GameObject mannequinObject)
        {
            bool played = false;
            if (!playedSeasonalVoiceline)
            {
                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                {
                    Util.PlaySound("Play_HaulerIroha_Lobby_Newyear", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 11 && System.DateTime.Today.Day == 16)
                {
                    Util.PlaySound("Play_HaulerIroha_Lobby_bday", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                {
                    Util.PlaySound("Play_HaulerIroha_Lobby_Halloween", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                {
                    Util.PlaySound("Play_HaulerIroha_Lobby_xmas", mannequinObject);
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
                    Util.PlaySound("Play_HaulerIroha_TitleDrop", mannequinObject);
                }
                else
                {
                    Util.PlaySound("Play_HaulerIroha_Lobby", mannequinObject);
                }
            }
        }

        private void InitNSE()
        {
            HaulerIrohaVoiceoverComponent.nseTank = RegisterNSE("Play_HaulerIroha_TankCannon");
            HaulerIrohaVoiceoverComponent.nseCommon = RegisterNSE("Play_HaulerIroha_CommonSkill");
        }

        public void RefreshNSE()
        {
            foreach (NSEInfo nse in nseList)
            {
                nse.ValidateParams();
            }
        }

        private NetworkSoundEventDef RegisterNSE(string eventName)
        {
            NetworkSoundEventDef nse = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            nse.eventName = eventName;
            Content.networkSoundEventDefs.Add(nse);
            nseList.Add(new NSEInfo(nse));
            return nse;
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
