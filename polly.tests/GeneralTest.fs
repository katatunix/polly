namespace polly.tests
open System
open NUnit.Framework

module GeneralTest =

    [<Test>]
    let `` `` () =
        let ts = TimeSpan 100000000000L
        Assert.AreEqual ("00.02:46:40", ts.ToString @"dd\.hh\:mm\:ss")
