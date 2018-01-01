namespace polly.tests

open System
open NUnit.Framework

open polly.Common
open polly.Config
open polly.ErrorDetection

module ErrorDetectionTest =

    let stuckProfile = { Tolerance = TimeMs (5L * 60000L); Action = "restart.bat" }

    [<Test>]
    let ``when all lines are okay, then no fire`` () =
        let profiles = [|
            { Bad = [| "fan=0%" |]; Tolerance = None; Action = None }
        |]
        let mutable fired = false
        let fire = fun _ -> fired <- true
        let agent = Agent (stuckProfile, profiles, fire)
        agent.Update "fan=10%"
        agent.Update "fan=20%"
        agent.Update "fan=30%"
        agent.Stop ()
        Assert.IsFalse fired

    [<Test>]
    let ``when a line is error and no tolerance, then fire`` () =
        let profiles = [|
            { Bad = [| "fan=0%" |]; Tolerance = None; Action = None }
        |]
        let mutable fired = false
        let fire = fun _ -> fired <- true
        let agent = Agent (stuckProfile, profiles, fire)
        agent.Update "fan=0%"
        agent.Update "fan=20%"
        agent.Update "fan=30%"
        agent.Stop ()
        Assert.IsTrue fired

    [<Test>]
    let ``when a line is error and over tolerance duration, then fire`` () =
        let profiles = [|
            {   Bad = [| "fan=0%" |]
                Tolerance = Some { Duration = TimeMs 1000L; Good = [| "fan=20%"|] }
                Action = None }
        |]
        let mutable fired = false
        let fire = fun _ -> fired <- true
        let agent = Agent (stuckProfile, profiles, fire)
        agent.Update "fan=0%"
        System.Threading.Thread.Sleep 2000
        agent.Update "fan=20%" // Good but late
        agent.Stop ()
        Assert.IsTrue fired

    [<Test>]
    let ``when a line is error but there is a good line before tolerance duration, then no fire`` () =
        let profiles = [|
            {   Bad = [| "fan=0%" |]
                Tolerance = Some { Duration = TimeMs 1000L; Good = [| "fan=20%" |] }
                Action = None }
        |]
        let mutable fired = false
        let fire = fun _ -> fired <- true
        let agent = Agent (stuckProfile, profiles, fire)
        agent.Update "fan=0%"
        agent.Update "fan=20%" // Good
        agent.Stop ()
        Assert.IsFalse fired
