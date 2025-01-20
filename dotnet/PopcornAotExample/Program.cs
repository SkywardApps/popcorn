using Popcorn;
using System.Text.Json.Serialization;
using over;
using Popcorn.Shared;
using System.Collections.Immutable;
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

app.MapGet("/todos", ([FromServices] IHttpContextAccessor contextAccess) => contextAccess.HttpContext.Respond((over.Todo?)new over.Todo(1, null, "Hello World", DateTimeOffset.Now, false)));
app.MapGet("/null", ([FromServices] IHttpContextAccessor contextAccess) => contextAccess.HttpContext.Respond<over.Todo?>(null));
app.MapGet("/sub", ([FromServices] IHttpContextAccessor contextAccess) => contextAccess.HttpContext.Respond(new over.Todo(1, new under.Todo(1, 2, 3), "Hello World", DateTimeOffset.Now, false)));

app.Run();


namespace under
{
    public record Todo(int i, int j, int k);
}

namespace over
{
    public record Todo([property: Always] int Id, [property: Default] under.Todo? ToDo, [property: Default] string? Title, [property: JsonPropertyName("DueDate")] DateTimeOffset? DueBy = null, [property: Never] bool IsComplete = false);
}

[JsonSerializable(typeof(ApiResponse<Todo>))]
[JsonSerializable(typeof(Todo), TypeInfoPropertyName = "MyTodo")]
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
