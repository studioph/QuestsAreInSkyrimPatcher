using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Synthesis.Util;
using Synthesis.Util.Quest;

namespace QuestsAreInSkyrimPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline
                .Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "QuestsAreInSkyrimPatcher.esp")
                .AddRunnabilityCheck(state =>
                {
                    state.LoadOrder.AssertListsAnyMod(QAIS.Versions.Keys);
                })
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var qaisMod = state.LoadOrder.ResolvePluginVersion(QAIS.Versions.Keys);
            var qaisInfo = QAIS.Versions[qaisMod.ModKey];

            // See https://github.com/studioph/QuestsAreInSkyrimPatcher/issues/1
            bool searchFunc(IConditionGetter condition) =>
                condition.Data.Function == Condition.Function.GetInCurrentLocFormList
                && ((IGetInCurrentLocFormListConditionDataGetter)condition.Data).FormList.Equals(
                    qaisInfo.FormList
                );

            var patcher = new QuestAliasConditionForwarder(qaisInfo.Condition, searchFunc);
            var pipeline = new SkyrimForwardPipeline(state.PatchMod);

            var questContexts = qaisMod.Quests.Select(quest =>
                quest.WithContext<ISkyrimMod, ISkyrimModGetter, IQuest, IQuestGetter>(
                    state.LinkCache
                )
            );

            pipeline.Run(patcher, questContexts);
        }
    }

    /// <summary>
    /// Random information about QAIS that can be predefined ahead of time
    /// </summary>
    internal class QAIS(ModKey modKey)
    {
        private static readonly ModKey QAIS_NonUSSEP = ModKey.FromNameAndExtension(
            "QuestsAreInSkyrim.esp"
        );
        private static readonly ModKey QAIS_USSEP = ModKey.FromNameAndExtension(
            "QuestsAreInSkyrimUSSEP.esp"
        );

        public static readonly IReadOnlyDictionary<ModKey, QAIS> Versions = new Dictionary<
            ModKey,
            QAIS
        >()
        {
            { QAIS_USSEP, new(QAIS_USSEP) },
            { QAIS_NonUSSEP, new(QAIS_NonUSSEP) }
        };

        /// <summary>
        /// The QAIS formlist that locations get added to when cleared.
        /// </summary>
        public readonly IFormLinkGetter<IFormListGetter> FormList = FormKey
            .Factory($"000D62:{modKey}")
            .ToLinkGetter<IFormListGetter>();

        /// <summary>
        /// The QAIS formlist condition for Location quest aliases. This is what ensures quest locations are within Skyrim.
        /// </summary>
        public IConditionGetter Condition => BuildCondition();

        /// <summary>
        /// Creates a GetInCurrentLocFormList condition object referencing the QAIS formlist.
        /// The condition isn't complex so it can be created up-front to avoid searching for it in the QAIS mod at runtime.
        /// </summary>
        /// <returns>A condition object that references the QAIS formlist</returns>
        private IConditionGetter BuildCondition()
        {
            IConditionFloat condition = new ConditionFloat();
            IGetInCurrentLocFormListConditionData data = new GetInCurrentLocFormListConditionData();
            data.FormList.Link.SetTo(FormList);
            condition.Data = (ConditionData)data;
            return condition;
        }
    }
}
