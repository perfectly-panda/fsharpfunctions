using System;
using System.Collections.Generic;
using System.Text;

namespace csharpfunctions.DataProcessing
{
    public class ItemCount : IEquatable<ItemCount>
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public List<ItemCount> Children { get; set; }

        public bool Equals(ItemCount item)
        {
            if (item == null) return false;
            return Name == item.Name;
        }
        public override bool Equals(object obj) {
            var count = obj as ItemCount;
            if(count != null)
            {
                return Equals(count);
            }
            else
            {
                return base.Equals(obj);
            }
        };
        public override int GetHashCode() => (Name,Count).GetHashCode();
    }
}
