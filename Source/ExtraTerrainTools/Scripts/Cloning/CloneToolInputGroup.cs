using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrainTools.Cloning
{
    public sealed record CloneToolInputGroup(string NameKey, int Order, CloneToolInput[] Inputs)
    {
        public static bool Equals(CloneToolInputGroup a, object b)
        {
            if (b is not CloneToolInputGroup groupB) return false;
            return a.NameKey == groupB.NameKey;
        }

        public static bool operator ==(CloneToolInputGroup a, object b)
        {
            return a.Equals(b);
        }

        public static bool operator ==(object a, CloneToolInputGroup b)
        {
            return b.Equals(a);
        }

        public static bool operator !=(CloneToolInputGroup a, object b)
        {
            return !a.Equals(b);
        }

        public static bool operator !=(object a, CloneToolInputGroup b)
        {
            return !b.Equals(a);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return NameKey.GetHashCode();
        }

        public static Builder DescribedAs(string nameKey) => new(nameKey);

        public class Builder
        {
            private string _nameKey;
            private int _order;
            private List<CloneToolInput> _inputs = new();

            internal Builder(string nameKey)
            {
                _nameKey = nameKey;
            }

            public Builder OrderedBy(int order)
            {
                _order = order;
                return this;
            }

            public Builder PerformedBy(Action<CloneToolInput.Builder> build)
            {
                var ib = new CloneToolInput.Builder();
                build(ib);
                _inputs.Add(ib.Build());
                return this;
            }
            public Builder PerformedBy(params CloneToolInput[] inputs) { _inputs.AddRange(inputs); return this; }
            public Builder Or(params CloneToolInput[] inputs) => PerformedBy(inputs);

            public CloneToolInputGroup Build() => new(_nameKey, _order, _inputs.ToArray());
            public static implicit operator CloneToolInputGroup(Builder builder) => builder.Build();
        }
    }
}