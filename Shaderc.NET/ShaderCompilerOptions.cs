// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Shaderc;

public class ShaderCompilerOptions : IDisposable, ICloneable {
    public IntPtr Handle { get; private set; }
    static int curId;//id counter
    readonly internal int id;//dic key
    readonly internal bool includeEnabled;

    /// <summary>
    /// The resolver used to find shaders included by scripts.
    /// </summary>
    private IShaderIncludeResolver includeResolver;
    public IShaderIncludeResolver IncludeResolver {
        get {
            return includeResolver == null ? IShaderIncludeResolver.Default : includeResolver; ;
        }
        set {
            includeResolver = value;
        }
    }

    static internal Dictionary<int, ShaderCompilerOptions> optionsDic = new Dictionary<int, ShaderCompilerOptions>();//context data for callbacks

    /// <summary>
    /// Create a new instance of the Options class.
    /// </summary>
    /// <param name="includeResolver">The resolver used to find shaders included by scripts.</param>
    public ShaderCompilerOptions(IShaderIncludeResolver includeResolver) : this(true) {
        this.includeResolver = includeResolver;
    }

    /// <summary>
    /// Create a new instance of the Options class.
    /// </summary>
    /// <param name="enableIncludes">If set to 'true' include resolution is activated.</param>
    public ShaderCompilerOptions(bool enableIncludes = true) : this(ShadercNativeMethods.shaderc_compile_options_initialize(), enableIncludes) { }

    ShaderCompilerOptions(IntPtr handle, bool enableIncludes) {
        this.Handle = handle;
        if (handle == IntPtr.Zero)
            throw new Exception("error");
        id = curId++;
        optionsDic.Add(id, this);
        includeEnabled = enableIncludes;
        if (enableIncludes)
            SetIncludeCallbacks();
    }

    static internal PFN_IncludeResolve resolve = HandlePFN_IncludeResolve;
    static internal PFN_IncludeResultRelease incResultRelease = HandlePFN_IncludeResultRelease;

    static IntPtr HandlePFN_IncludeResolve(IntPtr userData, string requestedSource, int type, string requestingSource, UIntPtr includeDepth) {

        ShaderCompilerOptions opts = optionsDic[userData.ToInt32()];
        string content = "", incFile = "";

        if (!opts.IncludeResolver.TryFindInclude(requestingSource, requestedSource, (ShaderIncludeType)type, out incFile, out content)) {
            content = "";
            incFile = "";
        }

        ShaderIncludeResult result = new ShaderIncludeResult(incFile, content, userData.ToInt32());
        IntPtr irPtr = Marshal.AllocHGlobal(Marshal.SizeOf<ShaderIncludeResult>());
        Marshal.StructureToPtr(result, irPtr, true);
        return irPtr;
    }

    static void HandlePFN_IncludeResultRelease(IntPtr userData, IntPtr includeResult) {
        Marshal.PtrToStructure<ShaderIncludeResult>(includeResult).FreeStrings();
        Marshal.FreeHGlobal(includeResult);
    }

    void SetIncludeCallbacks() {
        ShadercNativeMethods.shaderc_compile_options_set_include_callbacks(Handle, Marshal.GetFunctionPointerForDelegate(resolve),
            Marshal.GetFunctionPointerForDelegate(incResultRelease), (IntPtr)id);
    }

