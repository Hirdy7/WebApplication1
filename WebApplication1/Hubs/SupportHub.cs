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

            // 1. Ищем чат в базе
            var chat = await _context.SupportChats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null) return;

            // 2. Создаем новое сообщение на основе твоей модели SupportMessage
            var newMessage = new SupportMessage
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            // 3. Сохраняем в базу
            _context.SupportMessages.Add(newMessage);
            await _context.SaveChangesAsync();

            // 4. Определяем, кому отправить сообщение
            // Если отправитель — это владелец чата (клиент), то шлем модераторам
            // Если отправитель не владелец (значит модератор), шлем клиенту
            if (chat.UserId == senderId)
            {
                // Отправляем всем модераторам в специальную группу
                await Clients.Group("Moderators").SendAsync("ReceiveMessage", chatId, senderId, text, newMessage.CreatedAt);
            }
            else
            {
                // Отправляем конкретному клиенту — владельцу чата
                await Clients.User(chat.UserId.ToString()).SendAsync("ReceiveMessage", chatId, senderId, text, newMessage.CreatedAt);
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
