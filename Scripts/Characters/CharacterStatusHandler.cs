// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    [RequireComponent(typeof(HealthHandler))]
    [RequireComponent(typeof(ManaHandler))]
    [RequireComponent(typeof(LevelSystemHandler))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CharacterStateManager))]
    [RequireComponent(typeof(CharacterAnimatorHandler))]
    public abstract class CharacterStatusHandler : Component
    {
        [Header("Hitable Collider Settings")]
        [SerializeField] List<Collider> hitableColliderList;

        [Header("Force Settings")]
        [SerializeField, Range(0.1f, 5)] private float physicsSpeedThreshold = 1.5f;

        private Rigidbody rigidbody;
        private bool canMove;
        private bool canRotateTransform;
        private float speed;
        private bool inCombat;
        private bool isInvulnerable;
        private bool wantsToAttack;
        private bool isAttacking;
        private bool isDoingHeavyAttack;
        private bool wantsToCastSkill;
        private bool isCastingSkill;
        private bool hasHyperArmor;
        private bool isHitAnimationOnGoing;
        private bool isDead;

        private HealthHandler healthHandler;
        private ManaHandler manaHandler;
        private LevelSystemHandler levelSystemHandler;

        private readonly UnityEvent<GameObject> onHitByCharacterEvent = new UnityEvent<GameObject>();
        private readonly UnityEvent onStatusInitialized = new UnityEvent();
        private readonly UnityEvent onDeathEvent = new UnityEvent();

        public Rigidbody Rigidbody { get => rigidbody; }
        public bool CanMove { get => canMove; protected set => canMove = value; }
        public bool CanRotateTransform { get => canRotateTransform; protected set => canRotateTransform = value; }
        public float Speed { get => speed; set => speed = value; }
        public bool InCombat { get => inCombat; set => inCombat = value; }
        public bool IsInvulnerable { get => isInvulnerable; set => isInvulnerable = value; }
        public bool WantsToAttack { get => wantsToAttack; set => wantsToAttack = value; }
        public bool IsAttacking { get => isAttacking; set => isAttacking = value; }
        public bool WantsToCastSkill { get => wantsToCastSkill; set => wantsToCastSkill = value; }
        public bool IsCastingSkill { get => isCastingSkill; set => isCastingSkill = value; }
        public bool IsDoingHeavyAttack { get => isDoingHeavyAttack; set => isDoingHeavyAttack = value; }
        public bool HasHyperArmor { get => hasHyperArmor; set => hasHyperArmor = value; }
        public bool IsHitAnimationOnGoing { get => isHitAnimationOnGoing; }
        public bool IsDead { get => isDead; }
        public bool IsForceEffectOnGoing => rigidbody.velocity.magnitude > physicsSpeedThreshold;

        protected LevelSystemHandler LevelSystemHandler { get => levelSystemHandler; }
        
        public UnityEvent<GameObject> OnHitByCharacterEvent { get => onHitByCharacterEvent; }
        public UnityEvent OnStatusInitialized => onStatusInitialized;
        public UnityEvent OnDeathEvent { get => onDeathEvent; }


        protected override void UpdateFeaturesAfterModifiers()
        {
            float currentSpeedFactor = GetFeature(Utils.SPEED_FEATURE_NAME).CurrentValue;
            float baseWeight = GetFeature(Utils.WEIGHT_FEATURE_NAME).BaseValue;
            float currentWeight = GetFeature(Utils.WEIGHT_FEATURE_NAME).CurrentValue;

            // Update speed based on player weight
            float newSpeed = currentSpeedFactor * baseWeight / currentWeight;
            SetFeatureCurrentValue(Utils.SPEED_FEATURE_NAME, currentValue: newSpeed);
            rigidbody.mass = currentWeight;

            // Update damage based on level
            float currentDamage = GetFeature(Utils.DAMAGE_FEATURE_NAME).CurrentValue;
            float levelDamageMultiplier = levelSystemHandler.DamageMultiplier;

            Debug.Log("UpdateFeaturesAfterModifiers GameObject: " + gameObject.name);
            Debug.Log("UpdateFeaturesAfterModifiers Current damage: " + currentDamage);

            float newDamage = currentDamage * levelDamageMultiplier;
            SetFeatureCurrentValue(Utils.DAMAGE_FEATURE_NAME, currentValue: newDamage);

            Debug.Log("UpdateFeaturesAfterModifiers Leveled damage: " + newDamage);
        }

        protected override void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            
            healthHandler = GetComponent<HealthHandler>();
            manaHandler = GetComponent<ManaHandler>();
            levelSystemHandler = GetComponent<LevelSystemHandler>();

            hitableColliderList.ForEach(collider => collider.isTrigger = true);

            UpdateStatus();
            HandleHitableColliders();

            // Notify that the character status has been initialized for the first time
            onStatusInitialized.Invoke();
            
            base.Start();
        }

        protected override void Update()
        {
            UpdateStatus();
            HandleHitableColliders();

            base.Update();
        }

        private void HandleHitableColliders()
        {
            hitableColliderList.ForEach(collider => collider.enabled = !isInvulnerable);
        }

        public void ApplyDamage(float amount, GameObject damageApplier)
        {
            healthHandler.ApplyDamage(amount);

            if (damageApplier.GetComponent<CharacterStatusHandler>() != null)
            {
                onHitByCharacterEvent.Invoke(damageApplier);
            }
        }

        public void RestoreHealth(float amount)
        {
            healthHandler.RestoreHealth(amount);
        }

        public void ConsumeMana(float amount)
        {
            manaHandler.ConsumeMana(amount);
        }

        public void RestoreMana(float amount)
        {
            manaHandler.RestoreMana(amount);
        }

        public bool HasEnoughMana(float amount)
        {
            return manaHandler.HasEnoughMana(amount);
        }
        
        public void OnMakeCharacterInvulnerable()
        {
            isInvulnerable = true;
        }
        
        public void OnMakeCharacterVulnerable()
        {
            isInvulnerable = false;
        }

        protected virtual void HitCheck()
        {
            CharacterAnimatorHandler characterAnimatorHandler = GetComponent<CharacterAnimatorHandler>();
            int hitLayer = characterAnimatorHandler.Animator.GetLayerIndex(Utils.HIT_LAYER_NAME);

            isHitAnimationOnGoing = !characterAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(hitLayer).IsName(Utils.emptyAnimationStateName) &&
                                    !characterAnimatorHandler.Animator.GetNextAnimatorStateInfo(hitLayer).IsName(Utils.emptyAnimationStateName);
        }

        protected virtual void IsDeadCheck()
        {
            if (!isDead && healthHandler.IsDead())
            {
                // Turn on the flag and invoke the event.
                // After that, all the listeners have to be removed from onDeathEvent, since the character is dead and
                // a still active would therefore be useless.
                isDead = true;
                onDeathEvent.Invoke();
                onDeathEvent.RemoveAllListeners();
                
                CharacterAnimatorHandler characterAnimatorHandler = GetComponent<CharacterAnimatorHandler>();
                characterAnimatorHandler.Animator.SetBool(characterAnimatorHandler.IsDeadHash, true);
            }
        }
        
        protected abstract void UpdateStatus();
        protected abstract void InCombatCheck();
        protected abstract void MoveCheck();
        protected abstract void RotationCheck();
        protected abstract void AttackCheck();
        protected abstract void IsDoingHeavyAttackCheck();
        public abstract void SaveStatusForNextScene();
        public abstract IEnumerator AddInstantForceCoroutine(Vector3 force, float forceDuration);

        public bool IsHitableCollider(Collider collider)
        {
            return hitableColliderList.Contains(collider);
        }

        private void OnPlayBodyFallsOnTheGround()
        {
            AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.bodyFallsOnTheGroundAudioName, gameObject);
        }
        
        private void OnPlayDeathHit()
        {
            if (IsDead)
            {
                AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.deathHitAudioName, gameObject);
            }
        }
    }
}