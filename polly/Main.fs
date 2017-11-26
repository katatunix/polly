namespace polly

module Main =

    let o = obj ()
    let out x = lock o (fun _ -> printfn "%s" x)

    let start config =
        let ipTimer = PublicIp.startCheck out config
        let monitorWait, monitorStop = Monitor.startForever out config
        System.AppDomain.CurrentDomain.ProcessExit.Add
            (fun _ -> out "Quit [1] ..."; try monitorStop () with _ -> (); out "Quit [2] ...")
        monitorWait ()
        ipTimer.Stop ()

    [<EntryPoint>]
    let main argv =
        out ""
        out "=================================================================="
        out "    polly 2.1 - katatunix@gmail.com"
        out "    If you love this tool, you can buy me a cup of coffee via:"
        out "        BTC: 18gmEFLjEVhXz3P8cmubsGSQRZfssWyg7o"
        out "        ETH: 0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b"
        out "=================================================================="
        out ""
        match Config.load () with
        | Error msg ->
            out (sprintf "Error: %s" msg)
            1
        | Ok config ->
            start config
            0
