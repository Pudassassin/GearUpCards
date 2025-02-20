using HarmonyLib;
using UnityEngine;

namespace GearUpCards.Utils
{
    public class MoveTransformSimulator
    {
        public float gravity = 100f;

        public float drag = 0.1f;

        public float dragMinSpeed = 1f;

        public float velocitySpread;

        public float spread;

        public Vector3 localForce = new Vector3(0.0f, 0.0f, 50.0f);

        public Vector3 worldForce = new Vector3(0.0f, 5.0f, 0.0f);

        public float multiplier = 1f;

        public Vector3 velocity;

        public float distanceTravelled;

        public bool DontRunStart;

        public float selectedSpread;

        public bool allowStop;

        public int simulateGravity;

        // private int randomSeed;

        public float simulationSpeed = 1f;

        public Vector3 position;

        public MoveTransformSimulator(MoveTransform moveTransform)
        {
            gravity = moveTransform.gravity;
            drag = moveTransform.drag;
            dragMinSpeed = moveTransform.dragMinSpeed;
            velocitySpread = moveTransform.velocitySpread;
            spread = moveTransform.spread;
            localForce = moveTransform.localForce;
            worldForce = moveTransform.worldForce;
            multiplier = moveTransform.multiplier;
            velocity = moveTransform.velocity;
            distanceTravelled = moveTransform.distanceTravelled;
            allowStop = moveTransform.allowStop;
            simulateGravity = moveTransform.simulateGravity;
            simulationSpeed = Traverse.Create(moveTransform).Field("simulationSpeed").GetValue<float>();

            position = moveTransform.transform.position;
        }
        public MoveTransformSimulator(Gun gun)
        {
            localForce *= gun.projectileSpeed;
            simulationSpeed *= gun.projectielSimulatonSpeed;
            gravity *= gun.gravity;
            worldForce *= gun.gravity;
            drag = gun.drag;
            drag = Mathf.Clamp(drag, 0f, 45f);
            velocitySpread = Mathf.Clamp(this.spread * 50f, 0f, 50f);
            dragMinSpeed = this.dragMinSpeed;
            //localForce *= Mathf.Lerp(1f - component3.velocitySpread * 0.01f, 1f + component3.velocitySpread * 0.01f, randomSeed);
            selectedSpread = 0f;

            velocity = localForce.magnitude * gun.shootPosition.forward.normalized + worldForce;
            position = gun.shootPosition.position;
        }

        public void Update()
        {
            float num = Mathf.Clamp(TimeHandler.deltaTime, 0f, 0.02f);
            float deltaTime = TimeHandler.deltaTime;
            num *= simulationSpeed;
            deltaTime *= simulationSpeed;
            if (simulateGravity == 0)
            {
                velocity += gravity * Vector3.down * deltaTime * multiplier;
            }
            if ((velocity.magnitude > 2f || allowStop) && velocity.magnitude > dragMinSpeed)
            {
                velocity -= velocity * Mathf.Clamp(drag * num * Mathf.Clamp(multiplier, 0f, 1f), 0f, 1f);
            }
            position += velocity * deltaTime * multiplier;
            distanceTravelled += velocity.magnitude * deltaTime * multiplier;
        }

        public void Update(float deltaTime)
        {
            float num = Mathf.Clamp(deltaTime, 0f, 0.02f);
            num *= simulationSpeed;
            deltaTime *= simulationSpeed;
            if (simulateGravity == 0)
            {
                velocity += gravity * Vector3.down * deltaTime * multiplier;
            }
            if ((velocity.magnitude > 2f || allowStop) && velocity.magnitude > dragMinSpeed)
            {
                velocity -= velocity * Mathf.Clamp(drag * num * Mathf.Clamp(multiplier, 0f, 1f), 0f, 1f);
            }
            position += velocity * deltaTime * multiplier;
            distanceTravelled += velocity.magnitude * deltaTime * multiplier;
        }

        // public float GetUpwardsCompensation(Vector2 start, Vector2 end)
        // {
        //     start.y = 0.5f;
        //     end.y = 0.5f;
        //     return Mathf.Pow(Vector3.Distance(start, end), 2.06f) * gravity / velocity.magnitude * 0.012f;
        // }
    }
}
