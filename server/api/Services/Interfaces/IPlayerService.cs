using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;

// Handles CRUD and status changes for players
public interface IPlayerService
{
    Task<Player> CreatePlayer(CreatePlayerRequestDto dto);

    // List players with optional filter by activity and sorting
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