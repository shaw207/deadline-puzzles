using UnityEngine;

namespace DeadLine
{
    public static class MathUtils
    {
        public static float Map(float value, float min1, float max1, float min2, float max2)
        {
            // Map value from range [min1, max1] to range [min2, max2]
            // Mathf.InverseLerp expects (a, b, value)
            return Mathf.Lerp(min2, max2, Mathf.InverseLerp(min1, max1, value));
        }

        public static void Map(ref float value, float min1, float max1, float min2, float max2)
        {
            value = Map(value, min1, max1, min2, max2);
        }
        
        public static float Negative(float value){
            return value >= 0 ? value * -1 : value;
        }

    }

}
