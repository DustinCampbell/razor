// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.Test.Common;

// Used to abstract away platform-specific file/directory path information.
//
// The System.IO.Path methods don't processes Windows paths in a Windows way
// on *nix (rightly so), so we need to use platform-specific paths.
//
// Target paths are always Windows style.
internal static class TestProjectData
{
    static TestProjectData()
    {
        var baseDirectory = PlatformInformation.IsWindows ? @"c:\users\example\src" : "/home/example";
        SomeProjectPath = Path.Combine(baseDirectory, "SomeProject");
        var someProjectObjPath = Path.Combine(SomeProjectPath, "obj");

        SomeProject = new HostProject(Path.Combine(SomeProjectPath, "SomeProject.csproj"), someProjectObjPath, RazorConfiguration.Default, "SomeProject");
        SomeProjectFile1 = new HostDocument(Path.Combine(SomeProjectPath, "File1.cshtml"), "File1.cshtml", RazorSourceCodeKind.Legacy);
        SomeProjectFile2 = new HostDocument(Path.Combine(SomeProjectPath, "File2.cshtml"), "File2.cshtml", RazorSourceCodeKind.Legacy);
        SomeProjectImportFile = new HostDocument(Path.Combine(SomeProjectPath, "_ViewImports.cshtml"), "_ViewImports.cshtml", RazorSourceCodeKind.Legacy);
        SomeProjectNestedFile3 = new HostDocument(Path.Combine(SomeProjectPath, "Nested", "File3.cshtml"), "Nested\\File3.cshtml", RazorSourceCodeKind.Legacy);
        SomeProjectNestedFile4 = new HostDocument(Path.Combine(SomeProjectPath, "Nested", "File4.cshtml"), "Nested\\File4.cshtml", RazorSourceCodeKind.Legacy);
        SomeProjectNestedImportFile = new HostDocument(Path.Combine(SomeProjectPath, "Nested", "_ViewImports.cshtml"), "Nested\\_ViewImports.cshtml", RazorSourceCodeKind.Legacy);
        SomeProjectComponentFile1 = new HostDocument(Path.Combine(SomeProjectPath, "File1.razor"), "File1.razor", RazorSourceCodeKind.Component);
        SomeProjectComponentFile2 = new HostDocument(Path.Combine(SomeProjectPath, "File2.razor"), "File2.razor", RazorSourceCodeKind.Component);
        SomeProjectComponentImportFile1 = new HostDocument(Path.Combine(SomeProjectPath, "_Imports.razor"), "_Imports.razor", RazorSourceCodeKind.Component);
        SomeProjectNestedComponentFile3 = new HostDocument(Path.Combine(SomeProjectPath, "Nested", "File3.razor"), "Nested\\File3.razor", RazorSourceCodeKind.Component);
        SomeProjectNestedComponentFile4 = new HostDocument(Path.Combine(SomeProjectPath, "Nested", "File4.razor"), "Nested\\File4.razor", RazorSourceCodeKind.Component);
        SomeProjectCshtmlComponentFile5 = new HostDocument(Path.Combine(SomeProjectPath, "File5.cshtml"), "File5.cshtml", RazorSourceCodeKind.Component);

        var anotherProjectPath = Path.Combine(baseDirectory, "AnotherProject");
        var anotherProjectObjPath = Path.Combine(anotherProjectPath, "obj");

        AnotherProject = new HostProject(Path.Combine(anotherProjectPath, "AnotherProject.csproj"), anotherProjectObjPath, RazorConfiguration.Default, "AnotherProject");
        AnotherProjectFile1 = new HostDocument(Path.Combine(anotherProjectPath, "File1.cshtml"), "File1.cshtml", RazorSourceCodeKind.Legacy);
        AnotherProjectFile2 = new HostDocument(Path.Combine(anotherProjectPath, "File2.cshtml"), "File2.cshtml", RazorSourceCodeKind.Legacy);
        AnotherProjectImportFile = new HostDocument(Path.Combine(anotherProjectPath, "_ViewImports.cshtml"), "_ViewImports.cshtml", RazorSourceCodeKind.Legacy);
        AnotherProjectNestedFile3 = new HostDocument(Path.Combine(anotherProjectPath, "Nested", "File3.cshtml"), "Nested\\File1.cshtml", RazorSourceCodeKind.Legacy);
        AnotherProjectNestedFile4 = new HostDocument(Path.Combine(anotherProjectPath, "Nested", "File4.cshtml"), "Nested\\File2.cshtml", RazorSourceCodeKind.Legacy);
        AnotherProjectNestedImportFile = new HostDocument(Path.Combine(anotherProjectPath, "Nested", "_ViewImports.cshtml"), "Nested\\_ViewImports.cshtml", RazorSourceCodeKind.Legacy);
        AnotherProjectComponentFile1 = new HostDocument(Path.Combine(anotherProjectPath, "File1.razor"), "File1.razor", RazorSourceCodeKind.Component);
        AnotherProjectComponentFile2 = new HostDocument(Path.Combine(anotherProjectPath, "File2.razor"), "File2.razor", RazorSourceCodeKind.Component);
        AnotherProjectNestedComponentFile3 = new HostDocument(Path.Combine(anotherProjectPath, "Nested", "File3.razor"), "Nested\\File1.razor", RazorSourceCodeKind.Component);
        AnotherProjectNestedComponentFile4 = new HostDocument(Path.Combine(anotherProjectPath, "Nested", "File4.razor"), "Nested\\File2.razor", RazorSourceCodeKind.Component);
    }

    public static readonly HostProject SomeProject;
    public static readonly string SomeProjectPath;
    public static readonly HostDocument SomeProjectFile1;
    public static readonly HostDocument SomeProjectFile2;
    public static readonly HostDocument SomeProjectImportFile;
    public static readonly HostDocument SomeProjectNestedFile3;
    public static readonly HostDocument SomeProjectNestedFile4;
    public static readonly HostDocument SomeProjectNestedImportFile;
    public static readonly HostDocument SomeProjectComponentFile1;
    public static readonly HostDocument SomeProjectComponentFile2;
    public static readonly HostDocument SomeProjectComponentImportFile1;
    public static readonly HostDocument SomeProjectNestedComponentFile3;
    public static readonly HostDocument SomeProjectNestedComponentFile4;
    public static readonly HostDocument SomeProjectCshtmlComponentFile5;

    public static readonly HostProject AnotherProject;
    public static readonly HostDocument AnotherProjectFile1;
    public static readonly HostDocument AnotherProjectFile2;
    public static readonly HostDocument AnotherProjectImportFile;
    public static readonly HostDocument AnotherProjectNestedFile3;
    public static readonly HostDocument AnotherProjectNestedFile4;
    public static readonly HostDocument AnotherProjectNestedImportFile;
    public static readonly HostDocument AnotherProjectComponentFile1;
    public static readonly HostDocument AnotherProjectComponentFile2;
    public static readonly HostDocument AnotherProjectNestedComponentFile3;
    public static readonly HostDocument AnotherProjectNestedComponentFile4;
}
