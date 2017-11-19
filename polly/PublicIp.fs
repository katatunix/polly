namespace polly

open FSharp.Data
open System.IO

module PublicIp =

    let private IP_FILE = "ip.dat"

    let get () =
        Http.RequestString "http://api.ipify.org"

    let load () =
        try File.ReadAllText(IP_FILE) with _ -> ""

    let save (ip : string) =
        try File.WriteAllText(IP_FILE, ip) with _ -> ()
