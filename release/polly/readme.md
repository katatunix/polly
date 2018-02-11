# Polly - a tool for monitoring cryptocurrency miners

Run this tool on your mining rig, when any of predefined errors happens (e.g., `fan=0%` or `got incorrect share`) with your miner, a predefined action will be automatically executed.

## Features
* Continuously check console output of the miner to detect errors, and based on that the tool will fire an action which is specified in an executable file.
* Start miner again when it exits/crashes, fire an action if the exiting happens too quickly.
* If miner has not printed out anything to console for a while (stuck), then fire an action.
* Periodically check new public IP address of the rig.
* All important events -- errors happen / exit normally / exit too quickly / new public IP -- will be sent to a list of subscribed email addresses.
* Remove the developer fee of the miner.

## Usage
* Copy this tool to your rig.
* Open file `config.json` and modify it following the template below:
```
{
    "MinerPath" : "D:/Claymores/EthDcrMiner64.exe",
    "MinerArgs" : "-esm 1 -gser 0",
    "NoDevFee" : "yes",
    "Sender" : {
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
            "Bad" : [ "ETH - Total Speed:___Speed: 8___Speed: 9" ],
            "Tolerance" : { "DurationMinutes" : 5, "Good" : [ "ETH - Total Speed: 8", "ETH - Total Speed: 9" ] },
            "Action" : "restart.bat"
        },
        {
            "Bad" : [ "fan=0%" ],
            "Tolerance" : { "DurationMinutes" : 10, "Good" : [ "fan=___fan=0%" ] },
            "Action" : "restart.bat"
        },
        {
            "Bad" : [
                "cannot get current temperature",
                "unspecified launch failure",
                "an illegal instruction was encountered",
                "gpu error",
                "need to restart miner",
                "cuda error",
                "opencl error",
                "gpuminer cu_k1 failed",
                "gpuminer cu_kx failed",
                "gpuminer cu_k01 failed",
                "gpuminer kx failed",
                "cannot get fan speed"
            ],
            "Action" : "killminer.bat"
        },
        {
            "Bad" : [ "got incorrect share" ],
            "Action" : ""
        }
    ],
    "StuckProfile" : {
        "ToleranceMinutes" : 5,
        "Action" : "restart.bat"
    },
    "QuickExitProfile" : {
        "ToleranceMinutes" : 1,
        "Action" : "restart.bat"
    },
    "PublicIpCheckMinutes" : 30
}
```
* Most of options are self-explanatory. Regarding the `Sender` option, it is recommended to use Gmail. You should change the `pollymonitor2@gmail.com` to your own Gmail address (and password, of course). Remember to turn on the  `Allow less secure apps` option of your account at https://myaccount.google.com/lesssecureapps
* If a `Bad` or `Good` option contains one or many `___` (three underscore symbols), for example: `abc___xyz___123`, it means "contain `abc` but not `xyz` and not `123`". This is useful when you cannot specify the full list of bad/good strings. For example, with a `Bad` option of `[ "fan=0%" ]`, it's crazy to list all cases for the `Good` option like `[ "fan=1", "fan=2", "fan=3" ... ]`. Instead, you can just write `[ "fan=___fan=0%" ]`.
* Quit the miner if it is running, then execute the tool: `polly.exe`.

## Notes
* You can specify a different path of the config file other than the default `config.json`. Just pass the path as the first argument to `polly.exe`. The path can be either absolute or relative to the current execution folder. If no any path is provided, the default `config.json` will be used.
* You should use `/` or `\\` (not `\`) in all the options containing a path, such as `MinerPath` and `Action` options.
* The file or file path declared in every `Action` option is relative to the folder containing `polly.exe`.
* `StuckProfile.Action` and `QuickExitProfile.Action` cannot be empty but you can always specify a dummy `.bat` file.
* If you are using `Claymores` miner and its `config.txt` file, please leave the `MinerArgs` option as empty.
* The two useful actions `restart.bat` (for restarting rig) and `killminer.bat` (for killing `Claymores` miner process so the miner will be re-executed again) are also provided in the `release\polly` folder. If you are not using `Claymores`, open `killminer.bat` and replace `EthDcrMiner64.exe` with your miner process name. Miner process name can be seen in `Task Manager`.
* The `NoDevFee` feature is based on https://github.com/Demion/nodevfee and currently only works with Stratum protocol.

## Donations
If you love this tool, you can buy me a cup of coffee via:
* BTC: 1MNipFhuKu48xhjw1ihEkzbohMX3HRwiML
* ETH: 0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b
