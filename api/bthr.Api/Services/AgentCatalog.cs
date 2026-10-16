using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace bthr.Api.Services;

public sealed record AgentResource(string Name, string Path, string Table, Type Entity, Type Create, Type Update)
{
    public string SqlTable => string.Join(".", Table.Split('.').Select(x => "\"" + x + "\""));
}

public static class AgentCatalog
{
    // Discover only application domain CRUD controllers, never arbitrary API routes.
    public static readonly IReadOnlyDictionary<string, AgentResource> Resources = Build();
    private static IReadOnlyDictionary<string, AgentResource> Build()
    {
        var assembly = typeof(AgentCatalog).Assembly;
        var result = new Dictionary<string, AgentResource>();
        foreach (var controller in assembly.GetTypes().Where(t => t.Namespace == "bthr.Api.Controllers"))
        {
            var route = controller.GetCustomAttribute<RouteAttribute>()?.Template;
            if (route == null || !route.StartsWith("api/users/{userId}/")) continue;
            var create = controller.GetMethods().Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
                .SelectMany(m => m.GetParameters()).Select(p => p.ParameterType).FirstOrDefault(t => t.Name.StartsWith("Create") && t.Name.EndsWith("Request"));
            var update = controller.GetMethods().Where(m => m.GetCustomAttribute<HttpPutAttribute>() != null)
                .SelectMany(m => m.GetParameters()).Select(p => p.ParameterType).FirstOrDefault(t => t.Namespace == "bthr.Api.DTOs" && t.Name.EndsWith("Request"));
            if (create == null || update == null) continue;
            var entityName = create.Name[6..^7];
            var entity = assembly.GetType("bthr.Api.Models." + entityName)!;
            var table = entity.GetCustomAttribute<TableAttribute>()!;
            var name = route.Split('/')[^1];
            result.Add(name, new(name, "/" + route, (table.Schema ?? "public") + "." + table.Name, entity, create, update));
        }
        return result;
    }
    public static object Schema(Type type) => new {
        type = "object", additionalProperties = false,
        required = type.GetProperties().Where(p => p.IsDefined(typeof(RequiredAttribute))).Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)),
        properties = type.GetProperties().ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), p => {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            var schema = new Dictionary<string, object> { ["type"] = t == typeof(string) || t == typeof(DateTime) || t == typeof(DateOnly) || t == typeof(TimeOnly) ? "string" : t == typeof(bool) ? "boolean" : t == typeof(decimal) || t == typeof(double) ? "number" : "integer" };
            if (p.GetCustomAttribute<MaxLengthAttribute>() is {} max) schema["maxLength"] = max.Length;
            if (p.GetCustomAttribute<StringLengthAttribute>() is {} length) { schema["maxLength"] = length.MaximumLength; schema["minLength"] = length.MinimumLength; }
            if (t == typeof(DateTime)) schema["format"] = "date-time";
            if (t == typeof(DateOnly)) schema["format"] = "date";
            if (p.GetCustomAttribute<RangeAttribute>() is {} range) { schema["minimum"] = Convert.ToDouble(range.Minimum); schema["maximum"] = Convert.ToDouble(range.Maximum); }
            return schema;
        })
    };
}
