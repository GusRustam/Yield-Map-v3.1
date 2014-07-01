using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YieldMap.Database.Procedures.Additions {
    public interface IPropertyStorage {
        void Save(Dictionary<long, Dictionary<long, string>> values);
    }
}
