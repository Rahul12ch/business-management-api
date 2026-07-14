using client.Models;
using System.Text;

namespace client.Templates
{
    public static class EmailLayout
    {
        public static EmailMessage Create( string subject, string title, Dictionary<string, string> details)
        {
            var body = new StringBuilder();
            body.Append($@"
            <html>
            <body>
            <h3>{title}</h3>
            <table>");
            foreach (var item in details)
            {
                body.Append($@"
                <tr>
                    <td><b>{item.Key}</b></td>
                    <td>: {item.Value}</td>
                </tr>");
            }
            body.Append($@"
            </table>
            <br>
            <hr>
            <p> <b>RANA AIR CONDITIONING</b><br> AC Sales & Services </p>
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