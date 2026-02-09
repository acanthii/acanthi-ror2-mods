using System;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using RoR2.ExpansionManagement;
using System.Linq;
using UnityEngine.Networking;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using R2API.Networking;
using R2API.Networking.Interfaces;
using BepInEx.Logging;

namespace ChefBazaar
{
    internal class NetMessages
    {
        /// <summary>
        /// The Server sends this to the Client to enable the table stuff in the scene.
        /// </summary>
        public class SpawnChefTableMessage : INetMessage
        {

            public SpawnChefTableMessage()
            {
            }

            public void Deserialize(NetworkReader reader)
            {
            }

            public void OnReceived()
            {
                Log.Debug("SpawnChefTableMessage Called...");
                switch (SceneManager.GetActiveScene().name)
                {
                    case "moon2":
                    {
                        SpawnTools.EnableVanillaTable();
                        break;
                    }
                    case "bazaar":
                    {
                        SpawnTools.EnableVanillaTable(new Vector3(-82.2492f, -47.2163f, 14.0186f));
                            
                        break;
                    }
                    default:
                    {
                        Log.Debug("SpawnChefTableMessage called from invalid scene? How the fuck did you let this happen.");
                        break;
                    }
                }
            }

            public void Serialize(NetworkWriter writer)
            {
            }
        }

        /// <summary>
        /// Recieves Server-side. The Client sends this to the Server to let it know the Client is ready, when the server recieves this, it should immediately send the CHEF status to the Client.
        /// </summary>
        public class RequestChefMessage : INetMessage
        {
            NetworkIdentity playerNetID;
            public RequestChefMessage()
            {
            }
            public RequestChefMessage(NetworkIdentity netID)
            {
                playerNetID = netID;
            }

            public void Deserialize(NetworkReader reader)
            {
                playerNetID = reader.ReadNetworkIdentity();
            }

            public void OnReceived()
            {
                if (ChefBazaar.isChefInBazaar || SceneManager.GetActiveScene().name == "moon2") 
                {
                    //NetworkUser user;
                    //NetworkIdentity netId = user.netIdentity;

                    NetworkConnection target = playerNetID.connectionToClient;
                    Log.Debug(playerNetID.ToString());
                    if (!ChefBazaar.classicChef.Value) {
                        new NetMessages.SpawnChefTableMessage().Send(target);
                    }
                }
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(playerNetID);
            }
        }
    }
}
