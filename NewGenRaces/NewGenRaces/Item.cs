using R2API;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NewGenRaces {
    public class Item {

        public static string ItemName = "Timeless Crown";
        public static string ItemDescription = "Saved you from death, at the cost of time.";
        public static string ItemPickup = "You've met with a terrible fate, haven't you?";
        public static string ItemLore = "Each time the newt left the slipstream, he saw the same planet in different points of it's life. For a creature who can travel to any point in time, getting an exact lock was difficult. The first time the newt had visited the planet, it was quiet. The next, it was covered in ruins. The third time the newt came to this planet, it seemed alive. A tall, red-robed figure was standing before him. The newt stared. The robed figure stared back. The newt gave a slight wave with it's only good hand. The robed figure shared the gesture, sensing no hostility.\n\nA tension formed in the newt's mind. A newt's connection to time allows it to see the past, present, and future all at the same time. And every path for this robed figure lead to ruin. He had seen many creatures who's future led to ruin, and he wondered if maybe this time, it could prevent it. Maybe by intervening, the newt could save this robed figure. He only had so much time before the sickness of being out of the slipstream would set in.\n\nThe newt shared visions of what the robed figure's brother would do. Images of war, spearheaded by constructs of their making. As the robed figure's face changed, the newt could feel fate shift. He saw into the future again, and this time... saw a story of a brother who cast the other away instead, a brother who died, and a planet taken by the ever consuming void...\n\nThe newt was filled with anguish. He wanted to fix this, but his head writhed in pain. The sickness had kicked in, leaving him just out of time to fix what he had wronged. \n\nThere was never enough time.\n\nFrom that point on, the newt left markers of places he had been, stone idols of himself that allow him to pinpoint an exact moment in time. But he was never able to make it back to that robed figure.";
        public static ItemDef ItemDef;

        public static Sprite ItemIcon => NewGenRaces.Assets.LoadAsset<Sprite>("TimelessCrown.png");
        public static GameObject ItemPrefab => NewGenRaces.Assets.LoadAsset<GameObject>("TimelessCrown.prefab");

        public static GameObject itemDisplayPrefab = PrefabAPI.CreateEmptyPrefab("timelesscrown_empty");

        public static void Init() {
            CreateLang();
            CreateItem();
            On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.Init += LunarItemOrEquipmentCostTypeHelper_RemoveTimelessCrown; ;
        }

        private static void LunarItemOrEquipmentCostTypeHelper_RemoveTimelessCrown(On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.orig_Init orig) {
            orig();
            RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.lunarItemIndices =
            RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.lunarItemIndices
            .Where(x => x != ItemDef.itemIndex)
            .ToArray();
        }

        protected static void CreateItem() {
            ItemDef = ScriptableObject.CreateInstance<ItemDef>();
            ItemDef.name = "NEWGENRACES_TIMELESSCROWN_NAME";
            ItemDef.nameToken = "NEWGENRACES_TIMELESSCROWN_NAME";
            ItemDef.descriptionToken = "NEWGENRACES_TIMELESSCROWN_DESCRIPTION";
            ItemDef.pickupToken = "NEWGENRACES_TIMELESSCROWN_PICKUP";
            ItemDef.loreToken = "NEWGENRACES_TIMELESSCROWN_LORE";
            ItemDef.canRemove = false;
            ItemDef.tags = new ItemTag[]
            {
                ItemTag.AIBlacklist,
                ItemTag.CannotSteal,
                ItemTag.WorldUnique
            };
            ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();
            ItemDef.tier = ItemTier.Lunar;
            ItemDef.deprecatedTier = ItemTier.Lunar;
            ItemDef.pickupIconSprite = ItemIcon;
            ItemDef.pickupModelPrefab = ItemPrefab;


            var mdlParams = ItemDef.pickupModelPrefab.AddComponent<ModelPanelParameters>();
            mdlParams.focusPointTransform = new GameObject("FocusPoint").transform;
            mdlParams.focusPointTransform.SetParent(ItemDef.pickupModelPrefab.transform);

            mdlParams.cameraPositionTransform = new GameObject("CameraPosition").transform;
            mdlParams.cameraPositionTransform.SetParent(ItemDef.pickupModelPrefab.transform);

            ItemFollower itemFollower = itemDisplayPrefab.AddComponent<ItemFollower>();
            itemFollower.followerPrefab = ItemPrefab;
            itemFollower.distanceDampTime = 0.02f;
            itemFollower.distanceMaxSpeed = 35f;
            itemFollower.targetObject = itemDisplayPrefab;

            ItemAPI.Add(new CustomItem(ItemDef, ItemDisplayTransformations()));
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


        protected static void CreateLang() {
            LanguageAPI.Add("NEWGENRACES_TIMELESSCROWN_NAME", ItemName);
            LanguageAPI.Add("NEWGENRACES_TIMELESSCROWN_DESCRIPTION", ItemDescription);
            LanguageAPI.Add("NEWGENRACES_TIMELESSCROWN_PICKUP", ItemPickup);
            LanguageAPI.Add("NEWGENRACES_TIMELESSCROWN_LORE", ItemLore);
        }
    }
}
