using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var info = new
{
    Application = "CrossApp - практикум з крос-платформного програмування",
    Student = "Коваль Вадим, група ФЕІ-32",
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotnetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    BaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Склад (товари, партії, залишки, переміщення)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    string jsonOutput = JsonSerializer.Serialize(info, options);
    Console.WriteLine(jsonOutput);
}
else
{
    Console.WriteLine(info.Application);
    Console.WriteLine(info.Student);
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"OC (OSDescription)  : {info.OSDescription}");
    Console.WriteLine($"OC (Environment)    : {info.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)   : {info.DotnetVersion}");
    Console.WriteLine($"Runtime             : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку  : {info.BaseDirectory}");
    Console.WriteLine($"Поточний каталог    : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область   : {info.Domain}");
}