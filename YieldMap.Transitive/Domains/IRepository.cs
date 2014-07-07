namespace YieldMap.Transitive.Domains {
    public interface IRepository<T> : IReadOnlyRepository<T> {
        /// <summary>
        /// Inserts explicitly, updates all graph
        /// </summary>
        /// <param name="item">item in question</param>
        int Insert(T item);

        /// <summary>
        /// Marks item as added or inserted
        /// </summary>
        /// <param name="item">item in question</param>
        int Add(T item);

        int Remove(T item);
    }
}
