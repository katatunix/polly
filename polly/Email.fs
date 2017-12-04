namespace polly

open System
open System.Net
open System.Net.Mail

module Email =

    type SenderInfo = {
        SmtpHost : string
        SmtpPort : int
        Address : string
        Password : string
        DisplayedName : string }

    let private send (senderInfo : SenderInfo) (toAddresses : string []) subject body =
        use message = new MailMessage ()
        message.From <- MailAddress (senderInfo.Address, senderInfo.DisplayedName)
        for toAddress in toAddresses do
            message.To.Add (toAddress)
        message.Subject <- subject
        message.Body <- body

        use smtp =
            new SmtpClient (
                Host = senderInfo.SmtpHost,
                Port = senderInfo.SmtpPort,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = NetworkCredential (senderInfo.Address, senderInfo.Password) )

        smtp.Send message

    let private makeComputerText () =
        sprintf "[Computer] %s" Environment.MachineName

    let sendFire senderInfo toAddresses reason log =
        let subject = "Fire notification"

        let computer = makeComputerText ()
        let reason = sprintf "[Reason] %s" reason
        let log = sprintf "[Log] %s" log
        let body = sprintf "%s\n%s\n%s" computer reason log

        send senderInfo toAddresses subject body

    let sendPublicIp senderInfo toAddresses ip =
        let subject = "New IP address"

        let computer = makeComputerText ()
        let ip = sprintf "[IP address] %s" ip
        let body = sprintf "%s\n%s" computer ip

        send senderInfo toAddresses subject body

    let sendExit senderInfo toAddresses upTimeMs =
        let subject = "Exit notification"

        let computer = makeComputerText ()
        let upTime = sprintf "[Up time] %A" (TimeSpan.TicksPerMillisecond * upTimeMs |> TimeSpan)
        let body = sprintf "%s\n%s" computer upTime

        send senderInfo toAddresses subject body
