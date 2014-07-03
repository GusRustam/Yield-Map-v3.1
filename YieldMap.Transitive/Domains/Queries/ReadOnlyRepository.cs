using System;
using System.Data.Entity;

namespace YieldMap.Transitive.Domains.Queries {
    public abstract class ReadOnlyRepository<TContext> : IDisposable where TContext : DbContext, new() {
        private readonly TContext _context;

        protected ReadOnlyRepository() {
            _context = new TContext();
        }

        protected ReadOnlyRepository(TContext context) {
            _context = context;
        }

        public void Dispose() {
            _context.Dispose();
        }

        public TContext Context { get { return _context;  }}
    }
}