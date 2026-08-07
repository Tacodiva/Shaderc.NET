using Cake.Core.IO.Arguments;

string target = Argument("target", "Build");

bool shadercBuildLinux = Argument("build-linux", true);
bool shadercBuildWindows = Argument("build-windows", true);
ulong shadercBuildCpuCount = Argument("build-cpus", (ulong)(Environment.ProcessorCount - 1));

string shadercVersion = Argument("shaderc-version", "v2026.3");
DirectoryPath shadercPath = Argument("shaderc-path", "Native");
DirectoryPath shadercOutput = Argument("shaderc-output", "Shaderc.NET");

const string shadercContainerImageDefaultName = "shaderc-build";
string shadercContainerImage = Argument("container-image", "");
DirectoryPath shadercContainerShadercPath = Argument("container-shaderc-path", "/shaderc");
DirectoryPath shadercContainerBuildPath = Argument("container-build-path", "/shaderc/build");

string[] sharedCMakeOptions = [
    "-D", "CMAKE_BUILD_TYPE=Release",
    "-D", "SHADERC_ENABLE_HLSL=0",
    "-D", "SHADERC_SKIP_EXAMPLES=1",
    "-D", "SHADERC_SKIP_EXECUTABLES=1",
    "-D", "SHADERC_SKIP_TESTS=1",
];

string[] linuxCMakeOptions = [
    ..sharedCMakeOptions,
];

string[] windowsCMakeOptions = [
    ..sharedCMakeOptions,

    "-D", "CMAKE_TOOLCHAIN_FILE=cmake/linux-mingw-toolchain.cmake",
    "-D", "MINGW_COMPILER_PREFIX=x86_64-w64-mingw32",
    "-D", "SHADERC_ENABLE_SHARED_CRT=1"
];

Task("Clean")
    .Does(() => {
        DotNetClean(".");

        DeleteDirectory(shadercPath, new() {
            Force = true,
            Recursive = true
        });
    });

Task("Clone")
    .Does(() => {
        if (!DirectoryExists(shadercPath)) {
            Information("Cloning Shaderc...");
            GitClone("https://github.com/google/shaderc.git", shadercPath);
        } else {
            Information("Fetching Shaderc...");
            GitFetch(shadercPath);
        }

        Information($"Checking out Shaderc {shadercVersion}...");
        GitCheckout(shadercPath, shadercVersion);
    });

static string Escape(string arg) => new QuotedArgument(new TextArgument(arg)).Render();
static string EscapeDir(DirectoryPath arg) => Escape(arg.ToString());
static string EscapeFile(FilePath arg) => Escape(arg.ToString());

Task("Build")
    .IsDependentOn("Clone")
    .Does((ctx) => {

        if (string.IsNullOrEmpty(shadercContainerImage)) {
            shadercContainerImage = shadercContainerImageDefaultName;

            Information($"Building image {shadercContainerImage}...");

            DockerBuild(
                new() {
                    Tag = [shadercContainerImageDefaultName],
                },
                "."
            );
        }

        Information($"Running container with image {shadercContainerImage}...");

        DockerContainerRunSettings settings = new() {
            Detach = true,
            Interactive = true,
            Cpus = shadercBuildCpuCount
        };
        string containerID = DockerRun(settings, shadercContainerImage, "/bin/bash");

        Information($"Started container {containerID}");

        try {
            DockerCp(EscapeDir(MakeAbsolute(shadercPath)), $"{containerID}:{EscapeDir(shadercContainerShadercPath)}");

            void CheckPackages(params string[] packages) {
                DockerExec(containerID, "apt-get", [
                        "install", "-y", "--no-install-recommends", "--no-upgrade",
                        ..packages
                    ]
                );
            }

            Information($"Checking shared dependencies are present...");
            CheckPackages(
                "ca-certificates",
                "build-essential",
                "cmake",
                "python3",
                "git"
            );

            Information($"Downloading third party dependencies...");
            DockerExec(containerID, "python3", EscapeFile(shadercContainerShadercPath.CombineWithFilePath("utils/git-sync-deps")));

            if (shadercBuildLinux) {
                Information($"Making Linux build system...");
                DockerExec(containerID, "rm", "-rf", EscapeDir(shadercContainerBuildPath));
                DockerExec(containerID, "mkdir", "-p", EscapeDir(shadercContainerBuildPath));
                DockerExec(
                    containerID, "cmake",
                    args: [
                        "-S", EscapeDir(shadercContainerShadercPath),
                        "-B", EscapeDir(shadercContainerBuildPath),
                        .. linuxCMakeOptions
                    ]
                );

                Information($"Building for Linux...");
                DockerExec(
                    containerID, "cmake",
                    args: [
                        "--build", EscapeDir(shadercContainerBuildPath),
                        "-j", $"{shadercBuildCpuCount}",
                    ]
                );

                CreateDirectory(shadercOutput.Combine("runtimes/linux-x64/native"));
                DockerCp(
                    $"{containerID}:{EscapeFile(shadercContainerBuildPath.CombineWithFilePath("libshaderc/libshaderc_shared.so"))}",
                    EscapeDir(MakeAbsolute(shadercOutput.Combine("runtimes/linux-x64/native"))),
                    new() {
                        FollowLink = true
                    }
                );
            }

            if (shadercBuildWindows) {
                Information($"Checking Windows dependencies are present...");
                CheckPackages("mingw-w64");

                Information($"Making Windows build system...");
                DockerExec(containerID, "rm", "-rf", EscapeDir(shadercContainerBuildPath));
                DockerExec(containerID, "mkdir", "-p", EscapeDir(shadercContainerBuildPath));
                DockerExec(
                    containerID, "cmake",
                    args: [
                        "-S", EscapeDir(shadercContainerShadercPath),
                        "-B", EscapeDir(shadercContainerBuildPath),
                        .. windowsCMakeOptions
                    ]
                );

                Information($"Building for Windows...");
                DockerExec(
                    containerID, "cmake",
                    args: [
                        "--build", EscapeDir(shadercContainerBuildPath),
                        "-j", $"{shadercBuildCpuCount}"
                    ]
                );

                CreateDirectory(shadercOutput.Combine("runtimes/win-x64/native"));

                void CopyLibraryToHost(FilePath src) {
                    DockerCp(
                        $"{containerID}:{EscapeFile(src)}",
                        EscapeDir(MakeAbsolute(shadercOutput.Combine("runtimes/win-x64/native"))),
                        new() { FollowLink = true }
                    );
                }

                CopyLibraryToHost(shadercContainerBuildPath.CombineWithFilePath("libshaderc/libshaderc_shared.dll"));
                CopyLibraryToHost("/usr/lib/gcc/x86_64-w64-mingw32/10-win32/libgcc_s_seh-1.dll");
                CopyLibraryToHost("/usr/lib/gcc/x86_64-w64-mingw32/10-win32/libstdc++-6.dll");
                CopyLibraryToHost("/usr/x86_64-w64-mingw32/lib/libwinpthread-1.dll");
            }
        } finally {
            Information($"Removing container...");
            DockerRm(new DockerContainerRmSettings() {
                Force = true
            }, containerID);
        }
    });

RunTarget(target);