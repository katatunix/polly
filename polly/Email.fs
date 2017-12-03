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

    let sendReboot senderInfo toAddresses reason log =
        let subject = "Reboot notification"

        let computer = makeComputerText ()
        let reason = sprintf "\n[Reason] %s" reason
        let log = sprintf "\n[Log] %s" log
        let body = sprintf "%s%s%s" computer reason log

        send senderInfo toAddresses subject body

    let sendPublicIp senderInfo toAddresses ip =
        let subject = "New IP address"

        let computer = makeComputerText ()
        let ip = sprintf "[IP address] %s" ip
        let body = sprintf "%s\n%s\n" computer ip

        send senderInfo toAddresses subject body

    let sendCrash senderInfo toAddresses =
        let subject = "Crash notification"
        let body = makeComputerText ()
        send senderInfo toAddresses subject body
