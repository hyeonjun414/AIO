using System;

namespace Game.Framework.UI
{
    public readonly struct UIKey : IEquatable<UIKey>
    {
        public UIKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("UI key cannot be null or empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(UIKey other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UIKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator UIKey(string value)
        {
            return new UIKey(value);
        }
    }
}
