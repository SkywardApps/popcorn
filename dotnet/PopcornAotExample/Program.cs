using Popcorn;
using System.Text.Json.Serialization;
using over;
using Popcorn.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddPopcorn();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;// JsonNamingPolicy.KebabCaseUpper;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.AddPopcornOptions();
});

var app = builder.Build();

app.MapGet("/todos", ([FromServices] IPopcornAccessor contextAccess) => contextAccess.CreateResponse(new List<over.Todo?> { 
    new over.Todo(1, null, "Hello World", DateTimeOffset.Now, false),
    new over.Todo(2, new under.SubTodo(1, 2, 3), null, null, true)
}));

app.MapGet("/null", ([FromServices] IPopcornAccessor contextAccess) => contextAccess.CreateResponse<over.Todo?>(null));
app.MapGet("/sub", ([FromServices] IPopcornAccessor contextAccess) => contextAccess.CreateResponse(new over.Todo(1, new under.SubTodo(1, 2, 3), "Hello World", DateTimeOffset.Now, false)));

app.Run();

namespace under
{
    public record SubTodo(int i, int j, int k);
}

namespace over
{
    public record Todo([property: Always] int Id, [property: Default] under.SubTodo? ToDo, [property: Default] string? Title, [property: JsonPropertyName("DueDate")] DateTimeOffset? DueBy = null, [property: Never] bool IsComplete = false);
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
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}




/*
namespace One
{
    namespace Two
    {
        [Pop(typeof(Todo))]
        [Pop(typeof(List<Todo>))]
        public partial class PopcornRegistry 
        {
            public int Test { get; set; }
        }
    }
}*/
