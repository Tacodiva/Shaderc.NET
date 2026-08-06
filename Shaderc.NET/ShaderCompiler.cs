// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.IO;

namespace Shaderc;

public class ShaderCompiler : IDisposable {
    public IntPtr Handle { get; private set; }
    public ShaderCompilerOptions Options { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:shaderc.Compiler"/> class.
    /// If `Options` is null, a default one will be created with Includes resolution enabled.
    /// </summary>
    public ShaderCompiler(ShaderCompilerOptions options = null) {
        Handle = ShadercNativeMethods.shaderc_compiler_initialize();
        if (Handle == IntPtr.Zero)
            throw new Exception("error");
        Options = options ?? new ShaderCompilerOptions();
    }

    public static void GetSpvVersion(out SpirVVersion version, out uint revision) =>
        ShadercNativeMethods.shaderc_get_spv_version(out version, out revision);
    /// <summary>
    /// Try Parses the version and profile from a given null-terminated string,
    /// version and profile are returned through arguments.
    /// </summary>
    /// <returns>Returns false if the string can not be parsed. Returns true when the parsing succeeds.</returns>
    /// <param name="str">string containing both version and profile, like: '450core'.</param>
    /// <param name="version">Version.</param>
    /// <param name="profile">Profile.</param>
    public static bool TryParseVersionProfile(string str, out int version, out ShaderProfile profile) =>
        ShadercNativeMethods.shaderc_parse_version_profile(str, out version, out profile);

    // public ShaderCompilationResult Compile(string path, ShaderKind shaderKind, string entry_point = "main") {
    //     if (!File.Exists(path))
    //         throw new FileNotFoundException("SPIRV file not found", path);
    //     string source = "";
    //     using (StreamReader sr = new StreamReader(path))
    //         source = sr.ReadToEnd();
    //     return Compile(source, path, shaderKind, entry_point);
    // }

    // public ShaderCompilationResult Compile(string source, string fileName, ShaderKind shaderKind, string entry_point = "main") {
    //     return new ShaderCompilationResult(ShadercNativeMethods.shaderc_compile_into_spv(Handle, source, (ulong)source.Length, (byte)shaderKind, fileName, entry_point, Options.Handle));
    // }

    // public ShaderCompilationResult Preprocess(string source, string fileName, ShaderKind shaderKind, string entry_point = "main") {
    //     return new ShaderCompilationResult(ShadercNativeMethods.shaderc_compile_into_preprocessed_text(Handle, source, (ulong)source.Length, (byte)shaderKind, fileName, entry_point, Options.Handle));
    // }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (Handle == IntPtr.Zero)
            return;

        if (disposing)
            Options.Dispose();
        else
            Console.WriteLine("[ShadercNET] Warning: ShaderCompiler disposed by finalyser. Use the Dispose method instead. ");

        ShadercNativeMethods.shaderc_compiler_release(Handle);
        Handle = IntPtr.Zero;
    }
}
