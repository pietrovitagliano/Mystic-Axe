// Author: Pietro Vitagliano

using DungeonArchitect;
using DungeonArchitect.Builders.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MysticAxe
{
    public static class Utils
    {
        #region Humanoid Character Height In Percentage
        public const float FULL_HEIGHT = 1f;
        public const float HEAD_HEIGHT = 0.95f;
        public const float UPPER_CHEST_HEIGHT = 0.7f;
        public const float MID_HEIGHT = 0.5f;
        public const float UPPER_LEGS_HEIGHT = 0.4f;
        public const float LEGS_HEIGHT = 0.3f;
        #endregion

        #region Scene Names
        public static readonly string START_SCENE = "Start Scene";
        public static readonly string MAIN_MENU_SCENE = "Main Menu Scene";
        public static readonly string DUNGEON_SCENE = "Dungeon Scene";
        public static readonly string BOSS_FIGHT_SCENE = "Boss Scene";
        public static readonly string EMPTY_SCENE = "Empty Scene";
        #endregion

        #region Error Strings
        public static readonly string noModifierFoundWithIDError = "No modifier found with ID: ";
        #endregion

        #region Player Preferences Keys
        public static readonly string RESOLUTION_WIDTH_PREFERENCE_KEY = "resolutionWidth";
        public static readonly string RESOLUTION_HEIGHT_PREFERENCE_KEY = "resolutionHeight";
        public static readonly string GRAPHICS_PREFERENCE_KEY = "graphics";
        #endregion

        #region AudioMixer Strings
        public static readonly string AUDIO_MIXER_MASTER_VOLUME_NAME = "masterVolume";
        public static readonly string AUDIO_MIXER_MUSIC_VOLUME_NAME = "musicVolume";
        public static readonly string AUDIO_MIXER_SFX_VOLUME_NAME = "sfxVolume";
        #endregion

        #region Audio Clip Names
        public static readonly string thunderstormLoopAudioName = "Thunder Storm Loop";
        public static readonly string lightningStrikeImpactAudioName = "Lightning Strike Impact";
        public static readonly string playerAxeMeleeLightWhooshesAudioName = "Player Axe Melee Light Whooshes";
        public static readonly string playerAxeMeleeHeavyWhooshesAudioName = "Player Axe Melee Heavy Whooshes";
        public static readonly string cleaverMeleeWhooshesAudioName = "Cleaver Melee Whooshes";
        public static readonly string bossMeleeWhooshesAudioName = "Boss Melee Whooshes";
        public static readonly string playerAxeThrowWhooshesAudioName = "Player Axe Throw Whooshes";
        public static readonly string playerAxeWallHitAudioName = "Player Axe Wall Hit";
        public static readonly string playerAxeCatchAudioName = "Player Axe Catch";
        public static readonly string playerAxeEnemyLightHitAudioName = "Player Axe Enemy Light Hit";
        public static readonly string playerAxeEnemyHeavyHitAudioName = "Player Axe Enemy Heavy Hit";
        public static readonly string deathHitAudioName = "Death Hit";
        public static readonly string bodyFallsOnTheGroundAudioName = "Body Falls On The Ground";
        public static readonly string playerShieldHitAudioName = "Player Shield Hit";
        public static readonly string playerAxeThunderstormGroundHitAudioName = "Player Axe Thunderstorm Ground Hit";
        public static readonly string drinkPotionAudioName = "Drink Potion";
        public static readonly string UIConfirmAudioName = "UI Confirm";
        public static readonly string UINavigationAudioName = "UI Navigation";
        public static readonly string UIBackAudioName = "UI Back";
        public static readonly string coinsAudioName = "Coins";
        public static readonly string thunderBlessingExplosionAudioName = "Thunder Blessing Explosion";
        public static readonly string thunderBlessingLoopAudioName = "Thunder Blessing Loop";
        public static readonly string levelUpLoopAudioName = "Level Up Loop";
        public static readonly string levelUpExplosionAudioName = "Level Up Explosion";
        public static readonly string healEffectAudioName = "Heal Effect";
        public static readonly string battleOSTAudioName = "Battle OST";
        public static readonly string dungeonOSTAudioName = "Dungeon OST";
        public static readonly string playerWalkAudioName = "Player Walk";
        public static readonly string playerRunAudioName = "Player Run";
        public static readonly string playerLightLandingAudioName = "Player Light Landing";
        public static readonly string playerHeavyLandingAudioName = "Player Heavy Landing";
        public static readonly string enemyHeavyLandingAudioName = "Enemy Heavy Landing";
        public static readonly string playerRollAudioName = "Player Roll";
        public static readonly string enemyWalkAudioName = "Enemy Walk";
        public static readonly string enemyRunAudioName = "Enemy Run";
        public static readonly string safeAreaOSTAudioName = "Safe Area OST";
        public static readonly string bossFightOSTAudioName = "Boss Fight OST";
        public static readonly string keyObtainedAudioName = "Key Obtained";
        public static readonly string consumableCollectedAudioName = "Consumable Collected";
        #endregion

        #region In Game Item Names
        public static readonly string healthPotionItemName = "health_potion";
        public static readonly string manaPotionItemName = "mana_potion";
        #endregion

        #region Directory & File Names
        public static readonly string PLAYER_FEATURES_JSON_NAME = "player_features.json";
        public static readonly string PLAYER_AXE_FEATURES_JSON_NAME = "player_axe_features.json";
        public static readonly string ENEMY_FEATURES_JSON_NAME = "enemy_features.json";
        public static readonly string CLEAVER_FEATURES_JSON_NAME = "enemy_cleaver_features.json";
        public static readonly string MODIFIERS_JSON_NAME = "modifiers.json";
        public static readonly string ITEMS_JSON_NAME = "items.json";
        public static readonly string ENEMY_LEVELS_BY_DUNGEON_JSON_NAME = "enemyLevelsByDungeon.json";
        public static readonly string DUNGEON_LEVELS_JSON_NAME = "dungeonLevels.json";
        public static readonly string DUNGEON_TO_RARITY_LEVEL_JSON_NAME = "dungeonToRarityLevel.json";
        #endregion

        #region Push To Data Features Names
        public static readonly string SPEED_FEATURE_NAME = "speed";
        public static readonly string DAMAGE_FEATURE_NAME = "damage";
        public static readonly string RANGE_FEATURE_NAME = "range";
        public static readonly string HEAVY_ATTACK_MULTIPLIER_FEATURE_NAME = "heavy_attack_multiplier";
        public static readonly string WEIGHT_FEATURE_NAME = "weight";
        public static readonly string LOYALTY_TO_AZAZEL_FEATURE_NAME = "loyalty_to_azazel";
        public static readonly string ALERT_DISTANCE_FEATURE_NAME = "alert_distance";
        public static readonly string FIELD_OF_VIEW_FEATURE_NAME = "field_of_view";
        public static readonly string VIEW_DISTANCE_FEATURE_NAME = "view_distance";
        #endregion

        #region In Game Modifier IDs
        public static readonly string AZAZEL_STRENGTH_MODIFIER_ID = "001";
        public static readonly string THUNDER_BLESSING_MODIFIER_ID = "002";
        public static readonly string AXE_THROW_MODIFIER_ID = "003";
        public static readonly string PHYSICAL_DAMAGE_0_MODIFIER_ID = "004";
        public static readonly string THUNDERSTORM_ELECTRIC_DAMAGE_MODIFIER_ID = "005";
        #endregion

        #region Tag String Constants
        public static readonly string PLAYER_TAG = "Player";
        public static readonly string PLAYER_AXE_TAG = "PlayerAxe";
        public static readonly string WEAPON_FOLDER_TAG = "Weapon Folder";
        public static readonly string WEAPON_HOLDER_TAG = "Weapon Holder";
        public static readonly string PLAYER_SHIELD_HOLDER_TAG = "PlayerShieldHolder";
        public static readonly string BOSS_TAG = "Boss";
        public static readonly string TARGET_WHILE_AIMING_TAG = "TargetWhileAiming";
        public static readonly string CANVAS_LOCK_ON_TARGET_TAG = "CanvasLockOnTarget";
        public static readonly string CANVAS_CROSSHAIR_TAG = "CanvasCrosshair";
        public static readonly string EXPLORING_CAMERA_TAG = "ExploringCamera";
        public static readonly string AIMING_CAMERA_TAG = "AimingCamera";
        public static readonly string TARGETING_CAMERA_TAG = "TargetingCamera";
        public static readonly string CAMPFIRE_CAMERA_TAG = "CampfireCamera";
        public static readonly string LIGHTNING_RING_TAG = "LightningRing";
        public static readonly string CHEST_TAG = "Chest";
        public static readonly string PLAYER_STATS_TAG = "Player Stats";
        public static readonly string BASE_ENEMY_STATS_TAG = "Base Enemy Stats";
        public static readonly string BOSS_ENEMY_STATS_TAG = "Boss Enemy Stats";
        public static readonly string POTION_HOLDER_TAG = "Potion Holder";
        public static readonly string SUN_TAG = "Sun";
        public static readonly string AXE_RETURNING_MIDDLE_POINT_TAG = "Axe Returning Middle Point";
        public static readonly string PLAYER_SPAWN_POSITION_TAG = "Player Spawn Position";
        public static readonly string GROUND_TAG = "Ground";
        public static readonly string MOCK_AXE_TAG = "Mock Axe";
        #endregion

        #region Layer String Constants
        public static readonly string GROUND_LAYER_NAME = "Default";
        public static readonly string OBSTACLES_LAYER_NAME = "Obstacles";
        public static readonly string INVISIBLE_WALL_LAYER_NAME = "InvisibleWall";
        public static readonly string PLAYER_LAYER_NAME = "Player";
        public static readonly string PLAYER_WEAPON_LAYER_NAME = "PlayerWeapon";
        public static readonly string PLAYER_SHIELD_LAYER_NAME = "PlayerShield";
        public static readonly string ENEMY_LAYER_NAME = "Enemy";
        public static readonly string ENEMY_WEAPON_LAYER_NAME = "EnemyWeapon";
        #endregion

        #region Player Animator Layer Names
        public static readonly string MOVEMENT_LAYER_NAME = "MovementLayer";
        public static readonly string ATTACK_LAYER_NAME = "AttackLayer";
        public static readonly string HIT_LAYER_NAME = "HitLayer";
        public static readonly string EQUIP_UNEQUIP_LAYER_NAME = "EquipUnequipAxeLayer";
        public static readonly string BLOCK_LAYER_NAME = "BlockLayer";
        public static readonly string THROW_AXE_LAYER_NAME = "ThrowAxeLayer";
        public static readonly string RECALL_AXE_LAYER_NAME = "RecallAxeLayer";
        public static readonly string SKILLS_LAYER_NAME = "SkillsLayer";
        #endregion

        #region Player Animator Animation Names
        public static readonly string emptyAnimationStateName = "Empty State";
        public static readonly string landingLowAnimationStateName = "Landing - Low Height";
        public static readonly string landingMidAnimationStateName = "Landing - Mid Height";
        public static readonly string throwAxeAnimationStateName = "Throw Axe";
        #endregion

        #region Characters Animator Parameter Names
        public static readonly string speedParam = "speed";
        public static readonly string isDodgingParam = "isDodging";
        public static readonly string dodgeCounterParam = "dodgeCounter";
        public static readonly string isGroundedParam = "isGrounded";
        public static readonly string JUMP_STARTED_PARAM = "jumpStarted";
        public static readonly string FALLING_HEIGHT_PARAM = "fallingHeight";
        public static readonly string equipUnequipParam = "equipUnequipAxe";
        public static readonly string isAxeInHandParam = "isAxeInHand";
        public static readonly string isAimingParam = "isAiming";
        public static readonly string isThrowingAxeParam = "isThrowingAxe";
        public static readonly string inCombatParam = "inCombat";
        public static readonly string isRecallingAxeParam = "isRecallingAxe";
        public static readonly string catchAxeParam = "catchAxe";
        public static readonly string isBlockingParam = "isBlocking";
        public static readonly string IS_TARGETING_PARAM = "isTargeting";
        public static readonly string isAttackingParam = "isAttacking";
        public static readonly string canAttackBeInterruptedByMovementParam = "canAttackBeInterruptedByMovement";
        public static readonly string lightAttackParam = "lightAttack";
        public static readonly string heavyAttackParam = "heavyAttack";
        public static readonly string isCastingSpecialAttackParam = "isCastingSpecialAttack";
        public static readonly string skillIndexParam = "skillIndex";
        public static readonly string specialAttackEndParam = "specialAttackEnd";
        public static readonly string lightComboCounterParam = "lightComboCounter";
        public static readonly string heavyComboCounterParam = "heavyComboCounter";
        public static readonly string isRotatingParam = "isRotating";
        public static readonly string xRotationAngleParam = "xRotationAngle";
        public static readonly string movementXParam = "movementX";
        public static readonly string movementYParam = "movementY";
        public static readonly string drinkPotionParam = "drinkPotion";
        public static readonly string isUsingConsumableParam = "isUsingConsumable";
        public static readonly string hitLightlyParam = "hitLightly";
        public static readonly string hitHeavyParam = "hitHeavy";
        public static readonly string hitDirectionXParam = "hitDirectionX";
        public static readonly string hitDirectionYParam = "hitDirectionY";
        public static readonly string isDeadParam = "isDead";
        public static readonly string spawnParam = "spawn";
        public static readonly string comboChooserParam = "comboChooser";
        public static readonly string continueToAttackParam = "continueToAttack";
        #endregion

        #region Axe Animator Parameter Names
        public static readonly string rotateDuringThunderstormStartParam = "rotateDuringThunderstormStart";
        public static readonly string rotateDuringThunderstormEndParam = "rotateDuringThunderstormEnd";
        #endregion

        #region VFX Effects Names
        public static readonly string HEALTH_POTION_VFX_NAME = "VFX Health Potion";
        public static readonly string MANA_POTION_VFX_NAME = "VFX Mana Potion";
        public static readonly string DISINTEGRATION_VFX_NAME = "VFX Disintegration Particles";
        public static readonly string ELECTRICITY_BURST_VFX_NAME = "VFX Electricity Burst";
        public static readonly string AZAZEL_STRENGTH_VFX_NAME = "VFX Azazel Strength";
        public static readonly string LEVEL_UP_VFX_NAME = "VFX Level Up";
        #endregion

        #region Shader Graph Names
        public static readonly string DISSOLVE_SHADER_GRAPH_NAME = "Dissolve Shader Graph";
        #endregion

        #region NavMesh Area Names
        public static readonly string NAVMESH_WALKABLE_AREA_NAME = "Walkable";
        public static readonly string NAVMESH_NOT_WALKABLE_AREA_NAME = "Not Walkable";
        #endregion

        
        public static float FloatInterpolation(float currentValue, float targetValue, float maxDelta)
        {
            float newValue;

            if (currentValue + maxDelta < targetValue)
            {
                newValue = currentValue + maxDelta;
            }
            else if (currentValue - maxDelta > targetValue)
            {
                newValue = currentValue - maxDelta;
            }
            else
            {
                newValue = targetValue;
            }

            return newValue;
        }

        /**
         * Reset local position and rotation of the given transform
         */
        public static void ResetTransformLocalPositionAndRotation(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public static RaycastHit[] LookForObstacles(Vector3 startPosition, Vector3 movementDirection, float lookForObstacleDistance, LayerMask wallLayerMask)
        {
            // Find RaycastHits with non trigger colliders
            return Physics.RaycastAll(startPosition, movementDirection.normalized, lookForObstacleDistance, wallLayerMask, QueryTriggerInteraction.Ignore);
        }

        public static RaycastHit[] LookForObstacles(Vector3 startPosition, Vector3 movementDirection, float lookForObstacleDistance, LayerMask wallLayerMask, float maxSlopeAngle)
        {
            // Find RaycastHits with non trigger colliders that have an angle between normal and Vector3.up <= than the given one
            return LookForObstacles(startPosition, movementDirection, lookForObstacleDistance, wallLayerMask)
                        .Where(hit => Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
                        .ToArray();
        }

        /**
         * Compute transform movement direction relative to camera
         * taking into account whether the locomotion needs a 1D or 2D Blend Tree 
         */
        public static Vector3 ComputeMovementDirection(Transform transform, Vector2 input, bool isLocomotion1D)
        {
            Vector3 movementDirection = Vector3.zero;
            if (isLocomotion1D)
            {
                movementDirection.x = transform.forward.x;
                movementDirection.z = transform.forward.z;
            }
            else
            {
                movementDirection = ComputeInputDirectionRelativeToCamera(input);
            }

            return movementDirection.normalized;
        }

        public static Vector3 ComputeInputDirectionRelativeToCamera(Vector2 input)
        {
            Vector3 movementDirection = new Vector3(input.x, 0, input.y);
            movementDirection = Camera.main.transform.TransformDirection(movementDirection);
            movementDirection = Vector3.ProjectOnPlane(movementDirection, Vector3.up);
            
            return movementDirection.normalized;
        }

        /**
         * Rotate transform towards the given direction
         */
        public static void RotateTransformTowardsDirection(Transform transform, Vector3 direction, float rotationSpeed)
        {
            if (direction != Vector3.zero)
            {
                direction = Vector3.ProjectOnPlane(direction, Vector3.up);

                Quaternion desiredRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
            }
        }

        public static Vector3 GetCharacterHeightPosition(Transform characterTransform, float percentageHeight)
        {
            float characterHeight;
            if (characterTransform.TryGetComponent(out CharacterController characterController))
            {
                characterHeight = characterController.height;
            }
            else
            {
                if (characterTransform.TryGetComponent(out CapsuleCollider capsuleCollider))
                {
                    characterHeight = capsuleCollider.height;
                }
                else
                {
                    throw new MissingComponentException("CharacterController or CapsuleCollider component is missing!");
                }
            }

            // Take into account character scale along Y-Axis,
            // in order to compute correctly its height
            characterHeight *= characterTransform.localScale.y;
            
            return characterTransform.position + percentageHeight * characterHeight * Vector3.up;
        }

        /// <summary>
        /// Normalize the angle received as parameter between 0 and 359 degrees
        /// </summary>
        /// <param name="angle">The angle to normalize</param>
        /// <returns>The angle normalized</returns>
        public static float GetUnsignedEulerAngle(float angle)
        {
            int k = Mathf.FloorToInt(Mathf.Abs(angle) / 360);
            k = k <= 1 ? 1 : k;

            angle = angle >= 360 ? angle - k * 360 : angle;
            angle = angle < 0 ? angle + k * 360 : angle;

            return angle;
        }

        /// <summary>
        /// Normalize the angle received as parameter between -180 and 180 degrees
        /// </summary>
        /// <param name="angle">The angle to normalize</param>
        /// <returns>The angle normalized</returns>
        public static float GetSignedEulerAngle(float angle)
        {
            if (angle < 0 || angle >= 360)
            {
                angle = GetUnsignedEulerAngle(angle);
            }
            
            angle = angle > 180 ? angle - 360 : angle;
            angle = angle < -180 ? angle + 360 : angle;

            return angle;
        }
        
        public static bool IsTransformInsideScreen(Transform rootTransform)
        {
            // Create a list for the selected transforms (bones and transforms with a renderer)
            List<Transform> selectedTransforms = new List<Transform>();

            // Get root transform's animator (if present)
            Animator animator = rootTransform.GetComponent<Animator>();
            if (animator != null)
            {
                // Add all the transforms, which are animator's bones, to the list
                Enum.GetValues(typeof(HumanBodyBones))
                    .Cast<HumanBodyBones>()
                    .Where(bone => bone != HumanBodyBones.LastBone)
                    .Select(bone => animator.GetBoneTransform(bone))
                    .Where(boneTransform => boneTransform != null)
                    .ToList()
                    .ForEach(boneTransform => selectedTransforms.Add(boneTransform));
            }

            // Get all transform that also have a MeshRenderer or SkinnedMeshRenderer component,
            // since they are rendered, thus visible on screen
            Transform[] transformWithMeshArray = rootTransform.GetComponentsInChildren<Transform>()
                                                                .Where(transform => transform.GetComponent<MeshRenderer>() != null ||
                                                                                    transform.GetComponent<SkinnedMeshRenderer>() != null)
                                                                .ToArray();
            // Add the transforms to the list
            selectedTransforms.AddRange(transformWithMeshArray);

            // Check if any selected transform is inside the screen
            return selectedTransforms.Any(transform =>
            {
                Vector3 viewportPoint = Camera.main.WorldToViewportPoint(transform.position);

                return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                       viewportPoint.y >= 0 && viewportPoint.y <= 1;
            });
        }
        
        public static void AttachCanvas(RectTransform rectTransform, Transform target)
        {
            rectTransform.SetParent(target);
        }

        public static void DeattachCanvas(RectTransform rectTransform)
        {
            // In this case when player stops the targeting action, canvas target position is changed.
            // Since the target has to disappear while it's attached to the enemy,
            // it's necessary store its position and rotation
            Vector3 targetPosition = rectTransform.position;
            Quaternion targetRotation = rectTransform.rotation;

            rectTransform.SetParent(null);
            rectTransform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        /// <summary>
        /// Generates a random integer between 0 and the specified maximum value,
        /// with a probability to get the maximum value that is equal to the specified probability.
        /// </summary>
        /// <param name="maxValue">The maximum value for the generated integer.</param>
        /// <param name="maxValueProbability">The probability to get the maximum value.</param>
        /// <returns>A random integer between 0 and the specified maximum value.</returns>
        public static int GenerateRandomValue(int maxValue, float maxValueProbability)
        {
            // Generate a random value between 0 and 1.
            float u = Random.value;

            // If the random value is less than the maxValueProbability, return the maxValue.
            if (u >= 1 - maxValueProbability)
            {
                return maxValue;
            }
            // Otherwise, generate a number where smaller numbers are more likely.
            else
            {
                // Generate a random value between 0 and 1, raised to the power of 2 in order to have a PDF with a curve form.
                float pdf = Mathf.Pow(u, 2);

                // Scale this value to be between 0 and maxValue, then convert it to an integer.
                int value = Mathf.RoundToInt(pdf * maxValue);

                // The result has not to be greater than maxValue.
                return Mathf.Clamp(value, 0, maxValue - 1);
            }
        }

        // With this method is possible to asyncronously await for a certain amount of time,
        // taking into account if the game time is stopped or not.
        // (Example: if the game time is stopped, the wait will be paused until the time is resumed)
        public static async Task AsyncWaitTimeScaled(float durationInSeconds)
        {
            float startTime = Time.time;
            while (Time.time - startTime < durationInSeconds)
            {
                await Task.Yield();
            }
        }

        public static IEnumerator UpdateCharacterControllerLocalPositionCoroutine(CharacterController characterController, float duration)
        {
            Vector3 characterControllerCenter = characterController.center;
            
            float timeElapsed = 0;
            while (timeElapsed < duration)
            {
                characterController.center = characterControllerCenter;
                
                timeElapsed += Time.deltaTime;
                yield return null;
            }
        }

        public static AnimationCurve ScaleAnimationCurveYValues(AnimationCurve curve, float factor)
        {
            Keyframe[] newKeys = curve.keys;

            for (int i = 0; i < newKeys.Length; i++)
            {
                newKeys[i].value *= factor;
            }

            curve.keys = newKeys;

            return curve;
        }
        
        public static float LookUpForGroundYInWorldSpace(Vector3 position)
        {
            // Initialize ground and offset for raycast
            int groundLayer = LayerMask.GetMask(Utils.GROUND_LAYER_NAME);

            Vector3 start = position;
            RaycastHit[] hits = Physics.RaycastAll(start, Vector3.up, float.MaxValue, groundLayer, QueryTriggerInteraction.Ignore)
                                        .ToArray();
            if (hits.Length > 0)
            {
                Vector3 groundHit = hits.OrderBy(hit => Vector3.Distance(start, hit.point))
                                    .FirstOrDefault()
                                    .point;

                return groundHit.y;
            }

            return position.y;
        }

        public static GameObject FindGameObjectInTransformWithTag(Transform transform, string tag)
        {
            Transform transformFound = transform.GetComponentsInChildren<Transform>()
                                                .ToList()
                                                .Find(child => child.CompareTag(tag));

            return transformFound != null ? transformFound.gameObject : null;
        }

        public static bool IsShieldOnAttackLine(Transform player, Transform enemy)
        {
            int playerShieldLayerMask = LayerMask.GetMask(PLAYER_SHIELD_LAYER_NAME);
            Vector3 characterChestPosition = enemy.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest).position;
            Vector3 playerChestPosition = player.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest).position;

            return Physics.Linecast(characterChestPosition, playerChestPosition, playerShieldLayerMask, QueryTriggerInteraction.Collide);
        }

        public static Collider GetCharacterCollider(Transform character)
        {
            Collider characterCollider;
            if (character.TryGetComponent(out CharacterController characterController))
            {
                characterCollider = characterController;
            }
            else if (character.TryGetComponent(out CapsuleCollider capsuleCollider))
            {
                characterCollider = capsuleCollider;
            }
            else
            {
                throw new MissingComponentException("Character must have a CharacterController or a CapsuleCollider");
            }

            return characterCollider;
        }

        public static bool IsGridRoomCell(DungeonModel model, PropSocket socket)
        {
            if (model is GridDungeonModel)
            {
                GridDungeonModel gridModel = model as GridDungeonModel;
                Cell cell = gridModel.GetCell(socket.cellId);

                return cell != null && cell.CellType == CellType.Room;
            }

            return false;
        }

        public static bool IsGridCorridorCell(DungeonModel model, PropSocket socket)
        {
            if (model is GridDungeonModel)
            {
                GridDungeonModel gridModel = model as GridDungeonModel;
                Cell cell = gridModel.GetCell(socket.cellId);

                return cell != null && (cell.CellType == CellType.Corridor || cell.CellType == CellType.CorridorPadding);
            }

            return false;
        }
    }
}
