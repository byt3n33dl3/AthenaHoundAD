from mythic_container.MythicCommandBase import *
from mythic_container.MythicRPC import *

class NNidhoggUnHideThreadArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line, **kwargs)
        self.args = [
            CommandParameter(
                name="id",
                type=ParameterType.Number,
                description="The thread ID to hide",
                parameter_group_info=[
                    ParameterGroupInfo(
                        required=True,
                        )
                    ],
            )
        ]

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            if self.command_line[0] == "{":
                self.load_args_from_json_string(self.command_line)
            else:
                self.add_arg("id", self.command_line)
        else:
            raise ValueError("Missing arguments")

class NNidhoggUnHideThreadCommand(CommandBase):
    cmd = "nidhogg-unhidethread"
    needs_admin = False
    help_cmd = """nidhogg-unhidethread 1234"""
    description = "Protects a process from being killed"
    version = 1
    script_only = True
    supported_ui_features = []
    author = "@idov31"
    argument_class = NNidhoggUnHideThreadArguments
    attackmapping = []
    attributes = CommandAttributes(
        supported_os=[SupportedOS.Windows],
        builtin=False,
        load_only=True
    )
    async def create_go_tasking(self, taskData: PTTaskMessageAllData) -> PTTaskCreateTaskingMessageResponse:
        response = PTTaskCreateTaskingMessageResponse(
            TaskID=taskData.Task.ID,
            Success=True,
        )

        resp = await MythicRPC().execute("create_subtask_group", tasks=[
            {"command": "nidhogg", "params": {"command":"unhidethread", "id":taskData.args.get_arg("id")}},
            ], 
            subtask_group_name = "nidhogg", parent_task_id=taskData.Task.ID)

        # We did it!
        return response

    async def process_response(self, response: AgentResponse):
        pass