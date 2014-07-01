using System;
using YieldMap.Database.Procedures.Enums;

namespace YieldMap.Database.Tools {
    public class FieldResolver : IFieldResolver {
        private readonly IFieldGroups _groups;

        public FieldResolver(IFieldGroups groups) {
            _groups = groups;
        }

        public FieldGroup Resolve(String ric) {
            return ric.Contains("=MM") ? _groups.Micex : _groups.Default;
        }
    }
}