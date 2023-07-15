using Newtonsoft.Json.Bson;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Threading.Tasks;
using System;

namespace ClinicDentServer.BsonFormatter
{
    public class BsonOutputFormatter : OutputFormatter
    {
        public BsonOutputFormatter()
        {
            SupportedMediaTypes.Add("application/bson");
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            using (var writer = new BsonDataWriter(context.HttpContext.Response.Body))
            {
                var serializer = new Newtonsoft.Json.JsonSerializer();
                serializer.Serialize(writer, context.Object);
                await writer.FlushAsync();
            }
        }

        protected override bool CanWriteType(Type type)
        {
            // check if there are any BSON attributes on your model like BsonIgnore
            // or if the type directly converts to a BsonType.
            return base.CanWriteType(type);
        }
    }

}
