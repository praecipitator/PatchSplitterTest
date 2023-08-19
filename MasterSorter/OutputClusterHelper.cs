using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Exceptions;
using Mutagen.Bethesda.Plugins.Records;
using System.Collections.Immutable;

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

            // TODO pre-split the lists

            var linkCache = inputMod.ToUntypedImmutableLinkCache();
            // special case: if rec is from within inputMod itself, not only keept it in Cluster #0,
            // but also keep it's FormID. This should ensure it's FormKey stays constant,
            // because Cluster #0 should be always named the same as the input mod name.

            // do this in a separate pass from the rest, so that no override can block a created record from being added.
            foreach (var rec in inputMod.EnumerateMajorRecordContexts<IMajorRecord, IMajorRecordGetter>(linkCache))
            {
                if(rec.Record.FormKey.ModKey == inputMod.ModKey)
                {

                    // relevant
                    var masters = GetAllMasters(rec, inputMod.ModKey);
                    if (clusters.Count == 0)
                    {
                        var cluster = new Cluster
                        {
                            masters = masters
                        };
                        cluster.records.Add(rec);
                        clusters.Add(cluster);
                    } 
                    else
                    {
                        var cluster = clusters[0];

                        var missingMasters = masters.Except(cluster.masters);
                        if (cluster.masters.Count + missingMasters.Count() <= limit)
                        {
                            cluster.masters.UnionWith(masters);
                            cluster.records.Add(rec);
                        }
                        else
                        {
                            // this is bad 
                            cluster.masters.UnionWith(masters);
                            throw new TooManyMastersInNewFormsException(inputMod.ModKey, cluster.masters.ToArray(), limit);
                        }
                    }
                }
            }

            // second pass: overrides
            foreach (var rec in inputMod.EnumerateMajorRecordContexts<IMajorRecord, IMajorRecordGetter>(linkCache))
            {
                if (rec.Record.FormKey.ModKey == inputMod.ModKey)
                {
                    // this was done in the first pass
                    continue;
                }
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


                var linkCache = inputMod.ToUntypedImmutableLinkCache();

                foreach (var context in curCluster.records)
                {
                    if (context.Record.FormKey.ModKey == inputMod.ModKey)
                    {
                        // TODO: this must ensure the same FormID, somehow
                        // for now, just pass the EditorID as second parameter. This is sub-optimal, but I can't think of any other way RN
                        var resolvedRecord = context.Record.ToLinkGetter().TryResolve(linkCache);
                        var edid = resolvedRecord?.EditorID;
                        var newContext = context.DuplicateIntoAsNewRecord(newMod, edid);
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