    /// <summary>
    /// Returns a copy of the given shaderc Options.
    /// </summary>
    /// <returns>The clone.</returns>
    public object Clone() => new ShaderCompilerOptions(ShadercNativeMethods.shaderc_compile_options_clone(Handle), includeEnabled);
    /// <summary>
    /// Adds a predefined macro to the compilation options. This has the same
    /// effect as passing -Dname=value to the command-line compiler.
    /// </summary>
    /// <remarks>
    /// If value is NULL, it has the same effect as passing -Dname to the command-line
    /// compiler. If a macro definition with the same name has previously been
    /// added, the value is replaced with the new value. The macro name and
    /// value are passed in with char pointers, which point to their data, and
    /// the lengths of their data. The strings that the name and value pointers
    /// point to must remain valid for the duration of the call, but can be
    /// modified or deleted after this function has returned. In case of adding
    /// a valueless macro, the value argument should be a null pointer or the
    /// value_length should be 0u.
    /// </remarks>
    /// <param name="name">Name.</param>
    /// <param name="value">Value.</param>
    public void AddMacroDefinition(string name, string value = null) => ShadercNativeMethods.shaderc_compile_options_add_macro_definition(
        Handle, name, (ulong)name.Length, value, string.IsNullOrEmpty(value) ? 0 : (ulong)value.Length);
    /// <summary>
    /// Sets the source language.  The default is GLSL.
    /// </summary>
    public ShaderSourceLanguage SourceLanguage { set => ShadercNativeMethods.shaderc_compile_options_set_source_language(Handle, value); }
    /// <summary>
    /// Sets the compiler optimization level to the given level.
    /// </summary>
    public ShaderOptimizationLevel Optimization { set => ShadercNativeMethods.shaderc_compile_options_set_optimization_level(Handle, value); }
    /// <summary>
    /// Sets the compiler mode to generate debug information in the output.
    /// </summary>
    public void EnableDebugInfo() => ShadercNativeMethods.shaderc_compile_options_set_generate_debug_info(Handle);
    /// <summary>
    /// Sets the compiler mode to suppress warnings, overriding warnings-as-errors
    /// mode. When both suppress-warnings and warnings-as-errors modes are
    /// turned on, warning messages will be inhibited, and will not be emitted
    /// as error messages.
    /// </summary>
    public void DisableWarnings() => ShadercNativeMethods.shaderc_compile_options_set_suppress_warnings(Handle);
    /// <summary>
    /// Forces the GLSL language version and profile to a given pair. The version
    /// number is the same as would appear in the #version annotation in the source.
    /// Version and profile specified here overrides the #version annotation in the
    /// source. Use profile: 'shaderc_profile_none' for GLSL versions that do not
    /// define profiles, e.g. versions below 150.
    /// </summary>
    public void ForceVersionAndProfile(int version, ShaderProfile profile) =>
        ShadercNativeMethods.shaderc_compile_options_set_forced_version_profile(Handle, version, profile);
    /// <summary>
    /// Sets the target shader environment, affecting which warnings or errors will
    /// be issued.  The version will be for distinguishing between different versions
    /// of the target environment.  The version value should be either 0 or
    /// a value listed in shaderc_env_version.  The 0 value maps to Vulkan 1.0 if
    /// |target| is Vulkan, and it maps to OpenGL 4.5 if |target| is OpenGL.
    /// </summary>
    public void SetTargetEnvironment(ShaderTargetEnvironment target, ShaderEnvironmentVersion version) =>
        ShadercNativeMethods.shaderc_compile_options_set_target_env(Handle, target, version);
    /// <summary>
    /// Sets the target SPIR-V version. The generated module will use this version
    /// of SPIR-V.  Each target environment determines what versions of SPIR-V
    /// it can consume.  Defaults to the highest version of SPIR-V 1.0 which is
    /// required to be supported by the target environment.  E.g. Default to SPIR-V
    /// 1.0 for Vulkan 1.0 and SPIR-V 1.3 for Vulkan 1.1.
    /// </summary>
    public SpirVVersion TargetSpirVVersion {
        set => ShadercNativeMethods.shaderc_compile_options_set_target_spirv(Handle, value);
    }
    /// <summary>
    /// Sets the compiler mode to treat all warnings as errors. Note the
    /// suppress-warnings mode overrides this option, i.e. if both
    /// warning-as-errors and suppress-warnings modes are set, warnings will not
    /// be emitted as error messages.
    /// </summary>
    public void EnableWarningsAsErrors() =>
        ShadercNativeMethods.shaderc_compile_options_set_warnings_as_errors(Handle);
    /// <summary>
    /// Sets a resource limit.
    /// </summary>
    public void SetLimit(ShaderLimit limit, int value) =>
        ShadercNativeMethods.shaderc_compile_options_set_limit(Handle, limit, value);
    /// <summary>
    /// Sets whether the compiler should automatically assign bindings to uniforms
    /// that aren't already explicitly bound in the shader source.
    /// </summary>
    public bool AutoBindUniforms {
        set => ShadercNativeMethods.shaderc_compile_options_set_auto_bind_uniforms(Handle, value);
    }
    /// <summary>
    /// Sets whether the compiler should use HLSL IO mapping rules for bindings.
    /// Defaults to false.
    /// </summary>
    public bool HlslIoMapping {
        set => ShadercNativeMethods.shaderc_compile_options_set_hlsl_io_mapping(Handle, value);
    }
    /// <summary>
    /// Sets whether the compiler should determine block member offsets using HLSL
    /// packing rules instead of standard GLSL rules.  Defaults to false.  Only
    /// affects GLSL compilation.  HLSL rules are always used when compiling HLSL.
    /// </summary>
    public bool HlslOffsets {
        set => ShadercNativeMethods.shaderc_compile_options_set_hlsl_offsets(Handle, value);
    }
    /// <summary>
    /// Sets the base binding number used for for a uniform resource type when
    /// automatically assigning bindings.  For GLSL compilation, sets the lowest
    /// automatically assigned number.  For HLSL compilation, the regsiter number
    /// assigned to the resource is added to this specified base.
    /// </summary>
    public void SetBindingBase(ShaderUniformKind kind, UInt32 _base) =>
        ShadercNativeMethods.shaderc_compile_options_set_binding_base(Handle, kind, _base);
    /// <summary>
    /// Sets the base binding number used for for a uniform resource type when
    /// automatically assigning bindings when compiling a given shader stage.
    /// For GLSL compilation, sets the lowest automatically assigned number.  For HLSL compilation, the regsiter number
    /// assigned to the resource is added to this specified base.
    /// The stage is assumed to be one of vertex, fragment, tessellation evaluation, tesselation control, geometry, or compute.
    /// </summary>
    public void SetBindingBase(ShaderKind shaderKind, ShaderUniformKind kind, UInt32 _base) =>
        ShadercNativeMethods.shaderc_compile_options_set_binding_base_for_stage(Handle, shaderKind, kind, _base);
    /// <summary>
    /// Sets whether the compiler should automatically assign locations to
    /// uniform variables that don't have explicit locations in the shader source.
    /// </summary>
    public bool AutoMapLocations {
        set => ShadercNativeMethods.shaderc_compile_options_set_auto_map_locations(Handle, value);
    }
    /// <summary>
    /// Sets a descriptor set and binding for an HLSL register in the given stage.
    /// This method keeps a copy of the string data.
    /// </summary>
    public void SetHlslRegisterSetAndBinding(ShaderKind shaderKind, string reg, string set, string binding) =>
        ShadercNativeMethods.shaderc_compile_options_set_hlsl_register_set_and_binding_for_stage(Handle, shaderKind, reg, set, binding);
    /// <summary>
    /// Sets a descriptor set and binding for an HLSL register for all shader stages.
    /// This method keeps a copy of the string data.
    /// </summary>
    public void SetHlslRegisterSetAndBinding(string reg, string set, string binding) =>
        ShadercNativeMethods.shaderc_compile_options_set_hlsl_register_set_and_binding(Handle, reg, set, binding);

