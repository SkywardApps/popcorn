#nullable enable
namespace Popcorn.Shared
{
    public record class ApiError(string Code, string Message, string? Detail = null);
}
