using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;
using System.Threading.Tasks;

namespace ClinicDentServer.OutputFormatters
{
    public class TextPlainOutputFormatter : TextOutputFormatter
    {
        public TextPlainOutputFormatter()
        {
            SupportedMediaTypes.Add("text/plain");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;
            var data = context.Object.ToString();

            return response.WriteAsync(data, selectedEncoding);
        }
    }
}
