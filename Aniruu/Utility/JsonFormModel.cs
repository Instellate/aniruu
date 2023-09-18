using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aniruu.Utility;

public class JsonFormModel : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ValueProviderResult result =
            bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (result == ValueProviderResult.None)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        object? jsonResult;
        try
        {
            jsonResult = JsonSerializer.Deserialize(result.FirstValue ?? "{}",
                bindingContext.ModelType, new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }
        catch (JsonException)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        if (jsonResult is null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(jsonResult);
        return Task.CompletedTask;
    }
}
