namespace polly

open FSharp.Data

module PublicIp =

    let get () =
        Http.RequestString "http://api.ipify.org"
