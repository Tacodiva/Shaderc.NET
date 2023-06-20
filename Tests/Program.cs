// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Shaderc;

namespace Tests;

public static class Program {
    public static void Compile(ShaderCompiler comp, string path, ShaderKind shaderKind) {
        using (ShaderCompilationResult res = comp.Compile(path, shaderKind)) {
            Console.WriteLine($"{path}: {res.Status}");
            if (res.Status != ShaderCompilationStatus.Success) {
                Console.WriteLine($"\terrs:{res.ErrorCount} warns:{res.WarningCount}");
                Console.WriteLine($"\t{res.ErrorMessage}");

            }
        }
    }

    public static void Main(string[] args) {
        ShaderCompiler.GetSpvVersion(out SpirVVersion version, out uint revision);
        Console.WriteLine($"SpirV: version={version} revision={revision}");

        using (ShaderCompiler comp = new ShaderCompiler()) {

            Compile(comp, @"shaders/debug.vert", ShaderKind.VertexShader);
            Compile(comp, @"shaders/debug.frag", ShaderKind.FragmentShader);

            comp.Options.IncludeResolver = new SimpleShaderIncludeResolver("shaders");
            Compile(comp, @"shaders/deferred/GBuffPbr.frag", ShaderKind.FragmentShader);
        }
    }
}
