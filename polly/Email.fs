namespace polly

open System
open System.Net
open System.Net.Mail

module Email =

    let private subject = "Notification - " + Environment.MachineName

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

    let private makeUpTimeText upTimeMs =
        let ts = TimeSpan.TicksPerMillisecond * upTimeMs |> TimeSpan
        sprintf "[Up time] %s" (ts.ToString @"dd\.hh\:mm\:ss")

    let sendFire senderInfo toAddresses reason upTimeMs action log =
        let title = "FIRE!"
        let reason = sprintf "[Reason] %s" reason
        let upTime = makeUpTimeText upTimeMs
        let action = sprintf "[Action] %s" (action |> Option.defaultValue "<None>")
        let log = sprintf "[Log] %s" (log |> Option.defaultValue "<None>")
        let body = sprintf "%s\n%s\n%s\n%s\n%s" title reason upTime action log
        send senderInfo toAddresses subject body

    let sendPublicIp senderInfo toAddresses ip =
        let body = "New public IP address: " + ip
        send senderInfo toAddresses subject body
