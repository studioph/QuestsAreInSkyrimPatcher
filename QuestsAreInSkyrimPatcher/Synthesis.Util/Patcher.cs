using Mutagen.Bethesda.Plugins.Records;

namespace Synthesis.Util
{
    /// <summary>
    /// Root interface for all patcher archtypes
    /// </summary>
    /// <typeparam name="TMajor"></typeparam>
    /// <typeparam name="TMajorGetter"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface IPatcher<TMajor, TMajorGetter, TValue>
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
        where TValue : notnull
    {
        /// <summary>
        /// Patches a record with the given values
        /// </summary>
        /// <param name="target"></param>
        /// <param name="patchValues"></param>
        void Patch(TMajor target, TValue patchValues);
    }

    /// <summary>
    /// Interface for "forwarding" style patchers, where values from some "source/reference" record are applied to the winning "target" record
    /// </summary>
    /// <typeparam name="TMajor"></typeparam>
    /// <typeparam name="TMajorGetter"></typeparam>
    public interface IForwardPatcher<TMajor, TMajorGetter, TValue>
        : IPatcher<TMajor, TMajorGetter, TValue>
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
        where TValue : notnull
    {
        /// <summary>
        /// Compares a source record to the winning record and extracts the necessary information to patch
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns>A value object containing whatever data needed to patch the record</returns>
        TValue Analyze(TMajorGetter source, TMajorGetter target);

        /// <summary>
        /// Determines if a record needs patching based on the contents of the given value object
        /// </summary>
        /// <param name="analysis"></param>
        /// <returns>True if the record should be patched</returns>
        bool ShouldPatch(TValue analysis);
    }

    /// <summary>
    /// Interface for "transform" style patchers, where logic is applied directly to winning records
    /// </summary>
    /// <typeparam name="TMajor"></typeparam>
    /// <typeparam name="TMajorGetter"></typeparam>
    public interface ITransformPatcher<TMajor, TMajorGetter, TValue>
        : IPatcher<TMajor, TMajorGetter, TValue>
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
        where TValue : notnull
    {
        /// <summary>
        /// A pre-processing filter to exclude records based on some criteria
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        bool Filter(TMajorGetter record);

        /// <summary>
        /// Applies transformations or other logic to the record
        /// </summary>
        /// <param name="record"></param>
        /// <returns>The result of applying the transformations to the record</returns>
        TValue Apply(TMajorGetter record);
    }

    /// <summary>
    /// A variant of the transform patcher pattern where the transformation results need to be checked to determine whether to override a record.
    ///
    /// Normally, records that would not be overridden should be excluded before processing using the Filter method.
    /// However, in some circumstances it may be required (or more efficient) to compare the *results* of the transformations instead of/in addition to the input values.
    /// </summary>
    /// <typeparam name="TMajor"></typeparam>
    /// <typeparam name="TMajorGetter"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface IConditionalTransformPatcher<TMajor, TMajorGetter, TValue>
        : ITransformPatcher<TMajor, TMajorGetter, TValue>
        where TMajor : TMajorGetter
        where TMajorGetter : IMajorRecordQueryableGetter
        where TValue : notnull
    {
        /// <summary>
        /// Determines if a record needs patching based on the contents of the given value object
        /// </summary>
        /// <param name="analysis"></param>
        /// <returns>True if the record should be patched</returns>
        bool ShouldPatch(TValue analysis);
    }
}
