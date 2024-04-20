// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace MysticAxe
{
    public class CharacterDeathState : BaseState
    {
        private Transform character;
        private CharacterAnimatorHandler characterAnimatorHandler;

        public override void InitializeState(CharacterStateManager characterStateManager)
        {
            character = characterStateManager.transform;
            characterAnimatorHandler = character.GetComponent<CharacterAnimatorHandler>();
        }

        public override void EnterState()
        {
            // Stop all the particle systems of the character
            foreach (ParticleSystem particle in character.GetComponentsInChildren<ParticleSystem>().Where(particle => particle.gameObject.layer == character.gameObject.layer))
            {
                particle.Stop();
            }

            // Stop all the visual effects of the character
            foreach (VisualEffect vfx in character.GetComponentsInChildren<VisualEffect>().Where(vfx => vfx.gameObject.layer == character.gameObject.layer))
            {
                vfx.Stop();
            }

            // Trigger the death animation
            characterAnimatorHandler.Animator.SetBool(characterAnimatorHandler.IsDeadHash, true);
        }

        public override void UpdateState() { }
    }
}