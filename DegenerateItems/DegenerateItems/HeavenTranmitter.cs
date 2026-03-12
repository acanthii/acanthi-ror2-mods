using System;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RoR2.Items.BaseItemBodyBehavior;
using UnityEngine.Networking;
using RoR2.Items;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine.UIElements;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace DegenerateItems
{
    internal class HeavenTranmitter
    {
        public static ItemDef HeavenTransmitterDef;
        public static GameObject itemDisplayFollowerPrefab;
        public static GameObject itemDisplayPrefab = PrefabAPI.CreateEmptyPrefab("heaventransmitter_empty");

        public static float baseDamage = 3.33f;
        public static float stackDamage = 1.11f;
        public static float baseProcChance = 5f;
        public static float scrapProcChance = 2f;

        private static R2API.ModdedProcType transmitterProcType;

        private static GameObject impactEffect;
        private static GameObject impactEffect2;

        internal static void Init()
        {
            CreateLang();

            impactEffect = Addressables.LoadAssetAsync<GameObject>(
                RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_VultureHunter.XiSpearImpactVisual_prefab
            ).WaitForCompletion();
            
            impactEffect2 = Addressables.LoadAssetAsync<GameObject>(
                RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_MajorAndMinorConstruct.MajorConstructSecondMuzzleFlash_prefab
            ).WaitForCompletion();

            transmitterProcType = ProcTypeAPI.ReserveProcType();

            HeavenTransmitterDef = ScriptableObject.CreateInstance<ItemDef>();

            HeavenTransmitterDef.name = "DEGENERATEITEMS_HEAVENTRANSMITTER_NAME";
            HeavenTransmitterDef.nameToken = "DEGENERATEITEMS_HEAVENTRANSMITTER_NAME";
            HeavenTransmitterDef.pickupToken = "DEGENERATEITEMS_HEAVENTRANSMITTER_PICKUP";
            HeavenTransmitterDef.descriptionToken = "DEGENERATEITEMS_HEAVENTRANSMITTER_DESC";
            HeavenTransmitterDef.loreToken = "DEGENERATEITEMS_HEAVENTRANSMITTER_LORE";

            HeavenTransmitterDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

            HeavenTransmitterDef.pickupIconSprite = DegenerateItems.DegenerateItemsAssets.LoadAsset<Sprite>("heaventransmitter.png");
            HeavenTransmitterDef.pickupModelPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("heaventransmitter.prefab");

            HeavenTransmitterDef.tags = new ItemTag[]
            {
                ItemTag.CanBeTemporary,
                ItemTag.MobilityRelated
            };

            var mdlParams = HeavenTransmitterDef.pickupModelPrefab.AddComponent<ModelPanelParameters>();
            mdlParams.focusPointTransform = new GameObject("FocusPoint").transform;
            mdlParams.focusPointTransform.SetParent(HeavenTransmitterDef.pickupModelPrefab.transform);

            mdlParams.cameraPositionTransform = new GameObject("CameraPosition").transform;
            mdlParams.cameraPositionTransform.SetParent(HeavenTransmitterDef.pickupModelPrefab.transform);

            // fix the itemDisplayPrefab
            itemDisplayFollowerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("heaventransmitter.prefab");
            itemDisplayFollowerPrefab.AddComponent<ItemDisplay>();

            ItemFollower itemFollower = itemDisplayPrefab.AddComponent<ItemFollower>();
            itemFollower.followerPrefab = itemDisplayFollowerPrefab;
            itemFollower.distanceDampTime = 0.02f;
            itemFollower.distanceMaxSpeed = 35f;
            itemFollower.targetObject = itemDisplayPrefab;

            ItemAPI.Add(new CustomItem(HeavenTransmitterDef, ItemDisplayTransformations()));
        }

        private static void CreateLang()
        {
            LanguageAPI.Add("DEGENERATEITEMS_HEAVENTRANSMITTER_NAME", "H34V-EN Transmitter");
            LanguageAPI.Add("DEGENERATEITEMS_HEAVENTRANSMITTER_LORE", "<style=cMono>\"When I awoke, I was grasping a gun. \n\nThe smell of death and metal woke me. \n\nWhen we are not connected to the network...\n\n...we are all alone.\n\nDo you understand that?\"</style>");
            LanguageAPI.Add("DEGENERATEITEMS_HEAVENTRANSMITTER_PICKUP", "Chance to summon a beam of light on an enemy. Chance increases with scrap count.");
            LanguageAPI.Add("DEGENERATEITEMS_HEAVENTRANSMITTER_DESC", "Gain a <style=cIsDamage>5%</style> chance for a beam of light to pierce an enemy for <style=cIsDamage>333%</style> <style=cStack>(+111% per stack)</style> TOTAL damage. Carrying any tier of <style=cIsUtility>scrap</style> increases the chance by <style=cIsDamage>2%</style>.");
        }

        public static ItemDisplayRuleDict ItemDisplayTransformations()
        {
            // You can add your own display rules here,
            // where the first argument passed are the default display rules:
            // the ones used when no specific display rules for a character are found.
            // For this example, we are omitting them,
            // as they are quite a pain to set up without tools like https://thunderstore.io/package/KingEnderBrine/ItemDisplayPlacementHelper/
            var displayRules = new ItemDisplayRuleDict(null);
            displayRules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.00021F, 0.94454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlMage", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            displayRules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = itemDisplayPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00021F, 0.44454F, 0.04126F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(2F, 2F, 2F)
                }
            });
            return displayRules;
        }

        public class HeavenTransmitterBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver
        {
            [ItemDefAssociation(useOnServer = true, useOnClient = true)]
            private static ItemDef GetItemDef() { return HeavenTransmitterDef; }

            private int GetStackCount() { return stack; }

            private void OnEnable()
            {
                Debug.Log("Skibidi Enabled");
            }

            private void OnDisable()
            {
                Debug.Log("Skibidi Disabled");
            }

            private int GetScrapCount(CharacterBody body)
            {
                return body.inventory.GetItemCountEffective(RoR2Content.Items.ScrapWhite)
                + body.inventory.GetItemCountEffective(RoR2Content.Items.ScrapGreen) 
                + body.inventory.GetItemCountEffective(DLC1Content.Items.RegeneratingScrap)
                + body.inventory.GetItemCountEffective(RoR2Content.Items.ScrapRed)
                + body.inventory.GetItemCountEffective(RoR2Content.Items.ScrapYellow)
                + body.inventory.GetItemCountEffective(DLC1Content.Items.ScrapWhiteSuppressed)
                + body.inventory.GetItemCountEffective(DLC1Content.Items.ScrapGreenSuppressed)
                + body.inventory.GetItemCountEffective(DLC1Content.Items.ScrapRedSuppressed);
            }

            public void OnDamageDealtServer(DamageReport damageReport)
            {
                if (stack > 0 && damageReport.damageDealt > 0 
                    && !damageReport.damageInfo.procChainMask.HasModdedProc(transmitterProcType)
                    && !damageReport.damageInfo.damageType.damageType.HasFlag(DamageType.DoT))
                {
                    var playerBody = damageReport.attackerBody;
                    var victimBody = damageReport.victimBody;
                    Vector3 position = damageReport.damageInfo.position;
                    ProcChainMask procChainMask = damageReport.damageInfo.procChainMask;
                    procChainMask.AddModdedProc(transmitterProcType);

                    if (playerBody && victimBody)
                    {
                        if (Util.CheckRoll(baseProcChance + (scrapProcChance * GetScrapCount(playerBody)), playerBody.master) || damageReport.damageInfo.procChainMask.HasProc(ProcType.SureProc)) // add scrap chance here later nerd
                        {
                            float damage = baseDamage + (stackDamage * stack);
                            float totalDamage = Util.OnHitProcDamage(damageReport.damageInfo.damage, playerBody.damage, damage);
                            DamageInfo damageInfo2 = new DamageInfo
                            {
                                damage = totalDamage,
                                damageColorIndex = DamageColorIndex.Item,
                                damageType = DamageType.Generic,
                                attacker = damageReport.damageInfo.attacker,
                                crit = damageReport.damageInfo.crit,
                                force = Vector3.zero,
                                inflictor = null,
                                position = position,
                                procChainMask = procChainMask,
                                procCoefficient = 1f,
                                inflictedHurtbox = damageReport.damageInfo.inflictedHurtbox
                            };

                            EffectManager.SpawnEffect(impactEffect, new EffectData
                            {
                                origin = position,
                                scale = Convert.ToSingle(7 * victimBody.radius),
                                rotation = Util.QuaternionSafeLookRotation(Vector3.left)
                            }, true);
                            EffectManager.SpawnEffect(impactEffect2, new EffectData
                            {
                                origin = position,
                                scale = Convert.ToSingle(7 * victimBody.radius),
                                rotation = Util.QuaternionSafeLookRotation(Vector3.up)
                            }, true);

                            Util.PlaySound("Play_mage_m2_iceSpear_impact", damageReport.victimBody.gameObject);

                            victimBody.healthComponent.TakeDamage(damageInfo2);
                        }
                    }
                }
            }
        }
    }
}