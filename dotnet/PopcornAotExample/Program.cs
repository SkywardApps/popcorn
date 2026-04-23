using Popcorn;
using System.Text.Json.Serialization;
using over;
using Popcorn.Shared;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddPopcorn(o =>
{
    // Route error responses through the custom envelope shape declared below.
    // This is the AOT canary for the custom-envelope code path: generator emits the writer,
    // AddPopcornEnvelopes installs it at DI time (no reflection needed at runtime), and
    // UsePopcornExceptionHandler calls it on any unhandled exception.
    o.EnvelopeType = typeof(AotCustomEnvelope<>);
});
builder.Services.AddPopcornEnvelopes();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;// JsonNamingPolicy.KebabCaseUpper;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    options.SerializerOptions.AddPopcornOptions();
});

var app = builder.Build();
app.UsePopcornExceptionHandler();

app.MapGet("/todos", ([FromServices] IPopcornAccessor contextAccess) => contextAccess.CreateResponse(new List<over.Todo?> {
    new over.Todo(1, null, "Hello World", DateTimeOffset.Now, false),
    new over.Todo(2, new under.SubTodo(1, 2, 3), null, null, true)
}));

app.MapGet("/null", ([FromServices] IPopcornAccessor contextAccess) => contextAccess.CreateResponse<over.Todo?>(null));
app.MapGet("/sub", ([FromServices] IPopcornAccessor contextAccess) => contextAccess.CreateResponse(new over.Todo(1, new under.SubTodo(1, 2, 3), "Hello World", DateTimeOffset.Now, false)));

// Smoke endpoint: throws to exercise the custom error envelope through the AOT pipeline.
app.MapGet("/boom", () =>
{
    throw new InvalidOperationException("aot boom");
#pragma warning disable CS0162
    return Results.Ok();
#pragma warning restore CS0162
});

app.Run();

namespace under
{
    public record SubTodo(int i, int j, int k);
}

namespace over
{
    public record Todo([property: Always] int Id, [property: Default] under.SubTodo? ToDo, [property: Default] string? Title, [property: JsonPropertyName("DueDate")] DateTimeOffset? DueBy = null, [property: Never] bool IsComplete = false);
}

// Custom response envelope for the AOT example. Marker attributes let the generator emit a
// reflection-free error writer that the exception middleware dispatches through
// PopcornErrorWriterRegistry.
[PopcornEnvelope]
public record class AotCustomEnvelope<T>
{
    [PopcornSuccess] public bool Ok { get; init; } = true;
    [PopcornPayload] public Pop<T> Payload { get; init; }
    [PopcornError]   public ApiError? Problem { get; init; }
}

/*
 * For well-defined object graphs, including only the top-level types in [JsonSerializable(typeof(ApiResponse<?>))] attributes is sufficient
 *  because the metadata generator will traverse the object graph and include nested types.
 * For dynamic or polymorphic scenarios, explicitly list all possible runtime types.
 * Use tools like the ILLink analyzer to catch missing metadata during development.
 * If in doubt, be more explicit with [JsonSerializable] attributes to ensure all required types are covered.
 */
[JsonSerializable(typeof(ApiResponse<List<Todo?>>))]
[JsonSerializable(typeof(ApiResponse<Todo>))]
[JsonSerializable(typeof(AotCustomEnvelope<List<Todo?>>))]
[JsonSerializable(typeof(AotCustomEnvelope<Todo>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
