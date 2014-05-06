using System;
using System.IO;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    public static class BackupRestore {
        private static readonly string ConnStr;
        private static readonly string BackupFile = Path.Combine(Location.path, "main.bak");

        static BackupRestore() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static void Backup() {
            Cleanup();
            using (var ctx = new MainEntities(ConnStr)) {
                var sql = String.Format("BACKUP DATABASE main TO DISK='{0}'", BackupFile);
                ctx.Database.ExecuteSqlCommand(sql);
            }
            if (!File.Exists(BackupFile))
                throw new InvalidOperationException("No backup file found");
        }

        public static void Cleanup() {
            if (File.Exists(BackupFile)) File.Delete(BackupFile);
        }

        public static void Restore() {
            if (!File.Exists(BackupFile)) 
                throw new InvalidOperationException("No backup file found");
            using (var ctx = new MainEntities(ConnStr)) {
                var sql = String.Format("RESTORE DATABASE main FROM DISK='{0}'", BackupFile);
                ctx.Database.ExecuteSqlCommand(sql);
            }
            Cleanup();
        }
    }
}