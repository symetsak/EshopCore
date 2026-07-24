using Eshop.API.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Eshop.API.Middleware
{
    public class AddTenantHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Ελέγχουμε αν υπάρχει το [IgnoreTenant] στον Controller ή στο συγκεκριμένο Endpoint
            var hasIgnoreAttribute = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<IgnoreTenantAttribute>().Any() == true ||
                                     context.MethodInfo.GetCustomAttributes(true).OfType<IgnoreTenantAttribute>().Any();

            if (hasIgnoreAttribute)
            {
                // Αν έχει το attribute, ΔΕΝ προσθέτουμε το πεδίο στο Swagger!
                return;
            }

            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // Προσθέτουμε αυτόματα το X-Tenant-Id σε ΚΑΘΕ endpoint του Swagger
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Tenant-Id",
                In = ParameterLocation.Header,
                Required = true, // Το κάνουμε υποχρεωτικό για να μην το ξεχνάμε στο testing
                Schema = new OpenApiSchema
                {
                    Type = "string"
                },
                Description = "Το ID του πελάτη (π.χ. bobs-tools)"
            });
        }
    }
}