using System;
using System.IO;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    public static class BackupRestore {
        private static readonly string DbFile = Path.Combine(Location.path, "main.db");
        private static readonly string BackupFile = Path.Combine(Location.path, "main.bak");


        public static void Backup() {
            Cleanup();
            File.Copy(DbFile, BackupFile);
        }

        public static void Cleanup() {
            if (File.Exists(BackupFile)) File.Delete(BackupFile);
        }

        public static void Restore() {
            if (File.Exists(DbFile)) File.Delete(DbFile);
            File.Copy(BackupFile, DbFile);
            Cleanup();
        }
    }
}