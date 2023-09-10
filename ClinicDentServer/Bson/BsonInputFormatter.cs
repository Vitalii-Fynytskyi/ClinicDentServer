using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace ClinicDentServer.BsonFormatter
{
    public class BsonInputFormatter : InputFormatter
    {
        public BsonInputFormatter()
        {
            SupportedMediaTypes.Add("application/bson");
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using (var reader = new BsonDataReader(context.HttpContext.Request.Body))
            {
                var serializer = new Newtonsoft.Json.JsonSerializer();
                var result = serializer.Deserialize(reader, context.ModelType);
                return await InputFormatterResult.SuccessAsync(result);
            }
        }

        protected override bool CanReadType(Type type)
        {
            // check if there are any BSON attributes on your model like BsonIgnore
            // or if the type directly converts to a BsonType.
            return base.CanReadType(type);
        }
        //protected override bool CanReadType(Type type)
        //{
        //    return typeof(IEnumerable).IsAssignableFrom(type);
        //}
    }
}
