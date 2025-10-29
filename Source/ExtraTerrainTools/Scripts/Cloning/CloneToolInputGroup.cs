using System.Collections.Generic;
using Timberborn.ConstructionSites;

namespace TerrainTools.Cloning
{
    public class CloneToolInputGroup
    {
        public string NameKey { get; init; }
        public int Order { get; init; }
        public CloneToolInput[] Inputs { get; init; }

        private CloneToolInputGroup() { }

        public virtual bool Equals(CloneToolInputGroup other)
        {
            if (NameKey.Equals(other.NameKey))
            {
                return false;
            }

            return NameKey.Equals(other.NameKey);
        }
        public override bool Equals(object obj)
        {
            return obj is CloneToolInputGroup other && Equals(other);
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

        public class Builder
        {
            private string _groupDesc;
            private int _order;
            private List<CloneToolInput> _actions = new();

            public Builder() { }

            public Builder(string description, int order)
            {
                _groupDesc = description;
                _order = order;
            }

            public Builder SetDescription(string description)
            {
                _groupDesc = description;
                return this;
            }

            public Builder SetOrder(int order)
            {
                _order = order;
                return this;
            }

            public Builder AddInput(string descriptionKey, string keybindId)
            {
                _actions.Add(new()
                {
                    DescriptionKey = descriptionKey,
                    KeybindId = keybindId
                });
                return this;
            }

            public Builder AddDescriptionInput(string description)
            {
                _actions.Add(new()
                {
                    DescriptionKey = description,
                    KeybindId = ""
                });
                return this;
            }

            public Builder AddKeybindInput(string keybindId)
            {
                _actions.Add(new()
                {
                    DescriptionKey = "",
                    KeybindId = keybindId
                });
                return this;
            }

            public Builder Clear()
            {
                _groupDesc = "";
                _order = 0;
                _actions.Clear();

                return this;
            }

            public CloneToolInputGroup Build()
            {
                return new()
                {
                    NameKey = _groupDesc,
                    Order = _order,
                    Inputs = _actions.ToArray()
                };
            }
            public CloneToolInputGroup BuildAndClear()
            {
                var output = Build();
                Clear();
                return output;
            }


        }
    }
}