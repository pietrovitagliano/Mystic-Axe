// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public abstract class Component : MonoBehaviour, IEqualityComparer<Component>, ICloneable
    {
        private string id;
        private List<Feature> features;
        protected List<Modifier> modifiers;
        protected HashSet<Component> childrenComponents;
        protected string category;

        public List<Feature> Features { get => features; }
        public List<Modifier> Modifiers { get => modifiers; }
        public HashSet<Component> ChildrenComponents { get => childrenComponents; }
        public string Category { get => category; }

        public Modifier GetModifierByID(string modifierID)
        {
            return modifiers.Find(modifier => modifier.ModifierID == modifierID);
        }

        public bool HasModifier(Modifier modifier)
        {
            return modifiers.Contains(modifier);
        }

        public void AddModifier(Modifier newModifier)
        {
            Modifier foundModifier = GetModifierByID(newModifier.ModifierID);
            if (foundModifier != null)
            {
                if (!foundModifier.Infinite)
                {
                    foundModifier.ResetDuration();
                }
            }
            else
            {
                modifiers.Add(newModifier);
            }
        }
        
        public void RemoveModifier(Modifier modifier)
        {
            modifiers.Remove(modifier);
        }

        public void AddMoreModifiers(List<Modifier> list)
        {
            foreach (Modifier modifier in list)
            {
                AddModifier(modifier);
            }
        }
                
        private void HandleInvalidModifiers()
        {
            if (modifiers.Count > 0)
            {
                modifiers.RemoveAll(modifier => !modifier.IsValid());
            }
        }
        
        public Component GetComponentByCategory(string category)
        {
            foreach (Component component in childrenComponents)
            {
                if (component.category == category)
                {
                    return component;
                }
            }
            
            return null;
        }
        
        protected virtual void Awake()
        {
            // Generate a unique hex id
            id = Guid.NewGuid().ToString("N");

            // Features and modifiers initialization
            features = InitializeFeatures();
            modifiers = new List<Modifier>();
        }

        /// <summary>
        /// Find all the direct components of the transform passed as parameter.
        /// If a child has no components, search for components in its children.
        /// Otherwise, when a child has components, add them to the list and stop
        /// the search for components in its children.
        /// </summary>
        /// <param name="parent">The transform for which to find all components</param>
        /// <returns>The set of direct components found</returns>
        private HashSet<Component> GetNewComponentList(Transform parent)
        {
            HashSet<Component> directChildrenComponents = new HashSet<Component>(this);
            foreach (Transform child in parent)
            {
                if (child.TryGetComponent(out Component childComponent))
                {
                    directChildrenComponents.Add(childComponent);
                }
                else
                {
                    if (child.childCount > 0)
                    {
                        foreach (Component component in GetNewComponentList(child))
                        {
                            directChildrenComponents.Add(component);
                        }
                    }
                }
            }
            
            return directChildrenComponents;
        }

        // Start is used to invoke Push To Data functionalities continuously (before the first frame)
        protected virtual void Start()
        {
            UpdateComponentLoop();
        }

        // Update is used to invoke Push To Data functionalities continuously (every frame)
        protected virtual void Update()
        {
            UpdateComponentLoop();
        }

        private void UpdateComponentLoop()
        {
            childrenComponents = GetNewComponentList(transform);
            ReinitializeFeaturesWithComponents();
            HandleInvalidModifiers();
            ApplyModifiers();
            UpdateFeaturesAfterModifiers();
        }
        
        public List<Feature> GetFeaturesWithSimulatedComponents(HashSet<Component> simulatedComponents)
        {
            // Clone this component
            Component simulatedComponent = (Component)Clone();

            // Add the simulated components to the cloned one
            foreach (Component component in simulatedComponents)
            {
                simulatedComponent.childrenComponents.Add(component);
            }

            // Compute the features of the cloned component
            simulatedComponent.ReinitializeFeaturesWithComponents();
            simulatedComponent.HandleInvalidModifiers();
            simulatedComponent.ApplyModifiers();
            simulatedComponent.UpdateFeaturesAfterModifiers();

            return simulatedComponent.features;
        }

        public void ReinitializeFeaturesWithComponents()
        {
            foreach (Feature feature in features)
            {
                feature.UpdateFeature(childrenComponents.ToArray());
            }
        }

        public void ApplyModifiers()
        {
            foreach (Modifier modifier in modifiers)
            {
                modifier.Apply(features.ToArray());
            }
        }

        protected abstract List<Feature> InitializeFeatures();

        protected abstract void UpdateFeaturesAfterModifiers();

        // This method is necessary to implement IEqualityComparer<Component>
        public bool Equals(Component componentA, Component componentB)
        {
            return componentA.id == componentB.id;
        }

        // This method is necessary to implement IEqualityComparer<Component>
        public int GetHashCode(Component component)
        {
            return component.id.GetHashCode();
        }

        public Feature GetFeature(string name, string category = "")
        {
            foreach (Feature feature in features)
            {
                if (feature.Name == name && feature.Category == category)
                {
                    return feature;
                }
            }

            return null;
        }

        public void SetFeatureCurrentValue(string name, float currentValue, string category = "")
        {
            foreach (Feature feature in features)
            {
                if (feature.Name == name && feature.Category == category)
                {
                    feature.CurrentValue = currentValue;
                }
            }
        }

        public object Clone()
        {
            Component component = MemberwiseClone() as Component;
            component.features = new List<Feature>(features);
            component.modifiers = new List<Modifier>(modifiers);
            component.childrenComponents = new HashSet<Component>(childrenComponents);

            return component;
        }
    }
}