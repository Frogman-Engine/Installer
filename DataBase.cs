using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace Installer
{
    class DataBase
    {
        public static readonly string ABSLVersion = "20260107.1";
        public static readonly string AssimpVersion = "6.0.4";

        public static readonly string BoostUrl = "https://archives.boost.io/release/1.91.0/source/boost_1_91_0.zip";
        public static readonly string BoostVersion = "1.91.0";
        public static readonly string BoostDebugBuildB2Options = "architecture=x86 address-model=64 link=static runtime-link=static threading=multi variant=debug";
        public static readonly string BoostReleaseBuildB2Options = "architecture=x86 address-model=64 link=static runtime-link=static threading=multi variant=release";

        public static readonly string FrogmanEngineDeveloperGitHubProfileUrl = "https://github.com/Unknown-Stryker";
        public static readonly string FrogmanEngineGdkReleaseListUrl = "https://api.github.com/repos/UnknownStryker-Interactive-Technologies/Frogman-Engine/releases";
        public static readonly string FrogmanEngineThirdPartyFolderRelativePath = "SDK\\Third-Party\\Libraries";

        public static readonly string GDKInstallerVersion = "v2026.04.28";
        public static readonly string GDKSystemPathVariableNamePrefix = "FROGMAN_GDK_";
        public static readonly string GDKSystemPathVariableNameSuffix = "_PATH";
        public static readonly string GLFWUrl = "https://github.com/glfw/glfw/releases/download/3.4/glfw-3.4.bin.WIN64.zip";
        public static readonly string GLFWVersion = "3.4";

        public static readonly string LZ4Version = "1.10.0";

        public static readonly string ImGuiVersion = "1.91.6";

        public static readonly string VsWhereOptions = "-property installationPath";
        public static readonly string VsWhereUrl = "https://github.com/microsoft/vswhere/releases/download/3.1.7/vswhere.exe";

        public static string GenerateGDKSystemPathVariableName(string gdkversion)
        {
            return $"{GDKSystemPathVariableNamePrefix}{gdkversion}{GDKSystemPathVariableNameSuffix}";
        }
    }
}
