
<p align="center">
  <a href="https://www.nuget.org/packages/shaderc.net"><img src="https://buildstats.info/nuget/shaderc.net"></a>
  <a href="https://travis-ci.org/jpbruyere/shaderc.net">
      <img src="https://img.shields.io/travis/jpbruyere/shaderc.net.svg?&logo=travis&logoColor=white">
  </a>
  <a href="https://ci.appveyor.com/project/jpbruyere/shaderc-net">
    <img src="https://img.shields.io/appveyor/ci/jpbruyere/shaderc-net?logo=appveyor&logoColor=lightgrey">
  </a>
  <a href="https://www.paypal.me/GrandTetraSoftware">
    <img src="https://img.shields.io/badge/Donate-PayPal-green.svg">
  </a>
</p>

# Shaderc.NET

This is a fork of [shaderc.net](https://github.com/jpbruyere/shaderc.net) for my game engine.

Here's some of the things I changed:
 - Capitalized a lot of stuff.
 - Made it build for net 7.0.
 - Added Span methods to `Result`.
 - Renamed things to be less generic (like `Compiler` is now `ShaderCompiler` and `Result` is now `ShaderCompilationResult`).
 - Removed a lot of the build stuff.