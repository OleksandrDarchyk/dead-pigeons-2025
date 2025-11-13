using api.Models.Requests;
using api.Services;
using dataccess;
using Microsoft.AspNetCore.Mvc;
using Sieve.Models;

namespace api.Controllers;

public class LibraryController(
    ILibraryService libraryService,
    IAuthService authService) : ControllerBase
{
    [HttpPost(nameof(GetBooks))]
    public async Task<List<Book>> GetBooks([FromBody] SieveModel sieveModel)
    {
        var jwtClaims = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());

        return await libraryService.GetBooks(sieveModel, jwtClaims);
    }

    [HttpPost(nameof(CreateBook))]
    public async Task<Book> CreateBook([FromBody] CreateBookRequestDto dto)
    {
        var jwtClaims = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());

        return await libraryService.CreateBook(dto, jwtClaims);
    }

    [HttpPut(nameof(UpdateBook))]
    public async Task<Book> UpdateBook([FromBody] UpdateBookRequestDto dto)
    {
        var jwtClaims = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());

        return await libraryService.UpdateBook(dto, jwtClaims);
    }

    [HttpDelete(nameof(DeleteBook))]
    public async Task<Book> DeleteBook([FromQuery] string bookId)
    {
        var jwtClaims = await authService.VerifyAndDecodeToken(Request.Headers.Authorization.FirstOrDefault());

        return await libraryService.DeleteBook(bookId, jwtClaims);
    }
}