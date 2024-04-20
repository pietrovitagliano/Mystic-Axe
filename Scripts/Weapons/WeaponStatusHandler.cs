// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MysticAxe
{
    public abstract class WeaponStatusHandler : Component
    {
        [Header("Weapon Blood Spawner Settings")]
        [SerializeField] private WeaponBloodSpawner weaponBloodSpawner;
        [SerializeField] private GameObject bloodAttachedPrefab;
        [SerializeField] private BFX_BloodSettings[] bloodEffectPrefabArray;

        [Header("Hit Force Settings")]
        [SerializeField, Range(0.1f, 10)] private float hitForceMultiplier = 2f;
        [SerializeField, Range(0.1f, 5)] private float hitForceDuration = 0.6f;
        private Coroutine hitCoroutine;
        
        private Transform character;
        protected Transform weaponHand;
        private Rigidbody rigidbody;
        private Collider weaponCollider;

        private float drag;
        private float angularDrag;

        [Header("Mark Enemy As Hit Settings")]
        [SerializeField, Range(0f, 1f)] private float timeToHitSameCharacterAgain = 0.3f;
        private HashSet<GameObject> characterAlreadyHitSet = new HashSet<GameObject>();


        public Transform Character { get => character; set => character = value; }
        public Rigidbody Rigidbody { get => rigidbody; }
        public Collider WeaponCollider { get => weaponCollider; }
        public WeaponBloodSpawner WeaponBloodSpawner { get => weaponBloodSpawner; }
        public float Drag { get => drag; protected set => drag = value; }
        public float AngularDrag { get => angularDrag; protected set => angularDrag = value; }
        public HashSet<GameObject> EnemyAlreadyHitSet { get => characterAlreadyHitSet; }

        protected override void UpdateFeaturesAfterModifiers()
        {
            rigidbody.mass = GetFeature(Utils.WEIGHT_FEATURE_NAME).CurrentValue;
        }

        protected override void Start()
        {
            Character = transform.root;
            weaponHand = Utils.FindGameObjectInTransformWithTag(Character, Utils.WEAPON_HOLDER_TAG).transform;
            
            rigidbody = GetComponent<Rigidbody>();
            drag = rigidbody.drag;
            angularDrag = rigidbody.angularDrag;
            rigidbody.drag = 0;
            rigidbody.angularDrag = 0;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            weaponCollider = GetComponent<Collider>();


            InitializeWeapon();
            InitializeWeaponBloodSpawner();

            base.Start();
        }

        protected override void Update()
        {
            HandleClearCharacterAlreadyHitSet();

            base.Update();
        }

        // This method is used to do other initialization that depends from the particular kind of weapon.
        // Thus, they can only be done in a WeaponStatusHandler sub-class
        protected abstract void InitializeWeapon();
        protected abstract float ComputeDamageApplied();
        protected abstract void OnTriggerEnter(Collider otherCollider);
        
        public void HitCharacter(CharacterStatusHandler characterStatusHandler, Vector3 weaponPosition, bool ignoreHyperArmor = false)
        {
            if (characterStatusHandler.TryGetComponent(out CharacterHitHandler characterHitHandler))
            {
                characterHitHandler.HitCharacter(weaponPosition: weaponPosition, heavyHit: characterStatusHandler.IsDoingHeavyAttack, ignoreHyperArmor);
            }
        }
        
        public abstract Vector3 GetWeaponForwardDirection();

        public void ApplyDamage(CharacterStatusHandler characterStatusHandler)
        {
            if (!characterStatusHandler.IsInvulnerable)
            {
                float damage = ComputeDamageApplied();
                characterStatusHandler.ApplyDamage(damage, Character.gameObject);
            }
        }

        private void InitializeWeaponBloodSpawner()
        {
            weaponBloodSpawner = new WeaponBloodSpawner(transform, bloodAttachedPrefab, bloodEffectPrefabArray);
        }

        protected abstract void HandleClearCharacterAlreadyHitSet();

        public IEnumerator MarkCharacterAsHitCoroutine(GameObject character)
        {
            characterAlreadyHitSet.Add(character);
            yield return new WaitForSeconds(timeToHitSameCharacterAgain);
            characterAlreadyHitSet.Remove(character);
        }

        public bool HasCharacterBeenAlreadyHit(GameObject character)
        {
            return characterAlreadyHitSet.Contains(character);
        }

        public void ClearCharacterAlreadyHitSet()
        {
            characterAlreadyHitSet.Clear();
        }

        protected void ApplyHitForce(Transform characterHit)
        {
            // Get the rigidbody of the attacker
            Rigidbody attackerRigidbody = character.GetComponent<Rigidbody>();

            // Get the characterStatusHandler of the character hit
            CharacterStatusHandler characterHitStatusHandler = characterHit.GetComponent<CharacterStatusHandler>();

            // Calculate force direction
            // If the weapon is hold by a character, the direction will be from it towards characterHit.
            // Otherwise, if the weapon is not holded be someone (an arrow for example), the direction
            // will be from it weapon.position towards characterHit
            Vector3 forceStartPoint = transform.parent != null ? character.position : transform.position;
            Vector3 forceDirection = (characterHit.transform.position - forceStartPoint).normalized;

            // Calculate force magnitude based on mass and forceMultiplier
            // If the weapon is hold by a character, the mass will be the attacker mass
            // (using the component approach, the attacker mass include the weapon's one).
            // Otherwise, if the weapon is not holded be someone (an arrow for example), the mass
            // will be the weapon mass
            float weaponMass = transform.parent != null ? attackerRigidbody.mass : rigidbody.mass;
            float forceMagnitude = weaponMass * hitForceMultiplier;
            
            // If the force coroutine is already running, stop it and restart it
            if (hitCoroutine != null)
            {
                StopCoroutine(hitCoroutine);
            }

            // Apply force to character
            hitCoroutine = StartCoroutine(characterHitStatusHandler.AddInstantForceCoroutine(forceMagnitude * forceDirection, hitForceDuration));
        }

        public abstract void PlayMeleeLightWhoosh();
        public abstract void PlayMeleeHeavyWhoosh();
        public abstract void StopMeleeLightWhoosh();
        public abstract void StopMeleeHeavyWhoosh();

        public void ResetWeaponRigidbodyVelocityAndRotation()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }
}