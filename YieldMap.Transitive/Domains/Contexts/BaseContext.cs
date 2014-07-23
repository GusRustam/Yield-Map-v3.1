using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using YieldMap.Tools.Location;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class BaseContext<TContext> : DbContext where TContext : DbContext {
        // ReSharper disable StaticFieldInGenericType
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.BaseContext");
        private static readonly Dictionary<string, string> Variables = new Dictionary<string, string>();
        private static readonly string ConnectionString;
        // ReSharper restore StaticFieldInGenericType

        internal static void SetVariable(string name, string value) {
            Variables[name] = value;
        }

        private static string GetCnnString(string name) {
            try {
                return ConfigurationManager.ConnectionStrings[name].ConnectionString;
            } catch (Exception) {
                return name;
            }
        }

        internal static string GetConnectionString(string name) {
            return Variables.Aggregate(
                GetCnnString(name),
                (current, variable) => current.Replace(@"${" + variable.Key + "}", variable.Value));
        }

        static BaseContext() {
            SetVariable("PathToTheDatabase", Location.path);
            ConnectionString = GetConnectionString("TheMainEntities");
            System.Data.Entity.Database.SetInitializer<TContext>(null);
        }

        public BaseContext() : base(ConnectionString) {
            Logger.Info(string.Format("Aquiring context {0}", ConnectionString));
        }
    }
}
