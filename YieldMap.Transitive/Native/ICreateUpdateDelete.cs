using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YieldMap.Database;

namespace YieldMap.Transitive.Native {
    public interface ICreateUpdateDelete<in T> where T : IEquatable<T> {
        void Create(T item);
        void Update(T item);
        void Delete(T item);

        void Save();
    }

    public class InstrumentCud : ICreateUpdateDelete<NInstrument> {
        public void Create(NInstrument item) {
            throw new NotImplementedException();
        }

        public void Update(NInstrument item) {
            throw new NotImplementedException();
        }

        public void Delete(NInstrument item) {
            throw new NotImplementedException();
        }

        public void Save() {
            throw new NotImplementedException();
        }
    }

    public interface IIdentifyable {
        long id { get; set;  }
    }

    public enum Entitites {
        Instrument
    }

    public enum Operations {
        Create,
        Read,
        Update,
        Delete
    }

    public static class NEntityHelper {
        private static readonly Dictionary<Entitites, Dictionary<Operations, string>> Queries;

        static NEntityHelper() {
            Queries = new Dictionary<Entitites, Dictionary<Operations, string>>();

            var instrumentQueries = new Dictionary<Operations, string> {
                {Operations.Create, "INSERT INTO Instrument(Name, id_InstrumentType, id_Description) "},
                {Operations.Read, "SELECT id, Name, id_InstrumentType, id_Description FROM Instrument "},
                {Operations.Update, "UPDATE Instrument SET "},
                {Operations.Delete, "DELETE FROM Instrument "}
            };

            Queries.Add(Entitites.Instrument, instrumentQueries);
        }

        public static dynamic Read(this SQLiteDataReader reader, Entitites what) {
            if (what == Entitites.Instrument)
                return new NInstrument {
                            id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            id_InstrumentType = reader.GetInt32(2),
                            id_Description = reader.GetInt32(3)
                        };
            throw new ArgumentException("what");
        }

        public static void Create(Entitites what, dynamic data) {
            
        }
    }

    public class NInstrument : IIdentifyable, IEquatable<NInstrument> {
        public long id { get; set; }
        
        public string Name { get; set; }
        public long? id_InstrumentType { get; set; }
        public long? id_Description { get; set; }

        public bool Equals(NInstrument other) {
            if (other == null) return false;
            if (id != default(long) && other.id != default(long) && id == other.id) return true;
            return Name == other.Name && id_InstrumentType == other.id_InstrumentType &&
                   id_Description == other.id_Description;
        }
    }
}
