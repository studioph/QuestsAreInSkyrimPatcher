using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Exceptions;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;

namespace Synthesis.Utils
{
    public class LoadOrderUtil
    {
        // Resolves which version of a mod the user has in the load order given a list of possible versions/flavors of the plugin
        // Returns the first version of the plugin found
        public static IModListing<ISkyrimModGetter>? ResolvePluginVersion(ILoadOrderGetter<IModListing<ISkyrimModGetter>> loadOrder, IEnumerable<ModKey> pluginVersions)
        {
            foreach (var pluginVersion in pluginVersions)
            {
                if (loadOrder.ListsMod(pluginVersion)){
                    return loadOrder.GetIfEnabled(pluginVersion);
                }
            }

            return null;
        }

        // Asserts that the load order contains any ModKey in the provided list
        public static void AssertListsAnyMod(ILoadOrderGetter loadOrder, IEnumerable<ModKey> modKeys, string? message = null)
        {
            foreach (var modKey in modKeys)
            {
                if (loadOrder.ListsMod(modKey)){
                    return;
                }
            }

            var formattedMessage = message is null ? $"Unable to find any of the following mods in the load order: {modKeys}" : message;
            throw new MissingModException(modKeys, formattedMessage);
        }
    }
}

