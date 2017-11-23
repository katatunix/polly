namespace polly

open FSharp.Data
open System.IO
open NghiaBui.Common.Misc

module PublicIp =

    let private IP_FILE = "ip.dat"

    let get () =
        tryHard 3 1000 (fun _ -> Http.RequestString "http://api.ipify.org/")

    let load () =
        try File.ReadAllText(IP_FILE) with _ -> ""

    let save (ip : string) =
        try File.WriteAllText(IP_FILE, ip) with _ -> ()
