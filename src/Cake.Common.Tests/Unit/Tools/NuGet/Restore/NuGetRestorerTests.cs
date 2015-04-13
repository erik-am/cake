﻿using Cake.Common.Tests.Fixtures.Tools.NuGet;
using Cake.Common.Tools.NuGet;
using Cake.Core.IO;
using NSubstitute;
using Xunit;

namespace Cake.Common.Tests.Unit.Tools.NuGet.Restore
{
    public sealed class NuGetRestorerTests
    {
        public sealed class TheRestoreMethod
        {
            [Fact]
            public void Should_Throw_If_Target_File_Path_Is_Null()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.TargetFilePath = null;

                // When
                var result = Record.Exception(() => fixture.Restore());

                // Then
                Assert.IsArgumentNullException(result, "targetFilePath");
            }

            [Fact]
            public void Should_Throw_If_Settings_Are_Null()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings = null;
                fixture.GivenDefaultToolDoNotExist();

                // When
                var result = Record.Exception(() => fixture.Restore());

                // Then
                Assert.IsArgumentNullException(result, "settings");
            }

            [Fact]
            public void Should_Throw_If_NuGet_Executable_Was_Not_Found()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.GivenDefaultToolDoNotExist();

                // When
                var result = Record.Exception(() => fixture.Restore());

                // Then
                Assert.IsCakeException(result, "NuGet: Could not locate executable.");
            }

            [Theory]
            [InlineData("C:/nuget/nuget.exe", "C:/nuget/nuget.exe")]
            [InlineData("./tools/nuget/nuget.exe", "/Working/tools/nuget/nuget.exe")]
            public void Should_Use_NuGet_Executable_From_Tool_Path_If_Provided(string toolPath, string expected)
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.ToolPath = toolPath;
                fixture.GivenCustomToolPathExist(expected);

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Is<FilePath>(p => p.FullPath == expected),
                    Arg.Any<ProcessSettings>());
            }

            [Fact]
            public void Should_Throw_If_Process_Was_Not_Started()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.GivenProcessCannotStart();

                // When
                var result = Record.Exception(() => fixture.Restore());

                // Then
                Assert.IsCakeException(result, "NuGet: Process was not started.");
            }

            [Fact]
            public void Should_Throw_If_Process_Has_A_Non_Zero_Exit_Code()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.GivenProcessReturnError();

                // When
                var result = Record.Exception(() => fixture.Restore());

                // Then
                Assert.IsCakeException(result, "NuGet: Process returned an error.");
            }

            [Fact]
            public void Should_Find_NuGet_Executable_If_Tool_Path_Not_Provided()
            {
                // Given
                var fixture = new NuGetRestorerFixture();

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Is<FilePath>(p => p.FullPath == "/Working/tools/NuGet.exe"),
                    Arg.Any<ProcessSettings>());
            }

            [Fact]
            public void Should_Add_Mandatory_Arguments()
            {
                // Given
                var fixture = new NuGetRestorerFixture();

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p => 
                        p.Arguments.Render() == "restore \"/Working/project.sln\" -NonInteractive"));
            }

            [Fact]
            public void Should_Add_RequireConsent_To_Arguments_If_True()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.RequireConsent = true;

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "restore \"/Working/project.sln\" -RequireConsent -NonInteractive"));
            }

            [Fact]
            public void Should_Add_PackageDirectory_To_Arguments_If_Set()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.PackagesDirectory = "./package";

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "restore \"/Working/project.sln\" -PackagesDirectory \"/Working/package\" -NonInteractive"));
            }

            [Fact]
            public void Should_Add_Sources_To_Arguments_If_Set()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.Source = new[] { "A;B;C" };

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "restore \"/Working/project.sln\" -Source \"A;B;C\" -NonInteractive"));
            }

            [Fact]
            public void Should_Add_NoCache_To_Arguments_If_True()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.NoCache = true;

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "restore \"/Working/project.sln\" -NoCache -NonInteractive"));
            }

            [Fact]
            public void Should_Add_DisableParallelProcessing_To_Arguments_If_Set()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.DisableParallelProcessing = true;

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "restore \"/Working/project.sln\" -DisableParallelProcessing -NonInteractive"));
            }

            [Theory]
            [InlineData(NuGetVerbosity.Detailed, "restore \"/Working/project.sln\" -Verbosity detailed -NonInteractive")]
            [InlineData(NuGetVerbosity.Normal, "restore \"/Working/project.sln\" -Verbosity normal -NonInteractive")]
            [InlineData(NuGetVerbosity.Quiet, "restore \"/Working/project.sln\" -Verbosity quiet -NonInteractive")]
            public void Should_Add_Verbosity_To_Arguments_If_Set(NuGetVerbosity verbosity, string expected)
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.Verbosity = verbosity;

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == expected));
            }

            [Fact]
            public void Should_Add_ConfigFile_To_Arguments_If_Set()
            {
                // Given
                var fixture = new NuGetRestorerFixture();
                fixture.Settings.ConfigFile = "./nuget.config";

                // When
                fixture.Restore();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "restore \"/Working/project.sln\" -ConfigFile \"/Working/nuget.config\" -NonInteractive"));
            }
        }
    }
}
