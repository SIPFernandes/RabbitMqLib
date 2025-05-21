
# latest RabbitMQ 3.13

Register the service as singleton

# no credentials (dev environment)
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management

# using credentials: (dont forget to change respective app.config)
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=installuser -e RABBITMQ_DEFAULT_PASS=Password rabbitmq:3.13-management

# check RabbitMQ management: http://localhost:15672/

1- Intall Docker Desktop
2- Run Docker Desktop
3- Run the above docker command
4- Run command to increase number of channels (max 100): channel_max = 100

#Add Docker Desktop to run at Startup

#Create a Task in the deployment Server Task Scheduler

1- Press Win+R and type taskschd.msc
2- Click on Create Task
3- Give Name "RabbitMQ Queue Start", click "Run whether user is logged on or not" and choose "Run with highest privileges" if needed
4- In Trigger Tab click New, set the trigger to "At startup" and check "Delay task for:" 5min or 10min
5- In Actions Tab click New, set the action to "Start a program", in "Program/script" box, type "cmd.exe" without the ", in "Add arguments" box, type:

/c docker_command

being "docker_command" one of the commands above to run RabbitMQ Queue

6- In Conditions Tab, uncheck everything
7- In Settings Tab, only check "Allow task to be run on demand", check "Run task as soon as possible..." and check "If task fails, restart every:" 5min
8- Press OK and to test, find the task in the Task Scheduler and Run it