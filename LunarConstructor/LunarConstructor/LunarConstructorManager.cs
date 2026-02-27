using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using static RoR2.UI.CarouselController;
using RoR2.Hologram;
using static MonoMod.InlineRT.MonoModRule;
using static Rewired.InputMapper;
using RoR2.Audio;
using System.Linq;

namespace LunarConstructor
{
    public class ConstructorManager : NetworkBehaviour, IHologramContentProvider
    {
        public PurchaseInteraction purchaseInteraction;
        public PickupDisplay pickupDisplay;
        public HologramProjector hologram;
        public Highlight highlight;
        private GameObject shrineUseEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ShrineUseEffect.prefab").WaitForCompletion();
        private GameObject lunarRerollEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarRecycler/LunarRerollEffect.prefab").WaitForCompletion();

        private ItemTag bannedItemTag = ItemTag.CannotDuplicate;

        private float refreshTimer;
        private const float refreshDuration = 1.5f;
        private bool waitingForRefresh;
        UniquePickup refreshPrintItem;

        [SyncVar]
        private int whitePickupIndex = -1;
        [SyncVar]
        private int greenPickupIndex = -1;
        [SyncVar]
        private int redPickupIndex = -1;
        [SyncVar]
        private int yellowPickupIndex = -1;

        private PickupIndex whitePickup {
            get {
                if (whitePickupIndex == null || whitePickupIndex < 0) {
                    return PickupIndex.none;
                }
                return new PickupIndex(whitePickupIndex); 
            }
            set { whitePickupIndex = value.value; }
        }

        private PickupIndex greenPickup
        {
            get {
                if (greenPickupIndex == null || greenPickupIndex < 0)
                {
                    return PickupIndex.none;
                }
                return new PickupIndex(greenPickupIndex); 
            }
            set { greenPickupIndex = value.value; }
        }

        private PickupIndex redPickup
        {
            get {
                if (redPickupIndex == null || redPickupIndex < 0)
                {
                    return PickupIndex.none;
                }
                return new PickupIndex(redPickupIndex); 
            }
            set { redPickupIndex = value.value; }
        }

        private PickupIndex yellowPickup
        {
            get {
                if (yellowPickupIndex == null || yellowPickupIndex < 0)
                {
                    return PickupIndex.none;
                }
                return new PickupIndex(yellowPickupIndex); 
            }
            set { yellowPickupIndex = value.value; }
        }

        int currentPickupTierIndex;

        [SyncVar]
        private GameObject closestPlayerObject;

        private CharacterBody closestPlayer
        {
            get { return closestPlayerObject ? closestPlayerObject.GetComponent<CharacterBody>() : null; }
            set { closestPlayerObject = value ? value.gameObject : null; }
        }

        public void Start()
        {
            if (!pickupDisplay && !highlight && !hologram)
            {
                pickupDisplay = transform.Find("PickupDisplay").GetComponent<PickupDisplay>();
                highlight = transform.Find("PickupDisplay").GetComponent<Highlight>();
                pickupDisplay.highlight = highlight;
                highlight.highlightColor = Highlight.HighlightColor.pickup;
                hologram = transform.Find("Hologram").GetComponent<HologramProjector>();
            }

            if (NetworkServer.active && Run.instance)
            {
                purchaseInteraction.SetAvailable(true);

                RollItemsServer();
            }

            purchaseInteraction.onPurchase.AddListener(OnPurchase);

            refreshPrintItem = UniquePickup.none;
            hologram.contentProvider = this;
            hologram.disableHologramRotation = true;
        }

        [Server]
        public void RollItemsServer() {
            whitePickup = PickFromList(Run.instance.availableTier1DropList).pickupIndex;
            greenPickup = PickFromList(Run.instance.availableTier2DropList).pickupIndex;
            redPickup = PickFromList(Run.instance.availableTier3DropList).pickupIndex;
            yellowPickup = PickFromList(Run.instance.availableBossDropList).pickupIndex;
            RpcClearPickup();
        }

        [ClientRpc]
        public void RpcClearPickup() {
            pickupDisplay.SetPickup(UniquePickup.none, false);
        }

