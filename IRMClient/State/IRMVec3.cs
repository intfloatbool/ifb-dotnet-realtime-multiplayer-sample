using System;

namespace IRMClient.State
{
    public struct IRMVec3 : IEquatable<IRMVec3>
    {
        public float X;
        public float Y;
        public float Z;

        public IRMVec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(IRMVec3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object? obj)
        {
            return obj is IRMVec3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"( {X}, {Y}, {Z} )";
        }
    }
}