Polly &ndash; a tool for monitoring cryptocurrency miners
===================================================

Run this tool on your mining rig, when any of predefined errors happens (e.g., `fan=0%` or `got incorrect share`) with your miner, a predefined action will be automatically executed.

## Features
* Continuously check console output of the miner to detect errors, and based on that the tool will fire an action which is specified in an executable file.

* Start miner again when it exits/crashes, fire an action if the exiting happens too quickly.

* If miner has not printed out anything to console for a while (stuck), then fire an action.

* Periodically check new public IP address of the rig.

* All important events &ndash; errors happen / exit normally / exit too quickly / new public IP &ndash; will be sent to a list of subscribed email addresses.

* Remove the developer fee of the miner (NoDevFee).

## Usage
* Download: https://drive.google.com/drive/folders/1B-p-HME_HmSw10LV-MXqxDdhBHgltizh?usp=sharing or https://github.com/katatunix/polly/releases
* Extract to your rig.
* Open file `config.yml` and modify it following the template below:

```yaml
MinerPath: D:\Claymore14.7\EthDcrMiner64.exe
MinerArgs: -esm 1 -gser 0
NoDevFee: true
Sender:
  SmtpHost: smtp.gmail.com
  SmtpPort: 587
  Address: pollymonitor@gmail.com
  Password: 12345678
  DisplayedName: Polly
Subscribes:
- apple@gmail.com
- banana@gmail.com
Profiles:
- Bad:
  - cannot get current temperature
  - unspecified launch failure
  - an illegal instruction was encountered
  - gpu error
  - need to restart miner
  - opencl error
  - hangs in OpenCL call
  Action: restart.bat
- Bad:
  - got incorrect share
  Action:
- Bad:
  - 'Total Speed:___Speed: 8___Speed: 9'
  Tolerance:
    DurationMinutes: 5
    Good:
    - 'Total Speed: 8'
    - 'Total Speed: 9'
  Action: restart.bat
StuckProfile:
  ToleranceMinutes: 5
  Action: restart.bat
QuickExitProfile:
  ToleranceMinutes: 1
  Action: restart.bat
PublicIpCheckMinutes: 30
MaxLogLines: 50
```

* Most of options are self-explanatory. Regarding the `Sender` option, it is recommended to use Gmail. You should change the `pollymonitor@gmail.com` to your own Gmail address (and password, of course). Remember to turn on the  `Allow less secure apps` option of your account at https://myaccount.google.com/lesssecureapps

* If a `Bad` or `Good` option contains one or many `___` (three underscore symbols), for example: `abc___xyz___123`, it means "_contain `abc` but not `xyz` and not `123`_". This is useful when you cannot specify the full list of bad/good strings. For example, with a `Bad` option of `fan=0`, it's crazy to list all cases for the `Good` option like `fan=1`, `fan=2`, `fan=3` ... Instead, you can just write the `Good` option as `fan=___fan=0`.

* Quit the miner if it is running, then execute the tool: `polly.exe`.

## Notes
* You can specify a different path of the config file other than the default `config.yml`. Just pass the path as the first argument to `polly.exe`. The path can be either absolute or relative to the current execution folder. If no any path is provided, the default `config.yml` will be used.

* The file or file path declared in every `Action` option is relative to the folder containing `polly.exe`.

* If you are using `Claymore` miner and its `config.txt` file, please leave the `MinerArgs` option as empty.

* The two useful actions `restart.bat` (for restarting rig) and `killminer.bat` (for killing `Claymore` miner process so the miner will be re-executed again) are also provided. If you are not using `Claymore`, open `killminer.bat` and replace `EthDcrMiner64.exe` with your miner process name. Miner process name can be seen in `Task Manager`.

## NoDevFee
* The `NoDevFee` feature is based on https://github.com/Demion/nodevfee and currently only works with Stratum protocol and non-SSL `DevFee` pool connections.

* Open file `pools.txt` and specify all of your used pools here. This includes the main one and failover ones. The main one must be specified first.
    * If you are using `Claymore` in dual mode, all the pools of DCR mining must be also specified. Make sure the main pool of ETH mining is always specified first in the file `pools.txt`.

* If your miner uses SSL connections in its `DevFee` period, please try to avoid (or mitigate) this behavior. For example, with `Claymore 11.1+` and `ethermine.org`:
    * Use non-SSL pool connections (e.g., `asia.ethermine.org:4444`) for ETH mining.
    * Add `-allpools 1` to the config of `Claymore`.

## Donations
If you love this tool, you can buy me a cup of coffee via:
* BTC: 1MNipFhuKu48xhjw1ihEkzbohMX3HRwiML
* ETH: 0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b
* DCR: Dso4Y6BDdvXH6sEX1hy4UQwdFW71gPJNXXh
* SC: 3f0895ac7e7282c055c98660ca17c5f7414b31698e227451b967e3a7b2985c0c78e48afd9577
* BCH: 1JWUgrYPNMepvQUinQ1LD51UKrytCL7bn5
* BTG: GVTaR1vqRKvH7PK1QbpG6V2snCVkSXgQBf
* OMG: 0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b
* ETC: 0x94d9be21887bB9B480b291c962D68dA144eCBaCa
* ZEC: t1QYV46NcBH6KWHUrg35CAp9SfSWfeWLhTr
