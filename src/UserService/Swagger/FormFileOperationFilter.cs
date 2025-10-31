using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserService.Swagger
{
    public class FormFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Если в методе нет параметров, ничего не делаем
            var parameters = context.ApiDescription.ParameterDescriptions;
            if (parameters == null || !parameters.Any()) return;

            // Собираем параметры, которые приходят из формы и тип которых IFormFile
            var fileParams = parameters
                .Where(p => p.Source != null && p.Source.Id == "Form")
                .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile) ||
                            (p.ModelMetadata?.ModelType != null && typeof(IFormFile).IsAssignableFrom(p.ModelMetadata.ModelType)))
                .ToList();

            if (!fileParams.Any()) return;

            // Формируем схему multipart/form-data
            var schema = new OpenApiSchema { Type = "object", Properties = new System.Collections.Generic.Dictionary<string, OpenApiSchema>() };

            foreach (var p in parameters)
            {
                var name = p.Name;
                var isFile = fileParams.Any(fp => fp.Name == p.Name);
                if (isFile)
                {
                    schema.Properties[name] = new OpenApiSchema { Type = "string", Format = "binary" };
                }
                else
                {
                    // Оставляем остальные поля как строковые (simple handling)
                    schema.Properties[name] = new OpenApiSchema { Type = "string" };
                }
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };
        }
    }
}

