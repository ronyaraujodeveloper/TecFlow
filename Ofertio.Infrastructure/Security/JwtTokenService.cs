using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ofertio.Core.Entities;
using Ofertio.Core.Security;

namespace Ofertio.Infrastructure.Security
{
    public class JwtTokenService
    {
        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
        public static readonly TimeSpan ImpersonationLifetime = TimeSpan.FromHours(1);

        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(UserAccount usuario) =>
            WriteToken(BuildUserClaims(usuario, string.IsNullOrWhiteSpace(usuario.Role) ? "TenantUser" : usuario.Role), DefaultLifetime);

        /// <summary>Token com a identidade do inquilino emulado; o papel é sempre TenantUser e carrega o operador master.</summary>
        public string GenerateImpersonationToken(UserAccount target, UserAccount actor, string tenantName)
        {
            var claims = BuildUserClaims(target, "TenantUser");
            claims.Add(new Claim(OfertioClaimTypes.ImpersonatorId, actor.Id.ToString()));
            claims.Add(new Claim(OfertioClaimTypes.ImpersonatorEmail, actor.Email));
            claims.Add(new Claim(OfertioClaimTypes.ImpersonatedTenantName, tenantName));
            return WriteToken(claims, ImpersonationLifetime);
        }

        private static List<Claim> BuildUserClaims(UserAccount usuario, string role) =>
        [
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Name),
            new(OfertioClaimTypes.TenantId, usuario.TenantId.ToString()),
            new(ClaimTypes.Role, role)
        ];

        private string WriteToken(IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? _configuration["Jwt:Secret"];
            var key = Encoding.UTF8.GetBytes(jwtKey ?? string.Empty);
            var issuer = _configuration["Jwt:Issuer"] ?? "OfertioAPI";
            var audience = _configuration["Jwt:Audience"] ?? "OfertioClient";

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(lifetime),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
