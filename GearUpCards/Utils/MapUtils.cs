﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Photon.Pun;
using UnboundLib;

using UnityEngine;
using UnityEngine.SceneManagement;

using GearUpCards.Utils;

namespace GearUpCards.Utils
{
    public class MapUtils : MonoBehaviour
    {
        public class MapObject
        {
            public enum MapObjectType
            {
                invalid = -1,

                generic = 0,
                background,

                buzzSaw,
                boxDestructible,

                mapExtendedWWMOZones
            }

            public GameObject gameObject;
            public MapObjectType type;

            public MapObject(GameObject gameObject, MapObjectType type)
            {
                this.gameObject = gameObject;
                this.type = type;
            }

            public static MapObjectType CheckMapObject(GameObject gameObject)
            {
                // check if it is active and (mostly) intact
                if (!gameObject.activeInHierarchy)
                {
                    return MapObjectType.invalid;
                }

                // Vanilla Map > Buzz Saw
                if (gameObject.GetComponent<CircleCollider2D>() &&
                    gameObject.GetComponent<DamageBox>()
                    )
                {
                    return MapObjectType.buzzSaw;
                }

                // Custom Map > Buzz Saw
                if (gameObject.GetComponent<PolygonCollider2D>() &&
                    gameObject.GetComponent<DamageBox>()
                    )
                {
                    return MapObjectType.buzzSaw;
                }

                // Destructible boxes
                if (gameObject.GetComponent<BoxCollider2D>() &&
                    gameObject.GetComponent<DestructibleBoxDestruction>())
                {
                    return MapObjectType.boxDestructible;
                }

                // Custom Map: WWMO zones
                if (gameObject.GetComponent<SpriteRenderer>() &&
                    gameObject.name.Contains("WWMO.MapObjects") &&
                    gameObject.GetComponent<BoxCollider2D>()
                    )
                {
                    return MapObjectType.mapExtendedWWMOZones;
                }

                // general solid or BG, traversible box/rect and circle/ellipse
                // Vanilla Map > Boxes
                if (gameObject.GetComponent<SpriteRenderer>() &&
                    gameObject.GetComponent<BoxCollider2D>()
                    )
                {
                    SFPolygon sfPolygon = gameObject.GetComponent<SFPolygon>();
                    if (!sfPolygon.isActiveAndEnabled)
                    {
                        return MapObjectType.background;
                    }
                    return MapObjectType.generic;
                }

                // Vanilla Map > Circle
                if (gameObject.GetComponent<SpriteRenderer>() &&
                    gameObject.GetComponent<CircleCollider2D>()
                    )
                {
                    SFPolygon sfPolygon = gameObject.GetComponent<SFPolygon>();
                    if (!sfPolygon.isActiveAndEnabled)
                    {
                        return MapObjectType.background;
                    }
                    return MapObjectType.generic;
                }

                // Custom Map > Circle
                if (gameObject.GetComponent<SpriteRenderer>() &&
                    gameObject.GetComponent<PolygonCollider2D>()
                    )
                {
                    SFPolygon sfPolygon = gameObject.GetComponent<SFPolygon>();
                    if (!sfPolygon.isActiveAndEnabled)
                    {
                        return MapObjectType.background;
                    }
                    return MapObjectType.generic;
                }

                return MapObjectType.invalid;
            }
        }

        public static List<MapObject> mapObjects = null;
        public static Scene mapScene;

        // public List<MapObject> GetMapObjectsList()
        // {
        //     if (mapObjects == null)
        //     {
        //         RPCA_UpdateMapObjectsList();
        //     }
        //     return mapObjects;
        // }

