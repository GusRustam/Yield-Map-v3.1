using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using YieldMap.Database;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Domains.UnitsOfWork {
    public class PropertiesUnitOfWork : IPropertiesUnitOfWork {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("UnitsOfWork.Properties");
        public PropertiesUnitOfWork() {
            Context = new PropertiesContext();
        }

        public PropertiesUnitOfWork(PropertiesContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public int Save() {
            Context
                .ChangeTracker
                .Entries<Property>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted) 
                .ToList()
                .ForEach(e => {
                    if (e.Entity.PropertyValues.Any()) 
                        throw new InvalidOperationException("Please add and remove Properties and PropertyValues separately");
                    // todo bad idea
                });

            // All entries
            var changedPvs = Context.ChangeTracker.Entries<PropertyValue>().ToList();

            // The added
            var addedPvs = changedPvs
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();
            if (addedPvs.Any()) {
                // Adding the added
                addedPvs.ChunkedForEach(x => {
                    var sql = BulkInsertPropertyValues(x);
                    Logger.Info(String.Format("Sql is {0}", sql));
                    Context.Database.ExecuteSqlCommand(sql);
                }, 500);
            
                // Mark added as unchanged
                changedPvs.Where(e => e.State == EntityState.Added).ToList().ForEach(e => e.State = EntityState.Unchanged);
            }

            var deletedPvs = changedPvs.Where(e => e.State == EntityState.Deleted).ToList();
            if (deletedPvs.Any()) {
                var idsToDelete = String.Join(", ", deletedPvs.Select(e => e.Entity.id));
                Context.Database.ExecuteSqlCommand(string.Format("DELETE FROM PropertyValue WHERE id IN ({0})", idsToDelete));
                changedPvs.Where(e => e.State == EntityState.Deleted).ToList().ForEach(e => e.State = EntityState.Unchanged);
            }

            var dbEntityEntries = Context.ChangeTracker.Entries<PropertyValue>().ToList();
            Debug.Assert(dbEntityEntries.All(e => e.State != EntityState.Added));
            Debug.Assert(dbEntityEntries.All(e => e.State != EntityState.Deleted));

            // And the modified are to be saved by EF
            return Context.SaveChanges() + addedPvs.Count + deletedPvs.Count();
        }

        private static string BulkInsertPropertyValues(IEnumerable<PropertyValue> pvs) {
            var res = pvs.Aggregate(
                "INSERT INTO PropertyValue(id_Instrument, id_Property, 'Value') SELECT ",
                (current, i) => {
                    var idInstrument = i.id_Instrument.HasValue ? i.id_Instrument.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idProperty = i.id_Property.HasValue ? i.id_Property.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
                    return current + String.Format("{0}, {1}, '{2}' UNION SELECT ", idInstrument, idProperty, i.Value);
                });
            return res.Substring(0, res.Length - "UNION SELECT ".Length);
        }

        public PropertiesContext Context { get; private set; }
    }
}