using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Piraeus.HttpGateway.Formatters
{
    public class PlainTextInputFormatter : InputFormatter
    {
        private const string CONTENT_TYPE = "tex/plain";

        public PlainTextInputFormatter()
        {
            SupportedMediaTypes.Add(CONTENT_TYPE);
        }

        public override bool CanRead(InputFormatterContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            return context.HttpContext.Request.ContentType == CONTENT_TYPE;
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var contentType = context.HttpContext.Request.ContentType;

            if (contentType == CONTENT_TYPE)
            {
                using var ms = new MemoryStream();
                await request.Body.CopyToAsync(ms);
                var content = ms.ToArray();
                return await InputFormatterResult.SuccessAsync(content);
            }

            return await InputFormatterResult.FailureAsync();
        }
    }
}