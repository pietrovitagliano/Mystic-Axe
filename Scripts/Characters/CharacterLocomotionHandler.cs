// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public abstract class CharacterLocomotionHandler : MonoBehaviour
    {
        [Header("Speed Settings")]
        [SerializeField, Range(1, 3)] private float walkSpeed = 1.25f;
        [SerializeField, Range(1, 5)] private float targetingSpeed = 3.5f;
        [SerializeField, Range(1, 7)] private float baseRunSpeed = 5f;

        [Header("Acceleration Time Settings")]
        [SerializeField, Range(0.1f, 3)] private float startMovingTime = 0.3f;
        [SerializeField, Range(0.1f, 3)] private float stopMovingTime = 0.6f;

        [Header("Rotation Settings")]
        [SerializeField, Min(1)] private float rotationSpeed = 12;

        [Header("Movement Settings")]
        [SerializeField, Range(0, 1)] float movementLerpTime = 0.2f;
        
        
        public float WalkSpeed { get => walkSpeed; }
        public float TargetingSpeed { get => targetingSpeed; }
        public float BaseRunSpeed { get => baseRunSpeed; }
        protected float StartMovingTime { get => startMovingTime; }
        protected float StopMovingTime { get => stopMovingTime; }
        protected float RotationSpeed { get => rotationSpeed; }
        protected float MovementLerpTime { get => movementLerpTime; }


        protected virtual void Update()
        {
            HandleSpeed();
            HandleRotation();
        }

        protected abstract void UpdateAnimatorMovementParams();
        protected abstract void HandleSpeed();
        protected abstract void HandleRotation();

        protected abstract void OnPlayWalkSound();

        protected abstract void OnPlayRunSound();

        protected abstract void OnPlayLightLandingSound();
        protected abstract void OnPlayHeavyLandingSound();
    }
}