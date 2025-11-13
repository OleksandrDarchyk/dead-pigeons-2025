using System.ComponentModel.DataAnnotations;
using api.Models;
using api.Models.Requests;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;

namespace api.Services;

public class LibraryService(
    MyDbContext ctx,
    ISieveProcessor sieveProcessor,
    TimeProvider timeProvider) : ILibraryService
{
    public Task<List<Book>> GetBooks(SieveModel sieveModel, JwtClaims user)
    {
        IQueryable<Book> query = ctx.Books;

        // Apply Sieve FIRST (filtering, sorting, pagination)
        query = sieveProcessor.Apply(sieveModel, query);

        // Then include related data - but DON'T nest further to avoid cycles
        return query
            .Include(b => b.Genre) // Genre won't include its Books
            .Include(b => b.Authors) // Authors won't include their Books
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<Book> CreateBook(CreateBookRequestDto dto, JwtClaims requester)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);

        var book = new Book
        {
            Pages = dto.Pages,
            Createdat = timeProvider.GetUtcNow().DateTime.ToUniversalTime(),
            Id = Guid.NewGuid().ToString(),
            Title = dto.Title
        };
        ctx.Books.Add(book);
        await ctx.SaveChangesAsync();
        return book;
    }

    public async Task<Book> UpdateBook(UpdateBookRequestDto dto, JwtClaims requester)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);
        var book = ctx.Books.First(b => b.Id == dto.BookIdForLookupReference);
        await ctx.Entry(book).Collection(b => b.Authors).LoadAsync();

        book.Pages = dto.NewPageCount;
        book.Title = dto.NewTitle;
        book.Genre = dto.GenreId != null ? ctx.Genres.First(g => g.Id == dto.GenreId) : null;

        book.Authors.Clear();
        dto.AuthorsIds.ForEach(id => book.Authors.Add(ctx.Authors.First(a => a.Id == id)));

        await ctx.SaveChangesAsync();
        return book;
    }

    public async Task<Book> DeleteBook(string bookId, JwtClaims requester)
    {
        var book = ctx.Books.First(b => b.Id == bookId);
        ctx.Books.Remove(book);
        await ctx.SaveChangesAsync();
        return book;
    }
}