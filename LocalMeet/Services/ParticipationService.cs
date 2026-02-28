using Microsoft.EntityFrameworkCore;
using LocalMeet.Data;
using LocalMeet.Models;
using LocalMeet.Services.Interfaces;


namespace LocalMeet.Services
{
    public class ParticipationService : IParticipationService
    {
        private readonly ApplicationDbContext _context;

        public ParticipationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RegisterAsync(int eventId, string userId)
        {
            var ev = await _context.Events
                .Include(e => e.Participations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return false;

            // Проверка: уже зарегистрирован?
            if (ev.Participations.Any(p => p.UserId == userId))
                return false;

            // Проверка лимита
            if (ev.MaxParticipants.HasValue &&
                ev.Participations.Count >= ev.MaxParticipants.Value)
                return false;

            var participation = new Participation
            {
                EventId = eventId,
                UserId = userId
            };

            _context.Participations.Add(participation);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnregisterAsync(int eventId, string userId)
        {
            var participation = await _context.Participations
                .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);

            if (participation == null)
                return false;

            _context.Participations.Remove(participation);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsUserRegisteredAsync(int eventId, string userId)
        {
            return await _context.Participations
                .AnyAsync(p => p.EventId == eventId && p.UserId == userId);
        }
    }
}