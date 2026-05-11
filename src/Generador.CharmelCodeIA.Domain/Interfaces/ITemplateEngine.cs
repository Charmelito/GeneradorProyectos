namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface ITemplateEngine
{
    Task<string> RenderAsync(string templateContent, Dictionary<string, object> model);
    Task<string> RenderFileAsync(string templatePath, Dictionary<string, object> model);
}
