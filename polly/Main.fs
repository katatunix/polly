namespace polly

module Main =

    [<EntryPoint>]
    let main argv =
        printfn ""
        printfn "=================================================================="
        printfn "    polly 4.7 - katatunix@gmail.com"
        printfn "    If you love this tool, you can buy me a cup of coffee via:"
        printfn "        BTC: 1MNipFhuKu48xhjw1ihEkzbohMX3HRwiML"
        printfn "        ETH: 0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b"
        printfn "        DCR: Dso4Y6BDdvXH6sEX1hy4UQwdFW71gPJNXXh"
        printfn "        SC: 3f0895ac7e7282c055c98660ca17c5f7414b31698e227451b967e3a7b2985c0c78e48afd9577"
        printfn "        BCH: 1JWUgrYPNMepvQUinQ1LD51UKrytCL7bn5"
        printfn "        BTG: GVTaR1vqRKvH7PK1QbpG6V2snCVkSXgQBf"
        printfn "        OMG: 0xf8B7728dC0c1cB2FCFcc421E9a2b3Ed6cdf1B43b"
        printfn "        ETC: 0x94d9be21887bB9B480b291c962D68dA144eCBaCa"
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

            System.AppDomain.CurrentDomain.ProcessExit.Add (fun _ -> monitorStop.Run ())

            monitorWait.Run ()
            checkIpTimer.Stop ()
            0
