using System;
using System.Collections.Generic;
using YieldMap.Database.Access;
using YieldMap.Database.StoredProcedures;
using YieldMap.Database.StoredProcedures.Additions;

namespace YieldMap.Database {
    public static class Manager {
        public static IDbConn CreateDbConn() {
            return DbConn.Instance;
        }

        public static string Backup(IDbConn conn) {
            return conn.Backup();
        }

        public static void Restore(string name, IDbConn conn) {
            conn.Restore(name);
        }

        public static void Cleanup(IDbConn conn) {
            conn.Cleanup();
        }

        public static Dictionary<Mission, string[]> Classify(DateTime dt, string[] chainRics, IDbConn conn) {
            return conn.Classify(dt, chainRics);
        }

        public static bool NeedsRefresh(DateTime dt, IDbConn conn) {
            return conn.NeedsReload(dt);
        }

        public static Chain[] ChainsInNeed(DateTime dt, IDbConn conn) {
            return conn.ChainsInNeed(dt);
        }

        public static void StaleBondRics(DateTime dt, IDbConn conn) {
            conn.StaleBondRics(dt);
        }
        public static Ric[] AllBondRics(IDbConn conn) {
            return conn.AllBondRics();
        }
        public static void ObsoleteBondRics(DateTime dt, IDbConn conn) {
            conn.ObsoleteBondRics(dt);
        }

        public static Bonds CreateBonds(IDbConn conn) {
            return conn.CreateBonds();
        }

        public static ChainRics CreateChainRics(IDbConn conn) {
            return conn.CreateRics();
        }

        public static Ratings CreateRatings(IDbConn conn) {
            return conn.CreateRatings();
        }
    }
}
