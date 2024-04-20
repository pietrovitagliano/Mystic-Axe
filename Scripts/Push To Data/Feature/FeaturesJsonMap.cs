// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MysticAxe
{
    [Serializable]
    public class FeaturesJsonMap
    {
        [SerializeField] private List<Feature> features;

        public List<Feature> Features { get => features; }
    }
}