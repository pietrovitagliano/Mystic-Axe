// Author: Pietro Vitagliano

using System;
using UnityEngine;

namespace MysticAxe
{
    [Serializable]
    public class Feature
    {
        [SerializeField] private string name;
        [SerializeField] private float base_value;
        [SerializeField] private string category;
        [SerializeField] private string type;
        private float currentValue;

        public string Name { get => name; set => name = value; }
        public float BaseValue { get => base_value; set => base_value = value; }
        public string Category { get => category; set => category = value; }
        public string Type { get => type; set => category = value; }
        public float CurrentValue { get => currentValue; set => currentValue = value; }

        public FeatureType EnumType { get => (FeatureType)Enum.Parse(typeof(FeatureType), type.Substring(0, 1).ToUpper() + type.Substring(1).ToLower()); }

        
        public Feature(float baseValue, string name, string category, string type)
        {
            this.base_value = baseValue;
            this.currentValue = baseValue;
            this.name = name;
            this.category = category;
            this.type = type;
        }

        public enum FeatureType
        {
            Additive,
            Multiplicative
        }

        public float GetInitializedFactor()
        {
            float initialFactor;
            switch (EnumType)
            {
                case FeatureType.Additive:
                    initialFactor = 0;

                    break;

                case FeatureType.Multiplicative:
                    initialFactor = 1;

                    break;

                default:
                    throw new NotImplementedException("Feature type not implemented.");
            }

            return initialFactor;
        }

        /**
         * Apply factor to base value and save the result in current value
         */
        public void ApplyFactor(float factor)
        {
            switch (EnumType)
            {
                case FeatureType.Additive:
                    currentValue = base_value + factor;

                    break;

                case FeatureType.Multiplicative:
                    currentValue = base_value * factor;

                    break;

                default:
                    throw new NotImplementedException("Feature type not implemented.");
            }
        }

        /**
         * Returns the factor applied to the current value, without saving it
         */
        public float GetUpdatedFactor(float factor)
        {
            float updatedFactor;
            switch (EnumType)
            {
                case FeatureType.Additive:
                    updatedFactor = currentValue + factor;

                    break;

                case FeatureType.Multiplicative:
                    updatedFactor = currentValue * factor;

                    break;

                default:
                    throw new NotImplementedException("Feature type not implemented.");
            }

            return updatedFactor;
        }

        public bool CorrespondToModifier(Modifier modifier)
        {
            string currentFeatureName = name.ToString().ToLower();
            string modifierFeatureName = modifier.FeatureName.ToString().ToLower();

            string currentCategory = category.ToString().ToLower();
            string modifierCategory = modifier.FeatureCategory.ToString().ToLower();

            return currentFeatureName == modifierFeatureName && AreCategoriesCompatible(currentCategory, modifierCategory);
        }

        public void UpdateFeature(Component[] components)
        {
            float factor = GetInitializedFactor();

            string currentFeatureName = name.ToString().ToLower();
            string currentFeatureCategory = category.ToString().ToLower();

            foreach (Component component in components)
            {
                foreach (Feature componentFeature in component.Features)
                {
                    string componentFeatureName = componentFeature.Name.ToString().ToLower();
                    string componentFeatureCategory = componentFeature.Category.ToString().ToLower();

                    if (currentFeatureName == componentFeatureName && AreCategoriesCompatible(currentFeatureCategory, componentFeatureCategory))
                    {
                        // The factor is updated with feature's current value
                        factor = componentFeature.GetUpdatedFactor(factor);
                    }
                }
            }
            
            ApplyFactor(factor);
        }

        private bool AreCategoriesCompatible(string featureCategoryA, string featureCategoryB)
        {
            return featureCategoryA == featureCategoryB || (featureCategoryA == "" || featureCategoryB == "");
        }
    }
}