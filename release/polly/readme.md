# Polly - a tool for monitoring cryptocurrency miners

Run this tool on your mining rig, when any of predefined error indicators happen (e.g., `fan=0%` or `got incorrect share`) with your miner, your rig will be automatically rebooted.

## Features
* Continuously check miner's console output to detect errors.
* Before rebooting, notify by email to a list of subscribed email addresses.
* Periodically (10 minutes) notify by email the public IP address of the rig (if the IP is new).

## Usage
* Copy folder `/release/polly` to your rig.
* Open file `config.json` and modify it following the template below:
```
{
    "MinerPath" : "D:/Claymores/EthDcrMiner64.exe",
    "MinerArgs" : "-esm 1 -gser 0",
    "Polly" : {
        "SmtpHost" : "smtp.gmail.com",
        "SmtpPort" : 587,
        "Email" : "pollymonitor2@gmail.com",
        "Password" : "test",
        "DisplayedName" : "Polly"
    },
    "SubscribedEmails" : [
        "apple@gmail.com",
        "banana@yahoo.com"
    ],
    "ErrorIndicators" : [
        "got incorrect share",
        "fan=0%",
        "gpu error",
        "you need to restart miner",
        "cuda error",
        "opencl error",
        "gpuminer cu_k1 failed",
        "gpuminer cu_kx failed",
        "gpuminer cu_k01 failed",
        "gpuminer kx failed",
        "cannot get fan speed"
    ]
}
```
* Most of options are self-explanatory. Regarding the `Polly` option, it is recommended to use Gmail. You should change the `pollymonitor2@gmail.com` to your own Gmail address (and password, of course). Remember to turn on the  `Allow less secure apps` option of your account at https://myaccount.google.com/security
* Execute the tool: `polly.bat`.
* Make sure that the tool is executed every time your rig starts (e.g., create a shortcut of `polly.bat` and put it into the `Startup` folder of Windows).

## Notes
* You must use `/` (not `\`) in every option.
* It is recommended to set `Screen Buffer Width` (in `Properties/Layout` of the console) to be greater than the longest line in the console output of your miner. The typical value is 200. This should be done on the console window executed from the shortcut in the `Startup` folder, so that the setting will be applied permanently.