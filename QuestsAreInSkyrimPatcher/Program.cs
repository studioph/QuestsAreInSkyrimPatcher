using CommandLine;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Synthesis.Utils;
using Synthesis.Utils.Quests;

namespace QuestsAreInSkyrimPatcher
{
    public class Program
    {
        private static readonly ModKey QAIS = ModKey.FromNameAndExtension("QuestsAreInSkyrim.esp");
        private static readonly ModKey QAIS_USSEP = ModKey.FromNameAndExtension(
            "QuestsAreInSkyrimUSSEP.esp"
        );
        private static readonly IEnumerable<ModKey> qaisVersions = new ModKey[]
        {
            QAIS_USSEP,
            QAIS
        };

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline
                .Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "QuestsAreInSkyrimPatcher.esp")
                .AddRunnabilityCheck(state =>
                {
                    state.LoadOrder.AssertListsAnyMod(qaisVersions);
                })
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var qaisEsp = state.LoadOrder.ResolvePluginVersion(qaisVersions);
            if (qaisEsp.Mod is null)
            {
                Console.WriteLine($"WARNING: Unable to load {qaisEsp.FileName}, aborting");
                return;
            }

            var qaisFormList = qaisEsp
                .Mod.FormLists.Where(formList => formList.EditorID is not null)
                .Single(formList => formList.EditorID!.Equals("SkyrimHoldsFList"));

            if (qaisFormList is null)
            {
                Console.WriteLine("WARNING: Unable to locate QAIS FormList, aborting");
                return;
            }
            var affectedQuests = qaisEsp.Mod.Quests;

            // See https://github.com/studioph/QuestsAreInSkyrimPatcher/issues/1
            bool searchFunc(IConditionGetter condition) =>
                condition.Data.Function == Condition.Function.GetInCurrentLocFormList
                && condition
                    .Data.Cast<IGetInCurrentLocFormListConditionDataGetter>()
                    .FormList.Link.Equals(qaisFormList!.ToLinkGetter());

            var qaisCondition = QuestAliasConditionUtil.FindAliasCondition(
                affectedQuests,
                condition => searchFunc(condition) && !condition.Flags.HasFlag(Condition.Flag.OR)
            );

            if (qaisCondition is null)
            {
                Console.WriteLine(
                    "WARNING: Unable to find QAIS condition in quest aliases, aborting"
                );
                return;
            }

            var patcher = new QuestAliasConditionUtil(qaisCondition, searchFunc);
            patcher.PatchAll(affectedQuests, state);
        }
    }
}
