using System.Collections.Immutable;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;

namespace Synthesis.Utils.Quests
{
    // Generic utility class for patching quest aliases with a single condition
    public class QuestAliasConditionUtil
    {
        // The target condition to patch quest aliases with
        IConditionGetter Condition { get; }

        // Optional search criteria to use when checking if a quest alias already contains the target condition, instead of checking for the whole condition object
        Func<IConditionGetter, bool>? SearchFunc { get; }

        IDictionary<IQuestGetter, IList<IQuestAliasGetter>> PatchedRecords { get; } =
            new Dictionary<IQuestGetter, IList<IQuestAliasGetter>>();

        // Create a new patcher instance associated with the given condition
        public QuestAliasConditionUtil(
            IConditionGetter condition,
            Func<IConditionGetter, bool>? searchFunc = null
        )
        {
            Condition = condition;
            SearchFunc = searchFunc;
        }

        // Checks if the given alias has the condition
        public bool HasCondition(IQuestAliasGetter alias)
        {
            // Prefer searchFunc if present since that implies there's special circumstances
            if (SearchFunc is not null)
            {
                return alias.Conditions.Where(condition => this.SearchFunc(condition)).Any();
            }
            return alias.Conditions.Where(condition => condition.Equals(this.Condition)).Any();
        }

        // Finds the quest aliases that contain the condition
        public IEnumerable<IQuestAliasGetter> GetAliasesWithCondition(IQuestGetter quest)
        {
            return quest.Aliases.Where(HasCondition);
        }

        private IEnumerable<uint> GetAliasIDsWithCondition(IQuestGetter quest)
        {
            return GetAliasesWithCondition(quest).Select(alias => alias.ID);
        }

        private IEnumerable<uint> GetAliasesToPatch(
            IQuestGetter quest,
            IEnumerable<uint> requiredAliases
        )
        {
            var actualAliases = GetAliasIDsWithCondition(quest);
            var requiredSet = new HashSet<uint>(requiredAliases);
            return requiredSet.Except(actualAliases);
        }

        // Patches the quest by adding the condition to relevant aliases
        private void PatchQuestAliases(IQuest quest, IEnumerable<uint> aliases)
        {
            Console.WriteLine($"Patching quest: {quest.EditorID}");
            var aliasMap = quest.Aliases.ToImmutableDictionary(alias => alias.ID);
            foreach (var aliasId in aliases)
            {
                var alias = aliasMap[aliasId];
                alias.Conditions.Add(Condition.DeepCopy());
                Console.WriteLine($"Added condition to alias: {alias.Name}");
                PatchedRecords.AddOrUpdate(quest, alias);
            }
        }

        public void PatchQuest(
            IQuestGetter quest,
            IPatcherState<ISkyrimMod, ISkyrimModGetter> state
        )
        {
            var affectedAliases = GetAliasIDsWithCondition(quest);
            var winningQuest = quest.ToLinkGetter().TryResolve(state.LinkCache);

            if (winningQuest is null)
            {
                Console.WriteLine($"Unable to resolve FormKey: {quest.FormKey}");
                return;
            }

            var aliasesToPatch = GetAliasesToPatch(winningQuest, affectedAliases);
            if (aliasesToPatch.Any())
            {
                var patchQuest = state.PatchMod.Quests.GetOrAddAsOverride(winningQuest);
                PatchQuestAliases(patchQuest, aliasesToPatch);
            }
        }

        public void PatchAll(
            IEnumerable<IQuestGetter> quests,
            IPatcherState<ISkyrimMod, ISkyrimModGetter> state
        )
        {
            foreach (var quest in quests)
            {
                PatchQuest(quest, state);
            }

            var totalAliasesPatched = PatchedRecords.Aggregate(0, (sum, next) => sum + next.Value.Count);
            Console.WriteLine($"Patched {totalAliasesPatched} aliases across {PatchedRecords.Count} quests");
        }

        // Searches for an instance of a condition using the specified search criteria
        public static IConditionGetter? FindAliasCondition(
            IQuestGetter quest,
            Func<IConditionGetter, bool> searchFunc
        )
        {
            return quest
                .Aliases.SelectMany(alias => alias.Conditions)
                .Where(searchFunc)
                .FirstOrDefault();
        }

        // Searches for an instance of a condition using the specified search criteria
        public static IConditionGetter? FindAliasCondition(
            IEnumerable<IQuestGetter> quests,
            Func<IConditionGetter, bool> searchFunc
        )
        {
            return quests
                .SelectMany(quest => quest.Aliases)
                .SelectMany(alias => alias.Conditions)
                .Where(searchFunc)
                .FirstOrDefault();
        }
    }

    public static class Extensions
    {
        // Analagous to `setordefault` in Python
        public static void AddOrUpdate<TKey, TValue>(
            this IDictionary<TKey, IList<TValue>> dictionary,
            TKey key,
            TValue value
        )
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, new List<TValue>());
            }
            dictionary[key].Add(value);
        }
    }
}
