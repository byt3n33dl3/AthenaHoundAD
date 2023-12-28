﻿using Agent.Interfaces;
using Agent.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent
{
    public class ProcessRunner
    {
        private Process process;
        private Thread outputThread;
        private string task_id;
        private IMessageManager messageManager;
        public ProcessRunner(string command, string task_id, IMessageManager messageManager) {
            this.messageManager = messageManager;
            this.task_id = task_id;
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = ""
                }
            };
        }
        public void Start()
        {
            this.process.ErrorDataReceived += (sender, errorLine) => { if (errorLine.Data is not null) messageManager.Write(errorLine.Data + Environment.NewLine, this.task_id, false, "error"); };
            this.process.OutputDataReceived += (sender, outputLine) => { if (outputLine.Data is not null) messageManager.Write(outputLine.Data + Environment.NewLine, this.task_id, false); };
            this.process.Exited += Process_Exited;
            this.process.Start();
            this.process.BeginErrorReadLine();
            this.process.BeginOutputReadLine();

            this.process.WaitForExit();
        }

        public void Stop()
        {
            if (!this.process.HasExited)
            {
                this.process.Kill(true);
                this.process.Dispose();
            }
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            this.messageManager.AddResponse(new ResponseResult()
            {
                user_output = Environment.NewLine + "Process Finished.",
                task_id = this.task_id,
                completed = true,
                status = this.process.ExitCode == 0 ? "success" : "error"
            });
        }

        public void Write(byte[] input)
        {
            process.StandardInput.Write(input);
        }
        public void Write(string input)
        {
            process.StandardInput.WriteLine(input);
        }
        public void Write(byte input)
        {
            process.StandardInput.Write(input);
        }

    }
}