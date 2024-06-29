# Extensions
Extensions are located at `%appdata%/OrangemiumIDE/extensions`, if you add or remove an extension, IDE needs to be restarted.
## Creating an extension
* First you should create an folder for your extension, lets call it "testExtension". The location would be `%appdata%/OrangemiumIDE/extensions/testExtension`.
* Second, you should create 1 file: "extension.json", and make an icon.
### extension.json
```
{
  "Name": "(name of your extension)",
  "ShortDescription": "(short explaination of your extension, visible at Settings>Extensions)",
  "LongDescription": "(long explaination of your extension, currently not visible at anywhere)",
  "Icon": "(relative location of your icon, like Icon.png)",
  "Version": "(version string of your extension)",
  "VersionCode": "(version code of your extension)"
  
  "Debuggers": {
    "(ID of the debugger, lets say BATCH_DEBUGGER)": {
      "Name": "Batch Debugger (Name of the debugger)",
      "Available":"FileType=.bat,.cmd|Platform=Windows",
      "Comment1":"FileType is required, but Platform isn't (will be available for all), multiple items should be separated with a comma",
      "Windows (The platform, Windows, MacOS, Linux, Global)": {
        "DebugLogger":"InApp",
        "Executable":"cmd.exe (the executable you need to execute)",
        "Args": "/c %FILE% (args that you need)"
      }
      "Compiler": { "Comment2": "OPTIONAL, as an example, dotnet compiler will be shown. I know its not realted but I wanted to keep this short",
        "Global (platform)": 
        {
          "Executable":"dotnet",
          "Args": "build %FILE% /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Debug /p:Platform=\"AnyCPU\""
        }
      }
    }
  }
  "ExecutableInteractions": {
    "Windows (platform)": {
      "OIDEBatchTools.exe (the executable, relative to extension path)": {
        "codeTools": ".bat,.cmd (file types that IDE will ask this extension executable, like completetion and problems)"
      }
    }
  }
}
```
### Example extensions
[batchExtension.zip](https://github.com/user-attachments/files/16039101/batchExtension.zip)
[testExtension.zip](https://github.com/user-attachments/files/16039102/testExtension.zip)

