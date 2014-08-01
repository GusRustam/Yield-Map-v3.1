using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Tools {
    /// <summary>
    /// For a given RIC returns an appropriate field set
    /// </summary>
    public interface IFieldResolver {
        /// <summary>
        /// For a given RIC returns an appropriate field set
        /// </summary>
        /// <param name="ric">the ric to use</param>
        /// <returns>id in FieldGroup table</returns>
        NFieldGroup Resolve(string ric);
    }
}
