using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;

namespace MasterSorter
{
    public class OutputClusterHelper
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
            public List<IMajorRecord> records = new();
        }


        private static HashSet<ModKey> GetAllMasters(IMajorRecord majorRecord)
        {
            var result = new HashSet<ModKey>();

            var formLinks = majorRecord.EnumerateFormLinks();
            foreach(var formLink in formLinks)
            {
                result.Add(formLink.FormKey.ModKey);
            }

            return result;
        }

        public static List<Cluster> GenerateClusters(IMod inputMod, int limit)
        {
            var clusters = new List<Cluster>();

            var recs = inputMod.EnumerateMajorRecords();
            foreach(var rec in recs)
            {
                

                var masters = GetAllMasters(rec);
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
        public List<TMod> SplitOutputMod<TMod>(GameRelease release, TMod inputMod, int limit)
            where TMod : IMod//IModGetter
        {
            // edge case: if rec is supposed to recieve a certain FormID via the EditorID mechanism, it should also always go into the same filename
            // TODO think of a mechanism for this
            var result = new List<TMod>();
            var inputFileName = inputMod.ModKey.FileName;
            var baseName = inputFileName.NameWithoutExtension;
            var ext = inputFileName.Extension;// includes .


            var clusters = GenerateClusters(inputMod, limit);
            for(int i=0; i<clusters.Count; i++)
            {
                var curCluster = clusters[i];
                string? curFileName;
                if (i == 0)
                {
                    curFileName = inputFileName;
                }
                else
                {
                    curFileName = baseName + "_" + (i+1).ToString() + ext;
                }

                var newMod = ModInstantiator<TMod>.Activator(ModKey.FromFileName(curFileName), release);

                foreach(var rec in curCluster.records)
                {
                    // somehow put rec into newMod   
                }
                result.Add(newMod);
            }

            return result;
        }
    }

    
}
