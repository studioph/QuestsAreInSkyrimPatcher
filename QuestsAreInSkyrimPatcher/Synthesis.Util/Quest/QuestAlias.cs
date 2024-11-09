using System.Collections.Immutable;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace Synthesis.Util.Quest
{
    /// <summary>
    /// Generic utilities for working with quest aliases
    /// </summary>
    public static class QuestAliasUtil
    {
        /// <summary>
        /// Searches for an instance of a condition using the specified search criteria
        /// </summary>
        /// <param name="quest"></param>
        /// <param name="searchFunc"></param>
        /// <returns></returns>
        public static IConditionGetter? FindAliasCondition(
            this IQuestGetter quest,
            Func<IConditionGetter, bool> searchFunc
        ) => quest.Aliases.SelectMany(alias => alias.Conditions).Where(searchFunc).FirstOrDefault();

        /// <summary>
        /// Searches for an instance of a condition using the specified search criteria
        /// </summary>
        /// <param name="quests"></param>
        /// <param name="searchFunc"></param>
        /// <returns></returns>
        public static IConditionGetter? FindAliasCondition(
            IEnumerable<IQuestGetter> quests,
            Func<IConditionGetter, bool> searchFunc
        ) =>
            quests
                .SelectMany(quest => quest.Aliases)
                .SelectMany(alias => alias.Conditions)
                .Where(searchFunc)
                .FirstOrDefault();

        /// <summary>
        /// Checks if the given alias has a condition that satisfies the given predicate
        /// </summary>
        /// <param name="alias">The quest alias to check</param>
        /// <returns>True if the alias already contains a condition satisfying the predicate</returns>
        public static bool HasCondition(
            this IQuestAliasGetter alias,
            Func<IConditionGetter, bool> searchFunc
        ) => alias.Conditions.Where(condition => searchFunc(condition)).Any();

        /// <summary>
        /// Checks if the given alias has the condition
        /// </summary>
        /// <param name="alias">The quest alias to check</param>
        /// <returns>True if the alias already contains the condition</returns>
        public static bool HasCondition(this IQuestAliasGetter alias, IConditionGetter condition) =>
            alias.Conditions.Where(cond => cond.Equals(condition)).Any();

        /// <summary>
        /// Finds the quest aliases that contain the condition
        /// </summary>
        /// <param name="quest">The quest to search</param>
        /// <returns>The quest aliases which contain the condition</returns>
        public static IEnumerable<IQuestAliasGetter> GetAliasesWithCondition(
            this IQuestGetter quest,
            Func<IConditionGetter, bool> searchFunc
        ) => quest.Aliases.Where(alias => alias.HasCondition(searchFunc));

        /// <summary>
        /// Finds the quest aliases that contain the condition
        /// </summary>
        /// <param name="quest">The quest to search</param>
        /// <returns>The quest aliases which contain the condition</returns>
        public static IEnumerable<IQuestAliasGetter> GetAliasesWithCondition(
            this IQuestGetter quest,
            IConditionGetter condition
        ) => quest.Aliases.Where(alias => alias.HasCondition(condition));
    }

    public abstract class QuestAliasConditionPatcher(IConditionGetter condition)
        : IPatcher<IQuest, IQuestGetter, IEnumerable<uint>>
    {
        /// <summary>
        /// The target condition to patch quest aliases with
        /// </summary>
        public readonly IConditionGetter Condition = condition;

        public readonly IDictionary<IQuestGetter, IList<IQuestAliasGetter>> PatchedRecords =
            new Dictionary<IQuestGetter, IList<IQuestAliasGetter>>();

        public int PatchedAliasCount =>
            PatchedRecords.Aggregate(0, (sum, next) => sum + next.Value.Count);

        public void Patch(IQuest targetQuest, IEnumerable<uint> patchValues)
        {
            var aliasMap = targetQuest.Aliases.ToImmutableDictionary(alias => alias.ID);
            foreach (var aliasId in patchValues)
            {
                var alias = aliasMap[aliasId];
                alias.Conditions.Add(Condition.DeepCopy());
                Console.WriteLine($"Added condition to alias: {alias.Name}");
                PatchedRecords.GetOrAdd(targetQuest, () => []).Add(alias);
            }
        }
    }

    /// <summary>
    /// Patcher for forwarding a single condition to quest aliases
    /// </summary>
    /// <param name="condition">The condition to forward</param>
    /// <param name="searchFunc">Optional search function to check if a quest aliases needs patching. Default is to check for the existance of the target condition</param>
    public class QuestAliasConditionForwarder(
        IConditionGetter condition,
        Func<IConditionGetter, bool>? searchFunc = null
    )
        : QuestAliasConditionPatcher(condition),
            IForwardPatcher<IQuest, IQuestGetter, IEnumerable<uint>>
    {
        /// <summary>
        /// Optional search criteria to use when checking if a quest alias already contains the target condition, instead of checking for the whole condition object
        /// </summary>
        private readonly Func<IConditionGetter, bool>? _searchFunc = searchFunc;

        /// <summary>
        /// Gets the alias IDs of the quest aliases which contain the condition
        /// </summary>
        /// <param name="quest">The quest to search</param>
        /// <returns>The IDs of the quest aliases which contain the condition</returns>
        private IEnumerable<uint> GetAliasIDsWithCondition(IQuestGetter quest)
        {
            // Prefer searchFunc if present since that implies there's special circumstances
            var aliases = _searchFunc is not null
                ? quest.GetAliasesWithCondition(_searchFunc)
                : quest.GetAliasesWithCondition(Condition);

            return aliases.Select(alias => alias.ID);
        }

        /// <summary>
        /// Checks if the target quest aliases contain the condition
        /// </summary>
        /// <param name="sourceQuest"></param>
        /// <param name="targetQuest"></param>
        /// <returns>The alias IDs from the winning quest that need patchiing</returns>
        public IEnumerable<uint> Analyze(IQuestGetter sourceQuest, IQuestGetter targetQuest)
        {
            var affectedAliases = GetAliasIDsWithCondition(sourceQuest);
            var actualAliases = GetAliasIDsWithCondition(targetQuest);
            var aliasesToPatch = affectedAliases.Except(actualAliases);
            return aliasesToPatch;
        }

        public bool ShouldPatch(IEnumerable<uint> aliasIDs) => aliasIDs.Any();
    }
}
