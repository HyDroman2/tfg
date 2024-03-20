using UnityEngine;

namespace VectorMethods
{
    public static class Vect2Methods
    {

        public static float distance(this Vector2 p, Vector2 p2)
        {
            return (p - p2).sqrMagnitude;
        }

        public static float distance(this Vector2Int p, Vector2Int p2)
        {
            return (p - p2).sqrMagnitude;
        }

        public static Vector2Int toVect2Int(this Vector3 p) {
            return new Vector2Int((int)p.x, (int)p.y);
        }
    }
}

