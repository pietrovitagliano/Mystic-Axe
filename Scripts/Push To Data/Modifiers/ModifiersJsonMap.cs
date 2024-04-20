// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MysticAxe
{
    [Serializable]
    public class ModifiersJsonMap
    {
        [SerializeField] private List<Modifier> modifiers;

        public Modifier GetModifierByID(string modifierID)
        {
            foreach (Modifier modifier in modifiers)
            {
                if (modifier.ModifierID == modifierID && modifier.IsValid())
                {
                    // It's necessary to create a new instance of the Modifier class,
                    // since the list of modifiers is composed by instances create by json deserialization,
                    // thus only the serialized variables are initialized.
                    // On the contrary, with the constructor is possible to initialize the other variables.
                    return new Modifier(modifier.ModifierID, modifier.FeatureName, modifier.Factor, modifier.FeatureCategory, modifier.Duration, modifier.Infinite, modifier.Type);
                }
            }

            throw new DataException(Utils.noModifierFoundWithIDError + modifierID);
        }

        public List<Modifier> GetMoreModifiersByIdList(List<string> idList)
        {
            List<Modifier> newModifiers = new List<Modifier>();

            foreach (string id in idList)
            {
                Modifier modifier = GetModifierByID(id);
                if (modifier != null)
                {
                    Debug.Log("Modifier found:" + modifier.ToString());
                    
                    newModifiers.Add(modifier);
                }
            }
            
            if (modifiers.Count >= 0)
            {
                return newModifiers;
            }
            else
            {
                throw new DataException(Utils.noModifierFoundWithIDError + idList);
            }
        }
    }
}