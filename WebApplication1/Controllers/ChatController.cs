using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Hubs;
using WebApplication1.Models;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    // 1. Добавляем поле для контекста хаба
    private readonly IHubContext<SupportHub> _hubContext;

    // 2. Добавляем его в конструктор
    public ChatController(ApplicationDbContext context, IHubContext<SupportHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("my-chat")]
    public async Task<IActionResult> GetMyChat()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var chat = await _context.SupportChats
            .Include(c => c.Messages) // Убрал OrderBy внутри Include (может вызывать ошибки в зависимости от версии EF)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (chat == null)
        {
            chat = new SupportChat { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow };
            _context.SupportChats.Add(chat);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            chatId = chat.Id,
            messages = chat.Messages.OrderBy(m => m.CreatedAt).Select(m => new {
                m.Text,
                m.SenderId,
                m.CreatedAt,
                isSupportReply = m.SenderId != userId
            })
        });
    }

    [HttpPost("call-moderator")]
    public async Task<IActionResult> CallModerator([FromBody] Guid chatId)
    {
        var chat = await _context.SupportChats.FindAsync(chatId);
        if (chat == null) return NotFound();

        chat.IsWaitingForModerator = true;
        await _context.SaveChangesAsync();

        // Теперь _hubContext доступен! 
        // Уведомляем всех модераторов, что появился новый активный запрос
        await _hubContext.Clients.Group("Moderators").SendAsync("NewSupportRequest", chatId);

        return Ok();
    }

    // 1. Получение истории конкретного чата для модератора
    [HttpGet("{chatId}/messages")]
    [Authorize(Policy = "ModeratorOnly")] // Только модератор может лезть в чужие чаты
    public async Task<IActionResult> GetChatMessages(Guid chatId)
    {
        var messages = await _context.SupportMessages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new {
                m.Text,
                m.SenderId,
                m.CreatedAt,
                // Здесь логика простая: если отправитель НЕ тот, кто создал чат — значит это ответ саппорта
                isSupportReply = m.IsSupportReply
            })
            .ToListAsync();

        return Ok(messages);
    }

    // 2. Обновим твой метод, чтобы он возвращал реальные данные
    [HttpGet("active-requests")]
    public async Task<IActionResult> GetActiveRequests()
    {
        var requests = await _context.SupportChats
            .Where(c => !c.IsClosed) // Показываем все открытые чаты
            .OrderByDescending(c => c.LastMessageAt) // Свежие сверху
            .Select(c => new {
                c.Id,
                Name = c.User.Name ?? "Пользователь",
                isWaiting = c.IsWaitingForModerator,
                lastMessageAt = c.LastMessageAt
            })
            .ToListAsync();

        return Ok(requests);
    }
}