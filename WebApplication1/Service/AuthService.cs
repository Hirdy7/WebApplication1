using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.DTO;
using WebApplication1.Data;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;


namespace WebApplication1.Service
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly EmailService _emailService;

        public AuthService(ApplicationDbContext context, IOptions<JwtSettings> jwtOptions, EmailService emailService)
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
            _emailService = emailService;
        }   


        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return AuthResult.Fail("User already exists");

            DateTime birthDate = DateTime.MinValue;
            int age = 20;

            if (!string.IsNullOrEmpty(request.BirthDate) && DateTime.TryParse(request.BirthDate, out birthDate))
            {
                age = DateTime.Today.Year - birthDate.Year;
                if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;
            }
            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "User",
                Name = request.Name ?? "Не указано",
                Age = age,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return AuthResult.Success(new AuthResponse
            {
                Token = token,
                Email = user.Email,
                Role = user.Role
            });
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            Console.WriteLine($" Попытка входа: {request.Email}");

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                Console.WriteLine(" Пользователь не найден");
                return AuthResult.Fail("Invalid credentials");
            }

            Console.WriteLine($" Найден пользователь: {user.Email}, Role: {user.Role}");

            
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            Console.WriteLine($" Проверка пароля: {isPasswordValid}");

            if (!isPasswordValid)
            {
                Console.WriteLine(" Неверный пароль");
                
                Console.WriteLine($" Хеш в базе: {user.PasswordHash}");
                Console.WriteLine($" Введенный пароль: {request.Password}");

                return AuthResult.Fail("Invalid credentials");
            }

            var token = GenerateJwtToken(user);
            Console.WriteLine(" Вход успешен");
            return AuthResult.Success(new AuthResponse
            {
                Token = token,
                Email = user.Email,
                Role = user.Role
            });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        }),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task CreateDefaultData()
        {
            try
            {
                // 1. Инициализация Администратора
                var adminExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Email == "admin@admin.com");

                if (!adminExists)
                {
                    var admin = new User
                    {
                        Email = "admin@admin.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Role = "Admin",
                        Name = "Administrator"
                    };
                    _context.Users.Add(admin);
                    Console.WriteLine(" Администратор успешно создан.");
                }

                // 2. Инициализация Типов Отходов
                var wasteTypesExist = await _context.WasteTypes.AnyAsync();

                if (!wasteTypesExist)
                {
                    var defaultWasteTypes = new List<WasteType>
            {
                new WasteType
                {
                    Id = Guid.Parse("468f7798-268e-4252-9c3f-c24749f7b101"),
                    Name = "Пластик",
                    Description = "ПЭТ-бутылки, пластиковые контейнеры (маркировка 1, 2)",
                    Rewards = 10
                },
                new WasteType
                {
                    Id = Guid.Parse("71398864-4e7a-426b-952b-7c3905f56314"),
                    Name = "Стекло",
                    Description = "Стеклянные бутылки, банки (целые и битые)",
                    Rewards = 15
                },
                new WasteType
                {
                    Id = Guid.Parse("a297906d-5654-444f-801a-8e8f8f97e201"),
                    Name = "Бумага",
                    Description = "Макулатура, картон, старые газеты",
                    Rewards = 5
                },
                new WasteType
                {
                    Id = Guid.Parse("b512316e-1234-4567-890a-f0123456789b"),
                    Name = "Металл",
                    Description = "Алюминиевые банки, жесть",
                    Rewards = 20
                },
                new WasteType
                {
                    Id = Guid.Parse("c9876543-abcd-ef01-2345-6789abcdef01"),
                    Name = "Батарейки",
                    Description = "Опасные отходы, пальчиковые и мизинчиковые батарейки",
                    Rewards = 50
                }
            };

                    _context.WasteTypes.AddRange(defaultWasteTypes);
                    Console.WriteLine(" Дефолтные типы отходов добавлены.");
                }

                // Сохраняем все изменения одним махом
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка при инициализации данных: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return new ServiceResponse<bool> { IsSuccess = true };

            var resetCode = new Random().Next(100000, 999999).ToString();

            user.ResetCode = resetCode;
            user.ResetCodeExpires = DateTime.UtcNow.AddMinutes(15); // Код живет 15 минут

            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(email, "Код восстановления пароля", $"Ваш код: {resetCode}");

            return new ServiceResponse<bool> { IsSuccess = true };
        }

        public async Task<ServiceResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return new ServiceResponse<bool> { IsSuccess = false, Error = "Пользователь не найден" };

            // Проверка кода
            if (user.ResetCode == null || user.ResetCode != request.Code)
                return new ServiceResponse<bool> { IsSuccess = false, Error = "Неверный код восстановления" };

            // Проверка времени жизни кода
            if (user.ResetCodeExpires < DateTime.UtcNow)
                return new ServiceResponse<bool> { IsSuccess = false, Error = "Срок действия кода истек" };

            // Хешируем новый пароль (используй свой метод хеширования)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Очищаем код, чтобы его нельзя было использовать повторно
            user.ResetCode = null;
            user.ResetCodeExpires = null;

            await _context.SaveChangesAsync();

            return new ServiceResponse<bool> { IsSuccess = true };
        }
    }
}