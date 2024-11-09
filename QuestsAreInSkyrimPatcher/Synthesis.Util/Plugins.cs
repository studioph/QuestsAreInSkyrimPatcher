using System.Collections.Immutable;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Synthesis;

namespace Synthesis.Util
{
    /// <summary>
    /// Simple interface for patcher plugins.
    /// Patcher plugins are optional extensions to a patcher that conditionally run based on whether the load order contains certain mods.
    ///
    /// Like the core, plugins have access to the patcher state.
    /// </summary>
    /// <typeparam name="TMod">The mutable mod type</typeparam>
    /// <typeparam name="TModGetter">The mod getter type</typeparam>
    public interface IPatcherPlugin<TMod, TModGetter>
        where TMod : class, IMod, TModGetter
        where TModGetter : class, IModGetter
    {
        /// <summary>
        /// Executes the plugin code.
        /// </summary>
        /// <param name="state">The patcher state, the same is in RunPatch</param>
        void Run(IPatcherState<TMod, TModGetter> state);
    }

    /// <summary>
    /// Basic plugin information
    /// </summary>
    /// <param name="Name">The name of the plugin</param>
    /// <param name="Sentinal">The modkey to check the load order for to determine whether to load the plugin</param>
    /// <param name="Target">The modkey for the mod whose data the plugin will be using. In most cases this will be the same as Sentinal</param>
    public readonly record struct PluginData(string Name, ModKey Sentinal, ModKey Target)
    {
        public PluginData(string name, ModKey sentinal, ModKey? target = null)
            : this(name, sentinal, target ?? sentinal) { }
    };

    /// <summary>
    /// Interface for a plugin implementation that declares its own PluginData
    /// </summary>
    public interface IPluginData
    {
        static abstract PluginData Data { get; }
    }

    /// <summary>
    /// A loader class that registers and creates plugin instances.
    ///
    /// Plugin implementations are first registered with a loader,
    /// then the load order is scanned for any mods that are declared by the registered plugins.
    ///
    /// Plugins whose related mod is present in the load order are then created and returned.
    /// </summary>
    /// <typeparam name="TMod">The mutable mod type</typeparam>
    /// <typeparam name="TModGetter">The mod getter type</typeparam>
    public class PluginLoader<TMod, TModGetter>
        where TMod : class, IMod, TModGetter
        where TModGetter : class, IModGetter
    {
        /// <summary>
        /// All registered plugins
        /// </summary>
        private readonly IDictionary<
            PluginData,
            Func<TModGetter, IPatcherPlugin<TMod, TModGetter>>
        > _registry =
            new Dictionary<PluginData, Func<TModGetter, IPatcherPlugin<TMod, TModGetter>>>();

        /// <summary>
        /// Read-only view of the registered plugins
        /// </summary>
        public ImmutableArray<PluginData> RegisteredPlugins
        {
            get => [.. _registry.Keys];
        }

        /// <summary>
        /// Registers a new plugin implementation that self-declares its data with the loader.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin implementation type.</typeparam>
        /// <param name="factory">Factory function to create a new instance of the plugin from a mod object.</param>
        public void Register<TPlugin>(Func<TModGetter, IPatcherPlugin<TMod, TModGetter>> factory)
            where TPlugin : IPatcherPlugin<TMod, TModGetter>, IPluginData =>
            Register(TPlugin.Data, factory);

        /// <summary>
        /// Registers a new plugin implementation with the loader.
        /// </summary>
        /// <param name="data">The plugin data associated with the implementation</param>
        /// <param name="factory">Factory function to create a new instance of the plugin from a mod object.</param>
        public void Register(
            PluginData data,
            Func<TModGetter, IPatcherPlugin<TMod, TModGetter>> factory
        ) => _registry.Add(data, factory);

        /// <summary>
        /// Scans the load order and creates plugin instances based on the referenced mods.
        /// If the mod used by a plugin is present and enabled, a created plugin object is returned,
        /// otherwise the plugin is skippe and not loaded.
        /// </summary>
        /// <param name="loadOrder">The load order to load plugins for.</param>
        /// <returns>All loaded plugins</returns>
        public ImmutableArray<IPatcherPlugin<TMod, TModGetter>> Scan(
            ILoadOrder<IModListing<TModGetter>> loadOrder
        )
        {
            IList<IPatcherPlugin<TMod, TModGetter>> loaded = [];

            foreach (var (pluginData, factory) in _registry)
            {
                if (loadOrder.ModExists(pluginData.Sentinal, enabled: true))
                {
                    if (loadOrder.TryGetIfEnabledAndExists(pluginData.Target, out var found))
                    {
                        // The mod used by the plugin exists in the user's load order, load the plugin
                        loaded.Add(factory(found));
                    }
                    else
                    {
                        Console.WriteLine(
                            $"WARNING: Found {pluginData.Sentinal} in load order, but not {pluginData.Target} used by plugin: {pluginData.Name}, skipping"
                        );
                    }
                }
                // The mod used by the plugin was not in the load order, skip
            }

            Console.WriteLine(
                $"Detected and loaded the following plugins: [{string.Join(",", loaded)}]"
            );

            return [.. loaded];
        }
    }
}
