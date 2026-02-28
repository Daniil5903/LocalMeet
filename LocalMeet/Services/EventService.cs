using Microsoft.EntityFrameworkCore;
using LocalMeet.Data;
using LocalMeet.Models;
using LocalMeet.Models.Enums;
using LocalMeet.Services.Interfaces;

namespace LocalMeet.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;

        public EventService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateEventAsync(Event ev)
        {
            ev.Status = EventStatus.Pending;
            ev.CreatedAt = DateTime.UtcNow;

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Event>> GetApprovedEventsAsync()
        {
            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.Status == EventStatus.Approved && e.Date >= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Participations)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Event>> GetUserEventsAsync(string userId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Participations)
                .Where(e =>
                    e.CreatorId == userId ||
                    e.Participations.Any(p => p.UserId == userId))
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<List<Event>> GetPendingEventsAsync()
        {
            return await _context.Events
                .Include(e => e.Creator)
                .Where(e => e.Status == EventStatus.Pending)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task ApproveEventAsync(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return;

            ev.Status = EventStatus.Approved;
            await _context.SaveChangesAsync();
        }

        public async Task RejectEventAsync(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return;

            ev.Status = EventStatus.Rejected;
            await _context.SaveChangesAsync();
        }

        public async Task CancelEventAsync(int eventId, string reason)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return;

            ev.Status = EventStatus.Cancelled;
            ev.CancelReason = reason;

            await _context.SaveChangesAsync();
        }
        public async Task<(IEnumerable<Event> Events, int TotalCount)> GetFilteredEventsAsync(
            int? categoryId,
            string? search,
            bool showPast,
            int page,
            int pageSize)
        {
            var query = _context.Events
                .Include(e => e.Category)
                .Where(e => e.Status == EventStatus.Approved)
                .AsQueryable();

            if (!showPast)
                query = query.Where(e => e.Date >= DateTime.Now);

            if (categoryId.HasValue)
                query = query.Where(e => e.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Title.Contains(search));

            var totalCount = await query.CountAsync();

            var events = await query
                .OrderBy(e => e.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (events, totalCount);
        }
    }
}