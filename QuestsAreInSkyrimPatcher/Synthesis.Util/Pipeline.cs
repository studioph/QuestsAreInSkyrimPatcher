using System.Text;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace Synthesis.Util
{
    /// <summary>
    /// A base patcher pipeline.
    ///
    /// Due to some fundamental differences between patcher archtypes the base only does the patch step, as well as some other common bookkeeping
    /// </summary>
    /// <typeparam name="TMod">The mutable mod type</typeparam>
    /// <typeparam name="TModGetter">The mod getter type</typeparam>
    /// <param name="patchMod"></param>
    public abstract class PipelineBase<TMod, TModGetter>(TMod patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
    {
        protected readonly TMod _patchMod = patchMod;
        public uint PatchedCount { get; protected set; } = 0;

        /// <summary>
        /// Patches a record based on the provided patching data and patcher instance
        /// </summary>
        /// <typeparam name="TValue">The values used for patching</typeparam>
        /// <param name="item">The record and patching data</param>
        /// <param name="patcher">The patcher instance to use</param>
        protected void PatchRecord<TMajor, TMajorGetter, TValue>(
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue> item,
            IPatcher<TMajor, TMajorGetter, TValue> patcher
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull
        {
            var target = item.Context.GetOrAddAsOverride(_patchMod);
            patcher.Patch(target, item.Values);
            PatchedCount++;
            if (target is IMajorRecord major)
            {
                var builder = new StringBuilder($"Patched {major.FormKey}");
                if (!major.EditorID.IsNullOrWhitespace())
                {
                    builder.Append($":({major.EditorID})");
                }
                Console.WriteLine(builder.ToString());
            }
        }

        protected void PatchAll<TMajor, TMajorGetter, TValue>(
            IPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>> recordsToPatch
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull
        {
            foreach (var item in recordsToPatch)
            {
                PatchRecord(item, patcher);
            }
        }
    }

    /// <summary>
    /// A pipeline for applying multiple forwarding-style patchers to multiple collections of records
    /// </summary>
    /// <typeparam name="TMod">The mutable mod type</typeparam>
    /// <typeparam name="TModGetter">The mod getter type</typeparam>
    public class ForwardPatcherPipeline<TMod, TModGetter>(TMod patchMod)
        : PipelineBase<TMod, TModGetter>(patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
    {
        /// <summary>
        /// Processes records and outputs ones that should be patched along with the new values
        /// </summary>
        /// <param name="patcher">The patcher instance to use</param>
        /// <param name="records"></param>
        /// <returns>DTO objects containing the winning records and values needed to patch</returns>
        protected IEnumerable<
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>
        > GetRecordsToPatch<TMajor, TMajorGetter, TValue>(
            IForwardPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<ForwardRecordContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull =>
            records
                .Select(item =>
                    item.Winning.WithPatchingData(patcher.Analyze(item.Source, item.Winning.Record))
                )
                .Where(result => patcher.ShouldPatch(result.Values));

        public void Run<TMajor, TMajorGetter, TValue>(
            IForwardPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<ForwardRecordContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull => PatchAll(patcher, GetRecordsToPatch(patcher, records));
    }

    /// <summary>
    /// A pipeline for applying multiple transform-style patchers to multiple collections of records
    /// </summary>
    /// <typeparam name="TMod">The mutable mod type</typeparam>
    /// <typeparam name="TModGetter">The mod getter type</typeparam>
    /// <param name="patchMod"></param>
    public class TransformPatcherPipeline<TMod, TModGetter>(TMod patchMod)
        : PipelineBase<TMod, TModGetter>(patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
    {
        protected IEnumerable<
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>
        > GetRecordsToPatch<TMajor, TMajorGetter, TValue>(
            ITransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull =>
            records
                .Where(context => patcher.Filter(context.Record))
                .Select(context => context.WithPatchingData(patcher.Apply(context.Record)));

        public void Run<TMajor, TMajorGetter, TValue>(
            ITransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull => PatchAll(patcher, GetRecordsToPatch(patcher, records));
    }

    /// <summary>
    /// A pipeline for applying multiple conditional transform-style patchers to multiple collections of records
    /// </summary>
    /// <typeparam name="TMod">The mutable mod type</typeparam>
    /// <typeparam name="TModGetter">The mod getter type</typeparam>
    /// <param name="patchMod"></param>
    public class ConditionalTransformPatcherPipeline<TMod, TModGetter>(TMod patchMod)
        : TransformPatcherPipeline<TMod, TModGetter>(patchMod)
        where TMod : IMod, TModGetter
        where TModGetter : IModGetter
    {
        public IEnumerable<
            PatchingData<TMod, TModGetter, TMajor, TMajorGetter, TValue>
        > GetRecordsToPatch<TMajor, TMajorGetter, TValue>(
            IConditionalTransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull =>
            base.GetRecordsToPatch(patcher, records)
                .Where(result => patcher.ShouldPatch(result.Values));

        public void Run<TMajor, TMajorGetter, TValue>(
            IConditionalTransformPatcher<TMajor, TMajorGetter, TValue> patcher,
            IEnumerable<IModContext<TMod, TModGetter, TMajor, TMajorGetter>> records
        )
            where TMajor : TMajorGetter
            where TMajorGetter : IMajorRecordQueryableGetter
            where TValue : notnull => PatchAll(patcher, GetRecordsToPatch(patcher, records));
    }

    // Scoped pipelines per-game

    public class SkyrimForwardPipeline(ISkyrimMod patchMod)
        : ForwardPatcherPipeline<ISkyrimMod, ISkyrimModGetter>(patchMod);

    public class SkyrimTransformPipeline(ISkyrimMod patchMod)
        : TransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter>(patchMod);

    public class SkyrimConditionalPipeline(ISkyrimMod patchMod)
        : ConditionalTransformPatcherPipeline<ISkyrimMod, ISkyrimModGetter>(patchMod);
}
