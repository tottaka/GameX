using System;
using System.Collections.Generic;
using System.Text;

namespace GameX
{
    public abstract class Mathf
    {
        public static float Clamp(float value, float min, float max)
        {
            return value > max ? max : value < min ? min : value;
        }
    }
}
