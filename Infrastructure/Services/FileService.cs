using Eshop.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Eshop.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IHostEnvironment _environment;
        // Επιτρεπόμενα extensions για ασφάλεια
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public FileService(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveProductImageAsync(IFormFile file, string tenantId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Το αρχείο είναι άδειο.");

            // 1. Έλεγχος Extension (Ασφάλεια)
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Μη επιτρεπτός τύπος αρχείου. Μόνο JPG, JPEG, PNG και WEBP.");

            // 2. Δυναμική δημιουργία του φακέλου του Tenant (Just-In-Time!)
            // Παράγει: wwwroot/uploads/adidas-store/products
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", tenantId, "products");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder); // Αν δεν υπάρχει ο φάκελος του καταστήματος, τον φτιάχνει live!
            }

            // 3. Δημιουργία μοναδικού ονόματος αρχείου για να μην συμπίπτουν (π.χ. airmax_guid.jpg)
            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 4. Αποθήκευση του αρχείου στο δίσκο
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 5. Επιστροφή του σχετικού URL που θα αποθηκευτεί στη βάση και θα διαβάζει το frontend
            return $"/uploads/{tenantId}/products/{uniqueFileName}";
        }

        public void DeleteImage(string relativePath)
        {
            // 1. ΑΠΟΛΥΤΟΣ ΚΟΦΤΗΣ: Αν το path είναι κενό, null, ή σκέτο slash, ΜΗΝ ΑΓΓΙΖΕΙΣ ΤΙΠΟΤΑ!
            if (string.IsNullOrEmpty(relativePath) || relativePath == "/" || relativePath.Trim() == "")
            {
                return;
            }

            // 2. Καθαρίζουμε τα slashes για να παίζει σωστά σε Windows και Linux
            var cleanPath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);

            // 3. Σύνθεση του απόλυτου path στον δίσκο
            var fullPath = Path.Combine(_environment.ContentRootPath, "wwwroot", cleanPath);

            // 4. ΔΙΠΛΟΣ ΕΛΕΓΧΟΣ: Σιγουρεύουμε ότι υπάρχει ΚΑΙ ότι είναι ΑΡΧΕΙΟ (όχι φάκελος!) πριν σβήσουμε
            if (File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}