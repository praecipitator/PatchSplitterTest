using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using System.Security.Policy;

namespace MasterSorter.Tests
{
    [TestClass()]
    public class OutputClusterHelperTests
    {
        // just to be able to create unique edids
        private int lastEdidIndex = 0;

        /// <summary>
        /// Stores EDIDs of generated forms in the input class, to be able to track them in the generated files
        /// </summary>
        private HashSet<string> expectedEdids = new();

        /// <summary>
        /// Creates a new unique edid
        /// </summary>
        /// <param name="baseEdid"></param>
        /// <param name="addToExpected"></param>
        /// <returns></returns>
        private string getNewEdid(string baseEdid, bool addToExpected = true)
        {
            var newEdid = baseEdid + lastEdidIndex;
            lastEdidIndex++;
            if (addToExpected)
            {
                expectedEdids.Add(newEdid);
            }
            return newEdid;
        }

        [TestMethod()]
        public void GenerateClustersTest()
        {
            // try to make a fake mod
            var inputMod = new Fallout4Mod(ModKey.FromNameAndExtension("dummy.esp"));
            // and a fake master, because we don't want to move generated forms, only overrides
            var fakeMaster = new Fallout4Mod(ModKey.FromNameAndExtension("dummyMaster.esm"));

            // generate lots of forms from lots of files
            for (uint i = 0; i < 5; i++)
            {
                var flst = createFormListWithContents(new FormKey(fakeMaster.ModKey, 0x23 + i), 6, "dummyFile_" + i + "_", 0x666 + i);

                inputMod.FormLists.Add(flst);
            }

            for (uint i = 0; i < 5; i++)
            {
                var flst = createFormListWithContents(new FormKey(fakeMaster.ModKey, 0x523 + i), 3, "dummyFile2_" + i + "_", 0x777 + i);

                inputMod.FormLists.Add(flst);
            }

            var outputList = OutputClusterHelper<IFallout4Mod, IFallout4ModGetter>.SplitOutputMod(GameRelease.Fallout4, inputMod, 10);

            // now, we expect 5 clusters, each containing one of the 6-sized FLSTs and one of the 3-sized FLSTs
            // there are only 9 files in each cluster, because one slot is used by dummyMaster.esm
            Assert.AreEqual(5, outputList.Count);

            foreach (var mod in outputList)
            {
                var modMasters = GetAllMasters(mod);
                Assert.IsTrue(modMasters.Count <= 10);
                var recs = mod.EnumerateMajorRecords();

                foreach (var rec in recs)
                {
                    var edid = rec.EditorID;
                    if (edid != null)
                    {
                        expectedEdids.Remove(edid);
                    }
                }
            }

            Assert.AreEqual(0, expectedEdids.Count, "Not all generated dummy records were found in the split files");
        }

