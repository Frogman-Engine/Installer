using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
 The MIT License

Copyright (c) 2025 by UNKNOWN STRYKER

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/




namespace Installer
{
    class DataBase
    {
        public static readonly string ABSLVersion = "20250814.1";
        public static readonly string AssimpVersion = "6.0.4";

        public static readonly string BoostUrl = "https://github.com/boostorg/boost/releases/download/boost-1.87.0/boost-1.87.0-cmake.zip";
        public static readonly string BoostVersion = "1.87.0";
        public static readonly string BoostDebugBuildB2Options = "architecture=x86 address-model=64 link=static runtime-link=static threading=multi variant=debug";
        public static readonly string BoostReleaseBuildB2Options = "architecture=x86 address-model=64 link=static runtime-link=static threading=multi variant=release";

        public static readonly string FrogmanEngineDeveloperGitHubProfileUrl = "https://github.com/Unknown-Stryker";
        public static readonly string FrogmanEngineGdkReleaseListUrl = "https://api.github.com/repos/Project-Frogman/Frogman-Engine/releases";
        public static readonly string FrogmanEngineThirdPartyFolderRelativePath = "SDK\\Third-Party\\Libraries";

        public static readonly string GDKInstallerVersion = "v2026.04.05";
        public static readonly string GDKSystemPathVariableNamePrefix = "FROGMAN_GDK_";
        public static readonly string GDKSystemPathVariableNameSuffix = "_PATH";
        public static readonly string GLFWUrl = "https://github.com/glfw/glfw/releases/download/3.4/glfw-3.4.bin.WIN64.zip";
        public static readonly string GLFWVersion = "3.4";

        public static readonly string LZ4Version = "1.10.0";

        public static readonly string ImGuiVersion = "1.91.6";

        public static readonly string SIMD_JSON_Version = "4.2.1";

        public static readonly string VsWhereOptions = "-property installationPath";
        public static readonly string VsWhereUrl = "https://github.com/microsoft/vswhere/releases/download/3.1.7/vswhere.exe";

        public static string GenerateGDKSystemPathVariableName(string gdkversion)
        {
            return $"{GDKSystemPathVariableNamePrefix}{gdkversion}{GDKSystemPathVariableNameSuffix}";
        }
    }
}
