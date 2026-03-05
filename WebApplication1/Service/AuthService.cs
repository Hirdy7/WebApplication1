using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.DTO;
using WebApplication1.Data;
using Microsoft.Extensions.Options;


namespace WebApplication1.Service
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(ApplicationDbContext context, IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
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

        public async Task CreateDefaultAdmin()
        {
            try
            {
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
                    await _context.SaveChangesAsync();

                    Console.WriteLine(" Администратор успешно создан.");
                }
                else
                {
                    Console.WriteLine(" Администратор уже существует.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка при создании администратора: {ex.Message}");
            }
        }
    }
}