        // enlist all valid map objects
        [PunRPC]
        public static bool RPCA_UpdateMapObjectsList()
        {
            try
            {
                if (mapObjects != null)
                {
                    ClearMapObjectsList();
                }

                mapScene = SceneManager.GetSceneAt(1);
                if (!mapScene.IsValid()) return false;

                GameObject mapRootGM = mapScene.GetRootGameObjects()[0];
                if (!mapRootGM) return false;

                Transform[] mapGMTransforms = mapRootGM.GetComponentsInChildren<Transform>();
                MapObject.MapObjectType type;
                mapObjects = new List<MapObject>();

                foreach (Transform item in mapGMTransforms)
                {
                    type = MapObject.CheckMapObject(item.gameObject);
                    if (type != MapObject.MapObjectType.invalid)
                    {
                        mapObjects.Add(new MapObject(item.gameObject, type));
                    }
                }
                return true;
            }
            catch (System.Exception exception)
            {
                Miscs.LogError("[GearUp] MapDestructionMono.RPCA_UpdateMapObjectsList failed and caught!");
                Miscs.LogError(exception);
                return false;
            }
        }

        // check and destroy specific map object
        [PunRPC]
        public static bool RPCA_DestroyMapObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Miscs.Log("[GearUp] RPCA_DestroyMapObject: Game object not loaded!");
                return false;
            }

            bool checkSucess = RPCA_UpdateMapObjectsList();

            if (!mapScene.name.Equals(gameObject.scene.name))
            {
                Miscs.Log("[GearUp] RPCA_DestroyMapObject: Not a map game object!");
                return false;
            }
            
            if (checkSucess)
            {
                bool objectExists = false;

                foreach (MapObject item in mapObjects)
                {
                    if (ReferenceEquals(item.gameObject, gameObject))
                    {
                        objectExists = true;
                        break;
                    }
                }

                if (objectExists)
                {
                    Miscs.Log($"[GearUp] RPCA_DestroyMapObject: destroying [{gameObject.name}]");
                    Destroy(gameObject);

                    return true;
                }
                else
                {
                    Miscs.Log("[GearUp] RPCA_DestroyMapObject: target game object not exist/valid!");
                    return false;
                }
            }
            else return false;
        }

        // check and destroy all map objects within the area
        public static bool RPCA_DestroyMapObjectsAtArea(Vector3 groundZero, float radius)
        {
            bool checkSucess = RPCA_UpdateMapObjectsList();

            if (checkSucess)
            {
                float distance;
                Vector3 targetPos;
                Miscs.Log($"[GearUp] Area Map Destroy Ground Zero: [{groundZero}]");

                foreach (MapObject item in mapObjects)
                {
                    if (item.gameObject == null)
                    {
                        continue;
                    }
                    else if (!item.gameObject.activeInHierarchy)
                    {
                        // Miscs.Log($"[GearUp] RPCA_DestroyMapObject: skipping inactive [{item.gameObject.name}]");
                        continue;
                    }

                    targetPos = Vector3.Scale(item.gameObject.transform.position, new Vector3(1.0f, 1.0f, 0.0f));
                    distance = (targetPos - groundZero).magnitude;
                    // Miscs.Log($"Object: [{item.gameObject.name}] Pos: [{item.gameObject.transform.position}] Distance: [{distance}]");

                    if (distance <= radius)
                    {
                        Miscs.Log($"[GearUp] RPCA_DestroyMapObject: destroying [{item.gameObject.name}]");
                        Destroy(item.gameObject);
                        item.gameObject = null;

                        // continue;
                    }
                }

                return true;
            }
            else return false;
        }

        // clear the obsolete list on point end (to be called at main class mostly)
        public static void ClearMapObjectsList()
        {
            if (mapObjects != null)
            {
                mapObjects.Clear();
            }
            mapObjects = null;
        }

        // check if position point in colliders
        public static bool CheckPointInMapObject(Vector3 position, List<MapObject.MapObjectType> ignoreList)
        {
            // Miscs.Log("CheckPointInMapObject()");
            Vector2 checkPos = new Vector2(position.x, position.y);
            Collider2D collider;
            bool result = false;

            if (ignoreList == null)
            {
                ignoreList = new List<MapObject.MapObjectType>();
            }

            foreach (MapObject item in mapObjects)
            {
                //Miscs.Log("Check Ignore List");
                if (ignoreList.Contains(item.type))
                {
                    continue;
                }

                //Miscs.Log("Get Collider2D");
                collider = item.gameObject.GetComponent<Collider2D>();
                if (collider != null)
                {
                    //Miscs.Log("Check in Bounds");
                    // position.z = collider.bounds.center.z;

                    if (collider.OverlapPoint(checkPos))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
