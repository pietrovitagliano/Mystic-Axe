// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(AxeAnimatorHandler))]
    public class AxeStatusHandler : WeaponStatusHandler
    {
        private AxeAnimatorHandler axeAnimatorHandler;
        private Transform weaponFolder;

        private PlayerStatusHandler playerStatusHandler;
        private PlayerCombatHandler playerCombatHandler;

        private bool isThrown;
        private bool isReturning;
        private bool hasHitWall;
        private float currentRotationSpeed;

        public PlayerStatusHandler PlayerStatusHandler { get => playerStatusHandler; }
        public bool IsThrown { get => isThrown; set => isThrown = value; }
        public bool IsReturning { get => isReturning; set => isReturning = value; }
        public bool HasHitWall { get => hasHitWall; set => hasHitWall = value; }
        public float CurrentRotationSpeed { get => currentRotationSpeed; set => currentRotationSpeed = value; }

        
        protected override List<Feature> InitializeFeatures() 
        {
            return JsonDatabase.Instance.GetDataFromJson<FeaturesJsonMap>(Utils.PLAYER_AXE_FEATURES_JSON_NAME).Features;
        }
    
        protected override void InitializeWeapon()
        {            
            axeAnimatorHandler = GetComponent<AxeAnimatorHandler>();
            axeAnimatorHandler.Animator.enabled = false;

            weaponFolder = GameObject.FindGameObjectWithTag(Utils.WEAPON_FOLDER_TAG).transform;

            playerStatusHandler = Character.GetComponent<PlayerStatusHandler>();
            playerCombatHandler = Character.GetComponent<PlayerCombatHandler>();

            // Get and add the throw modifier to the axe, making it to be effective only when the axe is thrown
            ModifiersJsonMap modifiersJsonMap = JsonDatabase.Instance.GetDataFromJson<ModifiersJsonMap>(Utils.MODIFIERS_JSON_NAME);
            Modifier axeThrowModifier = modifiersJsonMap.GetModifierByID(Utils.AXE_THROW_MODIFIER_ID);
            axeThrowModifier.Condition = new Func<bool>(() => !IsAxeOnBody());
            AddModifier(axeThrowModifier);
        }

        protected override void Update()
        {
            HandleAxePhysicsWhenPlayerIsAlive();
            HandleCollider();

            base.Update();
        }

        // DEBUG
        private void LateUpdate()
        {
            Debug.Log("Weapon name: " + gameObject.name);
            foreach (Feature feature in Features)
            {
                Debug.Log("Weapon Feature Name: " + feature.Name);
                Debug.Log("Weapon Feature BaseValue: " + feature.BaseValue);
                Debug.Log("Weapon Feature CurrentValue: " + feature.CurrentValue);
                Debug.Log("Weapon Feature Category: " + feature.Category);
                Debug.Log("Weapon Feature Type: " + feature.Type);
            }

            Debug.Log("Axe Modifiers");
            foreach (Modifier modifier in Modifiers)
            {
                Debug.Log("Axe Modifier Name: " + modifier.FeatureName);
                Debug.Log("Axe Modifier ID: " + modifier.ModifierID);
                Debug.Log("Axe Modifier Value: " + modifier.Factor);
                Debug.Log("Axe Modifier Category: " + modifier.FeatureCategory);
                Debug.Log("Axe Modifier Type: " + modifier.Type);
                Debug.Log("Axe Modifier Duration: " + modifier.Duration);
            }
        }

        protected override void OnTriggerEnter(Collider otherCollider)
        {
            int enemyLayerValue = LayerMask.NameToLayer(Utils.ENEMY_LAYER_NAME);

            if (IsAxeOnBody() && playerCombatHandler.BaseAttackCanDamage && otherCollider.gameObject.layer == enemyLayerValue)
            {
                Transform character = otherCollider.transform.root;
                CharacterStatusHandler characterHitStatusHandler = character.GetComponent<CharacterStatusHandler>();
                
                if (!HasCharacterBeenAlreadyHit(character.gameObject) && !characterHitStatusHandler.IsDead && characterHitStatusHandler.IsHitableCollider(otherCollider))
                {
                    StartCoroutine(MarkCharacterAsHitCoroutine(character.gameObject));

                    // Apply damage and play hit animation
                    ApplyDamage(characterHitStatusHandler);
                    HitCharacter(characterHitStatusHandler, weaponPosition: transform.position);
                    WeaponBloodSpawner.MakeCharacterBleed(characterHitStatusHandler, otherCollider);

                    if (characterHitStatusHandler.IsDead)
                    {
                        // Play enemy death hit sound
                        AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.deathHitAudioName, gameObject);
                    }
                    else
                    {
                        // Play hit sound
                        string hitAudioName = !playerStatusHandler.IsDoingHeavyAttack ? Utils.playerAxeEnemyLightHitAudioName : Utils.playerAxeEnemyHeavyHitAudioName;
                        AudioManager.Instance.PlaySound(hitAudioName, gameObject);
                    }
                }
            }
        }
        
        protected override float ComputeDamageApplied()
        {            
            float damage;
            if (!IsAxeOnBody())
            {
                HashSet<Component> simulatedComponents = new HashSet<Component> { this };
                List<Feature> playerFeatures = playerStatusHandler.GetFeaturesWithSimulatedComponents(simulatedComponents);
                damage = playerFeatures.Find(feature => feature.Name == Utils.DAMAGE_FEATURE_NAME).CurrentValue;
            }
            else
            {
                damage = playerStatusHandler.GetFeature(Utils.DAMAGE_FEATURE_NAME).CurrentValue;
                
                if (playerStatusHandler.IsDoingHeavyAttack) 
                {
                    float heavyAttackMultiplier = playerStatusHandler.GetFeature(Utils.HEAVY_ATTACK_MULTIPLIER_FEATURE_NAME).CurrentValue;
                    damage *= heavyAttackMultiplier;
                }
            }

            return damage;
        }

        public bool IsAxeOnBody()
        {
            return transform.parent == weaponHand || transform.parent == weaponFolder;
        }

        public bool IsAxeIsInFolder()
        {
            return transform.parent == weaponFolder;
        }

        public bool IsAxeInHand()
        {
            return transform.parent == weaponHand;
        }            

        private void HandleAxePhysicsWhenPlayerIsAlive()
        {
            if (!playerStatusHandler.IsDead)
            {
                // The axe has been thrown or it's returning
                if (isThrown || isReturning)
                {
                    EnablesRigidBodyProperties(propertiesEnabled: false);

                    if (isThrown)
                    {
                        Rigidbody.isKinematic = false;
                    }

                    if (isReturning)
                    {
                        Rigidbody.isKinematic = true;
                    }
                }
                // The axe is neither returning, nor throwing. Thus, it is in player's hand or is on the ground
                else
                {
                    // Axe is going on the ground (throw ended) or is already on the ground
                    if (!IsAxeOnBody())
                    {
                        EnablesRigidBodyProperties(propertiesEnabled: true);
                    }
                    // Axe is on player body
                    else
                    {
                        EnablesRigidBodyProperties(propertiesEnabled: false);
                    }
                }
            }
        }

        public void EnablesRigidBodyProperties(bool propertiesEnabled)
        {
            if (propertiesEnabled)
            {
                Rigidbody.useGravity = true;
                Rigidbody.drag = Drag;
                Rigidbody.angularDrag = AngularDrag;
            }
            else
            {
                Rigidbody.useGravity = false;
                Rigidbody.drag = 0;
                Rigidbody.angularDrag = 0;
            }
        }

        private void HandleCollider()
        {
            if (IsAxeOnBody())
            {
                WeaponCollider.isTrigger = true;
                WeaponCollider.enabled = true;
                Rigidbody.isKinematic = true;
            }
        }

        public void PlayThunderstormGroundHit()
        {
            AudioManager.Instance.PlaySound(Utils.playerAxeThunderstormGroundHitAudioName, gameObject);
        }

        public override Vector3 GetWeaponForwardDirection()
        {
            return -1 * transform.forward;
        }

        public void RotateAxe()
        {
            // Vector3.back is needed to make the axe rotating forward
            // Rotate the axe forward to cut enemies on its path using the rotation speed computed before
            transform.localEulerAngles += currentRotationSpeed * Time.deltaTime * Vector3.back;
        }
        
        protected override void HandleClearCharacterAlreadyHitSet()
        {
            bool attackInputPressed = InputHandler.Instance.LightAttackPressed || InputHandler.Instance.HeavyAttackPressed;
            bool hasAttackStarted = playerStatusHandler.WantsToAttack || (playerCombatHandler.CanAttackBeInterruptedByNextAttack && attackInputPressed);

            if (EnemyAlreadyHitSet.Count > 0 && IsAxeOnBody() && (hasAttackStarted || !playerStatusHandler.IsAttacking))
            {
                ClearCharacterAlreadyHitSet();
            }
        }

        public override void PlayMeleeLightWhoosh()
        {
            AudioManager.Instance.PlaySound(Utils.playerAxeMeleeLightWhooshesAudioName, gameObject);
        }

        public override void PlayMeleeHeavyWhoosh()
        {
            AudioManager.Instance.PlaySound(Utils.playerAxeMeleeHeavyWhooshesAudioName, gameObject);
        }

        public override void StopMeleeLightWhoosh()
        {
            AudioManager.Instance.StopSound(Utils.playerAxeMeleeLightWhooshesAudioName, gameObject);
        }

        public override void StopMeleeHeavyWhoosh()
        {
            AudioManager.Instance.StopSound(Utils.playerAxeMeleeHeavyWhooshesAudioName, gameObject);
        }

        public void OnPlayThrowSoundWithFadeIn(float fadeTime = 0)
        {
            AudioManager.Instance.PlayMutuallyExclusiveSoundWithFadeIn(Utils.playerAxeThrowWhooshesAudioName, gameObject, fadeTime);
        }

        public void OnStopThrowSoundWithFadeOut(float fadeTime = 0)
        {
            AudioManager.Instance.StopSoundWithFadeOut(Utils.playerAxeThrowWhooshesAudioName, gameObject, fadeTime);
        }
    }
}