﻿using Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cursed.Finders
{
    public class EdgeFinder : IFinder
    {
        public string FindPath()
        {
            List<string> searchPaths;
            if (OperatingSystem.IsWindows())
            {
                searchPaths = new List<string>()
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "chrome.exe")
                };
            }
            else if (OperatingSystem.IsMacOS())
            {
                searchPaths = new List<string>()
                {
                    Path.Combine("Applications","Microsoft Edge.app","Contents","MacOS","Microsoft Edge")
                };
            }
            else if (OperatingSystem.IsLinux())
            {
                searchPaths = new List<string>()
                {
                };
            }
            else
            {
                return String.Empty;
            }

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
                Console.WriteLine(path + " doesn't exist.");
            }

            return String.Empty;
        }
    }
}