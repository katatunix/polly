# Polly - a tool for monitoring cryptocurrency miners

Run this tool on your mining rig, when any of predefined errors indicators happen (e.g., `fan=0%` or `got incorrect share`) with your miner, a predefined action will be automatically executed.

## Features
* Continuously check console output of the miner to detect errors, and based on that the tool will fire an action which is specified in a file called `fire.bat`.
* Start miner again when it exits/crashes, fire the action if the exiting happens too quickly.
* If miner has not printed out anything to console for 5 minutes, then fire the action.
* Periodically check new public IP address of the rig.
* All important events (fire, exit, new IP) will be sent to a list of subscribed email addresses.

## Usage
* Copy folder `/release/polly` to your rig.
* Open file `fire.bat` to specify your action, typically it is a call to restart the rig.
* Open file `config.json` and modify it following the template below:
```
{
    "MinerPath" : "D:/Claymores/EthDcrMiner64.exe",
    "MinerArgs" : "",
    "Polly" : {
        "SmtpHost" : "smtp.gmail.com",
        "SmtpPort" : 587,
        "Address" : "pollymonitor2@gmail.com",
        "Password" : "test",
        "DisplayedName" : "Polly"
    },
    "Subscribes" : [
        "apple@gmail.com",
        "banana@yahoo.com"
    ],
    "Profiles" : [
        {
            "Bad" : [ "ETH - Total Speed: 11", "ETH - Total Speed: 12", "ETH - Total Speed: 13" ],
            "Tolerance" : { "DurationMinutes" : 10, "Good" : [ "ETH - Total Speed: 14" ] }
        },
        {
            "Bad" : [
                "cannot get current temperature",
                "unspecified launch failure",
                "an illegal instruction was encountered",
                "got incorrect share",
                "fan=0%",
                "gpu error",
                "need to restart miner",
                "cuda error",
                "opencl error",
                "gpuminer cu_k1 failed",
                "gpuminer cu_kx failed",
                "gpuminer cu_k01 failed",
                "gpuminer kx failed",
                "cannot get fan speed"
            ]
        }
    ],
    "ExitToleranceMinutes" : 1,
    "PublicIpCheckMinutes" : 30
}
```
* Most of options are self-explanatory. Regarding the `Polly` option, it is recommended to use Gmail. You should change the `pollymonitor2@gmail.com` to your own Gmail address (and password, of course). Remember to turn on the  `Allow less secure apps` option of your account at https://myaccount.google.com/lesssecureapps
* `ExitToleranceMinutes` defines what a too-quick exiting is: if the miner exits within this value, then `fire.bat` will be executed.
* Execute the tool: `polly.bat`.
* Make sure that the tool is executed every time your rig starts (e.g., create a shortcut of `polly.bat` and put it into the `Startup` folder of Windows).

## Notes
* You should use `/` or `\\` (not `\`) in the `MinerPath` option.
* It is recommended to set `Screen Buffer Width` (in `Properties/Layout` of the console) to be greater than the longest line in the console output of your miner. The typical value is 200. This should be done on the console window executed from the shortcut in the `Startup` folder, so that the setting will be applied permanently.
