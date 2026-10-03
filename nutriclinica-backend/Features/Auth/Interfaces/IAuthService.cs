using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Features.Auth.DTOs;

namespace nutriclinica_backend.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<TokenRespuestaDto> LoginAsync(LoginDto dto);
    Task<TokenRespuestaDto> RefreshAsync(RefreshTokenDto dto);
    Task LogoutAsync(Guid nutricionistaId);
}
