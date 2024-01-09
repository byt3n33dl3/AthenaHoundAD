﻿using Agent.Interfaces;
using Agent.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent
{
    internal class ExecArgs
    {
        public int parent { get; set; } = 0;
        public string commandLine { get; set; } = "";
        public string spoofedcommandline { get; set; } = "";
        //public bool blockDlls { get; set; } = false;
        public bool output { get; set; } = false;
        //public bool spoofParent { get; set; } = false;

        public SpawnOptions getSpawnOptions(string task_id)
        {
            return new SpawnOptions()
            {
                parent = this.parent,
                commandline = this.commandLine,
                output = this.output,
                task_id = task_id,
                spoofedcommandline = this.spoofedcommandline
            };
        }
    }
}
