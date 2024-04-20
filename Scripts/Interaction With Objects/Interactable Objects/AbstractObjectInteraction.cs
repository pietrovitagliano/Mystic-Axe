// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(Collider))]
    public abstract class AbstractObjectInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField, Range(1, 360)] private float maxInteractionAngle = 45;
        [SerializeField] private Collider interactionCollider;
        private bool isInteractable = false;

        private InteractionIconAppearenceHandler interactionIconAppearenceHandler;
        
        private Transform player;
        private PlayerStatusHandler playerStatusHandler;
        private CharacterController playerCharacterController;

        protected bool IsInteractable { get => isInteractable; set => isInteractable = value; }
        protected Transform Player { get => player; }
        protected PlayerStatusHandler PlayerStatusHandler { get => playerStatusHandler; }
        public CharacterController PlayerCharacterController { get => playerCharacterController; }

        protected virtual void Start()
        {
            interactionCollider.isTrigger = true;

            player = FindObjectsOfType<Transform>(includeInactive: true).ToList().Find(transform => transform.CompareTag(Utils.PLAYER_TAG));
            playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
            playerCharacterController = player.GetComponent<CharacterController>();
            interactionIconAppearenceHandler = player.GetComponentInChildren<InteractionIconAppearenceHandler>();
            
            InitializeInteractability();
        }

        private void Update()
        {
            UpdateInteractability();

            // If icon is visible and the interaction button is pressed
            if (interactionIconAppearenceHandler.IsVisible && InputHandler.Instance.InteractionButtonPressed)
            {
                if (interactionCollider.bounds.Intersects(playerCharacterController.bounds))
                {
                    InputHandler.Instance.InteractionButtonPressed = false;
                    InteractWithObject();
                }
            }
        }
        
        /// <summary>
        /// This method is called in the Start() of each child class and
        /// it's used to establish if, at the start, the gameObject is interactable or not
        /// </summary>
        protected abstract void InitializeInteractability();

        /// <summary>
        /// This method is called in the Update() of each child class and
        /// it's used to establish, if necessary, how the gameObject interactability changes, at each frame.
        /// </summary>
        protected abstract void UpdateInteractability();

        /// <summary>
        /// This method is used to define establish what happens when the player interacts with the gameObject
        /// </summary>
        protected abstract void InteractWithObject();

        protected virtual void OnTriggerStay(Collider other)
        {
            if (other == playerCharacterController && other.bounds.Intersects(interactionCollider.bounds))
            {
                Vector3 playerToObjectDirection = (transform.position - player.position).normalized;
                float angleBetweenPlayerAndObject = Vector3.Angle(player.forward, playerToObjectDirection);

                bool isInteractionPossible = playerStatusHandler.CanInteract && angleBetweenPlayerAndObject <= maxInteractionAngle;
                if (isInteractionPossible)
                {
                    // This is needed to make the icon disappear if the interaction has to happen just once
                    interactionIconAppearenceHandler.IsVisible = isInteractable;
                }
                else
                {
                    interactionIconAppearenceHandler.IsVisible = false;
                }
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (other == playerCharacterController)
            {
                interactionIconAppearenceHandler.IsVisible = false;
            }
        }
    }
}