        public void FixedUpdate() {

            if (NetworkServer.active && Run.instance)
            {
                closestPlayer = GetClosestPlayerWithScrapServer();
            }

            UpdateDisplayClient();

            if (waitingForRefresh)
            {
                refreshTimer -= Time.fixedDeltaTime;
                if (refreshTimer <= 0f)
                {
                    purchaseInteraction.SetAvailable(true);
                    waitingForRefresh = false;
                    if (refreshPrintItem != UniquePickup.none) 
                    {
                        PickupDropletController.CreatePickupDroplet(refreshPrintItem, base.transform.position, (base.transform.forward + Vector3.up + Vector3.up) * 8f, true);
                        refreshPrintItem = UniquePickup.none;
                    }
                }
            }
        }

        public void UpdateDisplayClient() {
            //Debug.Log(pickupDisplay.GetPickupIndex().ToString());
            if (!pickupDisplay) {
                //Debug.Log("No pickupDisplay!");
                return; 
            }
            if (whitePickup == PickupIndex.none ||
                greenPickup == PickupIndex.none ||
                redPickup == PickupIndex.none ||
                yellowPickup == PickupIndex.none
            ) {
                //Debug.Log("pickups are set invalid!");
                return;
            }

            if (pickupDisplay.GetPickupIndex() == PickupIndex.none || pickupDisplay.GetPickupIndex() == null)
            {
                //Debug.Log("Setting pickup to whitePickup! " + whitePickup.ToString());

                pickupDisplay.SetPickup(new UniquePickup(whitePickup), false);
                if (currentPickupTierIndex != 1)
                {
                    DisplayRerollEffectClient();
                }
                currentPickupTierIndex = 1;
            }

            if (!closestPlayer || GetBodyHighestScrap(closestPlayer) == null) {
                return;
            }

            switch (GetBodyHighestScrap(closestPlayer).tier)
            {
                case ItemTier.Boss:
                    pickupDisplay.SetPickup(new UniquePickup(yellowPickup), false);
                    if (currentPickupTierIndex != 4)
                        DisplayRerollEffectClient();
                    currentPickupTierIndex = 4;
                    break;
                case ItemTier.Tier3:
                    pickupDisplay.SetPickup(new UniquePickup(redPickup), false);
                    if (currentPickupTierIndex != 3)
                        DisplayRerollEffectClient();
                    currentPickupTierIndex = 3;
                    break;
                case ItemTier.Tier2:
                    pickupDisplay.SetPickup(new UniquePickup(greenPickup), false);
                    if (currentPickupTierIndex != 2)
                        DisplayRerollEffectClient();
                    currentPickupTierIndex = 2;
                    break;
                default:
                    pickupDisplay.SetPickup(new UniquePickup(whitePickup), false);
                    if (currentPickupTierIndex != 1)
                        DisplayRerollEffectClient();
                    currentPickupTierIndex = 1;
                    break;
            }
        }

        public void DisplayRerollEffectClient() {
            EffectManager.SpawnEffect(lunarRerollEffect, new EffectData()
            {
                origin = pickupDisplay.transform.position,
                rotation = Quaternion.identity,
                scale = 2f
            }, true);
        }

        public ItemDef GetBodyHighestScrap(CharacterBody body) {
            return GetBodyHighestScrap(body, out ItemTier tier);
        }

