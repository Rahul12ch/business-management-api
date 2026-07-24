
using client.Models;
using System.Text;

namespace client.Templates
{
    public static class EmailLayout
    {
        public static EmailMessage Create(
            string subject,
            string title,
            Dictionary<string, string> details)
        {
            var body = new StringBuilder();

            body.Append(@"
<html>
<body style='font-family:Arial,Helvetica,sans-serif;background:#f5f5f5;padding:30px;'>
<div style='max-width:600px;margin:auto;background:#ffffff;border:1px solid #ddd;border-radius:8px;padding:30px;'>

<h2 style='color:#003399;margin-top:0;'>");
            body.Append(title);
            body.Append(@"</h2>

<p>Hello,</p>

<p>Thank you for choosing <strong>RANA AIR CONDITIONING</strong>.</p>

<table style='border-collapse:collapse;width:100%;'>");

            foreach (var item in details)
            {
                body.Append($@"
<tr>
    <td style='padding:8px;font-weight:bold;border-bottom:1px solid #eee;width:35%;'>{item.Key}</td>
    <td style='padding:8px;border-bottom:1px solid #eee;'>{item.Value}</td>
</tr>");
            }

            body.Append(@"
</table>

<p style='margin-top:25px;'>
If you have any questions, simply reply to this email or contact us.
</p>

<hr>

<p style='font-size:14px;color:#555;'>
<strong>RANA AIR CONDITIONING</strong><br>
AC Sales & Services
</p>

</div>
</body>
</html>");

            return new EmailMessage
            {
                Subject = subject,
                Body = body.ToString(),
                IsHtml = true
            };
        }
    }
}