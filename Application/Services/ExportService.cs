using ClosedXML.Excel;
using Eshop.Application.DTOs;
using Eshop.Core.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Eshop.Application.Services
{
    public class ExportService : IExportService
    {
        // ------------------ EXCEL EXPORT (ClosedXML) ------------------
        public byte[] GenerateOrdersExcel(IEnumerable<OrderResponseDto> orders)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Παραγγελίες");

            // 1. Επικεφαλίδες
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Ημερομηνία";
            worksheet.Cell(1, 3).Value = "Πελάτης";
            worksheet.Cell(1, 4).Value = "Email";
            worksheet.Cell(1, 5).Value = "Σύνολο (€)";
            worksheet.Cell(1, 6).Value = "Πληρωμή";
            worksheet.Cell(1, 7).Value = "Κατάσταση";

            // Στυλ επικεφαλίδων
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // 2. Γέμισμα δεδομένων
            int row = 2;
            foreach (var order in orders)
            {
                worksheet.Cell(row, 1).Value = order.Id;
                worksheet.Cell(row, 2).Value = order.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 3).Value = order.CustomerName ?? "Άγνωστος";
                worksheet.Cell(row, 4).Value = order.CustomerEmail ?? "-";
                worksheet.Cell(row, 5).Value = order.TotalAmount;
                worksheet.Cell(row, 6).Value = order.PaymentMethod;
                worksheet.Cell(row, 7).Value = order.Status;
                row++;
            }

            // Αυτόματη προσαρμογή πλάτους στηλών
            worksheet.Columns().AdjustToContents();

            // 3. Μετατροπή σε byte array για κατέβασμα
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ------------------ PDF EXPORT (QuestPDF) ------------------
        public byte[] GenerateOrdersPdf(IEnumerable<OrderResponseDto> orders)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); // Οριζόντιο για να χωράνε οι στήλες
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial)); // Arial για υποστήριξη Ελληνικών

                    // Κεφαλίδα
                    page.Header().Element(ComposeHeader);

                    // Πίνακας Δεδομένων
                    page.Content().Element(x => ComposeContent(x, orders));

                    // Υποσέλιδο
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Σελίδα ");
                        x.CurrentPageNumber();
                        x.Span(" από ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Αναφορά Παραγγελιών").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Ημερομηνία Εξαγωγής: {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            });
        }

        private void ComposeContent(IContainer container, IEnumerable<OrderResponseDto> orders)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                // Ορισμός στηλών
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);  // ID
                    columns.ConstantColumn(90); // Date
                    columns.RelativeColumn();    // Customer
                    columns.ConstantColumn(90);  // Amount
                    columns.ConstantColumn(120); // Payment
                    columns.ConstantColumn(100); // Status
                });

                // Επικεφαλίδες πίνακα
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("ID");
                    header.Cell().Element(CellStyle).Text("Ημερομηνία");
                    header.Cell().Element(CellStyle).Text("Πελάτης");
                    header.Cell().Element(CellStyle).AlignRight().Text("Σύνολο");
                    header.Cell().Element(CellStyle).Text("Πληρωμή");
                    header.Cell().Element(CellStyle).Text("Κατάσταση");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).PaddingHorizontal(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                // Γέμισμα Δεδομένων
                foreach (var order in orders)
                {
                    table.Cell().Element(CellStyle).Text($"#{order.Id}");
                    table.Cell().Element(CellStyle).Text(order.OrderDate.ToLocalTime().ToString("dd/MM/yyyy"));
                    table.Cell().Element(CellStyle).Text(order.CustomerName);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{order.TotalAmount:C2}");
                    table.Cell().Element(CellStyle).Text(order.PaymentMethod);
                    table.Cell().Element(CellStyle).Text(order.Status);

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(5).PaddingHorizontal(5); ;
                    }
                }
            });
        }

        // ------------------ EXCEL EXPORT ΓΙΑ ΕΠΙΣΤΡΟΦΕΣ ------------------
        public byte[] GenerateReturnsExcel(IEnumerable<OrderReturnResponseDto> returns)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Επιστροφές");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "ID Παραγγελίας";
            worksheet.Cell(1, 3).Value = "Ημερομηνία";
            worksheet.Cell(1, 4).Value = "Τύπος";
            worksheet.Cell(1, 5).Value = "Ποσό Επιστροφής (€)";
            worksheet.Cell(1, 6).Value = "Κατάσταση";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var ret in returns)
            {
                worksheet.Cell(row, 1).Value = ret.Id;
                worksheet.Cell(row, 2).Value = ret.OrderId;
                worksheet.Cell(row, 3).Value = ret.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 4).Value = ret.ReturnType == "Total" ? "Ολική" : "Μερική";
                worksheet.Cell(row, 5).Value = ret.RefundAmount;
                worksheet.Cell(row, 6).Value = ret.Status;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ------------------ PDF EXPORT ΓΙΑ ΕΠΙΣΤΡΟΦΕΣ ------------------
        public byte[] GenerateReturnsPdf(IEnumerable<OrderReturnResponseDto> returns)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(c =>
                    {
                        c.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("Αναφορά Επιστροφών").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"Ημερομηνία Εξαγωγής: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            });
                        });
                    });

                    page.Content().Element(x => ComposeReturnsContent(x, returns));

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Σελίδα ");
                        t.CurrentPageNumber();
                        t.Span(" από ");
                        t.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeReturnsContent(IContainer container, IEnumerable<OrderReturnResponseDto> returns)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);  // ID
                    columns.ConstantColumn(90);  // Order ID
                    columns.ConstantColumn(100); // Date
                    columns.RelativeColumn();    // Type
                    columns.ConstantColumn(120); // Amount
                    columns.ConstantColumn(100); // Status
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("ID");
                    header.Cell().Element(HeaderStyle).Text("Παραγγελία");
                    header.Cell().Element(HeaderStyle).Text("Ημερομηνία");
                    header.Cell().Element(HeaderStyle).Text("Τύπος");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Ποσό Επιστροφής");
                    header.Cell().Element(HeaderStyle).Text("Κατάσταση");

                    static IContainer HeaderStyle(IContainer container) =>
                        container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).PaddingHorizontal(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var ret in returns)
                {
                    table.Cell().Element(CellStyle).Text($"#{ret.Id}");
                    table.Cell().Element(CellStyle).Text($"#{ret.OrderId}");
                    table.Cell().Element(CellStyle).Text(ret.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy"));
                    table.Cell().Element(CellStyle).Text(ret.ReturnType == "Total" ? "Ολική" : "Μερική");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{ret.RefundAmount:C2}");
                    table.Cell().Element(CellStyle).Text(ret.Status);

                    static IContainer CellStyle(IContainer container) =>
                        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(5);
                }
            });
        }

        // ------------------ EXCEL EXPORT ΓΙΑ ΠΡΟΪΟΝΤΑ ------------------
        public byte[] GenerateProductsExcel(IEnumerable<ProductResponseDto> products)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Προϊόντα");

            // Επικεφαλίδες (ΧΩΡΙΣ ΕΙΚΟΝΑ)
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Προϊόν";
            worksheet.Cell(1, 3).Value = "Κατηγορία";
            worksheet.Cell(1, 4).Value = "Αρχική Τιμή (€)";
            worksheet.Cell(1, 5).Value = "Τιμή Έκπτωσης (€)";
            worksheet.Cell(1, 6).Value = "Απόθεμα";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var product in products)
            {
                worksheet.Cell(row, 1).Value = product.Id;
                worksheet.Cell(row, 2).Value = product.Name;
                worksheet.Cell(row, 3).Value = product.CategoryName ?? "-"; // Βάλε το σωστό property της κατηγορίας σου
                worksheet.Cell(row, 4).Value = product.Price;
                worksheet.Cell(row, 5).Value = product.SalePrice?.ToString() ?? "-"; // Αν δεν έχει έκπτωση
                worksheet.Cell(row, 6).Value = product.StockQuantity;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ------------------ PDF EXPORT ΓΙΑ ΠΡΟΪΟΝΤΑ ------------------
        public byte[] GenerateProductsPdf(IEnumerable<ProductResponseDto> products)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(c =>
                    {
                        c.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("Αναφορά Προϊόντων").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"Ημερομηνία Εξαγωγής: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            });
                        });
                    });

                    page.Content().Element(x => ComposeProductsContent(x, products));

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Σελίδα ");
                        t.CurrentPageNumber();
                        t.Span(" από ");
                        t.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeProductsContent(IContainer container, IEnumerable<ProductResponseDto> products)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);  // ID
                    columns.RelativeColumn();    // Name
                    columns.ConstantColumn(120); // Category
                    columns.ConstantColumn(90);  // Price
                    columns.ConstantColumn(90);  // Sale Price
                    columns.ConstantColumn(70);  // Stock
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("ID");
                    header.Cell().Element(HeaderStyle).Text("Προϊόν");
                    header.Cell().Element(HeaderStyle).Text("Κατηγορία");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Τιμή");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Τιμή Έκπτ.");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Απόθεμα");

                    static IContainer HeaderStyle(IContainer container) =>
                        container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).PaddingHorizontal(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var product in products)
                {
                    table.Cell().Element(CellStyle).Text($"#{product.Id}");
                    table.Cell().Element(CellStyle).Text(product.Name);
                    table.Cell().Element(CellStyle).Text(product.CategoryName ?? "-");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{product.Price:C2}");
                    table.Cell().Element(CellStyle).AlignRight().Text(product.SalePrice.HasValue ? $"{product.SalePrice:C2}" : "-");
                    table.Cell().Element(CellStyle).AlignCenter().Text(product.StockQuantity.ToString());

                    static IContainer CellStyle(IContainer container) =>
                        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(5);
                }
            });
        }
    }
}