        // you can use this to update the display, and even get the cost!
        public ItemDef GetBodyHighestScrap(CharacterBody body, out ItemTier tier) {
            if (body == null) {
                tier = ItemTier.NoTier;
                return null;
            };

            var inventory = body.inventory;
            if (body && inventory) {
                if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapYellow) > 0) // YELLOW SCRAP
                {
                    tier = ItemTier.Boss;
                    return RoR2Content.Items.ScrapYellow;
                }
                else if (inventory.GetItemCountPermanent(DLC1Content.Items.ScrapRedSuppressed) > 0) // RED SCRAP (Suppressor)
                {
                    tier = ItemTier.Tier3;
                    return DLC1Content.Items.ScrapRedSuppressed;
                }
                else if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapRed) > 0) // RED SCRAP
                {
                    tier = ItemTier.Tier3;
                    return RoR2Content.Items.ScrapRed;
                }
                else if (inventory.GetItemCountPermanent(DLC1Content.Items.RegeneratingScrap) > 0) // GREEN SCRAP (REGEN)
                {
                    tier = ItemTier.Tier2;
                    return DLC1Content.Items.RegeneratingScrap;
                }
                else if (inventory.GetItemCountPermanent(DLC1Content.Items.ScrapGreenSuppressed) > 0) // GREEN SCRAP (SUPPRESSED)
                {
                    tier = ItemTier.Tier2;
                    return DLC1Content.Items.ScrapGreenSuppressed;
                }
                else if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapGreen) > 0) // GREEN SCRAP
                {
                    tier = ItemTier.Tier2;
                    return RoR2Content.Items.ScrapGreen;
                }
                else if (inventory.GetItemCountPermanent(DLC1Content.Items.ScrapWhiteSuppressed) > 0) // WHITE SCRAP (SUPPRESSED)
                {
                    tier = ItemTier.Tier2;
                    return DLC1Content.Items.ScrapWhiteSuppressed;
                }
                else if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapWhite) > 0) // WHITE SCRAP
                {
                    tier = ItemTier.Tier1;
                    return RoR2Content.Items.ScrapWhite;
                }
            }
            tier = ItemTier.NoTier;
            return null;
        }

        [Server]
        public CharacterBody GetClosestPlayerServer() {
            if (!NetworkServer.active)
            {
                Debug.Log("GetClosestPlayerServer() called on client!");
            }

            CharacterBody closestBody = null;
            float closestDistance = 9999f;

            foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances) {

                if (pcmc == null) 
                {
                    continue;
                }

                if (pcmc.master.GetBody() == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    pcmc.master.GetBody().transform.position,
                    transform.position
                );

                // You are automatically closer than nothing. Congrats!
                if (closestBody == null || distance < closestDistance) 
                {
                    closestBody = pcmc.master.GetBody();
                    closestDistance = distance;
                    continue;
                }
            }

            return closestBody;
        }

        [Server]
        public CharacterBody GetClosestPlayerWithScrapServer()
        {
            if (!NetworkServer.active)
            {
                Debug.Log("GetClosestPlayerServerWithScrap() called on client!");
            }

            CharacterBody closestBody = null;
            float closestDistance = 9999f;

            foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances)
            {

                if (pcmc == null)
                {
                    continue;
                }

                if (pcmc.master.GetBody() == null)
                {
                    continue;
                }

                if (GetBodyHighestScrap(pcmc.master.GetBody()) == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    pcmc.master.GetBody().transform.position,
                    transform.position
                );

                // You are automatically closer than nothing. Congrats!
                if (closestBody == null || distance < closestDistance)
                {
                    closestBody = pcmc.master.GetBody();
                    closestDistance = distance;
                    continue;
                }
            }

            return closestBody;
        }

        public bool PassesFilter(PickupIndex pickupIndex)
        {
            if (bannedItemTag == ItemTag.Any)
            {
                return true;
            }
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef.itemIndex != ItemIndex.None)
            {
                return !ItemCatalog.GetItemDef(pickupDef.itemIndex).ContainsTag(bannedItemTag);
            }
            return true;
        }

        [Server]
        private UniquePickup PickFromList(List<PickupIndex> dropList)
        {
            if (!NetworkServer.active)
            {
                return UniquePickup.none;
            }
            UniquePickup currentPickup = UniquePickup.none;
            if (dropList != null && dropList.Count > 0)
            {
                currentPickup = new UniquePickup(Run.instance.treasureRng.NextElementUniform(dropList.Where(PassesFilter).ToList()));
            }
            //Debug.Log("PickFromList :: " + currentPickup.pickupIndex.pickupDef.nameToken);
            return currentPickup;
        }

        [Server]
        public void OnPurchase(Interactor interactor)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'ConstructorManager::OnPurchase(RoR2.Interactor)' called on client");
                return;
            }

            purchaseInteraction.SetAvailable(false);
            waitingForRefresh = true;
            refreshTimer = refreshDuration;

            CharacterBody interactorBody = interactor.GetComponent<CharacterBody>();
            ItemDef itemToTake = GetAndConsumeItem(interactorBody, out ItemTier scrapTier);

            if (itemToTake) {
                // remove the item
                PurchaseInteraction.CreateItemTakenOrb(interactorBody.corePosition, gameObject, itemToTake.itemIndex);

                Util.PlaySound("Play_voidJailer_m1_impact", gameObject);

                switch (scrapTier) {
                    case ItemTier.Boss:
                        refreshPrintItem = new UniquePickup(yellowPickup);
                        break;
                    case ItemTier.Tier3:
                        refreshPrintItem = new UniquePickup(redPickup);
                        break;
                    case ItemTier.Tier2:
                        refreshPrintItem = new UniquePickup(greenPickup);
                        break;
                    case ItemTier.Tier1:
                        refreshPrintItem = new UniquePickup(whitePickup);
                        break;
                    default:
                        refreshPrintItem = UniquePickup.none;
                        break;
                }
            }

            // generic effect + chat slop
            EffectManager.SpawnEffect(shrineUseEffect, new EffectData()
            {
                origin = gameObject.transform.position,
                rotation = Quaternion.identity,
                scale = 3f,
                color = Color.cyan
            }, true);
            //Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = "<style=cEvent><color=#307FFF>goont.</color></style>" });
        }

        private ItemDef GetAndConsumeItem(CharacterBody interactorBody, out ItemTier scrapTier)
        {
            ItemDef itemToTake = GetBodyHighestScrap(interactorBody, out var itemTier);

            bool flag = false;

            if (itemToTake == null) 
            {
                scrapTier = ItemTier.NoTier;
                return null; 
            }

            if (itemToTake == DLC1Content.Items.RegeneratingScrap)
            {
                ItemIndex originalItemIndex = itemToTake.itemIndex;
                Inventory.ItemTransformation itemTransformation = new Inventory.ItemTransformation
                {
                    originalItemIndex = originalItemIndex,
                    newItemIndex = ItemIndex.None,
                    maxToTransform = 1,
                    forbidTempItems = true
                };
                if (itemTransformation.originalItemIndex == DLC1Content.Items.RegeneratingScrap.itemIndex)
                {
                    flag = true;
                    itemTransformation.newItemIndex = DLC1Content.Items.RegeneratingScrapConsumed.itemIndex;
                }
                itemTransformation.TryTransform(interactorBody.inventory, out var result2);

                if (flag)
                {
                    EntitySoundManager.EmitSoundServer(NetworkSoundEventCatalog.FindNetworkSoundEventIndex("Play_item_proc_regenScrap_consume"), interactorBody.gameObject);
                    ModelLocator component = interactorBody.GetComponent<ModelLocator>();
                    if ((bool)component)
                    {
                        Transform modelTransform = component.modelTransform;
                        if ((bool)modelTransform)
                        {
                            CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
                            if ((bool)component2)
                            {
                                List<GameObject> itemDisplayObjects = component2.GetItemDisplayObjects(DLC1Content.Items.RegeneratingScrap.itemIndex);
                                if (itemDisplayObjects.Count > 0)
                                {
                                    GameObject gameObject = itemDisplayObjects[0];
                                    GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RegeneratingScrap/RegeneratingScrapExplosionDisplay.prefab").WaitForCompletion();
                                    EffectData effectData = new EffectData
                                    {
                                        origin = gameObject.transform.position,
                                        rotation = gameObject.transform.rotation
                                    };
                                    EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
                                }
                            }
                        }
                    }
                    EffectManager.SimpleMuzzleFlash(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RegeneratingScrap/RegeneratingScrapExplosionInPrinter.prefab").WaitForCompletion(), gameObject, "Circle", transmit: true);
                    CharacterMasterNotificationQueue.SendTransformNotification(interactorBody.master, DLC1Content.Items.RegeneratingScrap.itemIndex, DLC1Content.Items.RegeneratingScrapConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                }
            }
            else 
            {
                interactorBody.inventory.RemoveItemPermanent(itemToTake);
            }

            scrapTier = itemTier;
            return itemToTake;
        }

        public bool ShouldDisplayHologram(GameObject viewer)
        {
            return true;
        }

        public GameObject GetHologramContentPrefab()
        {
            return LunarConstructor.ConstructorHologramContent;
        }

        public void UpdateHologramContent(GameObject hologramContentObject, Transform viewerBody)
        {
            var component = hologramContentObject.GetComponent<ConstructorCostHologramContent>();
            if (component)
            {
                component.displayValue = "1 Scrap";
                switch (currentPickupTierIndex) {
                    case 1:
                        component.colorValue = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier1Item);
                        break;
                    case 2:
                        component.colorValue = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier2Item);
                        break;
                    case 3:
                        component.colorValue = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier3Item);
                        break;
                    case 4:
                        component.colorValue = ColorCatalog.GetColor(ColorCatalog.ColorIndex.BossItem);
                        break;
                    default:
                        component.colorValue = Color.white;
                        break;
                }
            }
        }
    }
}
