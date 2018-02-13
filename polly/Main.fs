namespace polly


module Main =

    [<EntryPoint>]
    let main argv =
        printfn ""
        printfn "=================================================================="
        printfn "    polly 4.3 - katatunix@gmail.com"
        printfn "    If you love this tool, you can buy me a cup of coffee via:"
        printfn "        BTC: 1MNipFhuKu48xhjw1ihEkzbohMX3HRwiML"
        printfn "        ETH: 0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b"
        printfn "=================================================================="
        printfn ""

        let configFile = if argv.Length = 0 then None else Some argv.[0]

        match Config.load configFile with
        | Error msg ->
            printfn "Error: %s" msg
            1
        | Ok config ->
            let checkIpTimer = PublicIp.startCheck config
            let monitorWait, monitorStop = Monitor.run config

            System.AppDomain.CurrentDomain.ProcessExit.Add (fun _ -> monitorStop ())

            monitorWait ()
            checkIpTimer.Stop ()
            0
