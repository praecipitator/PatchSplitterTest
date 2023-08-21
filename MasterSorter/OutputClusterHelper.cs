using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;

namespace MasterSorter
{
    public class OutputClusterHelper<TMod, TModGetter>
        where TMod : TModGetter, IMod, IMajorRecordContextEnumerable<TMod, TModGetter>
        where TModGetter : IModGetter
    {
        /// <summary>
        /// Helper class to contain data for a cluster, which will eventually become a file
        /// </summary>
        public class Cluster
        {
            /// <summary>
            /// The masters for the current cluster. The output file must have these masters.
            /// </summary>
            public HashSet<ModKey> masters = new();

            /// <summary>
            /// The record for the current cluster, these would be contained in the resulting file
            /// </summary>
            // public List<IMajorRecord> records = new();

            public List<IModContext<TMod, TModGetter, IMajorRecord, IMajorRecordGetter>> records = new();
        }

        /// <summary>
        /// Returns a HashSet of all masters relevant for `recordContext`, except the given exception
        /// </summary>
        /// <param name="recordContext"></param>
        /// <param name="except">To exclude the current inputMod</param>
        /// <returns></returns>
        private static HashSet<ModKey> GetAllMasters(IModContext<TMod, TModGetter, IMajorRecord, IMajorRecordGetter> recordContext, ModKey except)
        {
            var result = new HashSet<ModKey>();

            if (except != recordContext.Record.FormKey.ModKey)
            {
                result.Add(recordContext.Record.FormKey.ModKey);
            }

            var formLinks = recordContext.Record.EnumerateFormLinks();
            foreach (var formLink in formLinks)
            {
                // formLink only pulls the master it originates from, not from any forms it might reference. 
                result.Add(formLink.FormKey.ModKey);
            }

            return result;
        }

        /// <summary>
        /// Splits a given `inputMod` into n output Clusters, each containing at most `limit` masters.
        /// </summary>
        /// <param name="inputMod"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        private static List<Cluster> GenerateClusters(TMod inputMod, int limit)
        {
            var clusters = new List<Cluster>();

            var linkCache = inputMod.ToUntypedImmutableLinkCache();
            foreach (var rec in inputMod.EnumerateMajorRecordContexts<IMajorRecord, IMajorRecordGetter>(linkCache))
            {
                var masters = GetAllMasters(rec, inputMod.ModKey);
                var lastClusterIndex = -1;

                for (int i = 0; i < clusters.Count; i++)
                {
                    var curCluster = clusters[i];

                    var missingMasters = masters.Except(curCluster.masters);

                    if (curCluster.masters.Count + missingMasters.Count() <= limit)
                    {
                        // found an existing cluster where the current record fits
                        lastClusterIndex = i;
                        break;
                    }
                }

                if (lastClusterIndex < 0)
                {
                    // we didn't find any, create new
                    var newCluster = new Cluster
                    {
                        masters = masters
                    };
                    newCluster.records.Add(rec);

                    // TODO: at this point, we could add an index to `existingCluster`, using `newCluster` as key
                    clusters.Add(newCluster);
                    continue;
                }

                // found existing, union it with current masters
                var existingCluster = clusters[lastClusterIndex];
                existingCluster.masters.UnionWith(masters);
                // TODO: at this point, we could add an index to `existingCluster`, using `masters` as key
                existingCluster.records.Add(rec);
            }

            return clusters;
        }

        /// <summary>
        /// Splits a given `inputMod` into n output TMods, each containing at most `limit` masters.
        /// This is intended to be the main "entry point" for the splitting system.
        /// </summary>
        /// <param name="release">The relevant GameRelease (needed to create output TMods)</param>
        /// <param name="inputMod">Raw TMod, with potentially too many masters</param>
        /// <param name="limit">Maximal number of masters in each output. The only reason why this is even configurable is testing.</param>
        /// <returns></returns>
        public static List<TMod> SplitOutputMod(GameRelease release, TMod inputMod, int limit = 255)
        {
            var result = new List<TMod>();
            var inputFileName = inputMod.ModKey.FileName;
            var baseName = inputFileName.NameWithoutExtension;
            var ext = inputFileName.Extension;// includes .

            var clusters = GenerateClusters(inputMod, limit);
            for (int i = 0; i < clusters.Count; i++)
            {
                var curCluster = clusters[i];
                string curFileName;
                if (i == 0)
                {
                    // call the first output the same as the input, so Synthesis.esp stays Synthesis.esp
                    curFileName = inputFileName;
                }
                else
                {
                    // otherwise, suffix them with a number, making Synthesis_1.esp, Synthesis_2.esp, etc
                    curFileName = baseName + "_" + (i + 1).ToString() + ext;
                }

                var newMod = ModInstantiator<TMod>.Activator(ModKey.FromFileName(curFileName), release);

                foreach (var context in curCluster.records)
                {
                    if (context.Record.FormKey.ModKey == inputMod.ModKey)
                    {
                        // this is a Form which has been created within inputMod -> copy it right over
                        context.DuplicateIntoAsNewRecord(newMod);
                    }
                    else
                    {
                        // this is an override -> copy as override
                        context.GetOrAddAsOverride(newMod);
                    }
                }

                result.Add(newMod);
            }

            return result;
        }
    }
}
