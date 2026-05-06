namespace WebApplication1.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.EntityFrameworkCore;
    using WebApplication1.Models; // Замени на свой namespace
    using WebApplication1.Data;

    [Authorize]
    public class SupportHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public SupportHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // Метод для отправки сообщения (используется и клиентом, и модератором)
        public async Task SendMessage(Guid chatId, string text)
        {
            var userIdString = Context.UserIdentifier;
            if (!Guid.TryParse(userIdString, out var senderId)) return;

            var chat = await _context.SupportChats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null) return;

            // Определяем, является ли отправитель модератором
            bool isSupport = chat.UserId != senderId;

            var newMessage = new SupportMessage
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsSupportReply = isSupport // Устанавливаем флаг
            };

            _context.SupportMessages.Add(newMessage);

            // Обновляем время последнего сообщения в чате для сортировки в сайдбаре
            chat.LastMessageAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Отправляем сообщение ВСЕМ участникам, включая отправителя для подтверждения
            // Или используем логику разделения, как у вас, но с флагом isSupport
            if (!isSupport)
            {
                await Clients.Group("Moderators").SendAsync("ReceiveMessage", chatId, senderId, text, newMessage.CreatedAt, isSupport);
            }
            else
            {
                await Clients.User(chat.UserId.ToString()).SendAsync("ReceiveMessage", chatId, senderId, text, newMessage.CreatedAt, isSupport);
            }
        }

        // Метод для подключения к группе модераторов (вызывается модератором при входе в админку)
        public async Task JoinModeratorGroup()
        {
            if (Context.User.IsInRole("Moderator"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Moderators");
            }
        }
    }
}
