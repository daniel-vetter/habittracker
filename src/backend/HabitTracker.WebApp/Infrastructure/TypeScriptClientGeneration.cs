using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.Generation;

namespace HabitTracker.WebApp.Infrastructure;

public static class TypeScriptClientGeneration
{
    public static async Task<bool> CreateTypeScriptClient(this IServiceProvider sp, string[] args)
    {
        var index = Array.IndexOf(args, "--generateTypeScriptClient");
        if (index == -1) return false;

        var document = await sp.GetRequiredService<IOpenApiDocumentGenerator>().GenerateAsync("v1");
        var settings = new TypeScriptClientGeneratorSettings
        {
            Template = TypeScriptTemplate.Angular,
            InjectionTokenType = InjectionTokenType.InjectionToken,
            TypeScriptGeneratorSettings = { TypeScriptVersion = 6 },
            RxJsVersion = 7.8m
        };

        var json = document.ToJson();
        var typescript = new TypeScriptClientGenerator(
            await OpenApiDocument.FromJsonAsync(json), settings).GenerateFile();

        // NSwag 14.x has no setting for providedIn: 'root'. Without this replace,
        // every generated client would need to be manually registered as a provider.
        typescript = typescript.Replace("@Injectable()", "@Injectable({providedIn: 'root'})");

        await File.WriteAllTextAsync(args[index + 1], typescript + " ");
        Console.WriteLine("TypeScript client written to: " + Path.GetFullPath(args[index + 1]));
        return true;
    }
}
