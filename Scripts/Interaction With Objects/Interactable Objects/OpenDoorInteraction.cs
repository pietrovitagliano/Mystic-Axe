// Author: Pietro Vitagliano

using DG.Tweening;
using System;
using UnityEngine;

namespace MysticAxe
{
    public class OpenDoorInteraction : AbstractObjectInteraction
    {
        [Serializable]
        public class Door 
        {
            [SerializeField] private Transform doorToOpen;
            [SerializeField, Range(-180, 180)] private float openAngle;

            public Transform DoorToOpen { get => doorToOpen; }
            public float OpenAngle { get => openAngle; }
        }

        [Header("Open Door Settings")]
        [SerializeField] private Door[] doors;
        [SerializeField, Range(0.01f, 2)] private float openDelay = 0.3f;
        [SerializeField, Range(0.1f, 5)] private float openTime = 1.8f;

        protected override void Start()
        {
            base.Start();

            // Set the doors as not opened
            foreach (Door door in doors)
            {
                door.DoorToOpen.localEulerAngles = Vector3.zero;
            }
        }

        protected override void InitializeInteractability() { }

        // The door is interactable if the player has the dungeon key
        protected override void UpdateInteractability() 
        {
            IsInteractable = Player.GetComponent<PlayerConsumableHandler>().HasDungeonKey;
        }

        protected override void InteractWithObject()
        {
            // The dungeon key is used
            Player.GetComponent<PlayerConsumableHandler>().UseDungeonKey();

            // Until the door is opening, the interaction collider is disabled
            IsInteractable = false;

            foreach (Door door in doors)
            {
                // The rotation is around Y-Axis
                Vector3 eulearAnglesVector = door.OpenAngle * Vector3.up;
                door.DoorToOpen.DOLocalRotate(eulearAnglesVector, openTime)
                                    .SetDelay(openDelay)
                                    .OnStart(() =>
                                    {                                        
                                        // Play door opening sound
                                        GetComponentInChildren<AudioSource>().Play();
                                    })
                                    .OnComplete(() =>
                                    {
                                        // Remove the script from the gameObject, after the door is completely opened
                                        Destroy(this);
                                    });
            }
        }
    }
}