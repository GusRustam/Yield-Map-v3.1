using System;
using System.Data.Entity;

namespace YieldMap.Transitive.Domains {
    public interface IUnitOfWork<out TContext> : IDisposable where TContext : DbContext {
        int Save();
        TContext Context { get; }
    }
}