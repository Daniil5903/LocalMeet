using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Hubs
{
    [Authorize]
    public class EventChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EventChatHub(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task JoinEvent(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);

            if (currentUser == null)
            {
                throw new HubException("Пользователь не найден");
            }

            var canUseChat = await CanUseChatAsync(eventId, currentUser);

            if (!canUseChat)
            {
                throw new HubException("Нет доступа к чату мероприятия");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(eventId));
        }

        public async Task SendMessage(int eventId, string text)
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);

            if (currentUser == null)
            {
                throw new HubException("Пользователь не найден");
            }

            if (currentUser.IsBlocked)
            {
                throw new HubException("Заблокированный пользователь не может писать в чат");
            }

            var normalizedText = text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                throw new HubException("Сообщение не может быть пустым");
            }

            if (normalizedText.Length > 1000)
            {
                throw new HubException("Сообщение не должно превышать 1000 символов");
            }

            var eventEntity = await _context.Events
                .Include(e => e.Participations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                throw new HubException("Мероприятие не найдено");
            }

            var canUseChat = await CanUseChatAsync(eventId, currentUser);

            if (!canUseChat)
            {
                throw new HubException("Нет доступа к чату мероприятия");
            }

            var message = new EventMessage
            {
                EventId = eventId,
                UserId = currentUser.Id,
                Text = normalizedText,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.EventMessages.Add(message);

            var notificationRecipients = eventEntity.Participations
                .Select(p => p.UserId)
                .Append(eventEntity.CreatorId)
                .Where(userId => userId != currentUser.Id)
                .Distinct()
                .ToList();

            foreach (var recipientId in notificationRecipients)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = recipientId,
                    Title = "Новое сообщение в чате",
                    Message = $"{currentUser.FirstName} {currentUser.LastName} написал сообщение в чате мероприятия «{eventEntity.Title}».",
                    Link = $"/Events/Details/{eventEntity.Id}",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            var messageDto = new
            {
                id = message.Id,
                userId = currentUser.Id,
                authorName = $"{currentUser.FirstName} {currentUser.LastName}",
                authorAvatarPath = string.IsNullOrWhiteSpace(currentUser.AvatarPath)
                    ? "/images/default-avatar.png"
                    : currentUser.AvatarPath,
                text = message.Text,
                createdAt = message.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                isDeleted = false
            };

            await Clients.Group(GetGroupName(eventId))
                .SendAsync("ReceiveMessage", messageDto);
        }

        [Authorize(Roles = AppRole.Admin)]
        public async Task DeleteMessage(int messageId)
        {
            var message = await _context.EventMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                throw new HubException("Сообщение не найдено");
            }

            if (message.IsDeleted)
            {
                return;
            }

            message.IsDeleted = true;

            await _context.SaveChangesAsync();

            await Clients.Group(GetGroupName(message.EventId))
                .SendAsync("MessageDeleted", message.Id);
        }

        private async Task<bool> CanUseChatAsync(int eventId, User currentUser)
        {
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, AppRole.Admin);

            var eventEntity = await _context.Events
                .Include(e => e.Participations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return false;
            }

            if (isAdmin)
            {
                return true;
            }

            if (eventEntity.Status != EventStatus.Approved)
            {
                return false;
            }

            if (eventEntity.CreatorId == currentUser.Id)
            {
                return true;
            }

            return eventEntity.Participations.Any(p => p.UserId == currentUser.Id);
        }

        private static string GetGroupName(int eventId)
        {
            return $"event-chat-{eventId}";
        }
    }
}