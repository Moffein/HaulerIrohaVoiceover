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
    [BepInDependency(R2API.SoundAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.BaseVoiceoverLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Moffein.Potmobile", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Schale.HaulerIrohaSkin", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Schale.HaulerIrohaVoiceover", "HaulerIrohaVoiceover", "1.1.5")]
    public class HaulerIrohaVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<KeyboardShortcut> buttonTank, buttonCommon, buttonTitle, buttonCafe2, buttonDarui, buttonShikatanai, buttonThanks, buttonTonegawa, buttonSigh, buttonToramaru, buttonLaugh, buttonIntro;
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
            SoundBanks.Init();

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the Hauler Iroha Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;

            buttonTitle = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Blue Archive"), KeyboardShortcut.Empty);
            buttonIntro = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Introduction"), KeyboardShortcut.Empty);
            buttonToramaru = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Toramaru"), KeyboardShortcut.Empty);
            buttonTank = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Shuhou Hassha"), KeyboardShortcut.Empty);
            buttonCommon = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Kougeki"), KeyboardShortcut.Empty);
            buttonCafe2 = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Mendokusai"), KeyboardShortcut.Empty);
            buttonDarui = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Daaaarui"), KeyboardShortcut.Empty);
            buttonShikatanai = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Shikatanai"), KeyboardShortcut.Empty);
            buttonTonegawa = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Middle Manager"), KeyboardShortcut.Empty);
            buttonThanks = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Thanks"), KeyboardShortcut.Empty);
            buttonSigh = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Sigh"), KeyboardShortcut.Empty);
            buttonLaugh = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Laugh"), KeyboardShortcut.Empty);

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }

        }
        private void EnableVoicelines_SettingChanged(object sender, EventArgs e)
        {
            RefreshNSE();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("Iroha"));

            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonTitle));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonIntro));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonToramaru));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonTank));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonCommon));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonCafe2));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonDarui));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonShikatanai));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonTonegawa));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonThanks));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonSigh));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonLaugh));
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
            if (!enableVoicelines.Value) return;
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
            HaulerIrohaVoiceoverComponent.nseToramaru = RegisterNSE("Play_HaulerIroha_ExDeploy");
            HaulerIrohaVoiceoverComponent.nseCommon = RegisterNSE("Play_HaulerIroha_CommonSkill");
            HaulerIrohaVoiceoverComponent.nseTitle = RegisterNSE("Play_HaulerIroha_TitleDrop");
            HaulerIrohaVoiceoverComponent.nseCafe2 = RegisterNSE("Play_HaulerIroha_Cafe2");
            HaulerIrohaVoiceoverComponent.nseDarui = RegisterNSE("Play_HaulerIroha_Darui");
            HaulerIrohaVoiceoverComponent.nseShikatanai = RegisterNSE("Play_HaulerIroha_Shikatanai");
            HaulerIrohaVoiceoverComponent.nseThanks = RegisterNSE("Play_HaulerIroha_Thanks");
            HaulerIrohaVoiceoverComponent.nseTonegawa = RegisterNSE("Play_HaulerIroha_EventLobby4");
            HaulerIrohaVoiceoverComponent.nseSigh = RegisterNSE("Play_HaulerIroha_Sigh");
            HaulerIrohaVoiceoverComponent.nseLaugh = RegisterNSE("Play_HaulerIroha_Laugh");
            HaulerIrohaVoiceoverComponent.nseIntro = RegisterNSE("Play_HaulerIroha_Intro");
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
