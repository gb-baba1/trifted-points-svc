// Copyright (c) Kanject 2023
// Author:  Omogbolahan Akinsanya

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Trifted.Points.Api.Components.Filters;

/// <summary>
///     EnumSchemaFilter
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    ///     Apply
    /// </summary>
    /// <param name="model"></param>
    /// <param name="context"></param>
    public void Apply(IOpenApiSchema model, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            model.Enum?.Clear();
            Enum.GetNames(context.Type)
                .ToList()
                .AsParallel()
                .ForAll(name =>
                    model.Enum?.Add(new string($"{Convert.ToInt64(Enum.Parse(context.Type, name))} - {name}")));
        }
    }
}