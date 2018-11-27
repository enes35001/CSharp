using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fougerite;
using Fougerite.Events;
using RustPP;
using UnityEngine;
using System.Reflection;
using User = Fougerite.Player;

namespace AntiWallLoot
{
    public class AntiWallLootModule : Fougerite.Module
    {
        public override string Name { get { return "AntiWallLoot"; } }
        public override string Author { get { return "ice cold"; } }
        public override string Description { get { return "Blocks Wall looting"; } }
        public override Version Version { get { return new Version("1.1"); } }
        public static RaycastHit cachedRaycast;
        public static string cachedModelname;
        public static string cachedObjectname;
        public static float cachedDistance;
        public static Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
        public static bool cachedBoolean;
        public static Vector3 Vector3ABitLeft = new Vector3(-0.03f, 0f, -0.03f);
        public static Vector3 Vector3ABitRight = new Vector3(0.03f, 0f, 0.03f);
        public static Vector3 Vector3NoChange = new Vector3(0f, 0f, 0f);

        public override void Initialize()
        {
            Hooks.OnItemRemoved += itemRemove;
        }
        public override void DeInitialize()
        {
            Hooks.OnItemRemoved -= itemRemove;
        }
        public void itemRemove(InventoryModEvent e)
        {
            string name = e.Inventory.name;
            if(name == "WoodBoxLarge(Clone)" || name == "WoodBox(Clone)" || name == "Furnace(Clone)")
            {
                CheckWalloot(e);
            }
        }

        public void CheckWalloot(InventoryModEvent e)
        {
            User looter = e.Player;
            if (looter == null) return;
            if(IsWallooting(e.Player, e.Inventory))
            {
                ItemDataBlock block = DatablockDictionary.GetByName(e.ItemName);
                e.Inventory.AddItemAmount(block, 1);
                Notify(looter, e.Inventory);
                Server.GetServer().BroadcastFrom(Core.Name, "[color red] " + looter.Name + "[color red] Adlı Oyuncu Wall-Loot Yaptığı için Sunucuda Atıldı !");
                looter.Disconnect();               
            }
        }

        private void Notify(User looter, Inventory inv)
        {
            foreach (var pl in Server.GetServer().Players)
            {
                if (pl.Admin || pl.Moderator)
                {
                    pl.MessageFrom(Core.Name, string.Format("☢ [color cyan]{0} [color white]- Wall loot @ {1}", looter.Name, looter.Location));
                }
            }
        }

        public bool IsWallooting(User player, Inventory inventory)
        {
            var character = player.PlayerClient.controllable.GetComponent<Character>();
            if (!TraceEyes(character.eyesOrigin, character.eyesRay, Vector3NoChange, out cachedObjectname, out cachedModelname, out cachedDistance)) return false;
            if (inventory.name != cachedObjectname) return false;
            float distance = cachedDistance;
            if (TraceEyes(character.eyesOrigin, character.eyesRay, Vector3ABitLeft, out cachedObjectname, out cachedModelname, out cachedDistance))
            {
                if (cachedDistance < distance)
                {
                    if (cachedModelname.Contains("pillar") || cachedModelname.Contains("doorframe") || cachedModelname.Contains("wall"))
                    {
                        return true;
                    }
                    if (TraceEyes(character.eyesOrigin, character.eyesRay, Vector3ABitRight, out cachedObjectname, out cachedModelname, out cachedDistance))
                    {
                        if (cachedDistance < distance)
                        {
                            if (cachedModelname.Contains("pillar") || cachedModelname.Contains("doorframe") || cachedModelname.Contains("wall"))
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                    return false;
                }
                return false;
                   
            }
            return false;
        }

        bool TraceEyes(Vector3 origin, Ray ray, Vector3 directiondelta, out string objectname, out string modelname, out float distance)
        {
            modelname = string.Empty;
            objectname = string.Empty;
            distance = 0f;
            ray.direction += directiondelta;
            if (!MeshBatchPhysics.Raycast(ray, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) return false;
            if (cachedhitInstance != null) modelname = cachedhitInstance.graphicalModel.ToString();
            distance = cachedRaycast.distance;
            objectname = cachedRaycast.collider.gameObject.name;
            return true;
        }
    }
}
