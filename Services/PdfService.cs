using client.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace client.Services
{
    public class PdfService
    {
        private const string Blue = "#003399";

        public byte[] Generate(InvoiceDto invoice, string logoPath)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var qrImage = GenerateUpiQr(invoice.GrandTotal, invoice.OrderNo.ToString());
            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(28); page.DefaultTextStyle(x => x.FontSize(10));
                    page.Content().Column(col =>
                    {
                        col.Spacing(14); col.Item().Row(row => { row.ConstantItem(75).Image(logoPath); row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(invoice.CompanyName).FontSize(25).Bold(); c.Item().Text("AC Sales & Services").FontSize(12); });
                        });
                        col.Item().LineHorizontal(1); col.Item().Row(row =>{ row.RelativeItem().Text(x => { 
                            x.Span("Invoice No: ").Bold(); x.Span(invoice.OrderNo.ToString()); });
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Date: ").Bold(); x.Span(invoice.InvoiceDate.ToString("dd MMM yyyy"));
                            });
                        });
                        col.Item().Row(row => {  row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("BILL TO:").FontColor(Blue).Bold(); c.Item().PaddingTop(8).Text(invoice.CustomerName); c.Item().Text(invoice.PhoneNumber); c.Item().Text(invoice.Address);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("WORK DESCRIPTION:").FontColor(Blue).Bold(); c.Item().PaddingTop(8).Text(invoice.TaskName);
                            });
                        });
                        col.Item().Table(table => { table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40); c.RelativeColumn(); c.ConstantColumn(45); c.ConstantColumn(75); c.ConstantColumn(85);
                            });
                            foreach (var h in new[] { "No.", "DESCRIPTION", "QTY", "RATE (₹)", "AMOUNT (₹)" })
                            {
                                table.Cell() .Background(Blue) .Padding(7) .AlignCenter() .Text(h) .FontColor(Colors.White) .Bold();
                            }
                            int no = 1;
                        foreach (var i in invoice.Items)
                        {
                        Cell(table.Cell()).Text(no++.ToString()); Cell(table.Cell()).Text(i.Description); Cell(table.Cell()).Text(i.Qty.ToString()); Cell(table.Cell()).Text($"{i.Rate:N0}"); Cell(table.Cell()).Text($"{i.Amount:N0}");
                        }
                        });
                        col.Item().Row(row => { row.RelativeItem()
                        .Text($"Payment - {invoice.PaymentStatus}") .Bold();
                            row.ConstantItem(220).Column(c => { c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal"); r.RelativeItem().AlignRight().Text($"₹{invoice.SubTotal:N0}");
                                });
                                if (invoice.IsGstApplied) { c.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text($"GST ({invoice.GstPercent}%)"); r.RelativeItem().AlignRight().Text($"₹{invoice.GstAmount:N0}");
                                    });
                                }
                                c.Item().LineHorizontal(1); c.Item().PaddingVertical(5).Row(r =>
                                {
                                    r.RelativeItem().Text("GRAND TOTAL").Bold(); r.RelativeItem().AlignRight().Text($"₹{invoice.GrandTotal:N0}").Bold();
                                });
                                c.Item().LineHorizontal(1);
                            });
                        });
                        col.Item().PaddingTop(60).Row(row => { row.RelativeItem().Column(c =>
                            {
                             c.Item().Text("AUTHORIZED SIGNATURE").Bold(); c.Item() .PaddingTop(65) .Width(150) .LineHorizontal(1);
                            });
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item() .AlignCenter() .Text("SCAN TO PAY") .Bold();
                                c.Item() .PaddingTop(8) .AlignCenter() .Width(115) .Image(qrImage);
                                c.Item() .PaddingTop(3) .AlignCenter() .Text("UPI Payment");
                            });
                        });
                    });
                    page.Footer().Column(c =>
                    {
                        c.Spacing(8); c.Item().LineHorizontal(1); c.Item() .AlignCenter() .Text("Thank you for choosing us for business!");
                        c.Item().Row(row => { row.RelativeItem() .Text($"✉ {invoice.CompanyEmail}"); row.RelativeItem() .AlignRight() .Text($"☎ {invoice.CompanyPhone.Replace(" - ", "\n☎ ")}");
                        });
                    });
                });
            }).GeneratePdf();
        }
        private static byte[] GenerateUpiQr(decimal amount, string orderNo)
        {
            string upi = $"upi://pay?" + $"pa=8351998002@upi" + $"&pn=Rana Air Conditioning" +$"&am={amount}" +$"&cu=INR" + $"&tn=Invoice {orderNo}";
            using QRCodeGenerator generator = new();
            using QRCodeData data = generator.CreateQrCode( upi, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qr = new(data);
            return qr.GetGraphic(20);
        }
        private static IContainer Cell(IContainer c)
        {
            return c .Border(1) .BorderColor(Colors.Grey.Lighten1) .Padding(7) .AlignCenter();
        }
    }
}