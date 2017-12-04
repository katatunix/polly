namespace polly.tests
open System
open NUnit.Framework

open polly.Out

module OutTest =

    [<Test>]
    let ``test printSpecial`` () =
        printSpecial "Nghia Bui"
