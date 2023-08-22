using Mutagen.Bethesda.Plugins;

namespace MasterSorter
{
    internal class ModKeyComparer : IComparer<ModKey>
    {
        public int Compare(ModKey x, ModKey y)
        {
            return x.GetHashCode().CompareTo(y.GetHashCode());
        }
    }

    /// <summary>
    /// I think we have no choice than to use SortedSet here, I can't really think of a good way to implement Equals() otherwise
    /// </summary>
    internal class ComparableModKeySet: SortedSet<ModKey>
    {
        public static ModKeyComparer keyComparer = new();

        public ComparableModKeySet(IEnumerable<ModKey> inputSet) : base(inputSet, keyComparer) { }

        public ComparableModKeySet(): base() {}

        public override int GetHashCode()
        {
            HashCode hashCode = default;

            foreach (var entry in this)
            {
                hashCode.Add(entry);
            }
            return hashCode.ToHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is ComparableModKeySet other)
            {
                if (other.Count != Count || other.GetHashCode() != GetHashCode())
                {
                    return false;
                }

                for (var i = 0; i < Count; i++)
                {
                    if (this.ElementAt(i) != other.ElementAt(i))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }
        
        public static bool operator <(ComparableModKeySet a, ComparableModKeySet b)
        {
            return a.GetHashCode() < b.GetHashCode();
        }

        public static bool operator >(ComparableModKeySet a, ComparableModKeySet b)
        {
            return a.GetHashCode() > b.GetHashCode();
        }

    }
}
