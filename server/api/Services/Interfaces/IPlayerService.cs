//Notes with rules delete before exam:
//Task - We return Task because the method is asynchronous, and async methods usually perform I/O operations such as database access or network calls.


using api.Models.Requests;
using dataccess.Entities;

namespace api.Services;

public interface IPlayerService
{
    Task<Player> CreatePlayer(CreatePlayerRequestDto dto);
    Task<List<Player>> GetPlayers(bool? isActive = null);
    
    Task<Player> ActivatePlayer(string id);
    
    Task<Player> DeactivatePlayer(string id);
    
    Task<Player> SoftDeletePlayer(string playerId);
    
    Task<Player> GetPlayerById(string playerId);

    Task<Player> UpdatePlayer(UpdatePlayerRequestDto dto);

    
}