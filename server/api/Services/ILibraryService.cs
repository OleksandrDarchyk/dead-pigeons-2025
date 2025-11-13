using api.Models;
using api.Models.Requests;
using dataccess;
using Sieve.Models;

namespace api.Services;

public interface ILibraryService
{
    Task<Book> CreateBook(CreateBookRequestDto dto, JwtClaims requester);
    Task<Book> UpdateBook(UpdateBookRequestDto dto, JwtClaims requester);
    Task<Book> DeleteBook(string id, JwtClaims requester);
    Task<List<Book>> GetBooks(SieveModel sieveModel, JwtClaims requester);
}