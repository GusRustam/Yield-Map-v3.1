using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YieldMap.Database.Tools {
    /// <summary>
    /// For a given RIC returns an appropriate field set
    /// </summary>
    public interface IFieldResolver {
        /// <summary>
        /// For a given RIC returns an appropriate field set
        /// </summary>
        /// <param name="ric">the ric to use</param>
        /// <returns>id in FieldGroup table</returns>
        FieldGroup Resolve(string ric);
    }
}
