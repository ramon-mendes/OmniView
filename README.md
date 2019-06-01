# OmniView - Visual Studio HTML previewer for Sciter developers

![](http://misoftware.rs/Content/img/OmniView_viewer.png)

Source code is structured according to Mads Kristensen workflow (see [video](https://channel9.msdn.com/events/Build/2016/B886)).

**[Download from VS Gallery for VS 2015](https://marketplace.visualstudio.com/items?itemName=RamonFMendes.OmniView)**.

**[Download from VS Gallery for VS 2017](https://marketplace.visualstudio.com/items?itemName=RamonFMendes.OmniViewforVS2017)**.

**[More info here](http://misoftware.rs/Home/Post/OmniView)**


## Features

- supports VS2015, VS2017
- every time you save file (**CTRL + S**), viewer reloads the previewer, that is, loads again the corresponding .html or .tis file
- **console** area at the bottom of viewer which shows Sciter output/error messages (like CSS warnings)
- console has **REPL** prompt for evaluating TIScript code and showing the return value
- CTRL + E + V: toggles the previewer (and adds/removes the header textual flag)
- CTRL + E + B: toggles the console area
- you can detect whether your code is running inside OmniView's window by checking the following flags set in Sciter engine:
    - CSS: `@media omniview { .. }`
    - CSS: `[omniview] div { display: visible; }`
    - TIScript: `if(View.omniview) { .. }`