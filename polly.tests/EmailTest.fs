namespace polly.tests

open System
open NUnit.Framework

open polly

module EmailTest =

    [<Test>]
    let ``test uptime format`` () =
        Assert.AreEqual ("[Uptime] 11d 13:46", Email.makeUpTimeText 1000000000L)
