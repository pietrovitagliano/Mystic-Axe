// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    // This class handles the skills that a particular gameobject (a character, a weapon, etc) can cast.
    // It has to be attached to these gameobjects.
    public abstract class SkillSetHandler : MonoBehaviour
    {
        [Serializable]
        private struct SkillBinder
        {
            [SerializeField, Min(1)] private int key;
            [SerializeField] private ParticleSystem skill;

            public int Key { get => key; }
            public ParticleSystem Skill { get => skill; }

            public SkillBinder(int key, ParticleSystem skill)
            {
                this.key = key;
                this.skill = skill;
            }
        }


        protected CharacterStatusHandler characterStatusHandler;
        
        [SerializeField] private SkillBinder[] skillBinderArray;

        private Dictionary<int, SkillHandler> skillHandlerDict = new Dictionary<int, SkillHandler>();

        public Dictionary<int, SkillHandler> SkillHandlerDict { get => skillHandlerDict; }

        protected virtual void Start()
        {
            characterStatusHandler = GetComponentsInParent<CharacterStatusHandler>().FirstOrDefault();
            InitializeSkillHandlerDict();
        }

        private void InitializeSkillHandlerDict()
        {
            foreach (SkillBinder skillBinder in skillBinderArray)
            {
                SkillHandler skillHandler = skillBinder.Skill.GetComponent<SkillHandler>();
                if (skillHandler != null)
                {
                    skillHandlerDict.Add(skillBinder.Key, skillHandler);
                }
                else
                {
                    throw new System.Exception("SkillHandler not found!");
                }
            }
        }

        public abstract void CastSkill(int index);

        public bool IsIndexValid(int index)
        {
            return skillHandlerDict.ContainsKey(index);
        }

        public bool IsManaEnoughForSkill(int index)
        {
            SkillHandler skillHandler = skillHandlerDict[index];

            if (skillHandler != null)
            {
                int manaCost = skillHandler.ComputeManaCost();
                
                return characterStatusHandler.HasEnoughMana(manaCost);
            }

            return false;
        }

        public void ConsumeManaForSkill(int index)
        {
            SkillHandler skillHandler = skillHandlerDict[index];

            if (skillHandler != null)
            {
                int manaCost = skillHandler.ComputeManaCost();
                characterStatusHandler.ConsumeMana(manaCost);
            }
        }
    }
}