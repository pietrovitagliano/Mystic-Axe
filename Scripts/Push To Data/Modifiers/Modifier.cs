// Author: Pietro Vitagliano

using UnityEngine;
using System;

namespace MysticAxe
{
    [Serializable]
    public class Modifier
    {
        [SerializeField] private string modifier_id;
        [SerializeField] private string feature_name;
        [SerializeField] private float factor;
        [SerializeField] private string feature_category;
        [SerializeField] private float duration;
        [SerializeField] private bool infinite;
        [SerializeField] private string type;
        
        private Func<bool> condition = null;
        private readonly float baseDuration;

        
        public string ModifierID { get => modifier_id; }
        public string FeatureName { get => feature_name; }
        public float Factor { get => factor; set => factor = value; }
        public string FeatureCategory { get => feature_category; }
        public float Duration { get => duration; }
        public bool Infinite { get => infinite; }
        public string Type { get => type; }

        public ModifierType EnumType { get => (ModifierType)Enum.Parse(typeof(ModifierType), type.Substring(0, 1).ToUpper() + type.Substring(1).ToLower()); }
        public Func<bool> Condition { get => condition; set => condition = value; }


        public enum ModifierType
        {
            Additive,
            Multiplicative
        }

        public Modifier(string modifier_id, string feature_name, float factor, string feature_category, float duration, bool infinite, string type)
        {
            this.modifier_id = modifier_id;
            this.feature_name = feature_name;
            this.factor = factor;
            this.feature_category = feature_category;
            this.duration = duration;
            this.infinite = infinite;
            this.type = type;

            baseDuration = duration;
        }

        public bool IsValid()
        {
            if (infinite)
            {
                return true;
            }
            else
            {
                return duration > 0;
            }
        }

        public void ResetDuration()
        {
            if (!infinite)
            {
                duration = baseDuration;
            }
        }

        private void Apply(Feature feature)
        {
            float featureCurrentValue = (float)feature.CurrentValue;
            
            switch (EnumType)
            {
                case ModifierType.Additive:
                    feature.CurrentValue = featureCurrentValue + factor;

                    break;

                case ModifierType.Multiplicative:
                    feature.CurrentValue = featureCurrentValue * factor;

                    break;

                default:
                    throw new NotImplementedException("Modifier type not implemented.");
            }
        }

        public void Apply(Feature[] features)
        {
            // Check if the modifier is valid and if the condition is met
            if (IsValid() && (condition == null || condition()))
            {
                Debug.Log("ApplyToFeatures Modifier is valid: " + ToString());
                Debug.Log("ApplyToFeatures features.Length: " + features.Length);
                Debug.Log("ApplyToFeatures modifierFeature_name: " + feature_name);
                foreach (Feature feature in features)
                {
                    Debug.Log("ApplyToFeatures featureName: " + feature.Name);

                    // Check if the modifier can be applied to the given feature
                    if (feature.CorrespondToModifier(this))
                    {
                        Debug.Log("ApplyToFeatures Modifier will be applied to feature: " + feature.ToString());
                        Apply(feature);
                    }
                }
            }

            if (!infinite)
            {
                duration -= Time.deltaTime;
            }
        }

        public override string ToString()
        {
            return "Modifier ID: " + modifier_id +
                    "\nFeature Name: " + feature_name +
                    "\nFactor: " + factor +
                    "\nDuration: " + duration +
                    "\nInfinite: " + infinite +
                    "\nType: " + type +
                    "\n";
        }
    }
}