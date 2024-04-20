// Author: Pietro Vitagliano

using DG.Tweening;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticAxe
{
    public class DungeonDoorInteraction : AbstractObjectInteraction
    {
        [SerializeField] private BoxCollider portalPhysicalCollider;

        [Header("Walk Animation Settings")]
        [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(3.7f, 1), new Keyframe(4, 0));
        [SerializeField, Range(0.1f, 2)] private float firstPartWalkDuration = 0.5f;
        [SerializeField, Range(1, 10)] private float walkRotationSpeed = 4f;
        [SerializeField, Range(1, 10)] private float resetCameraAngleThreshold = 4.5f;
        [SerializeField, Min(10)] private float endPointOffset = 100f;

        [Header("Change Scene Settings")]
        [SerializeField, Range(0.1f, 10)] private float changeSceneDelay = 1f;
        [SerializeField, Range(0.1f, 10)] private float fadeInDuration = 0.6f;
        
        private PlayerInput playerInput;
        private CinemachineHandler cinamachineHandler;
        private PlayerAnimatorHandler playerAnimatorHandler;

        private OpenDoorInteraction openDoorInteraction;

        private Coroutine walkCoroutine = null;
        
        protected override void Start()
        {
            base.Start();

            playerInput = InputMapHandler.Instance.PlayerInput;
            cinamachineHandler = Camera.main.GetComponent<CinemachineHandler>();
            playerAnimatorHandler = Player.GetComponent<PlayerAnimatorHandler>();

            // This collider is a physical one
            portalPhysicalCollider.isTrigger = false;

            openDoorInteraction = GetComponent<OpenDoorInteraction>();
        }

        protected override void InitializeInteractability()
        {
            // The portal is interactable when the door (if present) is open (the interaction with openDoorInteraction has already happened)
            // and the physical collider has been disabled (it happens in the InteractWithObject() method)
            IsInteractable = openDoorInteraction == null && portalPhysicalCollider.enabled;
        }
        
        protected override void UpdateInteractability()
        {
            InitializeInteractability();
        }

        protected override void InteractWithObject()
        {
            IsInteractable = false;

            // The value of the keys in speedCurve go from 0 to 1 (in percentage),
            // thus it's necessary to multiply them by walk speed
            float walkSpeed = Player.GetComponent<PlayerLocomotionHandler>().WalkSpeed;
            speedCurve = Utils.ScaleAnimationCurveYValues(speedCurve, walkSpeed);

            // Compute walk direction
            // This method uses a raycast, which requires the physical collider to be enabled,
            // in order to compute correctly the walk direction: that's why the physical collider
            // has to be disabled after this computation
            Vector3 walkDirection = GetWalkDirection();

            // After that the walk direction has been computed, the physical collider can be disable,
            // in order to let player walk through it
            portalPhysicalCollider.enabled = false;

            // Play walk animation
            PlayWalkAnimation(walkDirection);
        }
        
        private void PlayWalkAnimation(Vector3 walkDirection)
        {
            // During the animation the player is invulnerable
            PlayerStatusHandler.IsInvulnerable = true;

            Vector3 entrancePosition = portalPhysicalCollider.transform.position;
            Vector3 playerInitialPosition = Player.position;
            
            Player.DOMove(entrancePosition, firstPartWalkDuration)
                    .OnStart(() =>
                    {
                        PlayerStatusHandler.IsPlayingWalkAnimation = true;
                        cinamachineHandler.EnableCameraInput(enabled: false);
                        playerInput.enabled = false;

                        // Set player speed during the first part of the walk animation
                        PlayerStatusHandler.Speed = speedCurve.Evaluate(0);

                        // Update playerSpeed in animator
                        playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.SpeedHash, PlayerStatusHandler.Speed);

                        // DOTween moves the player, not the character controller.
                        // This is needed to update the character controller's local position.
                        StartCoroutine(Utils.UpdateCharacterControllerLocalPositionCoroutine(PlayerCharacterController, firstPartWalkDuration));

                        // Reset camera
                        cinamachineHandler.ResetCamera(resetDuration: 0.35f, enableInputAfterReset: false);
                    })
                    .OnUpdate(() =>
                    {
                        // Rotate the player
                        Vector3 rotationDirection = (entrancePosition - Player.position).normalized;
                        Utils.RotateTransformTowardsDirection(Player, rotationDirection, rotationSpeed: walkRotationSpeed);
                    })
                    .OnComplete(() =>
                    {
                        walkCoroutine = StartCoroutine(HandleWalkAfterPortalCoroutine(walkDirection));
                        StartCoroutine(GoToDungeonNextLevelCoroutine());
                    });
        }

        private IEnumerator HandleWalkAfterPortalCoroutine(Vector3 forwardDirection)
        {
            Vector3 entrancePosition = portalPhysicalCollider.transform.position;

            Vector3 endPoint = entrancePosition + endPointOffset * forwardDirection;
            float curveDuration = speedCurve.keys[speedCurve.keys.Length - 1].time;
            
            float timeElapsed = 0;
            while (timeElapsed < curveDuration)
            {
                // Compute player speed
                PlayerStatusHandler.Speed = speedCurve.Evaluate(timeElapsed);

                // Update playerSpeed in animator
                playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.SpeedHash, PlayerStatusHandler.Speed);

                // Compute the movement direction
                Vector3 walkDirection = (endPoint - Player.position).normalized;

                // Move the player
                PlayerCharacterController.SimpleMove(PlayerStatusHandler.Speed * walkDirection);

                // Rotate the player
                Utils.RotateTransformTowardsDirection(Player, walkDirection, rotationSpeed: walkRotationSpeed);

                // When player rotation looks at walkDirection, start resetting the camera
                if (Vector3.Angle(Player.forward, walkDirection) < resetCameraAngleThreshold)
                {
                    // Reset camera
                    cinamachineHandler.ResetCamera(resetDuration: 0.35f, enableInputAfterReset: false);
                }

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            // After the animation the player returns vulnerable
            PlayerStatusHandler.IsInvulnerable = false;
            PlayerStatusHandler.IsPlayingWalkAnimation = false;
            playerInput.enabled = true;
            cinamachineHandler.EnableCameraInput(enabled: true);

            walkCoroutine = null;
        }

        private IEnumerator GoToDungeonNextLevelCoroutine()
        {
            yield return new WaitForSeconds(changeSceneDelay);

            DungeonGenerationHandler dungeonGenerationHandler = FindObjectOfType<DungeonGenerationHandler>();

            if (dungeonGenerationHandler != null)
            {
                dungeonGenerationHandler.GoToNextLevel();
            }
            else
            {
                SceneLoader.Instance.AsyncChangeScene(Utils.DUNGEON_SCENE, fadeInDuration: fadeInDuration);
            }

            if (walkCoroutine != null)
            {
                StopCoroutine(walkCoroutine);
                walkCoroutine = null;
                
                yield return new WaitForSeconds(fadeInDuration);

                PlayerStatusHandler.IsInvulnerable = false;
                PlayerStatusHandler.IsPlayingWalkAnimation = false;
                playerInput.enabled = true;
                cinamachineHandler.EnableCameraInput(enabled: true);
            }
        }
        
        private Vector3 GetWalkDirection()
        {
            Vector3 start = Utils.GetCharacterHeightPosition(Player, Utils.UPPER_CHEST_HEIGHT);
            Vector3 portalPosition = portalPhysicalCollider.transform.position;
            Vector3 playerToPortalDirection = (portalPosition - Player.position).normalized;
            float playerToPortalDistance = Vector3.Distance(portalPosition, Player.position);
            LayerMask portalLayerMask = LayerMask.GetMask(LayerMask.LayerToName(portalPhysicalCollider.gameObject.layer));

            RaycastHit[] hits = Physics.RaycastAll(start, playerToPortalDirection, playerToPortalDistance, portalLayerMask, QueryTriggerInteraction.Ignore)
                                    .Where(hit => hit.collider == portalPhysicalCollider)
                                    .ToArray();

            if (hits.Length > 0)
            {
                // The normal points towards the direction where the player comes from,
                // thus its opposite is exactly the walk direction
                return -hits[0].normal;
            }
            else
            {
                return Vector3.ProjectOnPlane(playerToPortalDirection, Vector3.up);
            }
        }

        protected override void OnTriggerStay(Collider other)
        {
            // The OnTriggerEnter will work only when the openDoorInteraction become null,
            // that is when the door has been opened
            if (openDoorInteraction == null)
            {
                base.OnTriggerStay(other);
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            // The OnTriggerExit will work only when the openDoorInteraction become null,
            // that is when the door has been opened
            if (openDoorInteraction == null)
            {
                base.OnTriggerExit(other);
            }
        }
    }
}