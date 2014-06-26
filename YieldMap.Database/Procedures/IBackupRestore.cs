namespace YieldMap.Database.Procedures {
    public interface IBackupRestore {
        string Backup(bool useUnion = false);
        void Cleanup();
        void Restore(string fileName);
    }
}