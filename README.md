#C# Wrapper for [WarfaceBot](https://github.com/Levak/warfacebot/)

##Usage
1. Copy files into your **existing project** or create a **shared project** (*only available on VS2015+*)
2. Download [WarfaceBot](https://github.com/Levak/warfacebot/) and compile it for a system you wish (Wrapper works under Mono as well).
**As of moment of writing, wrapper is intended to work with WarfaceBot compiled in debug mode.**
3. Go to **bin** folder of your solution - there you will find folders **Debug** and **Release**. If you haven't one, just run your project with the respective configuration.
4. Create folders called **bot** inside **Debug** and **Release** folders and copy **all** content from compiled WarfaceBot folder (*yes, even source folders*) into them.
5. In file properties of **wrapper.sh** enable copying in output directory
6. Add a code like
```C#
var bot = new Bot("youremail", "yourpassword", "eu", "");
new Task(bot.Run).Start();
```
This will run main bot's thread in a new Task. To "talk" to bot, use 2 queues: **Output** (to read output from bot) and **Input** (to send commands). It is preferrable to set separate tasks to manage queues.

##Sending own (user-input) queries
When you send a query to **Input** queue, it is expected to be a valid XMPP query. However, you can use these templates in your query which will be parsed automatically before actually sending a query to WarfaceBot:
- **{{QUERY_UID}}** - will be replaced by unique query id
- **{{NICKNAME}}** - will be replaced by a nickname of currently running account.

**Use templates ONLY after you receive *"ENABLED"* command in Output query, otherwise they WILL NOT be parsed! (Wrapper, for example, doesn't know a NICKNAME of account immediately after start)**