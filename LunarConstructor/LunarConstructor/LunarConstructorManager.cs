using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using static RoR2.UI.CarouselController;
using RoR2.Hologram;

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

        private float refreshTimer;
        private const float refreshDuration = 1.5f;
        private bool waitingForRefresh;
        UniquePickup refreshPrintItem;

        [SyncVar]
        private int whitePickupIndex;
        [SyncVar]
        private int greenPickupIndex;
        [SyncVar]
        private int redPickupIndex;
        [SyncVar]
        private int yellowPickupIndex;

        private PickupIndex whitePickup {
            get { return new PickupIndex(whitePickupIndex); }
            set { whitePickupIndex = value.value; }
        }

        private PickupIndex greenPickup
        {
            get { return new PickupIndex(greenPickupIndex); }
            set { greenPickupIndex = value.value; }
        }

        private PickupIndex redPickup
        {
            get { return new PickupIndex(redPickupIndex); }
            set { redPickupIndex = value.value; }
        }

        private PickupIndex yellowPickup
        {
            get { return new PickupIndex(yellowPickupIndex); }
            set { yellowPickupIndex = value.value; }
        }

        Xoroshiro128Plus rng;

        int currentPickupTierIndex;

        [SyncVar]
        private GameObject closestPlayerObject;

        private CharacterBody closestPlayer
        {
            get { return closestPlayerObject.GetComponent<CharacterBody>(); }
            set { closestPlayerObject = value.gameObject; }
        }

        public void Start()
        {
            if (NetworkServer.active && Run.instance)
            {
                purchaseInteraction.SetAvailable(true);

                rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);

                RollItemsServer();
            }

            if (!pickupDisplay && !highlight && !hologram)
            {
                pickupDisplay = transform.Find("PickupDisplay").GetComponent<PickupDisplay>();
                highlight = transform.Find("PickupDisplay").GetComponent<Highlight>();
                pickupDisplay.highlight = highlight;
                highlight.highlightColor = Highlight.HighlightColor.pickup;
                hologram = transform.Find("Hologram").GetComponent<HologramProjector>();
            }

            purchaseInteraction.onPurchase.AddListener(OnPurchase);

            refreshPrintItem = UniquePickup.none;
            hologram.contentProvider = this;
            hologram.disableHologramRotation = true;
        }

        public void RollItemsServer() {
            whitePickup = PickFromList(Run.instance.availableTier1DropList).pickupIndex;
            greenPickup = PickFromList(Run.instance.availableTier2DropList).pickupIndex;
            redPickup = PickFromList(Run.instance.availableTier3DropList).pickupIndex;
            yellowPickup = PickFromList(Run.instance.availableBossDropList).pickupIndex;
        }

        public void FixedUpdate() {

            if (NetworkServer.active && Run.instance)
            {
                closestPlayer = GetClosestPlayerServer();
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
                        PickupDropletController.CreatePickupDroplet(refreshPrintItem, base.transform.position, (base.transform.forward + Vector3.up + Vector3.up) * 8f);
                        refreshPrintItem = UniquePickup.none;
                    }
                }
            }
        }

        public void UpdateDisplayClient() {
            // TODO: when the item is changed, spawn the lunar reroll effect
            if ((bool)pickupDisplay &&
                whitePickup != PickupIndex.none &&
                greenPickup != PickupIndex.none &&
                redPickup != PickupIndex.none &&
                yellowPickup != PickupIndex.none)
            {
                if ((GetBodyHighestScrap(closestPlayer) != null) && (pickupDisplay.GetPickupIndex() == PickupIndex.none))
                {
                    pickupDisplay.SetPickup(new UniquePickup(whitePickup), false);
                    if (currentPickupTierIndex != 1)
                    {
                        DisplayRerollEffectClient();
                    }
                    currentPickupTierIndex = 1;
                    return;
                }
                else if (GetBodyHighestScrap(closestPlayer) != null) {
                    return;
                }
                switch (GetBodyHighestScrap(closestPlayer).tier)
                {
                    case ItemTier.Boss:
                        pickupDisplay.SetPickup(new UniquePickup(yellowPickup), false);
                        if (currentPickupTierIndex != 4)
                        {
                            DisplayRerollEffectClient();
                        }
                        currentPickupTierIndex = 4;
                        break;
                    case ItemTier.Tier3:
                        pickupDisplay.SetPickup(new UniquePickup(redPickup), false);
                        if (currentPickupTierIndex != 3)
                        {
                            DisplayRerollEffectClient();
                        }
                        currentPickupTierIndex = 3;
                        break;
                    case ItemTier.Tier2:
                        pickupDisplay.SetPickup(new UniquePickup(greenPickup), false);
                        if (currentPickupTierIndex != 2)
                        {
                            DisplayRerollEffectClient();
                        }
                        currentPickupTierIndex = 2;
                        break;
                    default:
                        pickupDisplay.SetPickup(new UniquePickup(whitePickup), false);
                        if (currentPickupTierIndex != 1)
                        {
                            DisplayRerollEffectClient();
                        }
                        currentPickupTierIndex = 1;
                        break;
                }
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
                if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapYellow) > 0) 
                {
                    tier = ItemTier.Boss;
                    return RoR2Content.Items.ScrapYellow;
                } else if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapRed) > 0)
                {
                    tier = ItemTier.Tier3;
                    return RoR2Content.Items.ScrapRed;
                }
                else if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapGreen) > 0)
                {
                    tier = ItemTier.Tier2;
                    return RoR2Content.Items.ScrapGreen;
                }
                else if (inventory.GetItemCountPermanent(RoR2Content.Items.ScrapWhite) > 0)
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
        private UniquePickup PickFromList(List<PickupIndex> dropList)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::PickFromList(System.Collections.Generic.List`1<RoR2.PickupIndex>)' called on client");
                return UniquePickup.none;
            }
            UniquePickup currentPickup = UniquePickup.none;
            if (dropList != null && dropList.Count > 0)
            {
                currentPickup = new UniquePickup(rng.NextElementUniform(dropList));
            }
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
            ItemDef itemToTake = GetBodyHighestScrap(interactorBody, out ItemTier scrapTier);

            if (itemToTake) {
                // remove the item
                PurchaseInteraction.CreateItemTakenOrb(interactorBody.corePosition, gameObject, itemToTake.itemIndex);

                interactorBody.inventory.RemoveItemPermanent(itemToTake);

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
