# Polly - a tool for monitoring Claymore's Miner

Run this tool on your rig, when any of predefined error indicators happen (e.g., `fan=0%`) with the Claymore's Miner, your rig will be automatically rebooted.

## Features
* When rebooting, notify by email to a list of subscribed email addresses .
* When starting, notify by email the public IP address of the rig (if the IP is new).

## Usage
* Make sure the `management mode` of your Claymore's Miner is enabled at a port (e.g., `3333`).
* Open file `config.json` and modify it following the template below:
```
{
    "Port" : 3333,
    "CheckIntervalMinutes" : 7,
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
* Most of options are self-explanatory. Regarding the `Polly` config, it is recommended to use Gmail. You should change the `pollymonitor2@gmail.com` to your own Gmail address (and password, of course). Remember to turn on the  `Allow less secure apps` option of your account at https://myaccount.google.com/security
* Run `polly.exe`
* Make sure that the tool is execute every time your rig starts (e.g., create a `.bat` file and put it into the `Startup` folder of Windows).
