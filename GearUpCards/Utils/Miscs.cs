using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using Photon.Pun;
using UnboundLib;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace GearUpCards.Utils
{
    internal class Miscs
    {
        public static bool debugLog = true;
        public static bool infoLog = true;
        public static bool suppressError = false;

        public static float compensateBase = 0.0f;			// was 2.0
        public static float compensatePower = 1.0f;			// was 1.0
        public static float compensateThreshold = 50.0f;	// was 25.0

        // Debug logs
        public static void LogInfo(object message)
        {
			if (infoLog)
			{
				UnityEngine.Debug.Log(message);
			}
        }
        public static void Log(object message)
        {
            if (debugLog)
            {
                UnityEngine.Debug.Log(message);
            }
        }
        public static void LogWarn(object message)
        {
            if (!suppressError)
            {
                UnityEngine.Debug.LogWarning(message);
            }
        }
        public static void LogError(object message)
        {
            if (!suppressError)
            {
                UnityEngine.Debug.LogError(message);
            }
        }

		// take away a life from a player (check condition first!)
		public static void KillOneLife(Player player)
		{
            if (player.data.stats.remainingRespawns > 0)
            {
				player.gameObject.GetComponent<HealthHandler>().InvokeMethod("RPCA_Die_Phoenix", Vector2.up);
                player.data.view.RPC("RPCA_Die_Phoenix", RpcTarget.Others, Vector2.up);
            }
            else
            {
                player.gameObject.GetComponent<HealthHandler>().InvokeMethod("RPCA_Die", Vector2.up);
                player.data.view.RPC("RPCA_Die", RpcTarget.Others, Vector2.up);
            }
        }

		// ultimate kill command
		public static void GodKill(Player player)
		{
            player.gameObject.GetComponent<HealthHandler>().InvokeMethod("RPCA_Die", Vector2.up);
            player.data.view.RPC("RPCA_Die", RpcTarget.Others, Vector2.up);
        }

		// get the 'time flow' speed
		public static float TimeSpeedCalc(float durationFrom, float durationInto)
		{
			return durationFrom / durationInto;
		}

		// Vector Utils
		// Rotate CCW!
		public static Vector3 RotateVector(Vector3 vector, float degree)
        {
			float sin = Mathf.Sin(degree * Mathf.Deg2Rad);
			float cos = Mathf.Cos(degree * Mathf.Deg2Rad);

			float prevX = vector.x;
			float prevY = vector.y;

			vector.x = (cos * prevX) - (sin * prevY);
			vector.y = (sin * prevX) + (cos * prevY);

			return vector;
        }

		public static Vector3 ScaleNormalize(Vector3 scale)
		{
			float max = Mathf.Max(scale.x, scale.y, scale.z);

            return new Vector3(scale.x / max, scale.y / max, scale.z / max);
		}

		public static Vector2 BulletDropCorrection(Vector2 toward, Vector2 velocity, float gravitySum)
		{
			if (StatsMath.ApproxEqual(velocity.x, 0.0f) || StatsMath.ApproxEqual(gravitySum, 0.0f))
			{
				return velocity;
			}

			Vector2 newVel = Vector2.zero;
			float speed = velocity.magnitude;

			//compensating inaccuracy
			float effSpeed = speed + Mathf.Pow((compensateThreshold - speed) * compensateBase, compensatePower);

            Vector2 gravProject = Vector3.Project(gravitySum * Vector2.down, toward);
            Log($"gravSplit = {gravitySum * Vector2.down} / {toward} = {gravProject}");

            float speedNew = Mathf.Sqrt(effSpeed * effSpeed + 2.0f * gravProject.magnitude * toward.magnitude);
			float bulletTime = (speedNew - effSpeed) / gravProject.magnitude;
            Log($"bulletTime = ({speedNew} - {effSpeed}) / {gravProject.x} = {bulletTime}");

            if (bulletTime < 0.02)
			{
                return velocity;
            }

			newVel.x = toward.x / bulletTime;
            Log($"newVel.x = {toward.x} / {bulletTime} = {newVel.x}");

            if (float.IsNaN(newVel.x))
			{
				LogWarn("newVel.x is NaN!");
                return velocity;
            }

			newVel.y = Mathf.Sqrt((speed * speed) - (newVel.x * newVel.x)) * Mathf.Sign(gravitySum);
            Log($"newVel.y^2 = ({speed})^2 - ({newVel.x})^2 = {newVel.y}");

            if (float.IsNaN(newVel.y))
            {
                LogWarn("newVel.y is NaN!");
                newVel.y = Mathf.Sqrt((speedNew * speedNew) - (newVel.x * newVel.x)) * Mathf.Sign(gravitySum);
                Log($"newVel.y^2 = ({speedNew})^2 - ({newVel.x})^2 = {newVel.y}");
            }
            if (float.IsNaN(newVel.y))
            {
                LogWarn("newVel.y is NaN!!!");
                return toward.normalized * speed;
            }

            return newVel.normalized * speed;
        }

		// Component Wrapper
		public class RemoveAfterSpawn : MonoBehaviour
        {
			public float timeToRemove;

			private void Update()
            {
				if (gameObject.GetComponent<SpawnedAttack>() == null)
				{
					return;
				}
				else
				{
					RemoveAfterSeconds remover = gameObject.GetOrAddComponent<RemoveAfterSeconds>();
					remover.seconds = timeToRemove;
					remover.enabled = true;
					Destroy(this);
				}
            }
        }

		public class SetColorToParticles : MonoBehaviour
        {
			public List<ParticleSystem> particleSystems = new List<ParticleSystem>();
			private Dictionary<ParticleSystem, Gradient> backupGradients = new Dictionary<ParticleSystem, Gradient>();

			public Color targetColor;

			public bool overrideHue = true;

			private void Awake()
            {
				particleSystems.Clear();
				backupGradients.Clear();

				particleSystems.AddRange(gameObject.GetComponentsInChildren<ParticleSystem>());
                foreach (var part in particleSystems)
                {
					Gradient gradient = new Gradient();
					Gradient partGrad = part.colorOverLifetime.color.gradient;

					gradient.alphaKeys = new GradientAlphaKey[partGrad.alphaKeys.Length];
					for (int i = 0; i < partGrad.alphaKeys.Length; i++)
                    {
						gradient.alphaKeys[i] = new GradientAlphaKey(partGrad.alphaKeys[i].alpha, partGrad.alphaKeys[i].time);
					}

					gradient.colorKeys = new GradientColorKey[partGrad.colorKeys.Length];
					for (int i = 0; i < partGrad.colorKeys.Length; i++)
					{
						gradient.colorKeys[i] = new GradientColorKey(partGrad.colorKeys[i].color, partGrad.colorKeys[i].time);
					}

                    //backupGradients.TryAdd(part, gradient);
                    if (!backupGradients.ContainsKey(part))
                    {
                        backupGradients.Add(part, gradient);
                    }
                }
            }

			private void Update()
            {
				if (overrideHue) SetHueAll();
            }

			public void SetHueAll()
            {
                foreach (var part in particleSystems)
                {
					// Gradient partGrad = part.colorOverLifetime.color.gradient;
                    // for (int i = 0; i < partGrad.colorKeys.Length; i++)
                    // {
					// 	Color color = partGrad.colorKeys[i].color;
					// 
					// 	float hue, sat, value;
					// 	Color.RGBToHSV(color, out _, out sat, out value);
					// 	Color.RGBToHSV(targetColor, out hue, out _, out _);
					// 
					// 	partGrad.colorKeys[i].color = Color.HSVToRGB(hue, sat, value);
					// }

					Gradient partGrad = part.colorOverLifetime.color.gradient;
					Gradient newGrad = new Gradient();
					GradientColorKey[] colorKeys = new GradientColorKey[partGrad.colorKeys.Length];
					
					for (int i = 0; i < partGrad.colorKeys.Length; i++)
					{
						Color color = partGrad.colorKeys[i].color;
					
						float hue, sat, value;
						Color.RGBToHSV(color, out _, out sat, out value);
						Color.RGBToHSV(targetColor, out hue, out _, out _);
					
						colorKeys[i] = new GradientColorKey(Color.HSVToRGB(hue, sat, value), partGrad.colorKeys[i].time);
					}

					newGrad.SetKeys(colorKeys, partGrad.alphaKeys);
					var col = part.colorOverLifetime;
					col.color = newGrad;
				}
            }
        }
		
		public class SimpleRemoveAfterUnparent : MonoBehaviour
		{
			public GameObject parent = null;
			public bool triggered = false;

			public float delayTime = 3.0f;
			public float timer = 0.0f;

			void Start ()
			{
				if (parent == null)
				{
					parent = transform.parent.gameObject;
				}
			}

			void Update ()
			{
                if (parent == null)
                {
                    triggered = true;
                }

                if (triggered)
				{
					timer += TimeHandler.deltaTime;
					if (timer >= delayTime)
					{
						Destroy(gameObject);
					}
				}
			}
		}

		// Get GameObject's child by hierachy path
		public static GameObject GetChildByHierachy(GameObject queryFrom, string path)
		{
			List<string> accessList = StringSplit(path, '\\');
			Transform result = queryFrom.transform;

			foreach (string access in accessList)
			{
				bool success = false;

				for (int i = 0; i < result.childCount; i++)
				{
					if (result.GetChild(i).name.Equals(access))
					{
						result = result.GetChild(i);
						success = true;
						break;
					}
				}

				if (!success)
				{
					break;
				}
			}

			return result.gameObject;
		}

		// String Utils
		public static List<string> StringSplit(string input, char splitAt)
		{
			List<string> result = new List<string>();
			string buffer = "";

			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] != splitAt)
				{
					buffer += input[i];
				}
				else
				{
					result.Add(buffer);
					buffer = "";
				}
			}
			if (buffer != "")
			{
				result.Add(buffer);
			}

			return result;
		}

		public static int ValidateStringQuery(string targetString, string query)
        {
			// prioritize literal accuracy rather than get the first hit (that is just totally off-target match)
			int baseWeight = 15;
			int queryIndex = 0;
			int result = 0;
			bool flagPerfectMatch = true;

            for (int i = 0; i < targetString.Length; i++)
            {
                if (targetString[i] == query[queryIndex])
                {
					result += baseWeight;
					queryIndex++;
                }
                else
                {
					flagPerfectMatch = false;
					baseWeight--;
					queryIndex++;
				}

                if (queryIndex == query.Length)
                {
                    if (flagPerfectMatch)
                    {
						result += baseWeight * 10;
                    }
					break;
                }
            }

			return result;
        }

		// Credits to Pykess
		public static void CopyGunStats(Gun copyFromGun, Gun copyToGun)
		{
			Miscs.Log("CopyGunStats() : calling base");
			CopyGunStatsBase(copyFromGun, copyToGun);

			Miscs.Log("CopyGunStats() : cloning ShootPojectileAction");
            if (copyFromGun.ShootPojectileAction != null)
            {
				copyToGun.ShootPojectileAction = (Action<GameObject>)copyFromGun.ShootPojectileAction.Clone();
			}
			copyToGun.shootPosition = copyFromGun.shootPosition;

			// dealing with private fields
			Miscs.Log("CopyGunStats() : cloning attackAction");
			Action action = Traverse.Create(copyFromGun).Field("attackAction").GetValue<Action>();
			if (action != null)
			{
				Traverse.Create(copyToGun).Field("attackAction").SetValue((Action)action.Clone());
			}

			Miscs.Log("CopyGunStats() : finishing");
			// Traverse.Create(copyToGun).Field("gunAmmo").SetValue((GunAmmo)Traverse.Create(copyFromGun).Field("gunAmmo").GetValue());
			// Traverse.Create(copyToGun).Field("gunID").SetValue((int)Traverse.Create(copyFromGun).Field("gunID").GetValue());

			Traverse.Create(copyToGun).Field("spreadOfLastBullet").SetValue((float)Traverse.Create(copyFromGun).Field("spreadOfLastBullet").GetValue());
			Traverse.Create(copyToGun).Field("forceShootDir").SetValue((Vector3)Traverse.Create(copyFromGun).Field("forceShootDir").GetValue());
		}
		public static void CopyGunStatsNoActions(Gun copyFromGun, Gun copyToGun)
		{
			CopyGunStatsBase(copyFromGun, copyToGun);

			copyToGun.holdable = null;
			// copyToGun.ShootPojectileAction = null;
			copyToGun.ShootPojectileAction = (GameObject _) => { };

			// dealing with private fields
			// Traverse.Create(copyToGun).Field("attackAction").SetValue((Action)Traverse.Create(copyFromGun).Field("attackAction").GetValue());
			Action noAction = () => { };
			Traverse.Create(copyToGun).Field("attackAction").SetValue(noAction);

			// Traverse.Create(copyToGun).Field("gunAmmo").SetValue(null);
			// Traverse.Create(copyToGun).Field("gunID").SetValue((int)Traverse.Create(copyFromGun).Field("gunID").GetValue());

			Traverse.Create(copyToGun).Field("spreadOfLastBullet").SetValue((float)Traverse.Create(copyFromGun).Field("spreadOfLastBullet").GetValue());
			Traverse.Create(copyToGun).Field("forceShootDir").SetValue((Vector3)Traverse.Create(copyFromGun).Field("forceShootDir").GetValue());
		}

		public static void CopyGunStatsBase(Gun copyFromGun, Gun copyToGun)
        {
			copyToGun.ammo = copyFromGun.ammo;
			copyToGun.ammoReg = copyFromGun.ammoReg;
			copyToGun.attackID = copyFromGun.attackID;
			copyToGun.attackSpeed = copyFromGun.attackSpeed;
			copyToGun.attackSpeedMultiplier = copyFromGun.attackSpeedMultiplier;
			copyToGun.bodyRecoil = copyFromGun.bodyRecoil;
			copyToGun.bulletDamageMultiplier = copyFromGun.bulletDamageMultiplier;
			copyToGun.bulletPortal = copyFromGun.bulletPortal;
			copyToGun.bursts = copyFromGun.bursts;
			copyToGun.chargeDamageMultiplier = copyFromGun.chargeDamageMultiplier;
			copyToGun.chargeEvenSpreadTo = copyFromGun.chargeEvenSpreadTo;
			copyToGun.chargeNumberOfProjectilesTo = copyFromGun.chargeNumberOfProjectilesTo;
			copyToGun.chargeRecoilTo = copyFromGun.chargeRecoilTo;
			copyToGun.chargeSpeedTo = copyFromGun.chargeSpeedTo;
			copyToGun.chargeSpreadTo = copyFromGun.chargeSpreadTo;
			copyToGun.cos = copyFromGun.cos;
			copyToGun.currentCharge = copyFromGun.currentCharge;
			copyToGun.damage = copyFromGun.damage;
			copyToGun.damageAfterDistanceMultiplier = copyFromGun.damageAfterDistanceMultiplier;
			copyToGun.defaultCooldown = copyFromGun.defaultCooldown;
			copyToGun.destroyBulletAfter = copyFromGun.destroyBulletAfter;
			copyToGun.dmgMOnBounce = copyFromGun.dmgMOnBounce;
			copyToGun.dontAllowAutoFire = copyFromGun.dontAllowAutoFire;
			copyToGun.drag = copyFromGun.drag;
			copyToGun.dragMinSpeed = copyFromGun.dragMinSpeed;
			copyToGun.evenSpread = copyFromGun.evenSpread;
			copyToGun.explodeNearEnemyDamage = copyFromGun.explodeNearEnemyDamage;
			copyToGun.explodeNearEnemyRange = copyFromGun.explodeNearEnemyRange;
			copyToGun.forceSpecificAttackSpeed = copyFromGun.forceSpecificAttackSpeed;
			copyToGun.forceSpecificShake = copyFromGun.forceSpecificShake;
			copyToGun.gravity = copyFromGun.gravity;
			copyToGun.hitMovementMultiplier = copyFromGun.hitMovementMultiplier;
			// copyToGun.holdable = copyFromGun.holdable;
			copyToGun.ignoreWalls = copyFromGun.ignoreWalls;
			copyToGun.isProjectileGun = copyFromGun.isProjectileGun;
			copyToGun.isReloading = copyFromGun.isReloading;
			copyToGun.knockback = copyFromGun.knockback;
			copyToGun.lockGunToDefault = copyFromGun.lockGunToDefault;
			copyToGun.multiplySpread = copyFromGun.multiplySpread;
			copyToGun.numberOfProjectiles = copyFromGun.numberOfProjectiles;
			// copyToGun.objectsToSpawn = copyFromGun.objectsToSpawn;
			copyToGun.overheatMultiplier = copyFromGun.overheatMultiplier;
			copyToGun.percentageDamage = copyFromGun.percentageDamage;
			copyToGun.player = copyFromGun.player;
			copyToGun.projectielSimulatonSpeed = copyFromGun.projectielSimulatonSpeed;
			copyToGun.projectileColor = copyFromGun.projectileColor;
			// copyToGun.projectiles = copyFromGun.projectiles;
			copyToGun.projectileSize = copyFromGun.projectileSize;
			copyToGun.projectileSpeed = copyFromGun.projectileSpeed;
			copyToGun.randomBounces = copyFromGun.randomBounces;
			copyToGun.recoil = copyFromGun.recoil;
			copyToGun.recoilMuiltiplier = copyFromGun.recoilMuiltiplier;
			copyToGun.reflects = copyFromGun.reflects;
			copyToGun.reloadTime = copyFromGun.reloadTime;
			copyToGun.reloadTimeAdd = copyFromGun.reloadTimeAdd;
			copyToGun.shake = copyFromGun.shake;
			copyToGun.shakeM = copyFromGun.shakeM;
			//copyToGun.ShootPojectileAction = copyFromGun.ShootPojectileAction;
			//copyToGun.shootPosition = copyFromGun.shootPosition;
			copyToGun.sinceAttack = copyFromGun.sinceAttack;
			copyToGun.size = copyFromGun.size;
			copyToGun.slow = copyFromGun.slow;
			copyToGun.smartBounce = copyFromGun.smartBounce;
			//copyToGun.soundDisableRayHitBulletSound = copyFromGun.soundDisableRayHitBulletSound;
			//copyToGun.soundGun = copyFromGun.soundGun;
			//copyToGun.soundImpactModifier = copyFromGun.soundImpactModifier;
			//copyToGun.soundShotModifier = copyFromGun.soundShotModifier;
			copyToGun.spawnSkelletonSquare = copyFromGun.spawnSkelletonSquare;
			copyToGun.speedMOnBounce = copyFromGun.speedMOnBounce;
			copyToGun.spread = copyFromGun.spread;
			copyToGun.teleport = copyFromGun.teleport;
			copyToGun.timeBetweenBullets = copyFromGun.timeBetweenBullets;
			copyToGun.timeToReachFullMovementMultiplier = copyFromGun.timeToReachFullMovementMultiplier;
			copyToGun.unblockable = copyFromGun.unblockable;
			copyToGun.useCharge = copyFromGun.useCharge;
			copyToGun.waveMovement = copyFromGun.waveMovement;

			// duping objectsToSpawn
			copyToGun.objectsToSpawn = copyFromGun.objectsToSpawn.ToList().ToArray();
			copyToGun.projectiles = copyFromGun.projectiles.ToList().ToArray();

			// List<ObjectsToSpawn> listObjects = new List<ObjectsToSpawn>();
			// ObjectsToSpawn objectsToSpawn;
			// 
			// foreach (ObjectsToSpawn item in copyFromGun.objectsToSpawn)
			// {
			// 	objectsToSpawn = new ObjectsToSpawn();
			// 
			// 	if (objectsToSpawn.AddToProjectile != null)
			// 	{
			// 		objectsToSpawn.AddToProjectile = GameObject.Instantiate(item.AddToProjectile);
			// 	}
			// 	if (objectsToSpawn.effect != null)
			// 	{
			// 		objectsToSpawn.effect = GameObject.Instantiate(item.effect);
			// 	}
			// 	objectsToSpawn.direction = item.direction;
			// 	objectsToSpawn.spawnOn = ObjectsToSpawn.SpawnOn.notPlayer;
			// 	objectsToSpawn.spawnAsChild = item.spawnAsChild;
			// 	objectsToSpawn.numberOfSpawns = item.numberOfSpawns;
			// 	objectsToSpawn.normalOffset = item.normalOffset;
			// 	objectsToSpawn.stickToBigTargets = item.stickToBigTargets;
			// 	objectsToSpawn.stickToAllTargets = item.stickToAllTargets;
			// 	objectsToSpawn.zeroZ = item.zeroZ;
			// 	objectsToSpawn.removeScriptsFromProjectileObject = item.removeScriptsFromProjectileObject;
			// 	objectsToSpawn.scaleStacks = item.scaleStacks;
			// 	objectsToSpawn.scaleStackM = item.scaleStackM;
			// 	objectsToSpawn.scaleFromDamage = item.scaleFromDamage;
			// 	objectsToSpawn.stacks = item.stacks;
			// 
			// 	listObjects.Add(objectsToSpawn);
			// }
			// 
			// copyToGun.objectsToSpawn = listObjects.ToArray();

			// dealing with private fields
			// Traverse.Create(copyToGun).Field("attackAction").SetValue((Action)Traverse.Create(copyFromGun).Field("attackAction").GetValue());
			// Traverse.Create(copyToGun).Field("gunAmmo").SetValue((GunAmmo)Traverse.Create(copyFromGun).Field("gunAmmo").GetValue());
			// Traverse.Create(copyToGun).Field("gunID").SetValue((int)Traverse.Create(copyFromGun).Field("gunID").GetValue());
			// Traverse.Create(copyToGun).Field("spreadOfLastBullet").SetValue((float)Traverse.Create(copyFromGun).Field("spreadOfLastBullet").GetValue());
			// Traverse.Create(copyToGun).Field("forceShootDir").SetValue((Vector3)Traverse.Create(copyFromGun).Field("forceShootDir").GetValue());
		}
	}
}
