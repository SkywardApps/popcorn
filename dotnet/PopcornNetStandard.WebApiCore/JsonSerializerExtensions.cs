using Newtonsoft.Json;

namespace Skyward.Popcorn
{
    public static class JsonSerializerExtensions
    {
        public static JsonSerializerSettings DeepCopy(this JsonSerializerSettings serializer)
        {
            var copiedSerializer = new JsonSerializerSettings
            {
                Context = serializer.Context,
                Culture = serializer.Culture,
                ContractResolver = serializer.ContractResolver,
                ConstructorHandling = serializer.ConstructorHandling,
                CheckAdditionalContent = serializer.CheckAdditionalContent,
                DateFormatHandling = serializer.DateFormatHandling,
                DateFormatString = serializer.DateFormatString,
                DateParseHandling = serializer.DateParseHandling,
                DateTimeZoneHandling = serializer.DateTimeZoneHandling,
                DefaultValueHandling = serializer.DefaultValueHandling,
                EqualityComparer = serializer.EqualityComparer,
                FloatFormatHandling = serializer.FloatFormatHandling,
                Formatting = serializer.Formatting,
                FloatParseHandling = serializer.FloatParseHandling,
                MaxDepth = serializer.MaxDepth,
                MetadataPropertyHandling = serializer.MetadataPropertyHandling,
                MissingMemberHandling = serializer.MissingMemberHandling,
                NullValueHandling = serializer.NullValueHandling,
                ObjectCreationHandling = serializer.ObjectCreationHandling,
                PreserveReferencesHandling = serializer.PreserveReferencesHandling,
                ReferenceResolver = serializer.ReferenceResolver,
                ReferenceLoopHandling = serializer.ReferenceLoopHandling,
                StringEscapeHandling = serializer.StringEscapeHandling,
                TraceWriter = serializer.TraceWriter,
                TypeNameHandling = serializer.TypeNameHandling,
                TypeNameAssemblyFormat = serializer.TypeNameAssemblyFormat,
                Binder = serializer.Binder,
                Error = serializer.Error,
                ReferenceResolverProvider = serializer.ReferenceResolverProvider
            };
            foreach (var converter in serializer.Converters)
            {
                copiedSerializer.Converters.Add(converter);
            }
            return copiedSerializer;
        }
    }
}