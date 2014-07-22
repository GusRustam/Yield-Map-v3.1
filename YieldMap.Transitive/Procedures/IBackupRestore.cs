namespace YieldMap.Transitive.Procedures {
    /// <summary>
    /// Provides basic backup/restore capability
    /// </summary>
    public interface IBackupRestore {
        string Backup(bool useUnion = false);
        void Cleanup();
        void Restore(string fileName);
    }
}