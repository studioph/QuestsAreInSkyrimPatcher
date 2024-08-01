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
        IConditionGetter Condition { get; }

        // Create a new patcher instance associated with the given condition
        public QuestAliasConditionUtil(IConditionGetter condition)
        {
            Condition = condition;
        }

        // Checks if the given alias has the condition
        public bool HasCondition(IQuestAliasGetter alias)
        {
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

        private IEnumerable<uint> GetAliasesToPatch(IQuestGetter quest, IEnumerable<uint> requiredAliases)
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
            foreach(var aliasId in aliases)
            {
                var alias = aliasMap[aliasId];
                alias.Conditions.Add(Condition.DeepCopy());
                Console.WriteLine($"Added condition to alias: {alias.Name}");
            }
        }

        public void PatchQuest(IQuestGetter quest, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
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

        public void PatchAll(IEnumerable<IQuestGetter> quests, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach(var quest in quests)
            {
                PatchQuest(quest, state);
            }
        }

        // Searches for an instance of a condition using the specified search criteria
        public static IConditionGetter? FindAliasCondition(IQuestGetter quest, Func<IConditionGetter, bool> searchFunc)
        {
            return quest.Aliases.SelectMany(alias => alias.Conditions).Where(searchFunc).FirstOrDefault();
        }
    }
}