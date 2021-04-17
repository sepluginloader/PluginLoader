# PluginLoader
A tool to load plugins for Space Engineers automatically.

## Installation
#### Workshop
To install via the workshop, subscribe to the [workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=2407984968) and add `-plugin ..\..\..\workshop\content\244850\2407984968\RunPluginLoader` to the [game launch options](https://support.steampowered.com/kb_article.php?ref=1040-JWMT-2947).

#### Manual
To install without the workshop, the files must be copied into the game folder manually.
1. Download and extract the 0Harmony.dll and PluginLoader.dll files from the Releases page. Make sure the files are not blocked before continuing by opening the file properties of each of the extracted files and checking the Unblock box if it exists. 
2. The extracted files can now be placed in the game Bin64 folder. You can find the Bin64 folder by right clicking on Space Engineers and selecting Properties. Then under the Local Files tab, select Browse and navigate to the Bin64 folder. 
3. Add the Plugin Loader to the [game launch options](https://support.steampowered.com/kb_article.php?ref=1040-JWMT-2947) using the following string: `-plugin PluginLoader.dll`
