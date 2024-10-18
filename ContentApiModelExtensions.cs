using System.ComponentModel.DataAnnotations;
using EPiServer.ContentApi.Core.Serialization.Models;

namespace Example;

public static class ContentApiModelExtensions
{
    public static ContentApiModel AddReadonlyProperties(this ContentApiModel contentApiModel, IContent content)
    {
        var readonlyProperties = content.GetType().GetProperties().Where(info => info.GetCustomAttributes(false).FirstOrDefault() is ScaffoldColumnAttribute);
        var readonlyContentTypeProperties = content.Property.Join(readonlyProperties, data => data.Name, info => info.Name, (_, info) => info);

        foreach (var property in readonlyContentTypeProperties)
        {
            contentApiModel.Properties[property.Name] = property.GetValue(content);
        }

        return contentApiModel;
    }
}