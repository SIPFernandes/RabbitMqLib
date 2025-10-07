
# latest RabbitMQ 3.13

Register the services as singleton:

    services.AddSingleton<RabbitMqService>();
    services.AddSingleton<IRabbitMqReceiverService>(provider =>
        provider.GetRequiredService<RabbitMqService>());
    services.AddSingleton<IRabbitMqSenderService>(provider =>
        provider.GetRequiredService<RabbitMqService>());

    services.AddSingleton<IRabbitMqSenderClient, RabbitMqSenderClient>();

    services.AddSingleton<RabbitMqReceiverClient>();
    services.AddHostedService(provider =>
        provider.GetRequiredService<RabbitMqReceiverClient>());

    services.AddScoped<IRabbitMqClient, ProcessQueueItemService>();

# no credentials (dev environment)

Add to appsettings: 

  "RabbitMQ": {
    "HostName": "localhost"
  },
  "SourceQueues": {
    "AdsQueue": ["Ads"],
    "SubscriptionQueue": ["Subscription"]
  },
  "TargetQueues": {
    "Topic": "TopicQueue",
    "Adt":  "AdtQueue"
  }

Run command:
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management

# using credentials: (dont forget to change respective app.config)

Add to appsettings:
  "RabbitMQ": {
    "UsingCredentials": "true",
    "UserName": "installuser",
    "Password": "Password",
    "HostName": "localhost"
  },
  "SourceQueues": {
    "AdsQueue": ["Ads"],
    "SubscriptionQueue": ["Subscription"]
  },
  "TargetQueues": {
    "Topic": "TopicQueue",
    "Adt":  "AdtQueue"
  }

Run command:
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=installuser -e RABBITMQ_DEFAULT_PASS=Password rabbitmq:3.13-management

# check RabbitMQ management: http://localhost:15672/

1- Intall Docker Desktop
2- Run Docker Desktop
3- Run the above docker command
4- Run command to increase number of channels (max 100): channel_max = 100

#Docker App vs IIS
- If your App is running on IIS then your Hostname is: localhost
- If your App is running in a Docker Container the Hostname is: rabbitmq (RabbitMQ container Name)
	-> Make sure both containers use same network
	 . docker network create mynetwork
	 . docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 --network mynetwork rabbitmq:3.13-management
	 . docker run -d --name IntCoreAds.Api --network mynetwork intcoreadsapi:dev


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