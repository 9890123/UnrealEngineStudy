@echo off

setlocal
echo Setting up Unreal Engine 4 project files...

if not exist "%~dp0..\..\Source" goto Error_BatchFileInWrongLocation

pushd "%~dp0..\..\Source"
if not exist ..\Build\BatchFiles\GenerateProjectFiles.bat goto Error_BatchFileInWrongLocation

if not exist ..\Build\BinaryPrerequisitesMarker.dat goto Error_MissingBinaryPrerequisites

call "%~dp0GetMSBuildPath.bat"
if errorlevel 1 goto Error_NoVisualStudioEnvironment

if not exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" goto NoVsWhere

set MSBUILD_15_EXE=
for /f "delims=" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath') do (
	if exist "%%i\MSBuild\15.0\Bin\MSBuild.exe" (
		set MSBUILD_15_EXE="%%i\MSBuild\15.0\Bin\MSBuild.exe"
		goto FoundMsBuild15
	)
)
:FoundMsBuild15

set MSBUILD_15_EXE_WITH_NUGET=
for /f "delims=" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest -products * -requires Microsoft.Component.MSBuild -requires Microsoft.VisualStudio.Component.NuGet -property installationPath') do (
	if exist "%%i\MSBuild\15.0\Bin\MSBuild.exe" (
		set MSBUILD_15_EXE_WITH_NUGET="%%i\MSBuild\15.0\Bin\MSBuild.exe"
		goto FoundMSBuild15WithNuget
	)
)
:FoundMsBuild15WithNuget

if not [%MSBUILD_EXE%] == [%MSBUILD_15_EXE%] goto NoVsWhere
if not [%MSBUILD_EXE%] == [%MSBUILD_15_EXE_WITH_NUGET%] goto Error_RequireNugetPackageManager

:NoVsWhere

md ..\Intermediate\Build >nul 2>nul
dir /s /b Programs\UnrealBuildTool\*cs >..\Intermediate\Build\UnrealBuildToolFiles.txt

if not exist ..\Platforms goto NoPlatforms
for /d %%D in (..\Platforms) do (
	if exist %%D\Source\Programs\UnrealBuildTool dir /s /b %%D\Source\Programs\UnrealBuildTool\*.cs >> ..\Intermediate\Build\UnrealBuildToolFiles.txt
)
:NoPlatforms

if not exist ..\Restricted goto NoRestricted
for /d %%D in (..\Restricted\*) do (
	if exist %%D\Source\Programs\UnrealBuildTool dir /s /b %%D\Source\Programs\UnrealBuildTool\*.cs >> ..\Intermediate\Build\UnrealBuildToolFiles.txt
)
:NoRestricted

fc /b ..\Intermediate\Build\UnrealBuildToolFiles.txt ..\Intermediate\Build\UnrealBuildToolPrevFiles.txt >nul 2>nul
if not errorlevel 1 goto skipClean
copy /y ..\Intermediate\Build\UnrealBuildToolFiles.txt ..\Intermediate\Build\UnrealBuildToolPrevFiles.txt >nul
%MSBUILD_EXE% /nologo /verbosity:quiet Programs\UnrealBuildTool\UnrealBuildTool.csproj /property:Configuration=Development /property:Platform=AnyCPU /target:Clean
:SkipClean
%MSBUILD_EXE% /nologo /verbosity:quiet Programs\UnrealBuildTool\UnrealBuildTool.csproj /property:Configuration=Development /property:Platform=AnyCPU /target:Build
if errorlevel 1 goto Error_UBTCompileFailed

..\Binaries\DotNET\UnrealBuildTool.exe -ProjectFiles %*
if errorlevel 1 goto Error_ProjectGenerationFailed

popd
exit /B 0

:Error_BatchFileInWrongLocation
echo.
echo GenerateProjectFiles ERROR: The batch file does not appear to be located in the /Engine/Build/BatchFiles directory. This script must be run from within that directory.
echo.
pause
goto Exit

:Error_MissingBinaryPrerequisites
echo.
echo GenerateProjectFiles ERROR: It looks like you're missing some files that are required in order to generate projects. Please check that you've downloaded and unpacked the engine source code, binaries, content and third-party dependencies before running this script.
echo.
pause
goto Exit

:Error_NoVisualStudioEnvironment
echo.
echo GenerateProjectFiles ERROR: Unable to find a valid installation of Visual Studio. Please check that you have Visual Studio 2017 or Visual Studio 2019 installed, and the MSBuild component is selected as part of your installation.
echo.
pause
goto Exit

:Error_RequireNugetPackageManager
echo.
echo UE4 requires the NuGet Package Manager to be installed to use %MSBUILD_EXE%. Please run the Visual Studio Installer and add it from the individual components list (in the 'Code Tools' category).
echo.
pause
goto Exit

:Error_UBTCompileFailed
echo.
echo GenerateProjectFiles ERROR: UnrealBuildTool failed to compile.
echo.
pause
goto Exit

:Error_ProjectGenerationFailed
echo.
echo GenerateProjectFiles ERROR: UnrealBuildTool was unable to generate project files.
echo.
pause
goto Exit

:Exit
popd
exit /B 1
