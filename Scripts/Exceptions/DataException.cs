// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MysticAxe
{
    public class DataException : Exception
    {
        public DataException(string message) : base(message) { }
    }
}
