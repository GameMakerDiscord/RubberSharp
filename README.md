# Rubber\#　
*An IGOR Wrapper Utility*

**Rubber#** (Rubber Sharp) utility will use the currently active runtime to compile a [GameMaker Studio 2](https://www.yoyogames.com/) project file. It is used with a command line tool or using the C# library.

## Command Line Usage
The simplest way to use Rubber# is with the [command line .exe file](https://github.com/GameMakerDiscord/RubberSharp/releases/latest).

`Rubber# <Project Path> <Platform> <Command> <Export Path>`

**Project Path** is the path to the project folder, NOT the .yyp<br>
**Platform** can be Windows, Mac, or Linux/Ubuntu<br>
**Command** can be Run/Test, Zip, or Installer<br>
**Export Path** is ignored if you choose Run/Test<br>

## C\# Library
All of the code is in the `RubberSharp` namespace.
```csharp
using RubberSharp;

static class Example {
    private static void Test() {
        int igorexitcode = Rubber.Compile(new RubberConfig(){
            ProjectPath = @"C:\Projects\Example",
            ExportPlatform = ExportPlatform.Windows,
            ExportType = ExportType.PackageZip,
            ExportOutputLocation = @"C:\Projects\Example\output.zip",
            YYC = true
        });
        Console.WriteLine("The Rubber# Build " (igorexitcode >= 0) ? "was successful" : "failed!");
    }
}
```

## Notes
- Using a project with Steam enabled is untested
- Using a project with shaders is untested
- Exporting to Mac is not implemented
- Exporting to Linux is not implemented
- The library used to read .yyp, [YoYoProject](https://github.com/GameMakerDiscord/YoYoProject), is in a work in progress state, so that could break something here.
