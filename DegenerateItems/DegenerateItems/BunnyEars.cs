using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Items;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace DegenerateItems {
    internal class BunnyEars {

        public static ItemDef BunnyEarsDef;

        internal static void Init() {
            CreateLang();

            BunnyEarsDef = ScriptableObject.CreateInstance<ItemDef>();

            BunnyEarsDef.name = "DEGENERATEITEMS_BUNNYEARS_NAME";
            BunnyEarsDef.nameToken = "DEGENERATEITEMS_BUNNYEARS_NAME";
            BunnyEarsDef.pickupToken = "DEGENERATEITEMS_BUNNYEARS_PICKUP";
            BunnyEarsDef.descriptionToken = "DEGENERATEITEMS_BUNNYEARS_DESC";
            BunnyEarsDef.loreToken = "DEGENERATEITEMS_BUNNYEARS_LORE";

            BunnyEarsDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();

            BunnyEarsDef.pickupIconSprite = DegenerateItems.DegenerateItemsAssets.LoadAsset<Sprite>("bunnyears.png");
            BunnyEarsDef.pickupModelPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab");

            BunnyEarsDef.tags = new ItemTag[]
            {
                ItemTag.CanBeTemporary,
                ItemTag.MobilityRelated
            };

            var mdlParams = BunnyEarsDef.pickupModelPrefab.AddComponent<ModelPanelParameters>();
            mdlParams.focusPointTransform = new GameObject("FocusPoint").transform;
            mdlParams.focusPointTransform.SetParent(BunnyEarsDef.pickupModelPrefab.transform);

            mdlParams.cameraPositionTransform = new GameObject("CameraPosition").transform;
            mdlParams.cameraPositionTransform.SetParent(BunnyEarsDef.pickupModelPrefab.transform);

            ItemAPI.Add(new CustomItem(BunnyEarsDef, ItemDisplayTransformations()));

            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            //On.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
            IL.RoR2.HealthComponent.TakeDamageProcess += (il) => {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.AfterLabel,
                        x => x.MatchLdloc(1),
                        x => x.MatchCallvirt<RoR2.CharacterMaster>("get_inventory"),
                        x => x.MatchLdsfld(typeof(RoR2Content.Items), nameof(RoR2Content.Items.NearbyDamageBonus)),
                        x => x.MatchCallvirt<RoR2.Inventory>("GetItemCountEffective")
                    )) {

                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldarg_1);
                    c.Emit(OpCodes.Ldloc_S, (byte)10);
                    c.EmitDelegate<Func<HealthComponent, DamageInfo, float, float>>((self, damageInfo, accumulatedDamage) => {
                        CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                        if (attackerBody != null) {
                            int count = attackerBody.inventory ? attackerBody.inventory.GetItemCount(BunnyEarsDef) : 0;
                            Vector3 vector = attackerBody.corePosition - damageInfo.position;
                            if (count > 0 && damageInfo.attacker.transform.position.y > self.body.gameObject.transform.position.y + self.body.radius) {
                                damageInfo.damageColorIndex = DamageColorIndex.Nearby;
                                EffectManager.SimpleImpactEffect(RoR2.HealthComponent.AssetReferences.diamondDamageBonusImpactEffectPrefab, damageInfo.position, vector, transmit: true);
                                return accumulatedDamage * (1f + ((float)count * 0.1f));
                            }
                        }

                        return accumulatedDamage;

                    });
                    c.Emit(OpCodes.Stloc_S, (byte)10);
                } else {
                    Log.Error(il.Method.Name + " IL Hook failed!");
                }
            };
        }

        //private static void HealthComponent_TakeDamageProcess(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        //{
        //    CharacterBody attacker = damageInfo.attacker.GetComponent<CharacterBody>();

        //    if (attacker != null) {
        //        int count = attacker.inventory ? attacker.inventory.GetItemCount(BunnyEarsDef) : 0;

        //        Vector3 vector = attacker.corePosition - damageInfo.position;

        //        if (count > 0 && damageInfo.attacker.transform.position.y > self.body.gameObject.transform.position.y + self.body.radius) {
        //            damageInfo.damageColorIndex = DamageColorIndex.Nearby;
        //            damageInfo.damage *= 1f + ((float)count * 0.1f);
        //            EffectManager.SimpleImpactEffect(RoR2.HealthComponent.AssetReferences.diamondDamageBonusImpactEffectPrefab, damageInfo.position, vector, transmit: true);
        //        }
        //    }

        //    orig(self, damageInfo);
        //}

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args) {
            int count = sender.inventory ? sender.inventory.GetItemCount(BunnyEarsDef) : 0;
            args.jumpPowerMultAdd += 0.1f * count;
        }

        public static void CreateLang() {
            LanguageAPI.Add("DEGENERATEITEMS_BUNNYEARS_NAME", "Bunny Ears");
            LanguageAPI.Add("DEGENERATEITEMS_BUNNYEARS_LORE", "\"bunny bunny bunny bunny bunny bunny\"\n\nTwo long pillars rise from the earth, capable of hearing across the cosmos.\n\n\"Brother, what are you-\"\n\n\"bunny bunny bunny bunny bunny bunny\"\n\nA small body, fitting of a warrior.\n\n\"I don't understan-\"\n\n\"bunny bunny bunny bunny bunny bunny\"\n\nTwo legs, capable of darting across the planet in a matter of hours.\n\n\"BROTHER.\"\n\n\"bunny bunny bunn-\"\n\nA loud slam shakes the earth. A pile of dust remains.");
            LanguageAPI.Add("DEGENERATEITEMS_BUNNYEARS_PICKUP", "Gain extra jump height. Enemies below you take more damage.");
            LanguageAPI.Add("DEGENERATEITEMS_BUNNYEARS_DESC", "Gain <style=cIsUtility>10%</style> <style=cStack>(+10% per stack)</style> jump height. Enemies below you take <style=cIsDamage>10%</style> <style=cStack>(+10% per stack)</style> more damage.");
        }

        public static ItemDisplayRuleDict ItemDisplayTransformations() {
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
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(-0.00018F, 0.21757F, -0.02505F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(5F, 5.0945F, 4F)
                }
            });
            displayRules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0F, 0.37F, 0F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(8F, 8F, 7F)
                }
            });
            displayRules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0.00034F, 0.30211F, -0.03092F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(6F, 6F, 5F)
                }
            });
            displayRules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(-0.00064F, 0.21457F, 0.01593F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(6F, 6F, 4.5F)
                }
            });
            displayRules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(-0.02132F, 1.20084F, 1.23591F),
                    localAngles = new Vector3(358.7148F, 266.8632F, 304.8483F),
                    localScale = new Vector3(80F, 80F, 80F)
                }
            });
            displayRules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Chest",
                    localPos = new Vector3(-0.00031F, 0.72016F, 0.04837F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(8F, 8F, 7F)
                }
            });
            displayRules.Add("mdlMage", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0.00097F, 0.17821F, -0.0016F),
                    localAngles = new Vector3(359.9852F, 90.08691F, 16.1103F),
                    localScale = new Vector3(6F, 6F, 4F)
                }
            });
            displayRules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0.00051F, 0.24972F, 0.0256F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(6F, 6F, 5F)
                }
            });
            displayRules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0.00068F, 0.24076F, 0.00063F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(6F, 6F, 5.5F)
                }
            });
            displayRules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0.0011F, 0.23103F, 0.01163F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(7F, 7F, 5.5F)
                }
            });
            displayRules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0.01966F, 0.41845F, 0.08426F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(8F, 8F, 8F)
                }
            });
            displayRules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(0.01027F, 0.18881F, -0.00274F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(8F, 8F, 7F)
                }
            });
            displayRules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(-0.00528F, 0.23442F, -0.00311F),
                    localAngles = new Vector3(0F, 90F, 0F),
                    localScale = new Vector3(6F, 6F, 5F)
                }
            });
            displayRules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(-0.28433F, 0.0394F, -0.00642F),
                    localAngles = new Vector3(0F, 0F, 90F),
                    localScale = new Vector3(7F, 7F, 6F)
                }
            });
            displayRules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(-0.31011F, 0.00211F, 0.00769F),
                    localAngles = new Vector3(0F, 180F, 270F),
                    localScale = new Vector3(8F, 8F, 7F)
                }
            });
            displayRules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = DegenerateItems.DegenerateItemsAssets.LoadAsset<GameObject>("bunnyears_curve.prefab"),
                    childName = "Head",
                    localPos = new Vector3(-0.02386F, 0.79044F, 1.40061F),
                    localAngles = new Vector3(359.2067F, 269.6524F, 263.2065F),
                    localScale = new Vector3(60F, 60F, 60F)
                }
            });
            return displayRules;
        }

        public class BunnyEarsBehavior : BaseItemBodyBehavior {
            [ItemDefAssociation(useOnServer = true, useOnClient = true)]
            private static ItemDef GetItemDef() { return BunnyEarsDef; }
            private int GetStackCount() { return stack; }
        }
    }
}