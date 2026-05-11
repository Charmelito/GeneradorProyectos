using Generador.CharmelCodeIA.Domain.Interfaces;
using Scriban;
using Scriban.Runtime;

namespace Generador.CharmelCodeIA.Infrastructure.ProjectScaffolding;

public sealed class TemplateEngine : ITemplateEngine
{
    public Task<string> RenderAsync(string templateContent, Dictionary<string, object> model)
    {
        var template = Template.Parse(templateContent);
        var scriptObject = new ScriptObject();

        foreach (var kvp in model)
        {
            scriptObject.Add(kvp.Key, kvp.Value);
        }

        var context = new TemplateContext
        {
            StrictVariables = false,
            MemberRenamer = member => member.Name
        };
        context.PushGlobal(scriptObject);

        var result = template.Render(context);
        return Task.FromResult(result);
    }

    public async Task<string> RenderFileAsync(string templatePath, Dictionary<string, object> model)
    {
        var content = await File.ReadAllTextAsync(templatePath);
        return await RenderAsync(content, model);
    }
}
