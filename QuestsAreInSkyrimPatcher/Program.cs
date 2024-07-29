using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Synthesis.Utils;
using Synthesis.Utils.Quests;

namespace QuestsAreInSkyrimPatcher
{
    public class Program
    {
        private static readonly ModKey QAIS = ModKey.FromNameAndExtension("QuestsAreInSkyrim.esp");
        private static readonly ModKey QAIS_USSEP = ModKey.FromNameAndExtension("QuestsAreInSkyrimUSSEP.esp");
        private static readonly IEnumerable<ModKey> qaisVersions = new ModKey[]{QAIS_USSEP, QAIS};

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "QuestsAreInSkyrimPatcher.esp")
                .AddRunnabilityCheck(state =>
                {
                    LoadOrderUtil.AssertListsAnyMod(state.LoadOrder, qaisVersions);
                })
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var qaisEsp = LoadOrderUtil.ResolvePluginVersion(state.LoadOrder, qaisVersions);
            if (qaisEsp is null || qaisEsp!.Mod is null)
            {
                return;
            }

            var qaisFormList = qaisEsp.Mod.FormLists.Where(formList => formList.EditorID is not null)
                .Single(formList => formList.EditorID!.Equals("SkyrimHoldsFList"));
            var affectedQuests = qaisEsp.Mod.Quests;

            var qaisCondition = QuestAliasConditionUtil.FindAliasCondition(affectedQuests.First(), condition => condition.Data.Reference.Equals(qaisFormList));

            if (qaisCondition is null)
            {
                Console.WriteLine($"Unable to find QAIS condition in quest aliases");
                return;
            }

            var patcher = new QuestAliasConditionUtil(qaisCondition);
            patcher.PatchAll(affectedQuests, state);
        }
    }
}
