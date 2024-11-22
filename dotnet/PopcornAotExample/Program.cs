using Popcorn;
using System.Text.Json.Serialization;
using over;
using Popcorn.Shared;
using System.Collections.Immutable;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.AddPopcorn();
});

var app = builder.Build();

app.MapGet("/todos", () => new Bundle<over.Todo> { Data = new over.Todo(1, null, "Hello World", DateTimeOffset.Now, false), PropertyReferences = PropertyReference.ParseIncludeStatement("[!default,DueDate]") });

app.Run();


namespace under
{
    public record Todo(int i);
}

namespace over
{
    public record Todo([property: Always] int Id, under.Todo ToDo, [property: Default] string? Title, [property: JsonPropertyName("DueDate")] DateTimeOffset? DueBy = null, [property: Never] bool IsComplete = false);
}


[Pop(typeof(over.Todo))]
[Pop(typeof(under.Todo))]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Bundle<Todo>))]
[JsonSerializable(typeof(under.Todo), TypeInfoPropertyName="UnderTodo")]
[JsonSerializable(typeof(Bundle<under.Todo>), TypeInfoPropertyName="BundleUnderTodo")]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(IList<Todo>))]
[JsonSerializable(typeof(ICollection<Todo>))]
[JsonSerializable(typeof(IEnumerable<Todo>))]
[JsonSerializable(typeof(IDictionary<string, Todo>))]
[JsonSerializable(typeof(System.Collections.Generic.IEnumerable<KeyValuePair<string, Todo>>))]
[JsonSerializable(typeof(HashSet<Todo>))]
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