    /// <summary>
    /// Sets whether the compiler should enable extension
    /// SPV_GOOGLE_hlsl_functionality1.
    /// </summary>
    public bool HlslFunctionality1 {
        set => ShadercNativeMethods.shaderc_compile_options_set_hlsl_functionality1(Handle, value);
    }
    /// <summary>
    /// Sets whether the compiler should invert position.Y output in vertex shader.
    /// </summary>
    public bool InvertY { set => ShadercNativeMethods.shaderc_compile_options_set_invert_y(Handle, value); }
    /// <summary>
    /// Sets whether the compiler generates code for max and min builtins which,
    /// if given a NaN operand, will return the other operand. Similarly, the clamp
    /// builtin will favour the non-NaN operands, as if clamp were implemented
    /// as a composition of max and min.
    /// </summary>
    public bool NanClamp {
        set => ShadercNativeMethods.shaderc_compile_options_set_nan_clamp(Handle, value);
    }


    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (Handle == IntPtr.Zero)
            return;
        if (disposing)
            optionsDic.Remove(id);
        else
            Console.WriteLine("[ShadercNET] Warning: ShaderCompilerOptions disposed by finalyser. Use the Dispose method instead. ");

        ShadercNativeMethods.shaderc_compile_options_release(Handle);
        Handle = IntPtr.Zero;
    }
}