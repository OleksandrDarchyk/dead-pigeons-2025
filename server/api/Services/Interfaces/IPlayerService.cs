using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;
public interface IPlayerService
{
    Task<Player> CreatePlayer(CreatePlayerRequestDto dto);
    Task<List<Player>> GetPlayers(
        bool? isActive = null,
        string? sortBy = null,
        string? direction = null);

    Task<Player> ActivatePlayer(string id);

    Task<Player> DeactivatePlayer(string id);

    Task<Player> SoftDeletePlayer(string playerId);

    Task<Player> GetPlayerById(string playerId);

    Task<Player> UpdatePlayer(UpdatePlayerRequestDto dto);
}