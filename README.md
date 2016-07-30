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
