// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.InteropServices;

namespace Shaderc;

public class ShaderCompilationResult : IDisposable {
    public IntPtr ResultHandle { get; private set; }

    internal ShaderCompilationResult(IntPtr handle) {
        this.ResultHandle = handle;
        if (handle == IntPtr.Zero)
            throw new ArgumentNullException(nameof(handle));
    }
    /// <summary>
    /// Returns the compilation status, indicating whether the compilation succeeded,
    /// or failed due to some reasons, like invalid shader stage or compilation
    /// errors.
    /// </summary>
    public ShaderCompilationStatus Status => ShadercNativeMethods.shaderc_result_get_compilation_status(ResultHandle);
    /// <summary>
    /// Returns the number of errors generated during the compilation.
    /// </summary>
    public uint ErrorCount => (uint)ShadercNativeMethods.shaderc_result_get_num_errors(ResultHandle);
    /// <summary>
    /// // Returns the number of warnings generated during the compilation.
    /// </summary>
    public uint WarningCount => (uint)ShadercNativeMethods.shaderc_result_get_num_warnings(ResultHandle);
    /// <summary>
    /// Returns a null-terminated string that contains any error messages generated
    /// during the compilation.
    /// </summary>
    public string ErrorMessage =>
            Marshal.PtrToStringAnsi(ShadercNativeMethods.shaderc_result_get_error_message(ResultHandle));

    /// <summary>
    /// Returns a pointer to the start of the compilation output data bytes, either
    /// SPIR-V binary or char string. When the source string is compiled into SPIR-V
    /// binary, this is guaranteed to be castable to a uint32_t*. If the result
    /// contains assembly text or preprocessed source text, the pointer will point to
    /// the resulting array of characters.
    /// </summary>
    public IntPtr CodePointer => ShadercNativeMethods.shaderc_result_get_bytes(ResultHandle);
    /// <summary>
    /// Returns the number of bytes of the compilation output data in a result object.
    /// </summary>
    public uint CodeLength => (uint)ShadercNativeMethods.shaderc_result_get_length(ResultHandle);
    /// <summary>
    /// Returns a span containing the bytes of the compilation output.
    /// </summary>
    public unsafe Span<byte> CodeSpan => new Span<byte>((void*)CodePointer, (int)CodeLength);
    /// <summary>
    /// Returns an array containing a copy of the bytes of the compilation output.
    /// </summary>
    public unsafe byte[] CodeArray => CodeSpan.ToArray();

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (ResultHandle == IntPtr.Zero)
            return;
        if (!disposing)
            Console.WriteLine("[ShadercNET] Warning: ShaderResult disposed by finalyser. Use the Dispose method instead. ");

        ShadercNativeMethods.shaderc_result_release(ResultHandle);
        ResultHandle = IntPtr.Zero;
    }
}
