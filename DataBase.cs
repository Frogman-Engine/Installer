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
        private static string gdkInstallerVersion = "v2025.02.24";
        public static string GDKInstallerVersion
        {
            get { return gdkInstallerVersion; }
        }

        private static string frogmanEngineThirdPartyFolderRelativePath = "SDK\\Third-Party\\Libraries";
        public static string FrogmanEngineThirdPartyFolderRelativePath
        {
            get { return frogmanEngineThirdPartyFolderRelativePath; }
        }

        private static string frogmanEngineDeveloperGitHubProfileUrl = "https://github.com/Unknown-Stryker";
        public static string FrogmanEngineDeveloperGitHubProfileUrl
        {
            get { return frogmanEngineDeveloperGitHubProfileUrl; }
        }   

        private static string vswhereUrl = "https://github.com/microsoft/vswhere/releases/download/3.1.7/vswhere.exe";
        public static string VsWhereUrl
        {
            get { return vswhereUrl; }
        }
        private static string vswhereOptions = "-property installationPath";
        public static string VsWhereOptions
        {
            get { return vswhereOptions; }
        }

        private static string cmakeUrl = "https://github.com/Kitware/CMake/releases/download/v3.31.5/cmake-3.31.5-windows-x86_64.msi";
        public static string CMakeUrl
        {
            get { return cmakeUrl; }
        }

        private static string frogmanEngineGdkReleaseListUrl = "https://api.github.com/repos/Project-Frogman/Frogman-Engine/releases";
        public static string FrogmanEngineGdkReleaseListUrl
        {
            get { return frogmanEngineGdkReleaseListUrl; }
        }

        private static string boostUrl = "https://github.com/boostorg/boost/releases/download/boost-1.87.0/boost-1.87.0-cmake.zip";
        public static string BoostUrl
        {
            get { return boostUrl; }
        }
        private static string boostVersion = "1.87.0";
        public static string BoostVersion
        {
            get { return boostVersion; }
        }
        private static string boostDebugBuildB2Options = "architecture=x86 address-model=64 link=static runtime-link=static threading=multi variant=debug";
        public static string BoostDebugBuildB2Options
        {
            get { return boostDebugBuildB2Options; }
        }
        private static string boostReleaseBuildB2Options = "architecture=x86 address-model=64 link=static runtime-link=static threading=multi variant=release";
        public static string BoostReleaseBuildB2Options
        {
            get { return boostReleaseBuildB2Options; }
        }

        private static string imGuiVersion = "1.91.6";
        public static string ImGuiVersion
        {
            get { return imGuiVersion; }
        }

        private static string gdkSystemPathVariableNamePrefix = "FROGMAN_GDK_";
        public static string GDKSystemPathVariableNamePrefix
        {
            get { return gdkSystemPathVariableNamePrefix; }
        }
        private static string gdkSystemPathVariableNameSuffix = "_PATH";
        public static string GDKSystemPathVariableNameSuffix
        {
            get { return gdkSystemPathVariableNameSuffix; }
        }
        public static string GenerateGDKSystemPathVariableName(string gdkversion)
        {
            return $"{gdkSystemPathVariableNamePrefix}{gdkversion}{gdkSystemPathVariableNameSuffix}";
        }
    }
}
