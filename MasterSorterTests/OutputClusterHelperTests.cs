using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;

namespace MasterSorter.Tests
{

    [TestClass()]
    public class OutputClusterHelperTests
    {
        private int lastEdidIndex = 0;

        private HashSet<string> expectedEdids = new();

        private string getNewEdid(string baseEdid)
        {
            var newEdid = baseEdid + lastEdidIndex;
            lastEdidIndex++;
            expectedEdids.Add(newEdid);
            return newEdid;
        }

        [TestMethod()]
        public void GenerateClustersTest()
        {
            // try to make a fake mod
            var testMod = new Fallout4Mod(ModKey.FromNameAndExtension("dummy.esp"));

            for (uint i = 0; i < 5; i++)
            {
                var flst = createFormListWithContents(new FormKey(testMod.ModKey, 0x23 + i), 7, "dummyFile_" + i + "_", 0x666 + i);
                
                testMod.FormLists.Add(flst);
            }

            for (uint i = 0; i < 5; i++)
            {
                var flst = createFormListWithContents(new FormKey(testMod.ModKey, 0x523 + i), 3, "dummyFile2_" + i + "_", 0x777 + i);

                testMod.FormLists.Add(flst);
            }

            var sut = new OutputClusterHelper();

            var outputList = sut.SplitOutputMod(GameRelease.Fallout4, testMod, 10);

            // now, we expect 5 clusters, each containing one of the 7-sized FLSTs and one of the 3-sized FLSTs
            Assert.AreEqual(5, outputList.Count);


            foreach (var mod in outputList)
            {
                var modMasters = GetAllMasters(mod);
                Assert.IsTrue(modMasters.Count <= 10);
                var recs = mod.EnumerateMajorRecords();

                foreach(var rec in recs)
                {
                    var edid = rec.EditorID;
                    if (edid != null)
                    {
                        expectedEdids.Remove(edid);
                    }
                }

            }

            Assert.AreEqual(0, expectedEdids.Count, "Not all generated dummy records were found in the split files");

            //Assert.Fail();
        }

        private FormList createFormListWithContents(FormKey key, int numFiles, string fileNameBase, uint startFormId)
        {
            var result = new FormList(key);
            result.EditorID = getNewEdid("testFormList_" + fileNameBase);

            fillFormListWithRecords(result, numFiles, fileNameBase, startFormId);

            return result;
        }

        private void fillFormListWithRecords(FormList flst, int numFiles, string fileNameBase = "foobar_", uint startFormId = 0x666)
        {
            for (uint i = 0; i < numFiles; i++)
            {
                var curFileName = fileNameBase + i;
                var dummyItem = new MiscItem(new FormKey(new ModKey(curFileName, ModType.Plugin), startFormId + i));
                dummyItem.EditorID = getNewEdid("TestMisc");
                dummyItem.Name = "Test Item from file " + curFileName;

                flst.Items.Add(dummyItem);
            }
        }

        private static HashSet<ModKey> GetAllMasters(IMod mod)
        {
            var recs = mod.EnumerateMajorRecords();
            var result = new HashSet<ModKey>();

            foreach (var majorRecord in recs)
            {
                if (mod.ModKey != majorRecord.FormKey.ModKey)
                {
                    result.Add(majorRecord.FormKey.ModKey);
                }
                var formLinks = majorRecord.EnumerateFormLinks();
                foreach (var formLink in formLinks)
                {
                    // is formLink a major record itself? but, do we even care?
                    // I think we don't have to follow it recursively
                    //formLink.
                    // result.UnionWith(GetAllMasters(formLink));
                    result.Add(formLink.FormKey.ModKey);
                }
            }

            return result;
        }
    }
}
