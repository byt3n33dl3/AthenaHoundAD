﻿using Athena.Models;
using Athena.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Plugins
{
    public class Tail : AthenaPlugin
    {
        public override string Name => "tail";
        public override void Execute(Dictionary<string, string> args)
        {
            if (!args.ContainsKey("path") || string.IsNullOrEmpty(args["path"].ToString()))
            {
                PluginHandler.Write("Please specify a path!", args["task-id"], true, "error");
                return;
            }
            string path = args["path"].ToString();
            int lines = 5;
            if (args.ContainsKey("lines"))
            {
                try
                {
                    lines = int.Parse(args["lines"]);
                }
                catch
                {
                    lines = 5;
                }
            }
            try
            {
                List<string> text = File.ReadLines(path).Reverse().Take(lines).ToList();
                text.Reverse();

                PluginHandler.AddResponse(new ResponseResult
                {
                    completed = "true",
                    user_output = string.Join(Environment.NewLine, text),
                    task_id = args["task-id"],
                });
            }
            catch (Exception e)
            {
                PluginHandler.Write(e.ToString(), args["task-id"], true, "error");
            }
        }
    }

}