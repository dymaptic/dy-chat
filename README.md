# dy-chat
An ArcGIS plugin for generating Arcade expressions.

The plugin and server are open source and available free to use.

<img width="1200" alt="Screenshot 2024-03-07 084450" src="https://github.com/dymaptic/dy-chat/assets/126698247/72164c7c-a417-4b41-be9f-256568ac4a5a">


How its organized:

![image](https://github.com/dymaptic/dy-chat/assets/126698247/ab73a65f-ceb2-452f-b82a-6a143b2d15b3)


**dymaptic.AI.ChatService** is designed to run on Azure and connect/work with the AI Language model

**dymaptic.Chat.Server** runs in-between the ChatService and the client

**dymaptic.Chat.ArcGIS** is the ArcGIS plugin. This will require ArcGIS to build/run

**dymaptic.Chat.Shared** contains shared classes between the server and ArcGIS files
