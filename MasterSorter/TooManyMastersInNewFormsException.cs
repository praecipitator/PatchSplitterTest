using Mutagen.Bethesda.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterSorter
{
    public class TooManyMastersInNewFormsException: Exception
    {
        public ModKey SourceMod { get; }

        private readonly ModKey[] _masters;

        public IReadOnlyList<ModKey> Masters => _masters;

        public TooManyMastersInNewFormsException(
            ModKey sourceMod,
            ModKey[] masters,
            int mastersLimit)
            : base($"Generated forms of {sourceMod} have too many masters, and cannot fit into one single cluster. {masters.Length} >= {mastersLimit}.")
        {
            SourceMod = sourceMod;
            _masters = masters;
        }
    }
}
