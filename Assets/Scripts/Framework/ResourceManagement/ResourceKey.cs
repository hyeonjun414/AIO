using System;

namespace Game.Framework.ResourceManagement
{
    public readonly struct ResourceKey : IEquatable<ResourceKey>
    {
        public ResourceKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Resource key cannot be null or empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(ResourceKey other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator ResourceKey(string value)
        {
            return new ResourceKey(value);
        }
    }
}
