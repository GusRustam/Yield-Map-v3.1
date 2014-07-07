using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Repositories {
    public class FeedRepository : IFeedRepository {
        private readonly EikonEntitiesContext _context;

        public FeedRepository(IUnitOfWork<EikonEntitiesContext> uow) {
            _context = uow.Context;
        }

        public FeedRepository() {
            _context = new EikonEntitiesContext();
        }

        public IQueryable<Feed> FindAll() {
            return _context.Feeds;
        }

        public IQueryable<Feed> FindAllIncluding(params Expression<Func<Feed, object>>[] inc) {
            return inc.Aggregate<Expression<Func<Feed, object>>, IQueryable<Feed>>(
                _context.Feeds, 
                (current, expression) => current.Include(expression));
        }

        public IQueryable<Feed> FindBy(Func<Feed, bool> predicate) {
            return _context.Feeds.Where(predicate).AsQueryable();
        }

        public Feed FindById(long id) {
            return _context.Feeds.Find(id);
        }

        public void Insert(Feed item) {
            _context.Feeds.Add(item);
            if (item.State != State.Added) 
                _context.ApplyStateChanges();
        }

        public void Add(Feed item) {
            _context.Entry(item).State = item.id == default(long) ? 
                EntityState.Added : 
                EntityState.Modified;
        }

        public void Remove(Feed item) {
            _context.Feeds.Remove(item);
        }

        public void Dispose() {
            _context.Dispose();
        }
    }
}