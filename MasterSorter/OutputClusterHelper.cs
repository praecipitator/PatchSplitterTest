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

        private static HashSet<ModKey> GetAllMasters(IModContext<TMod, TModGetter, IMajorRecord, IMajorRecordGetter> recordContext, ModKey except)
        {
            var result = new HashSet<ModKey>();

            // what if majorRecord is an override?
            if (except != recordContext.Record.FormKey.ModKey)
            {
                result.Add(recordContext.Record.FormKey.ModKey);
            }

            var formLinks = recordContext.Record.EnumerateFormLinks();
            foreach (var formLink in formLinks)
            {
                // is formLink a major record itself? but, do we even care?
                // I think we don't have to follow it recursively
                //formLink.
                // result.UnionWith(GetAllMasters(formLink));
                result.Add(formLink.FormKey.ModKey);
            }

            return result;
        }

        public static List<Cluster> GenerateClusters(TMod inputMod, int limit)
        {
            var clusters = new List<Cluster>();

            var linkCache = inputMod.ToUntypedImmutableLinkCache();
            foreach (var rec in inputMod.EnumerateMajorRecordContexts<IMajorRecord, IMajorRecordGetter>(linkCache))

            // var recs = inputMod.EnumerateMajorRecords();
            // foreach (var rec in recs)
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

                    clusters.Add(newCluster);
                    continue;
                }

                // found existing, union it with current masters
                var existingCluster = clusters[lastClusterIndex];
                existingCluster.masters.UnionWith(masters);
                existingCluster.records.Add(rec);
            }

            return clusters;
        }

        public static List<TMod> SplitOutputMod(GameRelease release, TMod inputMod, int limit = 255)
        {
            // edge case: if rec is supposed to recieve a certain FormID via the EditorID mechanism, it should also always go into the same filename
            // TODO think of a mechanism for this
            var result = new List<TMod>();
            var inputFileName = inputMod.ModKey.FileName;
            var baseName = inputFileName.NameWithoutExtension;
            var ext = inputFileName.Extension;// includes .

            var clusters = GenerateClusters(inputMod, limit);
            for (int i = 0; i < clusters.Count; i++)
            {
                var curCluster = clusters[i];
                string? curFileName;
                if (i == 0)
                {
                    curFileName = inputFileName;
                }
                else
                {
                    curFileName = baseName + "_" + (i + 1).ToString() + ext;
                }

                var newMod = ModInstantiator<TMod>.Activator(ModKey.FromFileName(curFileName), release);

                //var linkCache = inputMod.ToUntypedImmutableLinkCache();
                //foreach (var context in inputMod.EnumerateMajorRecordContexts<IMajorRecord, IMajorRecordGetter>(linkCache))
                foreach (var context in curCluster.records)
                {
                    if (context.Record.FormKey.ModKey == inputMod.ModKey)
                    {
                        context.DuplicateIntoAsNewRecord(newMod);
                    }
                    else
                    {
                        context.GetOrAddAsOverride(newMod);
                    }
                }

                result.Add(newMod);
            }

            return result;
        }
    }
}
