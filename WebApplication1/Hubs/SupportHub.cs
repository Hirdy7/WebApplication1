namespace WebApplication1.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.EntityFrameworkCore;
    using WebApplication1.Models;
    using WebApplication1.Data;

    [Authorize]
    public class SupportHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public SupportHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(Guid chatId, string text)
        {
            var userIdString = Context.UserIdentifier;

            if (!Guid.TryParse(userIdString, out var senderId))
                return;

            var chat = await _context.SupportChats
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return;

            // Теперь считаем ответом поддержки, если пишет либо Модератор, либо Админ
            bool isSupport = Context.User.IsInRole("Moderator") || Context.User.IsInRole("Admin");

            // Если же логика "isSupport" должна базироваться строго на том, что пишет НЕ владелец чата:
            // bool isSupport = chat.UserId != senderId;

            var message = new SupportMessage
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsSupportReply = isSupport
            };

            _context.SupportMessages.Add(message);
            chat.LastMessageAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var payload = new
            {
                chatId,
                senderId,
                text,
                createdAt = message.CreatedAt,
                isSupportReply = isSupport
            };

            // Отправляем пользователю (владельцу чата)
            await Clients.User(chat.UserId.ToString()).SendAsync("ReceiveMessage", payload);

            // Отправляем всем сотрудникам (и админам, и модераторам) в группу
            await Clients.Group("Moderators").SendAsync("ReceiveMessage", payload);
        }

        public async Task JoinModeratorGroup()
        {
            // ИСПРАВЛЕНИЕ: Добавляем проверку на обе роли
            if (Context.User.IsInRole("Moderator") || Context.User.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Moderators");
            }
        }
    }
}