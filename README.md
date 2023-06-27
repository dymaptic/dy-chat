# dy-chat
AI Chat Interactions


Notes
ai-endpoint-temp is in a separate branch on purpose, We should not merge it. This will be replaced by the Azure AI deployment when we get into that and I get it working, but for now, this should work.

The entire schema to call this thing will likely change when we move to Azure AI, so just keep that in mind, you don't need to reference my data types b/c they won't be around for long, and they won't exist like this when we move to Azure AI, so just copy what you need for now and go with it.

Deployment
This has a github action that is setup to auto deploy when you push to the branch ai-endpoint-temp.

Authentication
You will see on the swagger page and in the example, that you need to specify an API key, you will find an API key saved in 1password.