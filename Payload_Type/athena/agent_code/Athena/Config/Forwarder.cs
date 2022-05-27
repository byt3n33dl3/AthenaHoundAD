﻿using Athena.Models.Mythic.Response;
using Athena.Models.Mythic.Tasks;
using PluginBase;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;


//This is for when an operator does not want to include a Forwarder into their agent.
namespace Athena.Config
{
    public class Forwarder
    {
        public bool connected { get; set; }
        public List<DelegateMessage> messageOut { get; set; }
        public ConcurrentQueue<string> queueIn { get; set; }

        public Forwarder()
        {
            this.connected = false;
            this.messageOut = new List<DelegateMessage>();
            this.queueIn = new ConcurrentQueue<string>();
        }

        //Link to the Athena SMB Agent
        public async Task<ResponseResult> Link(MythicJob job)
        {
            return new ResponseResult
            {
                status = "error",
                completed = "true",
                task_id = job.task.id,
                user_output = "No Forwarder Installed."
            };
        }
        public async Task<bool> ForwardDelegateMessage(DelegateMessage dm)
        {
            return false;
        }
        public async Task<List<DelegateMessage>> GetMessages()
        {
            return new List<DelegateMessage>();
        }

        //Unlink from the named pipe
        public void Unlink()
        {

        }
    }
}