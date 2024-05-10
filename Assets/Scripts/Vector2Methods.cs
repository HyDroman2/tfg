using System;
using UnityEngine;

namespace VectorMethods
{
    public static class Vect2Methods
    {

        public static float distance(this Vector2Int p, Vector2Int p2)
        {
            return (p2-p).magnitude;
        }

        public static int manhattanDistance(this Vector2Int p, Vector2Int p2) {
            return Math.Abs(p2.x - p.x) + Math.Abs(p2.y - p.y); 
        
        }
        public static Vector2Int toVect2Int(this Vector3 p) {
            return new Vector2Int((int)p.x, (int)p.y);
        }
    }
}

