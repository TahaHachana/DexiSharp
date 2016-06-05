namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("DexiSharp")>]
[<assembly: AssemblyProductAttribute("DexiSharp")>]
[<assembly: AssemblyDescriptionAttribute("F# client library for dexi.io")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.1"
    let [<Literal>] InformationalVersion = "0.0.1"
