namespace YieldMap.Transitive.Queries {
    public interface IBackupRestore {
        string Backup(bool useUnion = false);
        void Cleanup();
        void Restore(string fileName);
    }
}