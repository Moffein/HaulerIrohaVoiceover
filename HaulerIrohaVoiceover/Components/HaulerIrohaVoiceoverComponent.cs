using BaseVoiceoverLib;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaulerIrohaVoiceover.Components
{
    public class HaulerIrohaVoiceoverComponent : BaseVoiceoverComponent
    {
        private float levelCooldown = 0f;
        private float primaryCooldown = 0f;
        private float utilityCooldown = 0f;
        private bool acquiredScepter = false;

        public static NetworkSoundEventDef nseTank;
        public static NetworkSoundEventDef nseCommon;

        protected override void Start()
        {
            base.Start();
            if (inventory && inventory.GetItemCount(scepterIndex) > 0) acquiredScepter = true;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (primaryCooldown > 0f) primaryCooldown -= Time.fixedDeltaTime;
            if (utilityCooldown > 0f) utilityCooldown -= Time.fixedDeltaTime;
            if (levelCooldown > 0f) levelCooldown -= Time.fixedDeltaTime;
        }

        public override void PlayDeath()
        {
            TryPlaySound("Play_HaulerIroha_Defeat", 3.3f, true);
        }

        public override void PlayLevelUp()
        {
            if (levelCooldown > 0f) return;
            bool played = Util.CheckRoll(50f, null) ? TryPlaySound("Play_HaulerIroha_LevelUp_Long", 7.8f, false) : TryPlaySound("Play_HaulerIroha_LevelUp_Short", 3.7f, false);
            if (played) levelCooldown = 60f;
        }

        public override void PlayPrimaryAuthority(GenericSkill skill)
        {
            if (primaryCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseTank, 1.5f, false);
            if (played) primaryCooldown = 20f;
        }

        public override void PlaySpawn()
        {
            TryPlaySound("Play_HaulerIroha_ExDeploy", 7.5f, true);
        }

        public override void PlayTeleporterFinish()
        {
            TryPlaySound("Play_HaulerIroha_Victory", 5.5f, false);
        }

        public override void PlayTeleporterStart()
        {
            TryPlaySound("Play_HaulerIroha_Ex", 1.95f, false);
        }

        public override void PlayUtilityAuthority(GenericSkill skill)
        {
            if (utilityCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseCommon, 0.85f, false);
            if (played) utilityCooldown = 20f;
        }

        public override void PlayVictory()
        {
            TryPlaySound("Play_HaulerIroha_EventLobby4", 4.85f, true);
        }

        protected override void Inventory_onItemAddedClient(ItemIndex itemIndex)
        {
            base.Inventory_onItemAddedClient(itemIndex);
            if (scepterIndex != ItemIndex.None && itemIndex == scepterIndex)
            {
                PlayAcquireScepter();
            }
            else
            {
                ItemDef id = ItemCatalog.GetItemDef(itemIndex);
                if (id && id.deprecatedTier == ItemTier.Tier3)
                {
                    PlayAcquireLegendary();
                }
            }
        }
        public void PlayAcquireScepter()
        {
            if (acquiredScepter) return;
            TryPlaySound("Play_HaulerIroha_AcquireScepter", 9f, true);
            acquiredScepter = true;
        }

        public void PlayAcquireLegendary()
        {
            TryPlaySound("Play_HaulerIroha_Relationship", 6.1f, false);
        }
    }
}