        [TestMethod()]
        public void FormsArePreservedTest()
        {
            // test that overrides stay overrides, and that local forms keep their FormIDs

            var inputMod = new Fallout4Mod(ModKey.FromNameAndExtension("dummy.esp"));
            // local: form is added by the inputMod itself
            // HashSet<string> edidsLocal = new();
            Dictionary<string, FormKey> localForms = new();
            // override: form is added by another mod, but is overridden in inputMod. it brings all of it's masters with it
            HashSet<string> edidsOverride = new();
            // remote: form is added by another mod, not overridden in inputMod, but is referenced. it brings only it's source file as a master with it
            HashSet<string> edidsRemote = new();

            var localFlst = new FormList(inputMod.GetNextFormKey())
            {
                EditorID = "LocalFlst"
            };
            // edidsLocal.Add(localFlst.EditorID);
            localForms.Add(localFlst.EditorID, localFlst.FormKey);

            inputMod.FormLists.Add(localFlst);

            for (var i = 0; i < 5; i++)
            {
                var curMisc = new MiscItem(inputMod.GetNextFormKey())
                {
                    EditorID = "localMisc1_" + i
                };
                localForms.Add(curMisc.EditorID, curMisc.FormKey);
                inputMod.MiscItems.Add(curMisc);
                localFlst.Items.Add(curMisc);
            }

            // now a local list, with some overrides
            var localWithOverridesFlst = new FormList(inputMod.GetNextFormKey())
            {
                EditorID = "LocalWithOverridesFlst"
            };
            localForms.Add(localWithOverridesFlst.EditorID, localWithOverridesFlst.FormKey);

            var otherFileModKey = new ModKey("Fallout4", ModType.Master);
            for (uint i = 0; i < 5; i++)
            {
                var curMisc = new MiscItem(new FormKey(otherFileModKey, 0x800 + i))
                {
                    EditorID = "overrideMisc_" + i
                };
                edidsOverride.Add(curMisc.EditorID);
                inputMod.MiscItems.Add(curMisc);
                localWithOverridesFlst.Items.Add(curMisc);
            }
            inputMod.FormLists.Add(localWithOverridesFlst);

            // now a formlist which is an override
            var overrideFlst = new FormList(new FormKey(otherFileModKey, 0x900))
            {
                EditorID = getNewEdid("overrideFlst")
            };
            edidsOverride.Add(overrideFlst.EditorID);

            // add some local records
            for (var i = 0; i < 5; i++)
            {
                var curMisc = new MiscItem(inputMod.GetNextFormKey())
                {
                    EditorID = getNewEdid("localMisc")
                };
                localForms.Add(curMisc.EditorID, curMisc.FormKey);
                inputMod.MiscItems.Add(curMisc);
                overrideFlst.Items.Add(curMisc);
            }
            // and some overrides
            for (uint i = 0; i < 5; i++)
            {
                var curMisc = new MiscItem(new FormKey(otherFileModKey, 0xa00 + i))
                {
                    EditorID = getNewEdid("ovrMisc")
                };
                edidsOverride.Add(curMisc.EditorID);
                inputMod.MiscItems.Add(curMisc);
                overrideFlst.Items.Add(curMisc);
            }

            // and some remote forms
            for (uint i = 0; i < 5; i++)
            {
                var curMisc = new MiscItem(new FormKey(otherFileModKey, 0xf00 + i))
                {
                    EditorID = getNewEdid("remoteMisc", false)
                };
                overrideFlst.Items.Add(curMisc);
                edidsRemote.Add(curMisc.EditorID);
            }
            inputMod.FormLists.Add(overrideFlst);

            var outputList = OutputClusterHelper<IFallout4Mod, IFallout4ModGetter>.SplitOutputMod(GameRelease.Fallout4, inputMod, 255);
            // expecting one file exactly, everything in expectedEdids to be present, and overrides to have stayed overrides
            // essentially, we should recieve one file, which is pretty much identical to inputMod
            Assert.AreEqual(1, outputList.Count, "Should only generate one output file");
            var mod = outputList[0];
            var recs = mod.EnumerateMajorRecords();

            foreach (var rec in recs)
            {
                var edid = rec.EditorID;
                if (edid == null) continue;

                if (edidsRemote.Contains(edid))
                {
                    Assert.Fail("Form which should have been referenced only was found within the file. Edid: " + edid);
                }
                else if (edidsOverride.Contains(edid))
                {
                    if (rec.FormKey.ModKey == mod.ModKey)
                    {
                        Assert.Fail("Overridden form turned into local form. Edid: " + edid);
                    }
                    edidsOverride.Remove(edid);
                }
                else if (localForms.ContainsKey(edid))
                {
                    if(rec.FormKey != localForms[edid])
                    {
                        Assert.Fail("FormKey for local form changed! Edid: " + edid+" old formkey: "+ localForms[edid]+" new formkey: "+rec.FormKey);
                    }

                    localForms.Remove(edid);
                }
                else
                {
                    Assert.Fail("Unexpected form in output file: " + edid+" "+rec.FormKey);
                }
            }
            Assert.IsTrue(localForms.Count == 0, "Not all local forms were found in output file");
            Assert.IsTrue(edidsOverride.Count == 0, "Not all overridden forms were found in output file");
        }

        private FormList createFormListWithContents(FormKey key, int numFiles, string fileNameBase, uint startFormId)
        {
            var result = new FormList(key);
            result.EditorID = getNewEdid("testFormList_" + fileNameBase);

            fillFormListWithRemoteRecords(result, numFiles, fileNameBase, startFormId);

            return result;
        }

        /// <summary>
        /// Generates MISC items from NOT within the current file
        /// </summary>
        /// <param name="flst"></param>
        /// <param name="numFiles"></param>
        /// <param name="fileNameBase"></param>
        /// <param name="startFormId"></param>
        private void fillFormListWithRemoteRecords(FormList flst, int numFiles, string fileNameBase = "foobar_", uint startFormId = 0x666)
        {
            for (uint i = 0; i < numFiles; i++)
            {
                var curFileName = fileNameBase + i;
                var dummyItem = new MiscItem(new FormKey(new ModKey(curFileName, ModType.Plugin), startFormId + i));
                dummyItem.EditorID = getNewEdid("TestMisc", false);
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
