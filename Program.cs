using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using NuGet.ProjectModel;

namespace AnalyzeDotNetProject
{
    class Program
    {
        static StreamWriter _logFileStream = null;
        static StreamWriter _resultFileStream = null;
        static StreamWriter _csvResultFileStream = null;
        static CsvWriter _csvResultWriter = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">
        ///  AnalyzeDotNetProject [<PROJECT>|<SOLUTION>] 
        /// </param>
        static void Main(string[] args)
        {
            _logFileStream = File.AppendText($"./logs-{DateTime.Now:yyy-MM-dd}.txt");
            try
            {
                // Replace to point to your project or solution
                string projectPath;
                if (IsDevelopment())
                {
                    projectPath = @"c:\development\jerriep\dotnet-outdated\DotNetOutdated.sln"; ;
                }
                else if (!args.Any())
                {
                    throw new Exception($"Solution or project file should be defined. The file should be the absolute path.");
                }
                else
                {
                    projectPath = args.First();
                }



                _resultFileStream = new StreamWriter("./result.txt");
                _csvResultFileStream = new StreamWriter("./result.csv");
                _csvResultWriter = new CsvWriter(_csvResultFileStream, CultureInfo.InvariantCulture);


                var dependencyGraphService = new DependencyGraphService();
                var dependencyGraph = dependencyGraphService.GenerateDependencyGraph(projectPath);

                _csvResultWriter.WriteHeader<ReportOutput>();
                _csvResultWriter.NextRecord();
                foreach (var project in dependencyGraph.Projects.Where(p =>
                             p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference))
                {
                    // Generate lock file
                    var lockFileService = new LockFileService();
                    var lockFile = lockFileService.GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);

                    WriteFlatFileResultLine(project.Name);

                    foreach (var targetFramework in project.TargetFrameworks)
                    {
                        WriteFlatFileResultLine($"  [{targetFramework.FrameworkName}]");

                        var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t =>
                            t.TargetFramework.Equals(targetFramework.FrameworkName));
                        if (lockFileTargetFramework != null)
                        {
                            foreach (var dependency in targetFramework.Dependencies)
                            {
                                var projectLibrary = lockFileTargetFramework.Libraries.First(library =>
                                    String.Equals(library.Name, dependency.Name, StringComparison.OrdinalIgnoreCase)
                                );

                                ReportDependency(projectLibrary,
                                    lockFileTargetFramework,
                                    1,
                                    project,
                                    targetFramework,
                                    new List<string>());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[ERR] {ex}");
                throw;
            }
            finally
            {
                if (_logFileStream != null)
                {
                    _logFileStream.Dispose();
                    _logFileStream = null;
                }

                if (_resultFileStream != null)
                {
                    _resultFileStream.Dispose();
                    _resultFileStream = null;
                }

                if (_csvResultWriter != null)
                {
                    _csvResultWriter.Dispose();
                    _csvResultWriter = null;
                }
                if (_csvResultFileStream != null)
                {
                    _csvResultFileStream.Dispose();
                    _csvResultFileStream = null;
                }
            }
        }
        private static void ReportDependency(
            LockFileTargetLibrary projectLibrary,
            LockFileTarget lockFileTargetFramework,
            int indentLevel,
            PackageSpec rootProject,
            TargetFrameworkInformation rootProjectTargetFramework,
            List<string> dependencyPath)
        {
            // Flat File
            WriteFlatFileResult(new String(' ', indentLevel * 2));
            WriteFlatFileResultLine($"{projectLibrary.Name}, v{projectLibrary.Version}");

            // Csv
            WriteCsvReport(new ReportOutput()
            {
                ProjectName = rootProject.Name,
                ProjectTargetFramework = rootProjectTargetFramework.FrameworkName.ToString(),
                PackageId = projectLibrary.Name,
                PackageVersion = projectLibrary.Version.ToString(),
                DependencyLevel = indentLevel,
                ParentPackage = dependencyPath.LastOrDefault(),
                DepdendencyPath = string.Join(" > ", dependencyPath)
            });

            var newDependencyPath = dependencyPath.Concat(new List<string>()
            {
                projectLibrary.Name
            }).ToList();

            foreach (var childDependency in projectLibrary.Dependencies)
            {
                var childLibrary = lockFileTargetFramework.Libraries.First(library => string.Equals(library.Name, childDependency.Id, StringComparison.OrdinalIgnoreCase));

                ReportDependency(childLibrary, lockFileTargetFramework, indentLevel + 1, rootProject, rootProjectTargetFramework, newDependencyPath);
            }
        }

        static bool IsDevelopment()
        {
            return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
        }
        static void WriteLog(string msg)
        {
            _logFileStream.WriteLine($"{DateTime.Now} {msg}");
            Console.WriteLine(msg);
        }
        static void WriteFlatFileResult(string msg)
        {
            // Flat File
            _resultFileStream.Write($"{msg}");
            Console.Write(msg);
        }
        static void WriteFlatFileResultLine(string msg)
        {
            // Flat File
            _resultFileStream.WriteLine($"{msg}");
            Console.WriteLine(msg);
        }
        static void WriteCsvReport(ReportOutput result)
        {

            // Csv file
            _csvResultWriter.WriteRecord(result);
            _csvResultWriter.NextRecord();
        }
    }
}
