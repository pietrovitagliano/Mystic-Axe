// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    // This class handles the single skill that a particular gameobject (a character, a weapon, etc) can cast.
    // It has to be attached to the gameobject (generally a ParticleSystem) that represents the skill itself.
    public abstract class SkillHandler : MonoBehaviour
    {
        [Header("Skill Settings")]
        [SerializeField, Range(1, 3)] private int skillLevel = 3;
        [SerializeField, Range(1, 200)] private int manaCost = 25;

        private GameObject character;
        private GameObject weapon;

        public int SkillLevel { get => skillLevel; }
        public int ManaCost { get => manaCost; }
        protected GameObject Character { get => character; }
        public GameObject Weapon { get => weapon; set => weapon = value; }

        private void Start()
        {
            WeaponStatusHandler weaponStatusHandler = weapon.GetComponent<WeaponStatusHandler>();
            character = weaponStatusHandler.Character.gameObject;

            // The skill always has the same rotation of the caster
            if (character.GetComponent<CharacterStatusHandler>() != null)
            {
                transform.rotation = character.transform.rotation;
            }

            InitializeSkill();
            CastSkill();
        }
        
        protected abstract void InitializeSkill();
        
        public abstract void CastSkill();

        public int ComputeManaCost()
        {
            return manaCost * skillLevel;
        }
    }
}