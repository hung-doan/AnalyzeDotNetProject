using System;
using System.Collections.Generic;
using System.Text;

namespace AnalyzeDotNetProject
{
    public class ReportOutput
    {
        public string ProjectName { get; set; }
        public string ProjectTargetFramework { get; set; }
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public int DependencyLevel { get; set; }
        public string ParentPackage { get; set; }
        public string DepdendencyPath { get; set; }

    }
}
